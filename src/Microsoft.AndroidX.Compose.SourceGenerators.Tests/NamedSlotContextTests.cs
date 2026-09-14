using System;
using System.IO;
using System.Linq;
using System.Runtime.Loader;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace AndroidX.Compose.SourceGenerators.Tests;

public class NamedSlotContextTests
{
    [Theory]
    [InlineData(2, false)]
    [InlineData(2, true)]
    [InlineData(3, false)]
    [InlineData(3, true)]
    public void DirectNamedSlotsEnterTheirInvocationComposer(int arity, bool optional)
    {
        var (output, diagnostics, emitted) = FacadeGeneratorTests.Run(ProbeSource(arity, optional), "NamedProbe");
        Assert.Empty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
        Assert.Empty(output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error));
        Assert.NotNull(emitted);
        string wrapper = $"global::AndroidX.Compose.ComposableLambdas.Wrap{arity}(__composer, " +
            "c => global::AndroidX.Compose.ComposableContentNode.RenderDirect(c, label, false))";
        Assert.Equal(2, Regex.Matches(emitted, Regex.Escape(wrapper)).Count);
        Assert.DoesNotContain("_ => label()", emitted);
        Assert.DoesNotContain($"Wrap{arity}(__composer, label)", emitted);
    }

    [Theory]
    [InlineData(2, false, false)]
    [InlineData(2, false, true)]
    [InlineData(2, true, false)]
    [InlineData(2, true, true)]
    [InlineData(3, false, false)]
    [InlineData(3, false, true)]
    [InlineData(3, true, false)]
    [InlineData(3, true, true)]
    public void LaterNamedSlotInvocationRestoresAmbientContext(int arity, bool optional, bool implicitComposer)
    {
        var (output, diagnostics, _) = FacadeGeneratorTests.Run(ProbeSource(arity, optional), "NamedProbe");
        Assert.Empty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
        var stubs = output.SyntaxTrees.First();
        var root = stubs.GetRoot();
        var replacedTypes = root.DescendantNodes().OfType<ClassDeclarationSyntax>()
            .Where(c => c.Identifier.ValueText is "ComposableContext" or "ComposableContentNode" or "ComposableLambdas");
        root = root.RemoveNodes(replacedTypes, SyntaxRemoveOptions.KeepNoTrivia)
            ?? throw new InvalidOperationException("Could not remove runtime stand-ins.");
        var rowFrame = root.DescendantNodes().OfType<StructDeclarationSyntax>()
            .Single(s => s.Identifier.ValueText == "RowFrame");
        root = root.ReplaceNode(rowFrame, rowFrame.WithModifiers(
            SyntaxFactory.TokenList(rowFrame.Modifiers.Where(t => !t.IsKind(SyntaxKind.RefKeyword))))
            .AddBaseListTypes(SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName("global::System.IDisposable"))));
        output = output.ReplaceSyntaxTree(stubs, root.SyntaxTree);

        var sourceDirectory = new DirectoryInfo(AppContext.BaseDirectory).Parent?.Parent?.Parent?.Parent
            ?? throw new InvalidOperationException("Could not locate the source directory from the test output.");
        string runtime = Path.Combine(sourceDirectory.FullName, "Microsoft.AndroidX.Compose");
        string[] runtimeFiles = ["ComposableContext.cs", "ComposableContextScope.cs", "ComposableContentNode.cs"];
        foreach (string file in runtimeFiles)
            output = output.AddSyntaxTrees(CSharpSyntaxTree.ParseText(File.ReadAllText(Path.Combine(runtime, file))));
        output = output.AddSyntaxTrees(CSharpSyntaxTree.ParseText(InvocationProbe));
        using var stream = new MemoryStream();
        var result = output.Emit(stream);
        Assert.True(result.Success, string.Join(Environment.NewLine, result.Diagnostics));
        stream.Position = 0;
        var context = new AssemblyLoadContext(nameof(LaterNamedSlotInvocationRestoresAmbientContext), isCollectible: true);
        try
        {
            var assembly = context.LoadFromStream(stream);
            var probe = assembly.GetType("AndroidX.Compose.InvocationProbe", throwOnError: true)
                ?? throw new InvalidOperationException("InvocationProbe was not emitted.");
            var run = probe.GetMethod("Run")
                ?? throw new InvalidOperationException("InvocationProbe.Run was not emitted.");
            run.Invoke(null, [implicitComposer]);
        }
        finally
        {
            context.Unload();
        }
    }

    static string ProbeSource(int arity, bool optional) => $$"""
            using AndroidX.Compose;
            using AndroidX.Compose.Runtime;
            using Kotlin.Jvm.Functions;
            [assembly: ComposeDefaults("NamedProbeDefault", "{{(optional ? "" : "!")}}label")]
            namespace AndroidX.Compose
            {
                public static partial class ComposeBridges
                {
                    [ComposeBridge(Class = "test/NamedProbe", JvmName = "NamedProbe",
                        Signature = "(Lkotlin/jvm/functions/Function{{arity}};Landroidx/compose/runtime/Composer;II)V",
                        Defaults = typeof(NamedProbeDefault))]
                    [ComposeFacade]
                    public static partial void NamedProbe([Slot("Label")] IFunction{{arity}}{{(optional ? "?" : "")}} label, IComposer composer);
                }
            }
            """;

    const string InvocationProbe = """
        global using System;
        global using AndroidX.Compose.Runtime;
        using System.Linq;
        namespace AndroidX.Compose
        {
            public sealed class ProbeComposer : Java.Lang.Object, IComposer
            {
                public int GroupDepth;
            }
            internal static class CompositionGroupKey
            {
                public static int Compute(int position, Type type) => 1;
            }
            public static class ProbeGroups
            {
                public static void StartReplaceableGroup(this IComposer composer, int key) =>
                    ((ProbeComposer)composer).GroupDepth++;
                public static void EndReplaceableGroup(this IComposer composer) =>
                    ((ProbeComposer)composer).GroupDepth--;
            }
            public sealed class CapturedSlot(Action<IComposer> body) : Java.Lang.Object,
                Kotlin.Jvm.Functions.IFunction2, Kotlin.Jvm.Functions.IFunction3
            {
                public void Invoke(IComposer composer) => body(composer);
            }
            public static class ComposableLambdas
            {
                public static CapturedSlot? Last;
                public static Kotlin.Jvm.Functions.IFunction2 Wrap2(IComposer composer, Action<IComposer> body) =>
                    Last = new CapturedSlot(body);
                public static Kotlin.Jvm.Functions.IFunction3 Wrap3(IComposer composer, Action<IComposer> body) =>
                    Last = new CapturedSlot(body);
            }
            public static class InvocationProbe
            {
                public static void Run(bool implicitComposer)
                {
                    var outer = new ProbeComposer();
                    var invocation = new ProbeComposer();
                    var prior = new ProbeComposer();
                    var failure = new InvalidOperationException("Deliberate slot failure");
                    bool throwFromSlot = false;
                    int calls = 0;
                    void Callback()
                    {
                        Require(ReferenceEquals(ComposableContext.Current, invocation), "Wrong callback-time composer.");
                        Require(invocation.GroupDepth == 1, "Missing content group.");
                        calls++;
                        if (throwFromSlot) throw failure;
                    }
                    void ExplicitCallback(IComposer composer)
                    {
                        Require(ReferenceEquals(composer, invocation), "Captured outer composer was forwarded.");
                        Callback();
                    }
                    var method = typeof(Composables).GetMethods(System.Reflection.BindingFlags.Public |
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
                        .Single(m => m.Name == "NamedProbe" &&
                            m.GetParameters().Length == (implicitComposer ? 1 : 2));
                    using (ComposableContext.Enter(outer))
                    {
                        object?[] args = implicitComposer
                            ? [(Action)Callback]
                            : [outer, (Action<IComposer>)ExplicitCallback];
                        method.Invoke(null, args);
                        Require(ReferenceEquals(ComposableContext.Current, outer), "Parent context changed.");
                    }
                    var slot = ComposableLambdas.Last ?? throw new InvalidOperationException("No slot was captured.");
                    RequireNoAmbient();
                    slot.Invoke(invocation);
                    RequireNoAmbient();
                    Require(invocation.GroupDepth == 0, "Content group leaked.");
                    using (ComposableContext.Enter(prior))
                    {
                        slot.Invoke(invocation);
                        Require(ReferenceEquals(ComposableContext.Current, prior), "Prior context was not restored.");
                        throwFromSlot = true;
                        bool caught = false;
                        try { slot.Invoke(invocation); }
                        catch (InvalidOperationException ex) when (ReferenceEquals(ex, failure)) { caught = true; }
                        Require(caught, "Slot exception was swallowed.");
                        Require(ReferenceEquals(ComposableContext.Current, prior), "Prior context lost on throw.");
                        Require(invocation.GroupDepth == 0, "Content group leaked on throw.");
                    }
                    RequireNoAmbient();
                    Require(calls == 3, "Not all callbacks executed.");
                    Require(outer.GroupDepth == 0 && prior.GroupDepth == 0, "Wrong composer received a group.");
                }
                static void RequireNoAmbient()
                {
                    try { _ = ComposableContext.Current; }
                    catch (InvalidOperationException ex) when (ex.Message.StartsWith("No composer is active.")) { return; }
                    throw new InvalidOperationException("Ambient composer leaked after scope exit.");
                }
                static void Require(bool condition, string message)
                {
                    if (!condition) throw new InvalidOperationException(message);
                }
            }
        }
        """;
}
