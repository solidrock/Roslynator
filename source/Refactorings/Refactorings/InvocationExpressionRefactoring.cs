﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslynator.CSharp.Refactorings.InlineMethod;

namespace Roslynator.CSharp.Refactorings
{
    internal static class InvocationExpressionRefactoring
    {
        public static async Task ComputeRefactoringsAsync(RefactoringContext context, InvocationExpressionSyntax invocationExpression)
        {
            if (context.IsAnyRefactoringEnabled(
                    RefactoringIdentifiers.UseElementAccessInsteadOfEnumerableMethod,
                    RefactoringIdentifiers.ReplaceAnyWithAllOrAllWithAny,
                    RefactoringIdentifiers.CallExtensionMethodAsInstanceMethod,
                    RefactoringIdentifiers.ReplaceStringContainsWithStringIndexOf))
            {
                ExpressionSyntax expression = invocationExpression.Expression;

                if (expression != null
                    && invocationExpression.ArgumentList != null)
                {
                    if (expression.IsKind(SyntaxKind.SimpleMemberAccessExpression)
                        && ((MemberAccessExpressionSyntax)expression).Name?.Span.Contains(context.Span) == true)
                    {
                        if (context.IsRefactoringEnabled(RefactoringIdentifiers.UseElementAccessInsteadOfEnumerableMethod))
                            await UseElementAccessInsteadOfEnumerableMethodRefactoring.ComputeRefactoringsAsync(context, invocationExpression).ConfigureAwait(false);

                        if (context.IsRefactoringEnabled(RefactoringIdentifiers.ReplaceAnyWithAllOrAllWithAny))
                            await ReplaceAnyWithAllOrAllWithAnyRefactoring.ComputeRefactoringAsync(context, invocationExpression).ConfigureAwait(false);

                        if (context.IsRefactoringEnabled(RefactoringIdentifiers.ReplaceStringContainsWithStringIndexOf))
                            await ReplaceStringContainsWithStringIndexOfRefactoring.ComputeRefactoringAsync(context, invocationExpression).ConfigureAwait(false);
                    }

                    if (context.IsRefactoringEnabled(RefactoringIdentifiers.CallExtensionMethodAsInstanceMethod))
                    {
                        SyntaxNodeOrToken nodeOrToken = CallExtensionMethodAsInstanceMethodRefactoring.GetNodeOrToken(expression);

                        if (nodeOrToken.Span.Contains(context.Span))
                        {
                            SemanticModel semanticModel = await context.GetSemanticModelAsync().ConfigureAwait(false);

                            CallExtensionMethodAsInstanceMethodRefactoring.AnalysisResult result =
                                CallExtensionMethodAsInstanceMethodRefactoring.Analyze(invocationExpression, semanticModel, context.CancellationToken);

                            if (result.Success)
                            {
                                context.RegisterRefactoring(
                                    "Call extension method as instance method",
                                    cancellationToken =>
                                    {
                                        return CallExtensionMethodAsInstanceMethodRefactoring.RefactorAsync(
                                            context.Document,
                                            result.InvocationExpression,
                                            result.NewInvocationExpression,
                                            context.CancellationToken);
                                    });
                            }
                        }
                    }
                }
            }

            if (context.IsRefactoringEnabled(RefactoringIdentifiers.ReplaceStringFormatWithInterpolatedString)
                && context.SupportsCSharp6)
            {
                await ReplaceStringFormatWithInterpolatedStringRefactoring.ComputeRefactoringsAsync(context, invocationExpression).ConfigureAwait(false);
            }

            if (context.IsRefactoringEnabled(RefactoringIdentifiers.UseBitwiseOperationInsteadOfCallingHasFlag))
            {
                SemanticModel semanticModel = await context.GetSemanticModelAsync().ConfigureAwait(false);

                if (UseBitwiseOperationInsteadOfCallingHasFlagRefactoring.CanRefactor(invocationExpression, semanticModel, context.CancellationToken))
                {
                    context.RegisterRefactoring(
                        UseBitwiseOperationInsteadOfCallingHasFlagRefactoring.Title,
                        cancellationToken =>
                        {
                            return UseBitwiseOperationInsteadOfCallingHasFlagRefactoring.RefactorAsync(
                                context.Document,
                                invocationExpression,
                                cancellationToken);
                        });
                }
            }

            if (context.IsRefactoringEnabled(RefactoringIdentifiers.InlineMethod))
                await InlineMethodRefactoring.ComputeRefactoringsAsync(context, invocationExpression).ConfigureAwait(false);
        }
    }
}
