﻿' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.VisualBasic.Utilities.IntrinsicOperators
Imports Microsoft.CodeAnalysis.QuickInfo

Namespace Microsoft.CodeAnalysis.VisualBasic.QuickInfo
    <ExportQuickInfoProvider(QuickInfoProviderNames.Semantic, LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicSemanticQuickInfoProvider
        Inherits CommonSemanticQuickInfoProvider

        Public Sub New()
        End Sub

        Protected Overrides Async Function BuildQuickInfoAsync(
                document As Document,
                token As SyntaxToken,
                cancellationToken As CancellationToken) As Task(Of QuickInfoItem)

            Dim parent = token.Parent

            Dim predefinedCastExpression = TryCast(parent, PredefinedCastExpressionSyntax)
            If predefinedCastExpression IsNot Nothing AndAlso token = predefinedCastExpression.Keyword Then
                Dim compilation = Await document.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(False)
                Dim documentation = New PredefinedCastExpressionDocumentation(predefinedCastExpression.Keyword.Kind, compilation)
                Return Await BuildContentForIntrinsicOperatorAsync(document, token, parent, documentation, Glyph.MethodPublic, cancellationToken).ConfigureAwait(False)
            End If

            Select Case token.Kind
                Case SyntaxKind.AddHandlerKeyword
                    If TypeOf parent Is AddRemoveHandlerStatementSyntax Then
                        Return Await BuildContentForIntrinsicOperatorAsync(document, token, parent, New AddHandlerStatementDocumentation(), Glyph.Keyword, cancellationToken).ConfigureAwait(False)
                    End If

                Case SyntaxKind.DimKeyword
                    If TypeOf parent Is FieldDeclarationSyntax Then
                        Return Await BuildContentAsync(document, token, DirectCast(parent, FieldDeclarationSyntax).Declarators, cancellationToken).ConfigureAwait(False)
                    ElseIf TypeOf parent Is LocalDeclarationStatementSyntax Then
                        Return Await BuildContentAsync(document, token, DirectCast(parent, LocalDeclarationStatementSyntax).Declarators, cancellationToken).ConfigureAwait(False)
                    End If

                Case SyntaxKind.CTypeKeyword
                    If TypeOf parent Is CTypeExpressionSyntax Then
                        Return Await BuildContentForIntrinsicOperatorAsync(document, token, parent, New CTypeCastExpressionDocumentation(), Glyph.MethodPublic, cancellationToken).ConfigureAwait(False)
                    End If

                Case SyntaxKind.DirectCastKeyword
                    If TypeOf parent Is DirectCastExpressionSyntax Then
                        Return Await BuildContentForIntrinsicOperatorAsync(document, token, parent, New DirectCastExpressionDocumentation(), Glyph.MethodPublic, cancellationToken).ConfigureAwait(False)
                    End If

                Case SyntaxKind.GetTypeKeyword
                    If TypeOf parent Is GetTypeExpressionSyntax Then
                        Return Await BuildContentForIntrinsicOperatorAsync(document, token, parent, New GetTypeExpressionDocumentation(), Glyph.MethodPublic, cancellationToken).ConfigureAwait(False)
                    End If

                Case SyntaxKind.GetXmlNamespaceKeyword
                    If TypeOf parent Is GetXmlNamespaceExpressionSyntax Then
                        Return Await BuildContentForIntrinsicOperatorAsync(document, token, parent, New GetXmlNamespaceExpressionDocumentation(), Glyph.MethodPublic, cancellationToken).ConfigureAwait(False)
                    End If

                Case SyntaxKind.IfKeyword
                    If parent.Kind = SyntaxKind.BinaryConditionalExpression Then
                        Return Await BuildContentForIntrinsicOperatorAsync(document, token, parent, New BinaryConditionalExpressionDocumentation(), Glyph.MethodPublic, cancellationToken).ConfigureAwait(False)
                    ElseIf parent.Kind = SyntaxKind.TernaryConditionalExpression Then
                        Return Await BuildContentForIntrinsicOperatorAsync(document, token, parent, New TernaryConditionalExpressionDocumentation(), Glyph.MethodPublic, cancellationToken).ConfigureAwait(False)
                    End If

                Case SyntaxKind.RemoveHandlerKeyword
                    If TypeOf parent Is AddRemoveHandlerStatementSyntax Then
                        Return Await BuildContentForIntrinsicOperatorAsync(document, token, parent, New RemoveHandlerStatementDocumentation(), Glyph.Keyword, cancellationToken).ConfigureAwait(False)
                    End If

                Case SyntaxKind.TryCastKeyword
                    If TypeOf parent Is TryCastExpressionSyntax Then
                        Return Await BuildContentForIntrinsicOperatorAsync(document, token, parent, New TryCastExpressionDocumentation(), Glyph.MethodPublic, cancellationToken).ConfigureAwait(False)
                    End If

                Case SyntaxKind.IdentifierToken
                    If SyntaxFacts.GetContextualKeywordKind(token.ToString()) = SyntaxKind.MidKeyword Then
                        If parent.Kind = SyntaxKind.MidExpression Then
                            Return Await BuildContentForIntrinsicOperatorAsync(document, token, parent, New MidAssignmentDocumentation(), Glyph.MethodPublic, cancellationToken).ConfigureAwait(False)
                        End If
                    End If
            End Select

            Return Await MyBase.BuildQuickInfoAsync(document, token, cancellationToken).ConfigureAwait(False)
        End Function

        Private Overloads Async Function BuildContentAsync(
                document As Document,
                token As SyntaxToken,
                declarators As SeparatedSyntaxList(Of VariableDeclaratorSyntax),
                cancellationToken As CancellationToken) As Task(Of QuickInfoItem)

            If declarators.Count = 0 Then
                Return Nothing
            End If

            Dim semantics = Await document.GetSemanticModelForNodeAsync(token.Parent, cancellationToken).ConfigureAwait(False)

            Dim types = declarators.SelectMany(Function(d) d.Names).Select(
                Function(n)
                    Dim symbol = semantics.GetDeclaredSymbol(n, cancellationToken)
                    If symbol Is Nothing Then
                        Return Nothing
                    End If

                    If TypeOf symbol Is ILocalSymbol Then
                        Return DirectCast(symbol, ILocalSymbol).Type
                    ElseIf TypeOf symbol Is IFieldSymbol Then
                        Return DirectCast(symbol, IFieldSymbol).Type
                    Else
                        Return Nothing
                    End If
                End Function).WhereNotNull().Distinct().ToList()

            If types.Count = 0 Then
                Return Nothing
            End If

            If types.Count > 1 Then
                Return QuickInfoItem.Create(token.Span, textBlocks:=ImmutableArray.Create(QuickInfoTextBlock.Create(QuickInfoTextKinds.Description, ImmutableArray.Create(New TaggedText(TextTags.Text, VBFeaturesResources.Multiple_Types)))))
            End If

            Return Await CreateContentAsync(document.Project.Solution.Workspace, token, semantics, types, supportedPlatforms:=Nothing, cancellationToken:=cancellationToken).ConfigureAwait(False)
        End Function

        Private Async Function BuildContentForIntrinsicOperatorAsync(document As Document,
                                                                     token As SyntaxToken,
                                                                     expression As SyntaxNode,
                                                                     documentation As AbstractIntrinsicOperatorDocumentation,
                                                                     glyph As Glyph,
                                                                     cancellationToken As CancellationToken) As Task(Of QuickInfoItem)
            Dim builder = New List(Of SymbolDisplayPart)

            builder.AddRange(documentation.PrefixParts)

            Dim semanticModel = Await document.GetSemanticModelForNodeAsync(expression, cancellationToken).ConfigureAwait(False)

            Dim position = expression.SpanStart

            For i = 0 To documentation.ParameterCount - 1
                If i <> 0 Then
                    builder.AddPunctuation(",")
                    builder.AddSpace()
                End If

                Dim typeNameToBind = documentation.TryGetTypeNameParameter(expression, i)

                If typeNameToBind IsNot Nothing Then
                    ' We'll try to bind the type name 
                    Dim typeInfo = semanticModel.GetTypeInfo(typeNameToBind, cancellationToken)

                    If typeInfo.Type IsNot Nothing Then
                        builder.AddRange(typeInfo.Type.ToMinimalDisplayParts(semanticModel, position))
                        Continue For
                    End If
                End If

                builder.AddRange(documentation.GetParameterDisplayParts(i))
            Next

            builder.AddRange(documentation.GetSuffix(semanticModel, position, expression, cancellationToken))

            Return QuickInfoItem.Create(
                token.Span,
                tags:=GlyphTags.GetTags(glyph),
                textBlocks:=ImmutableArray.Create(
                    QuickInfoTextBlock.Create(QuickInfoTextKinds.Description, builder.ToTaggedText()),
                    QuickInfoTextBlock.Create(QuickInfoTextKinds.DocumentationComments, ImmutableArray.Create(New TaggedText(TextTags.Text, documentation.DocumentationText)))))
        End Function
    End Class
End Namespace

