using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace AndroidX.Compose.SourceGenerators.Tests;

/// <summary>Tests for implicit-composer scope enforcement.</summary>
public class ComposableScopeAnalyzerTests
{
    const string Preamble = """
        namespace AndroidX.Compose
        {
            [System.AttributeUsage(System.AttributeTargets.Method)]
            public sealed class ComposableAttribute : System.Attribute { }

            [System.AttributeUsage(System.AttributeTargets.Parameter)]
            public sealed class ComposableContentAttribute : System.Attribute { }

            public static class Composables
            {
                public static void Text(string text) { }
                public static int DerivedStateOf(System.Func<int> calculation) =>
                    calculation();
                public static void Column(
                    [ComposableContent] System.Action content) { }
                public static void Button(
                    System.Action onClick,
                    [ComposableContent] System.Action content) { }
            }

            public sealed class CompositionLocal<T>
            {
                public T Current() => default!;
            }
        }
        """;

    static ImmutableArray<Diagnostic> Analyze(string source)
    {
        var tree = CSharpSyntaxTree.ParseText(
            Preamble + "\n" + source,
            new CSharpParseOptions(LanguageVersion.Preview));
        var compilation = CSharpCompilation.Create(
            "AnalyzerTest",
            [tree],
            Net.Sdk.References,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        return compilation
            .WithAnalyzers([new ComposableScopeAnalyzer()])
            .GetAnalyzerDiagnosticsAsync()
            .GetAwaiter()
            .GetResult();
    }

    [Fact]
    public void ImplicitCall_InPlainMethod_ReportsCN5009()
    {
        var diagnostics = Analyze("""
            static class App
            {
                public static void Render() =>
                    AndroidX.Compose.Composables.Text("outside");
            }
            """);

        Assert.Contains(diagnostics, d => d.Id == "CN5009");
    }

    [Fact]
    public void ImplicitCall_InComposableMethod_IsAllowed()
    {
        var diagnostics = Analyze("""
            static class App
            {
                [AndroidX.Compose.Composable]
                public static void Render() =>
                    AndroidX.Compose.Composables.Text("inside");
            }
            """);

        Assert.DoesNotContain(diagnostics, d => d.Id == "CN5009");
    }

    [Fact]
    public void OnlyMarkedContentLambda_InheritsComposableScope()
    {
        var diagnostics = Analyze("""
            static class App
            {
                [AndroidX.Compose.Composable]
                public static void Render()
                {
                    AndroidX.Compose.Composables.Column(() =>
                        AndroidX.Compose.Composables.Button(
                            () => AndroidX.Compose.Composables.Text("event"),
                            () => AndroidX.Compose.Composables.Text("content")));
                }
            }
            """);

        var scopeDiagnostics = diagnostics.Where(d => d.Id == "CN5009").ToArray();
        Assert.Single(scopeDiagnostics);
        Assert.Contains("\"event\"", scopeDiagnostics[0].Location.SourceTree
            ?.GetText().ToString(scopeDiagnostics[0].Location.SourceSpan));
    }

    [Fact]
    public void ImplicitCompositionLocalRead_InPlainMethod_ReportsCN5009()
    {
        var diagnostics = Analyze("""
            static class App
            {
                static readonly AndroidX.Compose.CompositionLocal<string> Local = new();
                public static string Read() => Local.Current();
            }
            """);

        Assert.Contains(diagnostics, d => d.Id == "CN5009");
    }

    [Fact]
    public void ComposerIndependentComposableHelper_InPlainMethod_IsAllowed()
    {
        var diagnostics = Analyze("""
            static class App
            {
                public static int Read() =>
                    AndroidX.Compose.Composables.DerivedStateOf(() => 42);
            }
            """);

        Assert.DoesNotContain(diagnostics, d => d.Id == "CN5009");
    }
}
