﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Composition;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.FileHeaders;

namespace Microsoft.CodeAnalysis.CSharp.FileHeaders
{
    /// <summary>
    /// Implements a code fix for file header diagnostics.
    /// </summary>
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(CSharpFileHeaderCodeFixProvider))]
    [Shared]
    internal class CSharpFileHeaderCodeFixProvider : AbstractFileHeaderCodeFixProvider
    {
        protected override AbstractFileHeaderHelper FileHeaderHelper => CSharpFileHeaderHelper.Instance;

        protected override SyntaxTrivia EndOfLine(string text)
            => SyntaxFactory.EndOfLine(text);

        protected override SyntaxTriviaList ParseLeadingTrivia(string text)
            => SyntaxFactory.ParseLeadingTrivia(text);
    }
}
