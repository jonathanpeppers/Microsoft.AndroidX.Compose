using System;
using System.IO;
using System.Linq;
using System.Runtime.Loader;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace AndroidX.Compose.SourceGenerators.Tests;

public class FlowOverflowScopeTests
{
    [Fact]
    public void ActualRuntimeAdapterDefersReadsAndRestoresNestedScopesOnThrow()
    {
        var sourceDirectory = new DirectoryInfo(AppContext.BaseDirectory).Parent?.Parent?.Parent?.Parent
            ?? throw new InvalidOperationException("Could not locate the source directory.");
        string runtime = Path.Combine(sourceDirectory.FullName, "Microsoft.AndroidX.Compose");
        string[] files =
        [
            "FlowOverflowScope.cs", "FlowOverflowContent.cs", "RenderContext.cs",
            "ScopeKind.cs", "ComposableContext.cs", "ComposableContextScope.cs",
        ];
        var trees = files.Select(file => CSharpSyntaxTree.ParseText(File.ReadAllText(Path.Combine(runtime, file))))
            .Append(CSharpSyntaxTree.ParseText(Probe));
        var compilation = CSharpCompilation.Create("FlowScopeContract", trees, Net.Sdk.References,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
                nullableContextOptions: NullableContextOptions.Enable));
        using var image = new MemoryStream();
        var result = compilation.Emit(image);
        Assert.True(result.Success, string.Join(Environment.NewLine, result.Diagnostics));
        image.Position = 0;
        var context = new AssemblyLoadContext(nameof(FlowOverflowScopeTests), isCollectible: true);
        try
        {
            var run = context.LoadFromStream(image).GetType("AndroidX.Compose.Probe")?.GetMethod("Run")
                ?? throw new InvalidOperationException("Flow scope probe missing.");
            run.Invoke(null, null);
        }
        finally { context.Unload(); }
    }

    const string Probe = """
        global using System;
        using System.Collections.Generic;
        using AndroidX.Compose.Runtime;
        namespace Android.Runtime { public enum JniHandleOwnership { DoNotTransfer } }
        namespace AndroidX.Compose.Runtime { public interface IComposer { } }
        namespace AndroidX.Compose.Animation { public interface IAnimatedVisibilityScope { } }
        namespace Kotlin.Jvm.Functions
        {
            public interface IFunction3 { void Invoke(IntPtr handle, IComposer composer); }
        }
        namespace Java.Lang
        {
            public class Object
            {
                public static Dictionary<IntPtr, object> Peers = [];
                public static T? GetObject<T>(IntPtr handle, Android.Runtime.JniHandleOwnership ownership) where T : class =>
                    Peers[handle] as T;
            }
        }
        namespace AndroidX.Compose.Foundation.Layout
        {
            public interface IFlowRowOverflowScope { int TotalItemCount { get; } int ShownItemCount { get; } }
            public interface IFlowColumnOverflowScope { int TotalItemCount { get; } int ShownItemCount { get; } }
        }
        namespace AndroidX.Compose
        {
            public abstract class ComposableNode { public abstract void Render(IComposer composer); }
            public static class ComposableLambdas
            {
                public static Kotlin.Jvm.Functions.IFunction3 Wrap3(IComposer composer,
                    Action<IntPtr, IComposer> body, int line, string file) => new Callback(body);
            }
            public sealed class Callback(Action<IntPtr, IComposer> body) : Kotlin.Jvm.Functions.IFunction3
            {
                public void Invoke(IntPtr handle, IComposer composer) => body(handle, composer);
            }
            public sealed class Composer : IComposer { }
            public sealed class Peer : Foundation.Layout.IFlowRowOverflowScope, Foundation.Layout.IFlowColumnOverflowScope
            {
                public int Total, Shown, Reads;
                public Exception? Failure;
                public int TotalItemCount { get { Reads++; return Total; } }
                public int ShownItemCount { get { Reads++; if (Failure is { } error) throw error; return Shown; } }
            }
            public static class Probe
            {
                public static void Run()
                {
                    var row = new Peer { Total = 8, Shown = 2 };
                    var column = new Peer { Total = 4, Shown = 1 };
                    Java.Lang.Object.Peers[1] = row;
                    Java.Lang.Object.Peers[2] = column;
                    var construction = new Composer();
                    var outerComposer = new Composer();
                    var innerComposer = new Composer();
                    var priorComposer = new Composer();
                    var failure = new InvalidOperationException("Deliberate nested indicator failure");
                    FlowOverflowScope? outerCounts = null, innerCounts = null;
                    var inner = FlowOverflowContent.FromAction(scope =>
                    {
                        Require(RenderContext.CurrentScope == 2 && RenderContext.CurrentScopeKind == ScopeKind.Column,
                            "Nested column scope was not entered.");
                        Require(ReferenceEquals(ComposableContext.Current, innerComposer), "Wrong nested invocation composer.");
                        innerCounts = scope;
                        throw failure;
                    }).Wrap(construction, false);
                    var outer = FlowOverflowContent.FromAction(scope =>
                    {
                        Require(RenderContext.CurrentScope == 1 && RenderContext.CurrentScopeKind == ScopeKind.Row,
                            "Outer row scope was not entered.");
                        Require(ReferenceEquals(ComposableContext.Current, outerComposer), "Captured stale construction composer.");
                        outerCounts = scope;
                        bool caught = false;
                        try { inner.Invoke(2, innerComposer); }
                        catch (InvalidOperationException error) when (ReferenceEquals(error, failure)) { caught = true; }
                        Require(caught, "Nested exception was swallowed.");
                        Require(RenderContext.CurrentScope == 1 && RenderContext.CurrentScopeKind == ScopeKind.Row,
                            "Nested callback did not restore row scope.");
                        Require(ReferenceEquals(ComposableContext.Current, outerComposer), "Nested callback did not restore composer.");
                    }).Wrap(construction, true);
                    using (RenderContext.PushScope(99, ScopeKind.Box))
                    using (ComposableContext.Enter(priorComposer))
                    {
                        outer.Invoke(1, outerComposer);
                        Require(RenderContext.CurrentScope == 99 && RenderContext.CurrentScopeKind == ScopeKind.Box,
                            "Outer callback leaked its row scope.");
                        Require(ReferenceEquals(ComposableContext.Current, priorComposer), "Outer callback leaked its composer.");
                    }
                    Require(row.Reads == 0 && column.Reads == 0, "Adapter eagerly read pre-measurement counts.");
                    var first = outerCounts ?? throw new Exception("Outer count accessor missing.");
                    var second = innerCounts ?? throw new Exception("Inner count accessor missing.");
                    Require(first.TotalItemCount == 8 && first.ShownItemCount == 2, "Wrong row counts.");
                    Require(second.TotalItemCount == 4 && second.ShownItemCount == 1, "Wrong nested counts.");
                    row.Total = 5;
                    row.Shown = 5;
                    Require(first.TotalItemCount == 5 && first.ShownItemCount == 5, "Managed adapter cached native values.");
                    row.Failure = failure;
                    bool forwarded = false;
                    try { _ = first.ShownItemCount; }
                    catch (InvalidOperationException error) when (ReferenceEquals(error, failure)) { forwarded = true; }
                    Require(forwarded, "Count getter hid or replaced the native error.");
                    Require(RenderContext.CurrentScopeKind == ScopeKind.None, "Scope leaked after nesting.");
                }
                static void Require(bool value, string message)
                {
                    if (!value) throw new Exception(message);
                }
            }
        }
        """;
}
