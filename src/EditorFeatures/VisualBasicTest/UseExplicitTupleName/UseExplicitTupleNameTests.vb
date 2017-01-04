﻿' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics
Imports Microsoft.CodeAnalysis.UseExplicitTupleName

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.UseExplicitTupleName
    Public Class UseExplicitTupleNameTests
        Inherits AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest

        Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace) As Tuple(Of DiagnosticAnalyzer, CodeFixProvider)
            Return Tuple.Create(Of DiagnosticAnalyzer, CodeFixProvider)(
                New UseExplicitTupleNameDiagnosticAnalyzer(),
                New UseExplicitTupleNameCodeFixProvider())
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExplicitTupleName)>
        Public Async Function TestNamedTuple1() As Task
            Await TestAsync(
"
class C
    Sub M()
        dim v1 as (i as integer, s as string)
        dim v2 = v1.[|Item1|]
    end sub
end class",
"
class C
    Sub M()
        dim v1 as (i as integer, s as string)
        dim v2 = v1.i
    end sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExplicitTupleName)>
        Public Async Function TestInArgument() As Task
            Await TestAsync(
"
class C
    Sub M()
        dim v1 as (i as integer, s as string)
        Foo(v1.[|Item1|])
    end sub

    Sub Foo(i as integer)
    end sub
end class",
"
class C
    Sub M()
        dim v1 as (i as integer, s as string)
        Foo(v1.i)
    end sub

    Sub Foo(i as integer)
    end sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExplicitTupleName)>
        Public Async Function TestNamedTuple2() As Task
            Await TestAsync(
"
class C
    Sub M()
        dim v1 as (i as integer, s as string)
        dim v2 = v1.[|Item2|]
    end sub
end class",
"
class C
    Sub M()
        dim v1 as (i as integer, s as string)
        dim v2 = v1.s
    end sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExplicitTupleName)>
        Public Async Function TestMissingOnMatchingName1() As Task
            Await TestMissingAsync(
"
class C
    Sub M()
        dim v1 as (integer, s as string)
        dim v2 = v1.[|Item1|]
    end sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExplicitTupleName)>
        Public Async Function TestMissingOnMatchingName2() As Task
            Await TestMissingAsync(
"
class C
    Sub M()
        dim v1 as (Item1 as integer, s as string)
        dim v2 = v1.[|Item1|]
    end sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExplicitTupleName)>
        Public Async Function TestWrongCasing() As Task
            Await TestMissingAsync(
"
class C
    Sub M()
        dim v1 as (item1 as integer, s as string)
        dim v2 = v1.[|Item1|]
    end sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExplicitTupleName)>
        Public Async Function TestFixAll1() As Task
            Await TestAsync(
"
class C
    Sub M()
        dim v1 as (i as integer, s as string)
        dim v2 = v1.{|FixAllInDocument:Item1|}
        dim v3 = v1.Item2
    end sub
end class",
"
class C
    Sub M()
        dim v1 as (i as integer, s as string)
        dim v2 = v1.i
        dim v3 = v1.s
    end sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExplicitTupleName)>
        Public Async Function TestFixAll2() As Task
            Await TestAsync(
"
class C
    Sub M()
        dim v1 as (i as integer, s as integer) 
        v1.{|FixAllInDocument:Item1|} = v1.Item2
    end sub
end class",
"
class C
    Sub M()
        dim v1 as (i as integer, s as integer) 
        v1.i = v1.s
    end sub
end class")
        End Function
    End Class
End Namespace