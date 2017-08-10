﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Roslynator.CSharp.SyntaxInfo.SyntaxInfoHelper;

namespace Roslynator.CSharp.SyntaxInfo
{
    public struct SimpleIfElseInfo
    {
        public SimpleIfElseInfo(
            IfStatementSyntax ifStatement,
            ExpressionSyntax condition,
            StatementSyntax whenTrue,
            StatementSyntax whenFalse)
        {
            IfStatement = ifStatement;
            Condition = condition;
            WhenTrue = whenTrue;
            WhenFalse = whenFalse;
        }

        public IfStatementSyntax IfStatement { get; }

        public ExpressionSyntax Condition { get; }

        public StatementSyntax WhenTrue { get; }

        public StatementSyntax WhenFalse { get; }

        public static bool TryCreate(
            IfStatementSyntax ifStatement,
            out SimpleIfElseInfo info,
            bool allowNullOrMissing = false,
            bool walkDownParentheses = true)
        {
            if (ifStatement?.IsParentKind(SyntaxKind.ElseClause) == false)
            {
                ElseClauseSyntax elseClause = ifStatement.Else;

                if (elseClause != null)
                {
                    StatementSyntax whenFalse = elseClause.Statement;

                    if (CheckNode(whenFalse, allowNullOrMissing)
                        && whenFalse?.IsKind(SyntaxKind.IfStatement) == false)
                    {
                        StatementSyntax whenTrue = ifStatement.Statement;

                        if (CheckNode(whenTrue, allowNullOrMissing))
                        {
                            ExpressionSyntax condition = ifStatement.Condition?.WalkDownParenthesesIf(walkDownParentheses);

                            if (CheckNode(condition, allowNullOrMissing))
                            {
                                info = new SimpleIfElseInfo(ifStatement, condition, whenTrue, whenFalse);
                                return true;
                            }
                        }
                    }
                }
            }

            info = default(SimpleIfElseInfo);
            return false;
        }

        public override string ToString()
        {
            return IfStatement?.ToString() ?? base.ToString();
        }
    }
}