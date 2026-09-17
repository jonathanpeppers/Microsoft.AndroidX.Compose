using System;
using System.IO;
using System.Linq;
using System.Runtime.Loader;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace AndroidX.Compose.SourceGenerators.Tests;

public class FlowOverflowFacadeTests
{
    [Theory]
    [InlineData("Row", "maxItemsInEachRow")]
    [InlineData("Column", "maxItemsInEachColumn")]
    public void ManagedOptionsPreserveCatalogArityAndOmission(string direction, string maxItems)
    {
        var (output, diagnostics, emitted) = FacadeGeneratorTests.Run(Contract(direction, maxItems), $"Flow{direction}");
        Assert.Empty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
        Assert.Empty(output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error));
        Assert.NotNull(emitted);
        Assert.Contains($"public global::AndroidX.Compose.Flow{direction}Overflow? Overflow", emitted);
        Assert.Contains($"Flow{direction}_PrimaryResource_Implicit_WithAddedSlots", emitted);
        Assert.Contains($"Flow{direction}Default.Overflow", emitted);
        Assert.Contains($"ScopeKind.{direction}", emitted);
        Assert.Contains("CallerArgumentExpressionAttribute(\"overflow\")", emitted);
        Assert.Contains($"Flow{direction}_PrimaryResource_Implicit_WithAddedSlots_WithArgumentPresence", emitted);

        output = output.AddSyntaxTrees(CSharpSyntaxTree.ParseText($$"""
            namespace AndroidX.Compose
            {
                public static class Probe
                {
                    public static int Run()
                    {
                        var c = new TestComposer();
                        var option = new Flow{{direction}}Overflow();
                        System.Action<System.Action, int, int, Modifier?> old = Composables.Flow{{direction}};
                        System.Action<Runtime.IComposer, System.Action, int, int, Modifier?, ulong, int>
                            oldTarget = Composables.Flow{{direction}}_PrimaryResource_Implicit;
                        oldTarget(c, () => {}, 2, 1, null, 0UL, int.MaxValue);
                        if (ComposeBridges.LastDefaults != 78 || ComposeBridges.LastOverflow is not null)
                            throw new System.Exception("Legacy caller did not omit added overflow.");
                        Composables.Flow{{direction}}_PrimaryResource_Implicit_WithAddedSlots(
                            c, () => {}, 2, 1, null, option, 0x8UL, 0);
                        if (ComposeBridges.LastDefaults != 15 ||
                            !ReferenceEquals(option, ComposeBridges.LastOverflow))
                            throw new System.Exception("Explicit overflow was not forwarded.");
                        Composables.Flow{{direction}}_PrimaryResource_Implicit_WithAddedSlots(
                            c, () => {}, 2, 1, null, null, 0x8UL, 0);
                        if (ComposeBridges.LastDefaults != 15 || ComposeBridges.LastOverflow is not null)
                            throw new System.Exception("Explicit null was treated as omitted.");
                        Composables.Flow{{direction}}_PrimaryResource_Implicit_WithAddedSlots(
                            c, () => {}, 2, 1, null, null, 0x18UL, 0);
                        if (ComposeBridges.LastDefaults != 79)
                            throw new System.Exception("Omitted overflow did not retain its default bit.");
                        return ComposeBridges.LastDefaults;
                    }
                }
            }
            """));
        using var stream = new MemoryStream();
        var result = output.Emit(stream);
        Assert.True(result.Success, string.Join(Environment.NewLine, result.Diagnostics));
        stream.Position = 0;
        var context = new AssemblyLoadContext(nameof(FlowOverflowFacadeTests), isCollectible: true);
        try
        {
            var run = context.LoadFromStream(stream).GetType("AndroidX.Compose.Probe")?.GetMethod("Run")
                ?? throw new InvalidOperationException("Flow contract probe missing.");
            Assert.Equal(79, run.Invoke(null, null));
        }
        finally { context.Unload(); }
    }

    [Theory]
    [InlineData("Row", "maxItemsInEachRow")]
    [InlineData("Column", "maxItemsInEachColumn")]
    public void ManagedOptionsRejectNonNullableProperties(string direction, string maxItems)
    {
        var (_, diagnostics, _) = FacadeGeneratorTests.Run(
            Contract(direction, maxItems).Replace($"Flow{direction}Overflow?", $"Flow{direction}Overflow"),
            $"Flow{direction}");
        Assert.Contains(diagnostics, d => d.Id == "CN3002");
    }

    [Fact]
    public void UnregisteredManagedReferenceStillRequiresExplicitSupport()
    {
        var (_, diagnostics, _) = FacadeGeneratorTests.Run(
            Contract("Row", "maxItemsInEachRow").Replace("FlowRowOverflow", "UnregisteredOverflow"), "FlowRow");
        Assert.Contains(diagnostics, d => d.Id == "CN3002");
    }

    [Theory]
    [InlineData("Row", "maxItemsInEachRow")]
    [InlineData("Column", "maxItemsInEachColumn")]
    public void UninterceptedPublicCallsPreserveOmissionAndRejectExplicitNull(string direction, string maxItems)
    {
        string contract = Contract(direction, maxItems).Replace(
            "LastOverflow = overflow;",
            """
            LastOverflow = overflow;
            if ((defaults & 64) == 0)
                System.ArgumentNullException.ThrowIfNull(overflow);
            """);
        var (output, diagnostics, _) = FacadeGeneratorTests.Run(contract, $"Flow{direction}");
        Assert.Empty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
        var stubs = output.SyntaxTrees.First();
        var root = stubs.GetRoot();
        var contextStub = root.DescendantNodes().OfType<ClassDeclarationSyntax>()
            .Single(c => c.Identifier.ValueText == "ComposableContext");
        var replacement = SyntaxFactory.ParseMemberDeclaration("""
            public static class ComposableContext
            {
                public static global::AndroidX.Compose.Runtime.IComposer Current { get; } =
                    new global::AndroidX.Compose.TestComposer();
            }
            """) ?? throw new InvalidOperationException("Could not parse the active composer stand-in.");
        output = output.ReplaceSyntaxTree(stubs, root.ReplaceNode(contextStub, replacement).SyntaxTree);
        output = output.AddSyntaxTrees(CSharpSyntaxTree.ParseText($$"""
            namespace AndroidX.Compose
            {
                public static class PublicCallProbe
                {
                    public static void Run()
                    {
                        var c = ComposableContext.Current;
                        Composables.Flow{{direction}}(() => {});
                        Require(ComposeBridges.LastDefaults == 127, "Omitted public overflow must use Clip.");
                        Composables.Flow{{direction}}(() => {}, maxLines: 1);
                        Require(ComposeBridges.LastDefaults == 95, "Named arguments must preserve omitted overflow.");
                        RejectNull(() => Composables.Flow{{direction}}(() => {}, overflow: null));
                        Flow{{direction}}Overflow? missing = null;
                        RejectNull(() => Composables.Flow{{direction}}(() => {}, overflow: missing));
                        RejectNull(() => Composables.Flow{{direction}}(() => {}, 3, 1, null, null));
                        System.Action<System.Action, int, int, Modifier?, Flow{{direction}}Overflow?> rich =
                            Composables.Flow{{direction}};
                        RejectNull(() => rich(() => {}, 3, 1, null, null));
                        System.Action<System.Action, int, int, Modifier?> legacy = Composables.Flow{{direction}};
                        legacy(() => {}, 3, 1, null);
                        Require(ComposeBridges.LastDefaults == 79, "Legacy public delegate must omit overflow.");
                        var option = new Flow{{direction}}Overflow();
                        Composables.Flow{{direction}}(() => {}, overflow: option);
                        Require(ComposeBridges.LastDefaults == 63 &&
                            ReferenceEquals(option, ComposeBridges.LastOverflow), "Supplied configuration was lost.");

                        Composables.Flow{{direction}}(c, _ => {});
                        Require(ComposeBridges.LastDefaults == 127, "Explicit composer must preserve omission.");
                        RejectNull(() => Composables.Flow{{direction}}(c, _ => {}, overflow: null));
                        RejectNull(() => Composables.Flow{{direction}}(c, _ => {}, 3, 1, null, missing));
                        Composables.Flow{{direction}}(c, _ => {}, overflow: option);
                        Require(ComposeBridges.LastDefaults == 63, "Explicit composer lost supplied overflow.");
                    }
                    static void RejectNull(System.Action call)
                    {
                        try { call(); }
                        catch (System.ArgumentNullException error) when (error.ParamName == "overflow") { return; }
                        throw new System.Exception("Explicit null did not reach the native-boundary guard.");
                    }
                    static void Require(bool condition, string message)
                    {
                        if (!condition) throw new System.Exception(message);
                    }
                }
            }
            """));
        using var image = new MemoryStream();
        var result = output.Emit(image);
        Assert.True(result.Success, string.Join(Environment.NewLine, result.Diagnostics));
        image.Position = 0;
        var context = new AssemblyLoadContext(nameof(UninterceptedPublicCallsPreserveOmissionAndRejectExplicitNull),
            isCollectible: true);
        try
        {
            var run = context.LoadFromStream(image).GetType("AndroidX.Compose.PublicCallProbe")?.GetMethod("Run")
                ?? throw new InvalidOperationException("Un-intercepted public call probe missing.");
            run.Invoke(null, null);
        }
        finally { context.Unload(); }
    }

    static string Contract(string direction, string maxItems) => $$"""
        using AndroidX.Compose;
        using AndroidX.Compose.Runtime;
        using AndroidX.Compose.UI;
        using Kotlin.Jvm.Functions;
        [assembly: ComposeDefaults("Flow{{direction}}Default", "modifier", "arrangement1", "arrangement2",
            "alignment", "{{maxItems}}", "maxLines", "overflow", "!content")]
        namespace AndroidX.Compose
        {
            public sealed class Flow{{direction}}Overflow { }
            public static partial class ComposeBridges
            {
                public static int LastDefaults;
                public static Flow{{direction}}Overflow? LastOverflow;
                [ComposeFacade(Defaults = typeof(Flow{{direction}}Default), Scope = "{{direction}}")]
                public static partial void Flow{{direction}}(IModifier? modifier, IFunction3 content,
                    [FacadeDefault(int.MaxValue)] int {{maxItems}},
                    [FacadeDefault(int.MaxValue)] int maxLines,
                    [FacadeAdded(PreserveArgumentPresence = true)] Flow{{direction}}Overflow? overflow,
                    int defaults, IComposer composer, int _changed = 0);
                public static partial void Flow{{direction}}(IModifier? modifier, IFunction3 content,
                    int {{maxItems}}, int maxLines, Flow{{direction}}Overflow? overflow,
                    int defaults, IComposer composer, int _changed)
                {
                    LastDefaults = defaults;
                    LastOverflow = overflow;
                }
            }
        }
        """;
}
