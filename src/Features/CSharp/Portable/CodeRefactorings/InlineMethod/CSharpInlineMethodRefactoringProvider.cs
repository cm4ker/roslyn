﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.LanguageServices;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.InlineMethod;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.CodeRefactorings.InlineMethod
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(PredefinedCodeRefactoringProviderNames.InlineMethod)), Shared]
    [Export(typeof(CSharpInlineMethodRefactoringProvider))]
    internal sealed class CSharpInlineMethodRefactoringProvider :
        AbstractInlineMethodRefactoringProvider<InvocationExpressionSyntax, ExpressionSyntax, ArgumentSyntax, MethodDeclarationSyntax>
    {
        /// <summary>
        /// All the syntax kind considered as the statement contains the invocation callee.
        /// </summary>
        private static readonly ImmutableHashSet<SyntaxKind> s_syntaxKindsConsideredAsStatementInvokesCallee =
            ImmutableHashSet.Create(
                SyntaxKind.DoStatement,
                SyntaxKind.ExpressionStatement,
                SyntaxKind.ForStatement,
                SyntaxKind.IfStatement,
                SyntaxKind.LocalDeclarationStatement,
                SyntaxKind.LockStatement,
                SyntaxKind.ReturnStatement,
                SyntaxKind.SwitchStatement,
                SyntaxKind.ThrowStatement,
                SyntaxKind.WhileStatement,
                SyntaxKind.TryStatement,
                SyntaxKind.UsingStatement,
                SyntaxKind.YieldReturnStatement);

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpInlineMethodRefactoringProvider() : base(CSharpSyntaxFacts.Instance, CSharpSemanticFactsService.Instance)
        {
        }

        private static bool CanStatementBeInlined(StatementSyntax statementSyntax)
        {
            if (statementSyntax.IsKind(SyntaxKind.ExpressionStatement))
            {
                return true;
            }

            if (statementSyntax is ReturnStatementSyntax returnStatementSyntax)
            {
                // In this case don't provide inline.
                // void Caller() { Callee(); }
                // void Callee() { return; }
                return returnStatementSyntax.Expression != null;
            }

            return false;
        }

        private static bool TryGetSingleStatementOrExpressionMethod(MethodDeclarationSyntax methodDeclarationSyntax, out ExpressionSyntax expressionSyntax)
        {
            var blockSyntaxNode = methodDeclarationSyntax.Body;
            expressionSyntax = null;
            if (blockSyntaxNode != null)
            {
                // 1. If it is an ordinary method with block
                var blockStatements = blockSyntaxNode.Statements;
                if (blockStatements.Count == 1 && CanStatementBeInlined(blockStatements[0]))
                {
                    expressionSyntax = GetExpressionFromStatementSyntaxNode(blockStatements[0]);
                    return true;
                }
            }
            else
            {
                // 2. If it is an Arrow Expression
                var arrowExpressionNode = methodDeclarationSyntax.ExpressionBody;
                if (arrowExpressionNode != null)
                {
                    expressionSyntax = arrowExpressionNode.Expression;
                    return true;
                }
            }

            return false;
        }

        private static ExpressionSyntax GetExpressionFromStatementSyntaxNode(StatementSyntax statementSyntax)
            => statementSyntax switch
            {
                // Check has been done before to make sure the argument is ReturnStatementSyntax or ExpressionStatementSyntax
                // and their expression is not null
                ReturnStatementSyntax returnStatementSyntax => returnStatementSyntax.Expression!,
                ExpressionStatementSyntax expressionStatementSyntax => expressionStatementSyntax.Expression,
                _ => throw ExceptionUtilities.Unreachable
            };

        protected override bool IsSingleStatementOrExpressionMethod(MethodDeclarationSyntax calleeMethodDeclarationSyntaxNode)
            => TryGetSingleStatementOrExpressionMethod(calleeMethodDeclarationSyntaxNode, out _);

        protected override ExpressionSyntax GetInlineStatement(MethodDeclarationSyntax calleeMethodDeclarationSyntaxNode)
        {
            if (TryGetSingleStatementOrExpressionMethod(calleeMethodDeclarationSyntaxNode, out var expressionSyntax))
            {
                return expressionSyntax;
            }

            // Check has been done before to make sure it will not hit here.
            throw ExceptionUtilities.Unreachable;
        }

        protected override SyntaxNode GenerateTypeSyntax(ITypeSymbol symbol)
            => symbol.GenerateTypeSyntax(allowVar: false);

        // TODO: Use the SyntaxGenerator array initialization when this
        // https://github.com/dotnet/roslyn/issues/46651 is resolved.
        protected override SyntaxNode GenerateArrayInitializerExpression(ImmutableArray<SyntaxNode> arguments)
            => SyntaxFactory.InitializerExpression(SyntaxKind.ArrayInitializerExpression, SyntaxFactory.SeparatedList(arguments));

        protected override bool ShouldConsideredAsContainingStatement(SyntaxNode syntaxNode)
            => s_syntaxKindsConsideredAsStatementInvokesCallee.Contains(syntaxNode.Kind());

        protected override ExpressionSyntax Parenthesize(ExpressionSyntax expressionSyntax)
            => expressionSyntax.Parenthesize();
    }
}
