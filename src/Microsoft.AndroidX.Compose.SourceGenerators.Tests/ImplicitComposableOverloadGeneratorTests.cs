using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace AndroidX.Compose.SourceGenerators.Tests;

/// <summary>
/// Generator tests for <see cref="ImplicitComposableOverloadGenerator"/>.
/// </summary>
public class ImplicitComposableOverloadGeneratorTests
{
    const string Preamble = """
        #nullable enable
        namespace AndroidX.Compose.Runtime
        {
            public interface IComposer { }
        }
        namespace AndroidX.Compose
        {
            [System.AttributeUsage(System.AttributeTargets.Method)]
            public sealed class ComposableAttribute : System.Attribute { }

            [System.AttributeUsage(System.AttributeTargets.Parameter)]
            public sealed class ComposableContentAttribute : System.Attribute { }

            [System.AttributeUsage(System.AttributeTargets.Method)]
            internal sealed class GenerateImplicitComposableAttribute : System.Attribute { }

            public static class ComposableContext
            {
                public static Runtime.IComposer Current =>
                    throw new System.NotImplementedException();
            }

            public static partial class Composables { }

            public readonly struct Token { }
            public enum Mode { First, Second }
        }
        """;

    static (
        Compilation Output,
        ImmutableArray<Diagnostic> Diagnostics,
        string? Emitted) Run(string source)
    {
        var parseOptions = new CSharpParseOptions(LanguageVersion.Preview);
        var compilation = CSharpCompilation.Create(
            "ImplicitOverloadTest",
            [CSharpSyntaxTree.ParseText(Preamble + "\n" + source,
                parseOptions)],
            references: Net.Sdk.References,
            options: new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary,
                nullableContextOptions: NullableContextOptions.Enable));

        CSharpGeneratorDriver.Create(
                [new ImplicitComposableOverloadGenerator().AsSourceGenerator()],
                parseOptions: parseOptions)
            .RunGeneratorsAndUpdateCompilation(
                compilation,
                out var output,
                out var diagnostics);

        string? emitted = output.SyntaxTrees
            .Where(static tree =>
                tree.FilePath.EndsWith(
                    "ImplicitComposableOverloads.g.cs",
                    System.StringComparison.Ordinal))
            .Select(static tree => tree.ToString())
            .FirstOrDefault();
        return (output, diagnostics, emitted);
    }

    [Fact]
    public void EmitsAmbientOverloadAndLowersComposableActions()
    {
        const string source = """
            namespace AndroidX.Compose
            {
                public static partial class Composables
                {
                    [System.Obsolete(
                        "Use Modern instead.",
                        DiagnosticId = "OLD001",
                        UrlFormat = "https://example.test/{0}")]
                    [Composable, GenerateImplicitComposable]
                    internal static void Panel(
                        Runtime.IComposer composer,
                        int count,
                        [ComposableContent] System.Action<Runtime.IComposer> header,
                        [ComposableContent] System.Action<string, Runtime.IComposer>? body = null,
                        bool enabled = true,
                        Token token = default,
                        Mode mode = Mode.Second)
                    {
                    }
                }
            }
            """;

        var (output, diagnostics, emitted) = Run(source);

        Assert.Empty(diagnostics);
        Assert.NotNull(emitted);
        Assert.Contains(
            "[global::AndroidX.Compose.ComposableContentAttribute] global::System.Action header",
            emitted);
        Assert.Contains(
            "global::System.Action<string>? body = null",
            emitted);
        Assert.Contains(
            "global::AndroidX.Compose.Token token = default(global::AndroidX.Compose.Token)",
            emitted);
        Assert.Contains(
            "global::AndroidX.Compose.Mode mode = (global::AndroidX.Compose.Mode)1",
            emitted);
        Assert.Contains("(__composer) => header()", emitted);
        Assert.Contains(
            "body is null ? null : (__p0, __composer) => body(__p0)",
            emitted);
        Assert.Contains(
            "[global::System.ObsoleteAttribute(\"Use Modern instead.\", DiagnosticId = \"OLD001\", UrlFormat = \"https://example.test/{0}\")]",
            emitted);
        Assert.Contains("public static void Panel(", emitted);
        Assert.DoesNotContain(
            output.GetDiagnostics(),
            static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    public void AcceptsPublicImplementationTarget()
    {
        const string source = """
            namespace AndroidX.Compose
            {
                public static partial class Composables
                {
                    [Composable, GenerateImplicitComposable]
                    public static void Panel(Runtime.IComposer composer)
                    {
                    }
                }
            }
            """;

        var (output, diagnostics, emitted) = Run(source);

        Assert.Empty(diagnostics);
        Assert.NotNull(emitted);
        Assert.Contains("public static void Panel()", emitted);
        Assert.DoesNotContain(
            output.GetDiagnostics(),
            static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    public void ReportsComposableContentWithoutTrailingComposer()
    {
        const string source = """
            namespace AndroidX.Compose
            {
                public static partial class Composables
                {
                    [Composable, GenerateImplicitComposable]
                    public static void Invalid(
                        Runtime.IComposer composer,
                        [ComposableContent] System.Action<string> content)
                    {
                    }
                }
            }
            """;

        var (_, diagnostics, emitted) = Run(source);

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("CN5010", diagnostic.Id);
        Assert.Contains(
            "must be System.Action<..., IComposer>",
            diagnostic.GetMessage());
        Assert.Null(emitted);
    }

    [Fact]
    public void PreservesNullObsoleteMessageAndErrorFlag()
    {
        const string source = """
            namespace AndroidX.Compose
            {
                public static partial class Composables
                {
                    [System.Obsolete(null, true)]
                    [Composable, GenerateImplicitComposable]
                    public static void Legacy(Runtime.IComposer composer)
                    {
                    }
                }
            }
            """;

        var (output, diagnostics, emitted) = Run(source);

        Assert.Empty(diagnostics);
        Assert.NotNull(emitted);
        Assert.Contains(
            "[global::System.ObsoleteAttribute(null, true)]",
            emitted);
        Assert.DoesNotContain(
            output.GetDiagnostics(),
            static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
    }
}
