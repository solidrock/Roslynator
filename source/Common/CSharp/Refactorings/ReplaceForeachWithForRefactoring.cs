﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using static Roslynator.CSharp.CSharpFactory;

namespace Roslynator.CSharp.Refactorings
{
    internal static class ReplaceForEachWithForRefactoring
    {
        public static bool CanRefactor(
            ForEachStatementSyntax forEachStatement,
            SemanticModel semanticModel,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            ITypeSymbol typeSymbol = semanticModel.GetTypeSymbol(forEachStatement.Expression, cancellationToken);

            if (typeSymbol?.IsErrorType() == false)
            {
                return typeSymbol.IsString()
                   || typeSymbol.IsArrayType()
                   || typeSymbol.ImplementsAny(SpecialType.System_Collections_Generic_IList_T, SpecialType.System_Collections_Generic_IReadOnlyList_T)
                   || (HasApplicableIndexer(typeSymbol, forEachStatement, semanticModel, cancellationToken)
                        && typeSymbol.Implements(semanticModel.GetTypeByMetadataName(MetadataNames.System_Collections_ICollection)));
            }

            return false;
        }

        private static bool HasApplicableIndexer(
            ITypeSymbol containingType,
            ForEachStatementSyntax forEachStatement,
            SemanticModel semanticModel,
            CancellationToken cancellationToken)
        {
            ForEachStatementInfo info = semanticModel.GetForEachStatementInfo(forEachStatement);

            foreach (ISymbol member in containingType.GetMembers("this[]"))
            {
                var propertySymbol = (IPropertySymbol)member;

                if (!propertySymbol.IsWriteOnly
                    && propertySymbol.SingleParameterOrDefault()?.Type.IsInt() == true
                    && semanticModel.IsAccessible(forEachStatement.SpanStart, propertySymbol.GetMethod)
                    && propertySymbol.Type.Equals(semanticModel.GetTypeSymbol(forEachStatement.Type, cancellationToken)))
                {
                    return true;
                }
            }

            return false;
        }

        public static Task<Document> RefactorAsync(
            Document document,
            ForEachStatementSyntax forEachStatement,
            SemanticModel semanticModel,
            bool reverseLoop = false,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            string name = NameGenerator.Default.EnsureUniqueLocalName(
                DefaultNames.ForVariable,
                semanticModel,
                forEachStatement.Statement.SpanStart,
                cancellationToken: cancellationToken);

            SyntaxToken identifier = Identifier(name);

            if (name != DefaultNames.ForVariable)
                identifier = identifier.WithRenameAnnotation();

            ExpressionSyntax forEachExpression = forEachStatement.Expression;

            MemberAccessExpressionSyntax countOrLengthMemberAccess = SimpleMemberAccessExpression(
                forEachExpression.WithoutTrivia(),
                IdentifierName(GetCountOrLengthPropertyName(forEachExpression, semanticModel, cancellationToken)));

            VariableDeclarationSyntax declaration = null;
            BinaryExpressionSyntax condition = null;
            PostfixUnaryExpressionSyntax incrementor = null;

            if (reverseLoop)
            {
                declaration = VariableDeclaration(
                    IntType(),
                    identifier,
                    EqualsValueClause(
                        SubtractExpression(
                            countOrLengthMemberAccess,
                            NumericLiteralExpression(1))));

                condition = GreaterThanOrEqualExpression(IdentifierName(name), NumericLiteralExpression(0));

                incrementor = PostDecrementExpression(IdentifierName(name));
            }
            else
            {
                declaration = VariableDeclaration(
                    IntType(),
                    identifier,
                    EqualsValueClause(NumericLiteralExpression(0)));

                condition = LessThanExpression(
                    IdentifierName(name),
                    countOrLengthMemberAccess);

                incrementor = PostIncrementExpression(IdentifierName(name));
            }

            StatementSyntax statement = forEachStatement.Statement.ReplaceNodes(
                GetVariableReferences(forEachStatement, semanticModel, cancellationToken),
                (node, rewrittenNode) =>
                {
                    return ElementAccessExpression(
                        forEachExpression.WithoutTrivia(),
                        BracketedArgumentList(SingletonSeparatedList(Argument(IdentifierName(name))))
                    ).WithTriviaFrom(node);
                });

            ForStatementSyntax forStatement = ForStatement(
                declaration: declaration,
                initializers: default(SeparatedSyntaxList<ExpressionSyntax>),
                condition: condition,
                incrementors: SingletonSeparatedList<ExpressionSyntax>(incrementor),
                statement: statement);

            forStatement = forStatement
                 .WithTriviaFrom(forEachStatement)
                 .WithFormatterAnnotation();

            return document.ReplaceNodeAsync(forEachStatement, forStatement, cancellationToken);
        }

        private static IEnumerable<IdentifierNameSyntax> GetVariableReferences(
            ForEachStatementSyntax forEachStatement,
            SemanticModel semanticModel,
            CancellationToken cancellationToken)
        {
            string name = forEachStatement.Identifier.ValueText;

            ILocalSymbol symbol = semanticModel.GetDeclaredSymbol(forEachStatement, cancellationToken);

            foreach (SyntaxNode node in forEachStatement.Statement.DescendantNodes())
            {
                if (node.IsKind(SyntaxKind.IdentifierName)
                    && symbol.Equals(semanticModel.GetSymbol(node, cancellationToken)))
                {
                    yield return (IdentifierNameSyntax)node;
                }
            }
        }

        private static string GetCountOrLengthPropertyName(ExpressionSyntax expression, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            ITypeSymbol typeSymbol = semanticModel.GetTypeSymbol(expression, cancellationToken);

            if (typeSymbol?.IsErrorType() == false)
            {
                if (typeSymbol.IsString()
                    || typeSymbol.IsArrayType()
                    || typeSymbol.IsConstructedFromImmutableArrayOfT(semanticModel))
                {
                    return "Length";
                }
            }

            return "Count";
        }
    }
}
