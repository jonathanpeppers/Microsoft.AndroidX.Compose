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
                public T Current() => throw new System.NotImplementedException();
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

    static Diagnostic[] ScopeDiagnostics(string source) =>
        Analyze(source).Where(d => d.Id == "CN5009").ToArray();

    static string SourceText(Diagnostic diagnostic) =>
        diagnostic.Location.SourceTree?.GetText()
            .ToString(diagnostic.Location.SourceSpan)
        ?? string.Empty;

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

    [Fact]
    public void ComposableMethodGroup_OnlyAllowedForComposableContent()
    {
        var diagnostics = Analyze("""
            static class App
            {
                [AndroidX.Compose.Composable]
                public static void Content() { }

                [AndroidX.Compose.Composable]
                public static void Render() =>
                    AndroidX.Compose.Composables.Button(Content, Content);
            }
            """);

        var scopeDiagnostics = diagnostics.Where(d => d.Id == "CN5009").ToArray();
        Assert.Single(scopeDiagnostics);
        Assert.Contains("Content", scopeDiagnostics[0].Location.SourceTree
            ?.GetText().ToString(scopeDiagnostics[0].Location.SourceSpan));
    }

    [Fact]
    public void ConditionalComposableMethodGroup_IsAllowedForComposableContent()
    {
        var diagnostics = Analyze("""
            static class App
            {
                [AndroidX.Compose.Composable]
                public static void Primary() { }

                [AndroidX.Compose.Composable]
                public static void Alternate() { }

                [AndroidX.Compose.Composable]
                public static void Render(bool usePrimary) =>
                    AndroidX.Compose.Composables.Column(
                        usePrimary ? Primary : Alternate);
            }
            """);

        Assert.DoesNotContain(diagnostics, d => d.Id == "CN5009");
    }

    [Fact]
    public void AsyncComposableContent_DoesNotInheritScope()
    {
        var diagnostics = Analyze("""
            static class App
            {
                [AndroidX.Compose.Composable]
                public static void Render() =>
                    AndroidX.Compose.Composables.Column(async () =>
                    {
                        await System.Threading.Tasks.Task.Yield();
                        AndroidX.Compose.Composables.Text("late");
                    });
            }
            """);

        Assert.Contains(diagnostics, d => d.Id == "CN5009");
    }

    [Fact]
    public void LocalVariable_ForwardedToComposableContent_IsAllowed()
    {
        var diagnostics = ScopeDiagnostics("""
            static class App
            {
                [AndroidX.Compose.Composable]
                public static void Render()
                {
                    System.Action content = () =>
                        AndroidX.Compose.Composables.Text("local");
                    AndroidX.Compose.Composables.Column(content);
                }
            }
            """);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public void LocalFunction_DirectCallAndMethodGroup_AreAllowed()
    {
        var diagnostics = ScopeDiagnostics("""
            static class App
            {
                [AndroidX.Compose.Composable]
                public static void Render()
                {
                    void Content() =>
                        AndroidX.Compose.Composables.Text("local function");

                    Content();
                    AndroidX.Compose.Composables.Column(Content);
                }
            }
            """);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public void PrivateReturn_ForwardedToComposableContent_IsAllowed()
    {
        var diagnostics = ScopeDiagnostics("""
            static class App
            {
                static System.Action CreateContent() => () =>
                    AndroidX.Compose.Composables.Text("returned");

                [AndroidX.Compose.Composable]
                public static void Render() =>
                    AndroidX.Compose.Composables.Column(CreateContent());
            }
            """);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public void UnmarkedParameter_ForwardedSynchronously_IsAllowed()
    {
        var diagnostics = ScopeDiagnostics("""
            static class App
            {
                static void Forward(System.Action content) =>
                    AndroidX.Compose.Composables.Column(content);

                [AndroidX.Compose.Composable]
                public static void Render() =>
                    Forward(() => AndroidX.Compose.Composables.Text("forwarded"));
            }
            """);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public void ConditionalAndCoalescingFlows_AreAllowed()
    {
        var diagnostics = ScopeDiagnostics("""
            static class App
            {
                [AndroidX.Compose.Composable]
                static void Primary() { }

                [AndroidX.Compose.Composable]
                static void Alternate() { }

                [AndroidX.Compose.Composable]
                public static void Render(bool choose, System.Action fallback)
                {
                    System.Action selected = choose ? Primary : Alternate;
                    AndroidX.Compose.Composables.Column(selected ?? fallback);
                }
            }
            """);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public void LocalVariable_UnmarkedArgument_ReportsArgumentEscape()
    {
        var diagnostics = ScopeDiagnostics("""
            static class App
            {
                [AndroidX.Compose.Composable]
                public static void Render()
                {
                    System.Action callback = () =>
                        AndroidX.Compose.Composables.Text("event");
                    AndroidX.Compose.Composables.Button(callback, () => { });
                }
            }
            """);

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("callback", SourceText(diagnostic));
    }

    [Fact]
    public void LocalVariable_WithValidAndEscapingUses_ReportsOnlyEscape()
    {
        var diagnostics = ScopeDiagnostics("""
            static class App
            {
                [AndroidX.Compose.Composable]
                public static void Render()
                {
                    System.Action content = () =>
                        AndroidX.Compose.Composables.Text("mixed");
                    AndroidX.Compose.Composables.Column(content);
                    AndroidX.Compose.Composables.Button(content, content);
                }
            }
            """);

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("content", SourceText(diagnostic));
    }

    [Fact]
    public void UnusedLocalDelegate_IsRejectedAsUnproven()
    {
        var diagnostics = ScopeDiagnostics("""
            static class App
            {
                public static void Render()
                {
                    System.Action unused = () =>
                        AndroidX.Compose.Composables.Text("never invoked");
                }
            }
            """);

        var diagnostic = Assert.Single(diagnostics);
        Assert.Contains("unused", SourceText(diagnostic));
    }

    [Fact]
    public void PublicUnmarkedForwarder_IsRejectedAsAmbiguous()
    {
        var diagnostics = ScopeDiagnostics("""
            static class Forwarder
            {
                public static void Forward(System.Action content) =>
                    AndroidX.Compose.Composables.Column(content);
            }

            static class App
            {
                [AndroidX.Compose.Composable]
                public static void Render() =>
                    Forwarder.Forward(() =>
                        AndroidX.Compose.Composables.Text("ambiguous contract"));
            }
            """);

        Assert.Equal(2, diagnostics.Length);
        Assert.Contains(diagnostics, d => SourceText(d).Contains("() =>"));
        Assert.Contains(diagnostics, d => SourceText(d).Contains("Column(content)"));
    }

    [Fact]
    public void FieldStorage_ReportsFieldInitializerEscape()
    {
        var diagnostics = ScopeDiagnostics("""
            static class App
            {
                static readonly System.Action Stored = () =>
                    AndroidX.Compose.Composables.Text("stored");
            }
            """);

        var diagnostic = Assert.Single(diagnostics);
        Assert.Contains("Stored", SourceText(diagnostic));
    }

    [Fact]
    public void PublicReturn_ReportsReturnEscape()
    {
        var diagnostics = ScopeDiagnostics("""
            static class App
            {
                public static System.Action CreateContent() => () =>
                    AndroidX.Compose.Composables.Text("public return");
            }
            """);

        var diagnostic = Assert.Single(diagnostics);
        Assert.Contains("=>", SourceText(diagnostic));
    }

    [Fact]
    public void PrivateReturn_ToUnmarkedArgument_ReportsArgumentEscape()
    {
        var diagnostics = ScopeDiagnostics("""
            static class App
            {
                static System.Action CreateContent() => () =>
                    AndroidX.Compose.Composables.Text("returned event");

                [AndroidX.Compose.Composable]
                public static void Render() =>
                    AndroidX.Compose.Composables.Button(CreateContent(), () => { });
            }
            """);

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("CreateContent()", SourceText(diagnostic));
    }

    [Fact]
    public void AsyncForwarder_ReportsArgumentEscape()
    {
        var diagnostics = ScopeDiagnostics("""
            static class App
            {
                static async System.Threading.Tasks.Task Forward(System.Action content)
                {
                    await System.Threading.Tasks.Task.Yield();
                    content();
                }

                [AndroidX.Compose.Composable]
                public static void Render() =>
                    Forward(() => AndroidX.Compose.Composables.Text("async"));
            }
            """);

        var diagnostic = Assert.Single(diagnostics);
        Assert.Contains("() =>", SourceText(diagnostic));
    }

    [Fact]
    public void DeferredAnonymousFunction_ReportsDeferredArgumentEscape()
    {
        var diagnostics = ScopeDiagnostics("""
            static class App
            {
                static void Forward(System.Action content) =>
                    System.Threading.Tasks.Task.Run(() => content());

                [AndroidX.Compose.Composable]
                public static void Render() =>
                    Forward(() => AndroidX.Compose.Composables.Text("deferred"));
            }
            """);

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("() => content()", SourceText(diagnostic));
    }

    [Fact]
    public void LocalFunction_CalledOutsideComposableScope_ReportsCallSite()
    {
        var diagnostics = ScopeDiagnostics("""
            static class App
            {
                public static void Render()
                {
                    void Content() =>
                        AndroidX.Compose.Composables.Text("outside local");

                    Content();
                }
            }
            """);

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("Content()", SourceText(diagnostic));
    }

    [Fact]
    public void NestedLocalFunction_FieldEscape_IsRejected()
    {
        var diagnostics = ScopeDiagnostics("""
            static class App
            {
                static System.Action Stored = () => { };

                static void Capture(System.Action content)
                {
                    void Store() => Stored = content;
                    Store();
                }

                [AndroidX.Compose.Composable]
                public static void Render() =>
                    Capture(() => AndroidX.Compose.Composables.Text("leaked"));
            }
            """);

        var diagnostic = Assert.Single(diagnostics);
        Assert.Contains("Stored", SourceText(diagnostic));
    }

    [Fact]
    public void AsyncLocalFunction_IsRejected()
    {
        var diagnostics = ScopeDiagnostics("""
            static class App
            {
                static void Capture(System.Action content)
                {
                    async System.Threading.Tasks.Task Deferred()
                    {
                        await System.Threading.Tasks.Task.Yield();
                        content();
                    }
                    _ = Deferred();
                }

                [AndroidX.Compose.Composable]
                public static void Render() =>
                    Capture(() => AndroidX.Compose.Composables.Text("late local"));
            }
            """);

        var diagnostic = Assert.Single(diagnostics);
        Assert.Contains("async", SourceText(diagnostic));
    }

    [Fact]
    public void ImmediateInvocation_OutsideComposableScope_IsRejected()
    {
        var diagnostics = ScopeDiagnostics("""
            static class App
            {
                public static void Render() =>
                    ((System.Action)(() =>
                        AndroidX.Compose.Composables.Text("immediate")))();
            }
            """);

        var diagnostic = Assert.Single(diagnostics);
        Assert.Contains("System.Action", SourceText(diagnostic));
    }

    [Fact]
    public void ImmediateInvocation_InComposableScope_IsAllowed()
    {
        var diagnostics = ScopeDiagnostics("""
            static class App
            {
                [AndroidX.Compose.Composable]
                public static void Render() =>
                    ((System.Action)(() =>
                        AndroidX.Compose.Composables.Text("immediate")))();
            }
            """);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public void PrivateHelperMethodGroup_ComposableContent_IsAllowed()
    {
        var diagnostics = ScopeDiagnostics("""
            static class App
            {
                static void Content() =>
                    AndroidX.Compose.Composables.Text("method group content");

                [AndroidX.Compose.Composable]
                public static void Render() =>
                    AndroidX.Compose.Composables.Column(Content);
            }
            """);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public void PrivateHelperMethodGroup_DeferredCallback_IsRejected()
    {
        var diagnostics = ScopeDiagnostics("""
            static class App
            {
                static void Event() =>
                    AndroidX.Compose.Composables.Text("method group event");

                [AndroidX.Compose.Composable]
                public static void Render() =>
                    AndroidX.Compose.Composables.Button(Event, () => { });
            }
            """);

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("Event", SourceText(diagnostic));
    }

    [Fact]
    public void UncalledPrivateHelper_IsRejectedAsUnproven()
    {
        var diagnostics = ScopeDiagnostics("""
            static class App
            {
                static void NeverCalled() =>
                    AndroidX.Compose.Composables.Text("orphan");
            }
            """);

        var diagnostic = Assert.Single(diagnostics);
        Assert.Contains("Text", SourceText(diagnostic));
    }
}
