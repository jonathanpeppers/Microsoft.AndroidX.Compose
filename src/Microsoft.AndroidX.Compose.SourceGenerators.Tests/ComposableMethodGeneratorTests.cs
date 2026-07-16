using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace AndroidX.Compose.SourceGenerators.Tests;

/// <summary>
/// Generator tests for <see cref="ComposableMethodGenerator"/> — synthetic
/// compilations exercising the Tier 2 interceptor-emission shapes.
/// </summary>
public class ComposableMethodGeneratorTests
{
    const string Preamble = """
        #nullable enable
        using AndroidX.Compose;
        using AndroidX.Compose.Runtime;
        using Kotlin.Jvm.Functions;
        using System.Runtime.CompilerServices;

        namespace AndroidX.Compose.Runtime
        {
            public interface IComposer
            {
                IComposer StartRestartGroup(int key);
                IScopeUpdateScope? EndRestartGroup();
                bool Skipping { get; }
                void SkipToGroupEnd();
                void StartReplaceableGroup(int key);
                void EndReplaceableGroup();
                object? RememberedValue();
                void UpdateRememberedValue(object? value);
            }
            public interface IScopeUpdateScope
            {
                void UpdateScope(Kotlin.Jvm.Functions.IFunction2 block);
            }
        }
        namespace Kotlin.Jvm.Functions
        {
            public interface IFunction0 { }
            public interface IFunction1 { }
            public interface IFunction2 { }
        }
        namespace AndroidX.Compose
        {
            [System.AttributeUsage(System.AttributeTargets.Method)]
            public sealed class ComposableAttribute : System.Attribute { }

            [System.AttributeUsage(System.AttributeTargets.Method)]
            internal sealed class ComposableDirectTargetAttribute(
                System.Type containingType, string methodName) : System.Attribute { }

            public enum ChangedBits { Uncertain = 0, Same = 1, Different = 2, Static = 4 }

            public sealed class ComposableLambda2 : Kotlin.Jvm.Functions.IFunction2
            {
                public ComposableLambda2(System.Action<AndroidX.Compose.Runtime.IComposer> body) { }
                public ComposableLambda2(System.Action<AndroidX.Compose.Runtime.IComposer, int> body) { }
            }

            public static class ComposableContext
            {
                public static AndroidX.Compose.Runtime.IComposer Current => throw new System.NotImplementedException();
                public static Scope Enter(AndroidX.Compose.Runtime.IComposer composer) => default;

                public readonly struct Scope : System.IDisposable
                {
                    public void Dispose() { }
                }
            }

            public static class ComposeExtensions
            {
                public static int DiffSlotShift(int paramIndex) => 1 + paramIndex * 3;
                public static int DiffSlot<T>(this AndroidX.Compose.Runtime.IComposer composer, T? value, int bitOffset,
                    [CallerLineNumber] int line = 0, [CallerFilePath] string file = "") => 0;
            }
        }
        """;

    static readonly CSharpParseOptions ParseOpts =
        new CSharpParseOptions(LanguageVersion.Preview).WithFeatures(
            [new KeyValuePair<string, string>(
                "InterceptorsPreviewNamespaces",
                "Microsoft.AndroidX.Compose.Generated")]);

    static (Compilation Output, ImmutableArray<Diagnostic> Diagnostics, string? Emitted) Run(string userSource)
    {
        var src = CSharpSyntaxTree.ParseText(Preamble + "\n" + userSource, ParseOpts);
        var compilation = CSharpCompilation.Create(
            "Tier2Test",
            [src],
            references: Net.Sdk.References,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
                nullableContextOptions: NullableContextOptions.Enable));

        CSharpGeneratorDriver.Create([new ComposableMethodGenerator().AsSourceGenerator()],
                parseOptions: ParseOpts)
            .RunGeneratorsAndUpdateCompilation(compilation, out var output, out var diags);

        string? emitted = null;
        foreach (var tree in output.SyntaxTrees)
        {
            var path = tree.FilePath;
            if (path.EndsWith("Composable.Interceptors.g.cs", System.StringComparison.Ordinal))
            {
                emitted = tree.GetText().ToString();
                break;
            }
        }
        return (output, diags, emitted);
    }

    [Fact]
    public void NoUserParams_EmitsRestartWrapperWithForceAwareGuard()
    {
        var (output, diags, emitted) = Run("""
            namespace App
            {
                public static class Screens
                {
                    [AndroidX.Compose.Composable]
                    public static void Splash(AndroidX.Compose.Runtime.IComposer composer) { }

                    public static void CallSite(AndroidX.Compose.Runtime.IComposer c) => Splash(c);
                }
            }
            """);

        Assert.Empty(diags);
        Assert.NotNull(emitted);
        Assert.Contains("[global::System.Runtime.CompilerServices.InterceptsLocationAttribute(", emitted);
        Assert.Contains("StartRestartGroup", emitted);
        Assert.Contains("if (__forceExecute || (__dirty & 0x1) != 0 || !__c.Skipping)", emitted);
        Assert.Contains("SkipToGroupEnd", emitted);
        Assert.Contains("EndRestartGroup", emitted);
        Assert.Contains("UpdateScope", emitted);
        Assert.Contains("global::AndroidX.Compose.ComposableLambda2", emitted);
        Assert.Contains("(__c2, __force)", emitted);
        Assert.Contains("__force | 0b1", emitted);
        // Wrapper invokes the original method by fully-qualified name.
        Assert.Contains("global::App.Screens.Splash(__c)", emitted);
        AssertNoCompileErrors(output);
    }

    [Fact]
    public void ImplicitComposer_EmitsAmbientRestartWrapper()
    {
        var (output, diags, emitted) = Run("""
            namespace App
            {
                public static class Screens
                {
                    [AndroidX.Compose.Composable]
                    public static void Greeting(string name) { }

                    public static void CallSite() => Greeting("world");
                }
            }
            """);

        Assert.Empty(diags);
        Assert.NotNull(emitted);
        Assert.Contains(
            "_Core(global::AndroidX.Compose.ComposableContext.Current, name, 0)",
            emitted);
        Assert.Contains(
            "using var __composerScope = global::AndroidX.Compose.ComposableContext.Enter(__c)",
            emitted);
        Assert.Contains("global::App.Screens.Greeting(name)", emitted);
        Assert.Contains("_Core(__c2, name, __force | 0b1)", emitted);
        AssertNoCompileErrors(output);
    }

    [Fact]
    public void ObsoleteComposable_SuppressesGeneratedForwardingWarning()
    {
        var (output, diags, emitted) = Run("""
            namespace App
            {
                [System.Obsolete(
                    "Use Modern instead.",
                    DiagnosticId = "OLD001")]
                public static class Screens
                {
                    [AndroidX.Compose.Composable]
                    public static void Legacy() { }
                }

                public static class Caller
                {
                    public static void CallSite()
                    {
            #pragma warning disable CS0618
                        Screens.Legacy();
            #pragma warning restore CS0618
                    }
                }
            }
            """);

        Assert.Empty(diags);
        Assert.NotNull(emitted);
        Assert.Contains("#pragma warning disable OLD001", emitted);
        Assert.Contains("global::App.Screens.Legacy()", emitted);
        Assert.Contains("#pragma warning restore OLD001", emitted);
        AssertNoCompileErrors(output);
    }

    [Fact]
    public void ImplicitComposer_NoParameters_EmitsValidCoreSignature()
    {
        var (output, diags, emitted) = Run("""
            namespace App
            {
                public static class Screens
                {
                    [AndroidX.Compose.Composable]
                    public static void Splash() { }

                    public static void CallSite() => Splash();
                }
            }
            """);

        Assert.Empty(diags);
        Assert.NotNull(emitted);
        Assert.Contains(
            "global::AndroidX.Compose.Runtime.IComposer __composer, int __changed",
            emitted);
        Assert.Contains("global::App.Screens.Splash()", emitted);
        AssertNoCompileErrors(output);
    }

    [Fact]
    public void KeywordParameter_IsEscapedThroughoutInterceptor()
    {
        var (output, diags, emitted) = Run("""
            namespace App
            {
                public static class Screens
                {
                    [AndroidX.Compose.Composable]
                    public static void Checkbox(bool @checked) { }

                    public static void CallSite() => Checkbox(true);
                }
            }
            """);

        Assert.Empty(diags);
        Assert.NotNull(emitted);
        Assert.Contains("bool @checked", emitted);
        Assert.Contains("Checkbox(@checked)", emitted);
        Assert.Contains("DiffSlot<bool>(@checked, 1)", emitted);
        AssertNoCompileErrors(output);
    }

    [Fact]
    public void OneParam_EmitsDiffSlotAndSkipMask()
    {
        var (output, diags, emitted) = Run("""
            namespace App
            {
                public static class Screens
                {
                    [AndroidX.Compose.Composable]
                    public static void Greeting(AndroidX.Compose.Runtime.IComposer composer, string name) { }

                    public static void CallSite(AndroidX.Compose.Runtime.IComposer c) => Greeting(c, "world");
                }
            }
            """);

        Assert.Empty(diags);
        Assert.NotNull(emitted);
        Assert.Contains("__dirty |= __c.DiffSlot<string>(name, 1)", emitted);
        // Wrapper invokes the user method by fully-qualified name.
        Assert.Contains("global::App.Screens.Greeting(__c, name)", emitted);
        // Kotlin-shape skip check for one param:
        //   mask = 0b001 | (0b101 << 1) = 0xB
        //   expected = (0b001 << 1) = 0x2
        Assert.Contains("(__dirty & 0xB) != 0x2", emitted);
        AssertNoCompileErrors(output);
    }

    [Fact]
    public void TwoParams_EmitsTwoDiffSlotsWithCorrectMaskAndExpected()
    {
        var (output, diags, emitted) = Run("""
            namespace App
            {
                public static class Screens
                {
                    [AndroidX.Compose.Composable]
                    public static void Counter(AndroidX.Compose.Runtime.IComposer composer, int count, string label) { }

                    public static void CallSite(AndroidX.Compose.Runtime.IComposer c) => Counter(c, 0, "x");
                }
            }
            """);

        Assert.Empty(diags);
        Assert.NotNull(emitted);
        Assert.Contains("__dirty |= __c.DiffSlot<int>(count, 1)", emitted);
        Assert.Contains("__dirty |= __c.DiffSlot<string>(label, 4)", emitted);
        // mask = 0b001 | (0b101 << 1) | (0b101 << 4) = 1 | 0xA | 0x50 = 0x5B
        // expected = (0b001 << 1) | (0b001 << 4) = 0x2 | 0x10 = 0x12
        Assert.Contains("(__dirty & 0x5B) != 0x12", emitted);
        Assert.Contains("global::App.Screens.Counter(__c, count, label)", emitted);
        AssertNoCompileErrors(output);
    }

    [Fact]
    public void ElevenParams_TracksFirstTenAndForcesExecution()
    {
        var (output, diags, emitted) = Run("""
            namespace App
            {
                public static class Screens
                {
                    [AndroidX.Compose.Composable]
                    public static void Wide(
                        AndroidX.Compose.Runtime.IComposer composer,
                        int p0, int p1, int p2, int p3, int p4, int p5,
                        int p6, int p7, int p8, int p9, int p10) { }

                    public static void CallSite(AndroidX.Compose.Runtime.IComposer c) =>
                        Wide(c, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
                }
            }
            """);

        Assert.Empty(diags);
        Assert.NotNull(emitted);
        Assert.Contains("DiffSlot<int>(p9, 28)", emitted);
        Assert.DoesNotContain("DiffSlot<int>(p10", emitted);
        Assert.Contains("__forceExecute = true;", emitted);
        Assert.DoesNotContain("__dirty |= 0b1;", emitted);
        AssertNoCompileErrors(output);
    }

    [Fact]
    public void NullableReferenceParam_PreservesInterceptorSignature()
    {
        var (output, diags, emitted) = Run("""
            namespace App
            {
                public static class Screens
                {
                    [AndroidX.Compose.Composable]
                    public static void Optional(
                        AndroidX.Compose.Runtime.IComposer composer,
                        System.Action<string>? callback) { }

                    public static void CallSite(AndroidX.Compose.Runtime.IComposer c) =>
                        Optional(c, null);
                }
            }
            """);

        Assert.Empty(diags);
        Assert.NotNull(emitted);
        Assert.Contains("global::System.Action<string>? callback", emitted);
        Assert.Contains("DiffSlot<global::System.Action<string>?>(callback, 1)", emitted);
        AssertNoCompileErrors(output);
    }

    [Theory]
    [InlineData("int", "42")]
    [InlineData("string?", "null")]
    [InlineData("System.Action?", "null")]
    [InlineData("Dp?", "null")]
    public void DirectTarget_OmittedOptionalArgument_SetsSurfacedParameterBit(
        string parameterType,
        string defaultValue)
    {
        var (output, diags, emitted) = Run($$"""
            namespace AndroidX.Compose
            {
                public readonly struct Dp { }
            }

            namespace App
            {
                public static class Direct
                {
                    public static void Widget(
                        AndroidX.Compose.Runtime.IComposer composer,
                        string label,
                        {{parameterType}} setting,
                        ulong omittedArguments,
                        int changed) { }
                }

                public static class Screens
                {
                    [AndroidX.Compose.Composable]
                    [AndroidX.Compose.ComposableDirectTarget(typeof(Direct), nameof(Direct.Widget))]
                    public static void Widget(string label, {{parameterType}} setting = {{defaultValue}}) { }

                    public static void CallSite() => Widget("label");
                }
            }
            """);

        Assert.Empty(diags);
        Assert.NotNull(emitted);
        var directCall = System.Text.RegularExpressions.Regex.Match(
            emitted,
            @"global::App\.Direct\.Widget\([^\r\n]+").Value;
        Assert.Equal(
            "global::App.Direct.Widget(__c, label, setting, 0x2UL, __dirty);",
            directCall);
        AssertNoCompileErrors(output);
    }

    [Fact]
    public void DirectTarget_ExplicitNull_DoesNotSetOmittedBit()
    {
        var (output, diags, emitted) = Run("""
            namespace App
            {
                public static class Direct
                {
                    public static void Widget(
                        AndroidX.Compose.Runtime.IComposer composer,
                        string? value,
                        ulong omittedArguments,
                        int changed) { }
                }

                public static class Screens
                {
                    [AndroidX.Compose.Composable]
                    [AndroidX.Compose.ComposableDirectTarget(typeof(Direct), nameof(Direct.Widget))]
                    public static void Widget(string? value = null) { }

                    public static void CallSite() => Widget(value: null);
                }
            }
            """);

        Assert.Empty(diags);
        Assert.NotNull(emitted);
        Assert.Contains(
            "global::App.Direct.Widget(__c, value, 0x0UL, __dirty)",
            emitted);
        AssertNoCompileErrors(output);
    }

    [Fact]
    public void GenericDirectTarget_ForwardsTypeArguments()
    {
        var (output, diags, emitted) = Run("""
            namespace App
            {
                public static class Direct
                {
                    public static void Widget<T>(
                        AndroidX.Compose.Runtime.IComposer composer,
                        ulong omittedArguments,
                        int changed) { }
                }

                public static class Screens
                {
                    [AndroidX.Compose.Composable]
                    [AndroidX.Compose.ComposableDirectTarget(typeof(Direct), nameof(Direct.Widget))]
                    public static void Widget<T>() { }

                    public static void CallSite() => Widget<int>();
                }
            }
            """);

        Assert.Empty(diags);
        Assert.NotNull(emitted);
        Assert.Contains("global::App.Direct.Widget<T>(__c, 0x0UL, __dirty)", emitted);
        AssertNoCompileErrors(output);
    }

    [Fact]
    public void DirectTarget_AllTrailingOptionalArguments_SetOmittedBits()
    {
        var (output, diags, emitted) = Run("""
            namespace App
            {
                public static class Direct
                {
                    public static void Widget(
                        AndroidX.Compose.Runtime.IComposer composer,
                        string required,
                        int p1, int p2, int p3, int p4, int p5,
                        int p6, int p7, int p8, int p9, int p10,
                        int p11, int p12, int p13, int p14, int p15,
                        ulong omittedArguments,
                        int changed) { }
                }

                public static class Screens
                {
                    [AndroidX.Compose.Composable]
                    [AndroidX.Compose.ComposableDirectTarget(typeof(Direct), nameof(Direct.Widget))]
                    public static void Widget(
                        string required,
                        int p1 = 0, int p2 = 0, int p3 = 0, int p4 = 0, int p5 = 0,
                        int p6 = 0, int p7 = 0, int p8 = 0, int p9 = 0, int p10 = 0,
                        int p11 = 0, int p12 = 0, int p13 = 0, int p14 = 0, int p15 = 0) { }

                    public static void CallSite() => Widget("required");
                }
            }
            """);

        Assert.Empty(diags);
        Assert.NotNull(emitted);
        Assert.Contains("0xFFFEUL, __dirty", emitted);
        AssertNoCompileErrors(output);
    }

    [Fact]
    public void DirectTarget_NamedSlotOmission_UsesCatalogParameterOrder()
    {
        var (output, diags, emitted) = Run("""
            namespace App
            {
                public static class Direct
                {
                    public static void Dialog(
                        AndroidX.Compose.Runtime.IComposer composer,
                        System.Action content,
                        System.Action? icon,
                        System.Action? title,
                        ulong omittedArguments,
                        int changed) { }
                }

                public static class Screens
                {
                    [AndroidX.Compose.Composable]
                    [AndroidX.Compose.ComposableDirectTarget(typeof(Direct), nameof(Direct.Dialog))]
                    public static void Dialog(
                        System.Action content,
                        System.Action? icon = null,
                        System.Action? title = null) { }

                    public static void CallSite() => Dialog(
                        content: static () => { },
                        title: null);
                }
            }
            """);

        Assert.Empty(diags);
        Assert.NotNull(emitted);
        Assert.Contains(
            "global::App.Direct.Dialog(__c, content, icon, title, 0x2UL, __dirty)",
            emitted);
        AssertNoCompileErrors(output);
    }

    [Fact]
    public void NotStatic_ReportsCN5001()
    {
        var (_, diags, _) = Run("""
            namespace App
            {
                public class Screens
                {
                    [AndroidX.Compose.Composable]
                    public void Foo(AndroidX.Compose.Runtime.IComposer composer) { }
                }
            }
            """);

        Assert.Contains(diags, d => d.Id == "CN5001");
    }

    [Fact]
    public void NotVoid_ReportsCN5002()
    {
        var (_, diags, _) = Run("""
            namespace App
            {
                public static class Screens
                {
                    [AndroidX.Compose.Composable]
                    public static int Foo(AndroidX.Compose.Runtime.IComposer composer) => 0;
                }
            }
            """);

        Assert.Contains(diags, d => d.Id == "CN5002");
    }

    [Fact]
    public void NoComposerFirst_ReportsCN5003()
    {
        var (_, diags, _) = Run("""
            namespace App
            {
                public static class Screens
                {
                    [AndroidX.Compose.Composable]
                    public static void Foo(int x, AndroidX.Compose.Runtime.IComposer composer) { }
                }
            }
            """);

        Assert.Contains(diags, d => d.Id == "CN5003");
    }

    [Fact]
    public void MultipleComposers_ReportCN5003()
    {
        var (_, diags, _) = Run("""
            namespace App
            {
                public static class Screens
                {
                    [AndroidX.Compose.Composable]
                    public static void Foo(
                        AndroidX.Compose.Runtime.IComposer first,
                        AndroidX.Compose.Runtime.IComposer second) { }
                }
            }
            """);

        Assert.Contains(diags, d => d.Id == "CN5003");
    }

    [Fact]
    public void InaccessibleMethodOrContainingType_ReportsCN5004AndDoesNotEmitInterceptor()
    {
        var (_, diags, emitted) = Run("""
            namespace App
            {
                public static class Screens
                {
                    [AndroidX.Compose.Composable]
                    static void Private(AndroidX.Compose.Runtime.IComposer composer) { }

                    private static class Hidden
                    {
                        [AndroidX.Compose.Composable]
                        public static void Nested(AndroidX.Compose.Runtime.IComposer composer) { }
                    }

                    public static void CallSite(AndroidX.Compose.Runtime.IComposer c)
                    {
                        Private(c);
                        Hidden.Nested(c);
                    }
                }

                file static class FileLocalScreens
                {
                    [AndroidX.Compose.Composable]
                    public static void Local(AndroidX.Compose.Runtime.IComposer composer) { }

                    public static void CallSite(AndroidX.Compose.Runtime.IComposer c) => Local(c);
                }
            }
            """);

        Assert.Equal(3, diags.Count(d => d.Id == "CN5004"));
        Assert.Null(emitted);
    }

    [Fact]
    public void AsyncMethod_ReportsCN5005AndDoesNotEmitInterceptor()
    {
        var (_, diags, emitted) = Run("""
            namespace App
            {
                public static class Screens
                {
                    [AndroidX.Compose.Composable]
                    public static async void Foo(AndroidX.Compose.Runtime.IComposer composer)
                    {
                        await System.Threading.Tasks.Task.Yield();
                    }

                    public static void CallSite(AndroidX.Compose.Runtime.IComposer c) => Foo(c);
                }
            }
            """);

        Assert.Contains(diags, d => d.Id == "CN5005");
        Assert.Null(emitted);
    }

    [Fact]
    public void ExtensionMethod_ReportsCN5006AndDoesNotEmitInterceptor()
    {
        var (_, diags, emitted) = Run("""
            namespace App
            {
                public static class Screens
                {
                    [AndroidX.Compose.Composable]
                    public static void Foo(
                        this AndroidX.Compose.Runtime.IComposer composer) { }

                    public static void CallSite(AndroidX.Compose.Runtime.IComposer c) => c.Foo();
                }
            }
            """);

        Assert.Contains(diags, d => d.Id == "CN5006");
        Assert.Null(emitted);
    }

    [Fact]
    public void GenericMethod_EmitsGenericInterceptor()
    {
        var (output, diags, emitted) = Run("""
            namespace App
            {
                public static class Screens
                {
                    [AndroidX.Compose.Composable]
                    public static void Foo<T>(
                        AndroidX.Compose.Runtime.IComposer composer, T value) { }

                    public static void CallSite(AndroidX.Compose.Runtime.IComposer c) => Foo(c, 1);
                }
            }
            """);

        Assert.Empty(diags);
        Assert.NotNull(emitted);
        Assert.Contains("public static void Composable_", emitted);
        Assert.Contains("<T>(global::AndroidX.Compose.Runtime.IComposer composer, T value)", emitted);
        Assert.Contains("_Core<T>(composer, value, 0)", emitted);
        Assert.Contains("global::App.Screens.Foo<T>(__c, value)", emitted);
        Assert.Contains("_Core<T>(__c2, value, __force | 0b1)", emitted);
        AssertNoCompileErrors(output);
    }

    [Fact]
    public void GenericMethod_PreservesTypeParameterConstraints()
    {
        var (output, diags, emitted) = Run("""
            namespace App
            {
                public static class Screens
                {
                    [AndroidX.Compose.Composable]
                    public static void Foo<TValue, TFactory>(TValue value, TFactory factory)
                        where TValue : class?
                        where TFactory : System.Collections.Generic.IEnumerable<TValue>, new() { }

                    public static void CallSite() =>
                        Foo<string?, System.Collections.Generic.List<string?>>(null, new());
                }
            }
            """);

        Assert.Empty(diags);
        Assert.NotNull(emitted);
        Assert.Contains("<TValue, TFactory>", emitted);
        Assert.Contains("where TValue : class?", emitted);
        Assert.Contains(
            "where TFactory : global::System.Collections.Generic.IEnumerable<TValue>, new()",
            emitted);
        Assert.Contains("global::App.Screens.Foo<TValue, TFactory>(value, factory)", emitted);
        AssertNoCompileErrors(output);
    }

    [Fact]
    public void GenericMethod_PreservesConstructedContainingType()
    {
        var (output, diags, emitted) = Run("""
            namespace App
            {
                public static class Screens<TContainer>
                {
                    [AndroidX.Compose.Composable]
                    public static void Foo<TValue>(TContainer container, TValue value) { }
                }

                public static class Caller
                {
                    public static void CallSite() => Screens<string>.Foo("value", 42);
                }
            }
            """);

        Assert.Empty(diags);
        Assert.NotNull(emitted);
        Assert.Contains(
            "<TValue>(string container, TValue value)",
            emitted);
        Assert.Contains(
            "global::App.Screens<string>.Foo<TValue>(container, value)",
            emitted);
        AssertNoCompileErrors(output);
    }

    [Fact]
    public void GenericMethod_DeclaresOpenContainingTypeParameters()
    {
        var (output, diags, emitted) = Run("""
            namespace App
            {
                public static class Screens<TContainer>
                    where TContainer : class
                {
                    [AndroidX.Compose.Composable]
                    public static void Foo<TValue>(TContainer container, TValue value) { }
                }

                public static class Caller
                {
                    public static void CallSite<TOuter>(TOuter container)
                        where TOuter : class =>
                        Screens<TOuter>.Foo(container, 42);
                }
            }
            """);

        Assert.Empty(diags);
        Assert.NotNull(emitted);
        Assert.Contains(
            "<TOuter, TValue>(TOuter container, TValue value)",
            emitted);
        Assert.Contains("where TOuter : class", emitted);
        Assert.Contains(
            "global::App.Screens<TOuter>.Foo<TValue>(container, value)",
            emitted);
        AssertNoCompileErrors(output);
    }

    [Fact]
    public void GenericMethod_DeclaresNestedOpenContainingTypeParameters()
    {
        var (output, diags, emitted) = Run("""
            namespace App
            {
                public class Outer<TOuter>
                {
                    public class Inner<TInner>
                    {
                        [AndroidX.Compose.Composable]
                        public static void Foo<TValue>(
                            TOuter outer, TInner inner, TValue value) { }
                    }
                }

                public static class Caller
                {
                    public static void CallSite<TFirst, TSecond>(
                        TFirst first, TSecond second) =>
                        Outer<TFirst>.Inner<TSecond>.Foo(first, second, 42);
                }
            }
            """);

        Assert.Empty(diags);
        Assert.NotNull(emitted);
        Assert.Contains(
            "<TFirst, TSecond, TValue>(TFirst outer, TSecond inner, TValue value)",
            emitted);
        Assert.Contains(
            "global::App.Outer<TFirst>.Inner<TSecond>.Foo<TValue>(outer, inner, value)",
            emitted);
        AssertNoCompileErrors(output);
    }

    [Fact]
    public void CollectionParameter_ForcesExecutionForInPlaceMutation()
    {
        var (output, diags, emitted) = Run("""
            namespace App
            {
                public static class Screens
                {
                    [AndroidX.Compose.Composable]
                    public static void Items<T>(
                        System.Collections.Generic.IReadOnlyList<T> items,
                        string label) { }

                    public static void CallSite(
                        System.Collections.Generic.IReadOnlyList<int> items) =>
                        Items(items, "Items");
                }
            }
            """);

        Assert.Empty(diags);
        Assert.NotNull(emitted);
        Assert.Contains("__forceExecute = true;", emitted);
        Assert.DoesNotContain("__dirty |= 0b1;", emitted);
        Assert.DoesNotContain("DiffSlot<global::System.Collections.Generic.IReadOnlyList<T>>", emitted);
        Assert.Contains("__c.DiffSlot<string>(label, 4)", emitted);
        AssertNoCompileErrors(output);
    }

    [Fact]
    public void GenericMethod_CollectsConstraintTypeParameterDependencies()
    {
        var (output, diags, emitted) = Run("""
            namespace App
            {
                public static class Screens<TContainer>
                {
                    [AndroidX.Compose.Composable]
                    public static void Foo(TContainer container) { }
                }

                public static class Caller
                {
                    public static void CallSite<TItem, TElement>(TItem item)
                        where TItem : System.Collections.Generic.IEnumerable<TElement> =>
                        Screens<TItem>.Foo(item);
                }
            }
            """);

        Assert.Empty(diags);
        Assert.NotNull(emitted);
        Assert.Contains("<TItem, TElement>", emitted);
        Assert.Contains(
            "where TItem : global::System.Collections.Generic.IEnumerable<TElement>",
            emitted);
        AssertNoCompileErrors(output);
    }

    [Fact]
    public void GenericMethod_AliasesCollidingTypeParameterNames()
    {
        var (output, diags, emitted) = Run("""
            #pragma warning disable CS0693
            namespace App
            {
                public static class Screens<T>
                {
                    [AndroidX.Compose.Composable]
                    public static void Foo<T>(T value) { }
                }

                public static class Caller
                {
                    public static void CallSite<T>(T value) =>
                        Screens<T>.Foo(value);
                }
            }
            """);

        Assert.Empty(diags);
        Assert.NotNull(emitted);
        Assert.Contains("<T, T_1>(T_1 value)", emitted);
        Assert.Contains("global::App.Screens<T>.Foo<T_1>(value)", emitted);
        AssertNoCompileErrors(output);
    }

    [Fact]
    public void NonGenericEnumerableParameter_ForcesExecution()
    {
        var (output, diags, emitted) = Run("""
            namespace App
            {
                public static class Screens
                {
                    [AndroidX.Compose.Composable]
                    public static void Items(System.Collections.IEnumerable items) { }

                    public static void CallSite(System.Collections.IEnumerable items) =>
                        Items(items);
                }
            }
            """);

        Assert.Empty(diags);
        Assert.NotNull(emitted);
        Assert.Contains("__forceExecute = true;", emitted);
        Assert.DoesNotContain("__dirty |= 0b1;", emitted);
        Assert.DoesNotContain("DiffSlot<global::System.Collections.IEnumerable>", emitted);
        AssertNoCompileErrors(output);
    }

    [Fact]
    public void UnconstrainedGenericParameter_ForcesExecution()
    {
        var (output, diags, emitted) = Run("""
            namespace App
            {
                public static class Screens
                {
                    [AndroidX.Compose.Composable]
                    public static void Value<T>(T value) { }

                    public static void CallSite(
                        System.Collections.Generic.List<int> items) =>
                        Value(items);
                }
            }
            """);

        Assert.Empty(diags);
        Assert.NotNull(emitted);
        Assert.Contains("__forceExecute = true;", emitted);
        Assert.DoesNotContain("__dirty |= 0b1;", emitted);
        Assert.DoesNotContain("DiffSlot<T>(value", emitted);
        AssertNoCompileErrors(output);
    }

    [Fact]
    public void ByRefParameters_ReportCN5008AndDoNotEmitInterceptors()
    {
        var (_, diags, emitted) = Run("""
            namespace App
            {
                public static class Screens
                {
                    [AndroidX.Compose.Composable]
                    public static void WithRef(
                        AndroidX.Compose.Runtime.IComposer composer, ref int value) { }

                    [AndroidX.Compose.Composable]
                    public static void WithOut(
                        AndroidX.Compose.Runtime.IComposer composer, out int value) => value = 0;

                    [AndroidX.Compose.Composable]
                    public static void WithIn(
                        AndroidX.Compose.Runtime.IComposer composer, in int value) { }

                    public static void CallSite(AndroidX.Compose.Runtime.IComposer c)
                    {
                        int value = 0;
                        WithRef(c, ref value);
                        WithOut(c, out value);
                        WithIn(c, in value);
                    }
                }
            }
            """);

        Assert.Equal(3, diags.Count(d => d.Id == "CN5008"));
        Assert.Null(emitted);
    }

    [Fact]
    public void NoInvocation_NoInterceptorEmitted()
    {
        // A [Composable] method that is never *called* anywhere in user
        // code still validates (no diagnostics) but produces no
        // interceptor file — nothing to intercept.
        var (output, diags, emitted) = Run("""
            namespace App
            {
                public static class Screens
                {
                    [AndroidX.Compose.Composable]
                    public static void Unused(AndroidX.Compose.Runtime.IComposer composer) { }
                }
            }
            """);

        Assert.Empty(diags);
        Assert.Null(emitted);
        AssertNoCompileErrors(output);
    }

    [Fact]
    public void InvocationOfNonComposable_NotIntercepted()
    {
        // Plain static methods without [Composable] are left alone —
        // the generator only emits interceptors for [Composable] targets.
        var (output, diags, emitted) = Run("""
            namespace App
            {
                public static class Screens
                {
                    public static void Plain(AndroidX.Compose.Runtime.IComposer composer) { }
                    public static void CallSite(AndroidX.Compose.Runtime.IComposer c) => Plain(c);
                }
            }
            """);

        Assert.Empty(diags);
        Assert.Null(emitted);
        AssertNoCompileErrors(output);
    }

    [Fact]
    public void RecursiveComposableCall_BothCallSitesIntercepted()
    {
        // A [Composable] method whose body calls another [Composable]
        // method produces TWO interceptor entries — one per call site.
        // This is the core property that makes Tier 2 compose all the
        // way down.
        var (output, diags, emitted) = Run("""
            namespace App
            {
                public static class Screens
                {
                    [AndroidX.Compose.Composable]
                    public static void Leaf(AndroidX.Compose.Runtime.IComposer composer, string text) { }

                    [AndroidX.Compose.Composable]
                    public static void Parent(AndroidX.Compose.Runtime.IComposer composer)
                    {
                        Leaf(composer, "hello");
                    }

                    public static void Root(AndroidX.Compose.Runtime.IComposer c) => Parent(c);
                }
            }
            """);

        Assert.Empty(diags);
        Assert.NotNull(emitted);
        // Two distinct wrapper methods: one for the Leaf call inside
        // Parent's body, one for the Parent call from Root.
        var interceptorCount = System.Text.RegularExpressions.Regex.Matches(
            emitted, @"public static void Composable_\d+_[0-9A-F]{8}\(").Count;
        Assert.Equal(2, interceptorCount);
        Assert.Contains("global::App.Screens.Leaf(__c, text)", emitted);
        Assert.Contains("global::App.Screens.Parent(__c)", emitted);
        AssertNoCompileErrors(output);
    }

    [Fact]
    public void OverloadedComposable_BothOverloadsIntercepted()
    {
        var (output, diags, emitted) = Run("""
            namespace App
            {
                public static class Screens
                {
                    [AndroidX.Compose.Composable]
                    public static void Foo(AndroidX.Compose.Runtime.IComposer composer) { }

                    [AndroidX.Compose.Composable]
                    public static void Foo(AndroidX.Compose.Runtime.IComposer composer, int n) { }

                    public static void Root(AndroidX.Compose.Runtime.IComposer c)
                    {
                        Foo(c);
                        Foo(c, 1);
                    }
                }
            }
            """);

        Assert.Empty(diags);
        Assert.NotNull(emitted);
        Assert.Contains("global::App.Screens.Foo(__c)", emitted);
        Assert.Contains("global::App.Screens.Foo(__c, n)", emitted);
        AssertNoCompileErrors(output);
    }

    [Fact]
    public void EmittedInterceptorIsAttributedAndUnderGeneratedNamespace()
    {
        // Anchor test for the interceptor shape — emitted file must
        // declare InterceptsLocationAttribute as a `file`-scoped class
        // in System.Runtime.CompilerServices, and place wrappers in
        // the Microsoft.AndroidX.Compose.Generated namespace that's
        // opted into <InterceptorsPreviewNamespaces>.
        var (output, diags, emitted) = Run("""
            namespace App
            {
                public static class Screens
                {
                    [AndroidX.Compose.Composable]
                    public static void Foo(AndroidX.Compose.Runtime.IComposer composer) { }

                    public static void CallSite(AndroidX.Compose.Runtime.IComposer c) => Foo(c);
                }
            }
            """);

        Assert.Empty(diags);
        Assert.NotNull(emitted);
        Assert.Contains("namespace System.Runtime.CompilerServices", emitted);
        Assert.Contains("file sealed class InterceptsLocationAttribute", emitted);
        Assert.Contains("namespace Microsoft.AndroidX.Compose.Generated", emitted);
        Assert.Contains("internal static class ComposableInterceptors", emitted);
        AssertNoCompileErrors(output);
    }

    static void AssertNoCompileErrors(Compilation compilation)
    {
        var errors = compilation.GetDiagnostics()
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            // Ignore CS missing-reference errors that come from the
            // synthetic stub (e.g. no real Android.Runtime).
            .Where(d => d.Id != "CS0234" && d.Id != "CS0246" && d.Id != "CS0518")
            .ToList();
        Assert.Empty(errors);
    }
}
