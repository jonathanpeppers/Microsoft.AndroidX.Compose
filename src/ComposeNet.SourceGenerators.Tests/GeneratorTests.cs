using System.Collections.Immutable;
using System.Linq;
using ComposeNet.SourceGenerators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace ComposeNet.SourceGenerators.Tests;

/// <summary>
/// Runs <see cref="ComposeDefaultsGenerator"/> against synthetic
/// compilations that provide just enough of the Compose / Kotlin shape
/// for the generator to operate on. We assert the emitted source text
/// directly — no real Compose binding DLLs needed.
/// </summary>
public class GeneratorTests
{
    const string SharedHeader = """
        namespace AndroidX.Compose.Runtime { public interface IComposer { } }
        namespace Kotlin.Jvm.Functions
        {
            public interface IFunction0 { }
            public interface IFunction2 { }
            public interface IFunction3 { }
        }
        """;

    static (Compilation Output, ImmutableArray<Diagnostic> Diagnostics, string? Emitted) RunGenerator(string assemblyAttribute, string syntheticBindings)
    {
        var input = CSharpSyntaxTree.ParseText(assemblyAttribute);
        var bindings = CSharpSyntaxTree.ParseText(SharedHeader + "\n" + syntheticBindings);

        var compilation = CSharpCompilation.Create(
            "Test",
            new[] { input, bindings },
            references: Net.Sdk.References,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));

        var driver = CSharpGeneratorDriver.Create(new ComposeDefaultsGenerator())
            .RunGeneratorsAndUpdateCompilation(compilation, out var output, out var diags);

        string? emitted = null;
        foreach (var tree in output.SyntaxTrees)
        {
            var path = tree.FilePath;
            if (path.EndsWith(".g.cs", System.StringComparison.Ordinal) &&
                !path.EndsWith("ComposeDefaultsAttribute.g.cs", System.StringComparison.Ordinal))
            {
                emitted = tree.GetText().ToString();
                break;
            }
        }

        return (output, diags, emitted);
    }

    [Fact]
    public void HappyPath_EmitsNamedBitsAndAll()
    {
        var bindings = """
            namespace Foo
            {
                public static class FooKt
                {
                    public static void Foo(int alpha, string beta,
                        AndroidX.Compose.Runtime.IComposer _composer, int _changed, int _default) { }
                }
            }
            """;
        var attr = """
            [assembly: ComposeNet.ComposeDefaults<Foo.FooKt>("Foo", "FooDefault")]
            """;

        var (_, diags, emitted) = RunGenerator(attr, bindings);
        Assert.Empty(diags);
        Assert.NotNull(emitted);
        Assert.Contains("internal enum FooDefault", emitted);
        Assert.Contains("Alpha = 1 << 0,", emitted);
        Assert.Contains("Beta  = 1 << 1,", emitted);
        Assert.Contains("All = Alpha | Beta,", emitted);
        Assert.Contains("None = 0,", emitted);
    }

    [Fact]
    public void FunctionTypedParameters_AreSkipped_HolesPreserved()
    {
        var bindings = """
            namespace Foo
            {
                public static class ButtonKt
                {
                    public static void Button(
                        Kotlin.Jvm.Functions.IFunction0 onClick,
                        int alpha, int beta, int gamma, int delta,
                        Kotlin.Jvm.Functions.IFunction3 content,
                        AndroidX.Compose.Runtime.IComposer _composer, int _changed, int _default) { }
                }
            }
            """;
        var attr = """
            [assembly: ComposeNet.ComposeDefaults<Foo.ButtonKt>("Button", "ButtonDefault")]
            """;

        var (_, diags, emitted) = RunGenerator(attr, bindings);
        Assert.Empty(diags);
        Assert.NotNull(emitted);
        Assert.Contains("Alpha = 1 << 1,", emitted);
        Assert.Contains("Beta  = 1 << 2,", emitted);
        Assert.Contains("Gamma = 1 << 3,", emitted);
        Assert.Contains("Delta = 1 << 4,", emitted);
        Assert.DoesNotContain("OnClick =", emitted);
        Assert.Contains("// bit 0: onClick", emitted);
        Assert.Contains("// bit 5: content", emitted);
        Assert.Contains("All = Alpha | Beta | Gamma | Delta,", emitted);
    }

    [Fact]
    public void MangledKotlinName_IsResolvedByExactString()
    {
        var bindings = """
            namespace Foo
            {
                public static class TextKt
                {
                    public static void TextDashDashFoo(string text, int alpha,
                        AndroidX.Compose.Runtime.IComposer _composer, int _changed) { }
                }
            }
            """;
        var attr = """
            [assembly: ComposeNet.ComposeDefaults<Foo.TextKt>("TextDashDashFoo", "TextDefault")]
            """;

        var (_, diags, emitted) = RunGenerator(attr, bindings);
        Assert.Empty(diags);
        Assert.NotNull(emitted);
        Assert.Contains("Text  = 1 << 0,", emitted);
        Assert.Contains("Alpha = 1 << 1,", emitted);
    }

    [Fact]
    public void OverloadSelection_PicksLongest()
    {
        var bindings = """
            namespace Foo
            {
                public static class FooKt
                {
                    public static void Foo(int a,
                        AndroidX.Compose.Runtime.IComposer _composer, int _changed) { }
                    public static void Foo(int a, int b, int c, int d,
                        AndroidX.Compose.Runtime.IComposer _composer, int _changed, int _default) { }
                }
            }
            """;
        var attr = """
            [assembly: ComposeNet.ComposeDefaults<Foo.FooKt>("Foo", "FooDefault")]
            """;

        var (_, diags, emitted) = RunGenerator(attr, bindings);
        Assert.Empty(diags);
        Assert.NotNull(emitted);
        Assert.Contains("D = 1 << 3,", emitted);
        Assert.Contains("All = A | B | C | D,", emitted);
    }

    [Fact]
    public void MissingMethod_ReportsCN1001()
    {
        var bindings = """
            namespace Foo
            {
                public static class FooKt
                {
                    public static void Bar(int x,
                        AndroidX.Compose.Runtime.IComposer _composer, int _changed) { }
                }
            }
            """;
        var attr = """
            [assembly: ComposeNet.ComposeDefaults<Foo.FooKt>("Foo", "FooDefault")]
            """;

        var (_, diags, _) = RunGenerator(attr, bindings);
        Assert.Contains(diags, d => d.Id == "CN1001");
    }

    [Fact]
    public void NoComposer_ReportsCN1002()
    {
        var bindings = """
            namespace Foo
            {
                public static class FooKt
                {
                    public static void Foo(int x, int y) { }
                }
            }
            """;
        var attr = """
            [assembly: ComposeNet.ComposeDefaults<Foo.FooKt>("Foo", "FooDefault")]
            """;

        var (_, diags, _) = RunGenerator(attr, bindings);
        Assert.Contains(diags, d => d.Id == "CN1002");
    }

    [Fact]
    public void SyntheticParameterName_FallsBackToParamN()
    {
        var bindings = """
            namespace Foo
            {
                public static class FooKt
                {
                    public static void Foo(int alpha, int p1, int gamma,
                        AndroidX.Compose.Runtime.IComposer _composer, int _changed, int _default) { }
                }
            }
            """;
        var attr = """
            [assembly: ComposeNet.ComposeDefaults<Foo.FooKt>("Foo", "FooDefault")]
            """;

        var (_, diags, emitted) = RunGenerator(attr, bindings);
        Assert.Empty(diags);
        Assert.NotNull(emitted);
        Assert.Contains("Alpha  = 1 << 0,", emitted);
        Assert.Contains("Param1 = 1 << 1,", emitted);
        Assert.Contains("Gamma  = 1 << 2,", emitted);
        Assert.Contains("All = Alpha | Param1 | Gamma,", emitted);
    }

    [Fact]
    public void DeclarativeAttribute_EmitsBitsFromNamesList()
    {
        var attr = """
            [assembly: ComposeNet.ComposeDefaults("ButtonDefault",
                "!onClick", "modifier", "enabled", "shape", "!content")]
            """;

        var (_, diags, emitted) = RunGenerator(attr, "// nothing");
        Assert.Empty(diags);
        Assert.NotNull(emitted);
        Assert.Contains("internal enum ButtonDefault", emitted);
        // !onClick at bit 0 is consumed but no member emitted.
        Assert.DoesNotContain("OnClick =", emitted);
        Assert.Contains("// bit 0: onClick", emitted);
        Assert.Contains("Modifier = 1 << 1,", emitted);
        Assert.Contains("Enabled  = 1 << 2,", emitted);
        Assert.Contains("Shape    = 1 << 3,", emitted);
        // !content at bit 4 is consumed but no member emitted.
        Assert.DoesNotContain("Content =", emitted);
        Assert.Contains("// bit 4: content", emitted);
        Assert.Contains("All = Modifier | Enabled | Shape,", emitted);
    }

    [Fact]
    public void DeclarativeAttribute_RequiresNoBindingSymbols()
    {
        // No synthetic Kt class — proves the declarative path doesn't need IMethodSymbol.
        var attr = """
            [assembly: ComposeNet.ComposeDefaults("FooDefault", "alpha", "beta", "gamma")]
            """;

        var (_, diags, emitted) = RunGenerator(attr, "// nothing");
        Assert.Empty(diags);
        Assert.NotNull(emitted);
        Assert.Contains("Alpha = 1 << 0,", emitted);
        Assert.Contains("Beta  = 1 << 1,", emitted);
        Assert.Contains("Gamma = 1 << 2,", emitted);
        Assert.Contains("All = Alpha | Beta | Gamma,", emitted);
    }

    [Fact]
    public void AttributeIsAddedViaPostInit()
    {
        var (output, _, _) = RunGenerator("// nothing", "// nothing");
        var attrTree = output.SyntaxTrees.FirstOrDefault(t => t.FilePath.EndsWith("ComposeDefaultsAttribute.g.cs", System.StringComparison.Ordinal));
        Assert.NotNull(attrTree);
        Assert.Contains("ComposeDefaultsAttribute<T>", attrTree.GetText().ToString());
        Assert.Contains("AttributeTargets.Assembly", attrTree.GetText().ToString());
    }
}

/// <summary>Minimal reference set so the synthetic source compiles.</summary>
internal static class Net
{
    public static class Sdk
    {
        public static readonly ImmutableArray<MetadataReference> References = BuildReferences();

        static ImmutableArray<MetadataReference> BuildReferences()
        {
            var trustedAssemblies = ((string?)System.AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")) ?? string.Empty;
            return trustedAssemblies
                .Split(System.IO.Path.PathSeparator)
                .Where(p => p.Length > 0 && System.IO.File.Exists(p))
                .Select(p => (MetadataReference)MetadataReference.CreateFromFile(p))
                .ToImmutableArray();
        }
    }
}
