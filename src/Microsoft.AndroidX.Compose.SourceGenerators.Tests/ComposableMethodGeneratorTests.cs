using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace AndroidX.Compose.SourceGenerators.Tests;

/// <summary>
/// Generator tests for <see cref="ComposableMethodGenerator"/> — synthetic
/// compilations exercising the Tier 2 emission shapes.
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

            public enum ChangedBits { Uncertain = 0, Same = 1, Different = 2, Static = 4 }

            public sealed class ComposableLambda2 : Kotlin.Jvm.Functions.IFunction2
            {
                public ComposableLambda2(System.Action<AndroidX.Compose.Runtime.IComposer> body) { }
            }

            public static class ComposeExtensions
            {
                public static int DiffSlotShift(int paramIndex) => 1 + paramIndex * 3;
                public static int DiffSlot<T>(this AndroidX.Compose.Runtime.IComposer composer, T? value, int bitOffset,
                    [CallerLineNumber] int line = 0, [CallerFilePath] string file = "") => 0;
            }
        }
        """;

    static (Compilation Output, ImmutableArray<Diagnostic> Diagnostics, string? Emitted) Run(string userSource)
    {
        var src = CSharpSyntaxTree.ParseText(Preamble + "\n" + userSource);
        var compilation = CSharpCompilation.Create(
            "Tier2Test",
            new[] { src },
            references: Net.Sdk.References,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
                nullableContextOptions: NullableContextOptions.Enable));

        CSharpGeneratorDriver.Create(new ComposableMethodGenerator())
            .RunGeneratorsAndUpdateCompilation(compilation, out var output, out var diags);

        string? emitted = null;
        foreach (var tree in output.SyntaxTrees)
        {
            var path = tree.FilePath;
            if (path.Contains("AndroidX.Compose.Composable.", System.StringComparison.Ordinal) &&
                path.EndsWith(".g.cs", System.StringComparison.Ordinal))
            {
                emitted = tree.GetText().ToString();
                break;
            }
        }
        return (output, diags, emitted);
    }

    [Fact]
    public void NoParameters_EmitsRestartWrapperWithSkippingOnlyGuard()
    {
        var (output, diags, emitted) = Run("""
            namespace App
            {
                public static partial class Screens
                {
                    [AndroidX.Compose.Composable]
                    public static partial void Splash(AndroidX.Compose.Runtime.IComposer composer);

                    static void SplashImpl(AndroidX.Compose.Runtime.IComposer composer) { }
                }
            }
            """);

        Assert.Empty(diags);
        Assert.NotNull(emitted);
        Assert.Contains("StartRestartGroup", emitted);
        // No user params → skip prelude collapses to "if (!__c.Skipping)".
        Assert.Contains("if (!__c.Skipping)", emitted);
        Assert.Contains("SplashImpl(__c)", emitted);
        Assert.Contains("SkipToGroupEnd", emitted);
        Assert.Contains("EndRestartGroup", emitted);
        Assert.Contains("UpdateScope", emitted);
        Assert.Contains("ComposableLambda2", emitted);
        // The whole user compilation + emitted source must compile clean.
        AssertNoCompileErrors(output);
    }

    [Fact]
    public void OneParam_EmitsDiffSlotAndBodyCall()
    {
        var (output, diags, emitted) = Run("""
            namespace App
            {
                public static partial class Screens
                {
                    [AndroidX.Compose.Composable]
                    public static partial void Greeting(AndroidX.Compose.Runtime.IComposer composer, string name);

                    static void GreetingImpl(AndroidX.Compose.Runtime.IComposer composer, string name) { }
                }
            }
            """);

        Assert.Empty(diags);
        Assert.NotNull(emitted);
        Assert.Contains("__dirty |= __c.DiffSlot<string>(name, 1)", emitted);
        Assert.Contains("GreetingImpl(__c, name)", emitted);
        // Different bit for one param at offset 1 → 0b010 << 1 = 0x4.
        Assert.Contains("(__dirty & 0x4)", emitted);
        AssertNoCompileErrors(output);
    }

    [Fact]
    public void TwoParams_EmitsTwoDiffSlotsWithCorrectOffsets()
    {
        var (output, diags, emitted) = Run("""
            namespace App
            {
                public static partial class Screens
                {
                    [AndroidX.Compose.Composable]
                    public static partial void Counter(AndroidX.Compose.Runtime.IComposer composer, int count, string label);

                    static void CounterImpl(AndroidX.Compose.Runtime.IComposer composer, int count, string label) { }
                }
            }
            """);

        Assert.Empty(diags);
        Assert.NotNull(emitted);
        Assert.Contains("__dirty |= __c.DiffSlot<int>(count, 1)", emitted);
        Assert.Contains("__dirty |= __c.DiffSlot<string>(label, 4)", emitted);
        // differentMask = (0b010 << 1) | (0b010 << 4) = 0x4 | 0x20 = 0x24.
        Assert.Contains("(__dirty & 0x24)", emitted);
        Assert.Contains("CounterImpl(__c, count, label)", emitted);
        AssertNoCompileErrors(output);
    }

    [Fact]
    public void MissingImpl_ReportsCN5002()
    {
        var (_, diags, _) = Run("""
            namespace App
            {
                public static partial class Screens
                {
                    [AndroidX.Compose.Composable]
                    public static partial void Foo(AndroidX.Compose.Runtime.IComposer composer);
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
                public static partial class Screens
                {
                    [AndroidX.Compose.Composable]
                    public static partial void Foo(int x, AndroidX.Compose.Runtime.IComposer composer);

                    static void FooImpl(int x, AndroidX.Compose.Runtime.IComposer composer) { }
                }
            }
            """);

        Assert.Contains(diags, d => d.Id == "CN5003");
    }

    [Fact]
    public void NotPartial_ReportsCN5001()
    {
        var (_, diags, _) = Run("""
            namespace App
            {
                public static partial class Screens
                {
                    [AndroidX.Compose.Composable]
                    public static void Foo(AndroidX.Compose.Runtime.IComposer composer) { }
                }
            }
            """);

        Assert.Contains(diags, d => d.Id == "CN5001");
    }

    [Fact]
    public void ImplSignatureMismatch_ReportsCN5004()
    {
        var (_, diags, _) = Run("""
            namespace App
            {
                public static partial class Screens
                {
                    [AndroidX.Compose.Composable]
                    public static partial void Foo(AndroidX.Compose.Runtime.IComposer composer, int x);

                    static void FooImpl(AndroidX.Compose.Runtime.IComposer composer, string x) { }
                }
            }
            """);

        Assert.Contains(diags, d => d.Id == "CN5004");
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
