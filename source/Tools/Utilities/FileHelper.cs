﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Roslynator.Utilities
{
    public static class FileHelper
    {
        public const string AutoGeneratedTag = "<auto-generated>";

        public static void WriteCompilationUnit(
            string path,
            CompilationUnitSyntax compilationUnit,
            string banner = null,
            bool autoGenerated = false,
            bool normalizeWhitespace = true)
        {
            if (!File.Exists(path))
            {
                Console.WriteLine($"file not found '{path}'");
                return;
            }

            if (normalizeWhitespace)
                compilationUnit = compilationUnit.NormalizeWhitespace();

            SyntaxTriviaList leadingTrivia = TriviaList();

            if (!string.IsNullOrEmpty(banner))
                leadingTrivia = AddSingleLineComment(leadingTrivia, banner);

            if (autoGenerated)
                leadingTrivia = AddSingleLineComment(leadingTrivia, AutoGeneratedTag);

            compilationUnit = compilationUnit.WithLeadingTrivia(leadingTrivia);

            string s = compilationUnit.ToFullString();

            if (!string.Equals(s, File.ReadAllText(path, Encoding.UTF8), StringComparison.Ordinal))
            {
                File.WriteAllText(path, s, Encoding.UTF8);
                Console.WriteLine($"file saved: '{path}'");
            }
            else
            {
                Console.WriteLine($"file unchanged: '{path}'");
            }
        }

        private static SyntaxTriviaList AddSingleLineComment(SyntaxTriviaList trivia, string text)
        {
            trivia = trivia.Add(Comment($"// {text}"));
            trivia = trivia.Add(CarriageReturnLineFeed);
            trivia = trivia.Add(CarriageReturnLineFeed);

            return trivia;
        }

        public static void WriteAllText(string path, string content, Encoding encoding, bool onlyIfChanges = true, bool fileMustExists = true)
        {
            if (fileMustExists
                && !File.Exists(path))
            {
                Console.WriteLine($"file not found '{path}'");
                return;
            }

            if (!onlyIfChanges
                || !File.Exists(path)
                || !string.Equals(content, File.ReadAllText(path, encoding), StringComparison.Ordinal))
            {
                File.WriteAllText(path, content, encoding);
                Console.WriteLine($"file saved: '{path}'");
            }
            else
            {
                Console.WriteLine($"file unchanged: '{path}'");
            }
        }
    }
}
