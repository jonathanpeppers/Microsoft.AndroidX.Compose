using System;
using System.IO;
using System.Linq;
using System.Runtime.Loader;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace AndroidX.Compose.SourceGenerators.Tests;

public class FabFrameCommitTests
{
    [Theory]
    [InlineData(true, true, false, false)]
    [InlineData(true, false, false, false)]
    [InlineData(false, false, false, false)]
    [InlineData(true, false, true, false)]
    [InlineData(true, false, false, true)]
    [InlineData(true, false, true, true)]
    public async Task NativeCallbackOwnershipSurvivesManagedCancellation(
        bool observerAlive, bool unregisters, bool completesFirst, bool callbackThrows)
    {
        var sourceDirectory = new DirectoryInfo(AppContext.BaseDirectory).Parent?.Parent?.Parent?.Parent
            ?? throw new InvalidOperationException("Could not locate the source directory from test output.");
        string deviceTests = Path.Combine(sourceDirectory.FullName, "Microsoft.AndroidX.Compose.DeviceTests");
        var activity = CSharpSyntaxTree.ParseText(File.ReadAllText(Path.Combine(deviceTests, "FabStylingTestActivity.cs")));
        var methods = activity.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>()
            .Where(m => m.Identifier.ValueText is "NewCompletion" or "RetireAfterNativeCompletion");
        var support = CSharpSyntaxTree.ParseText(
            "namespace Microsoft.AndroidX.Compose.DeviceTests { internal static class FabStylingTestActivity { " +
            string.Join(Environment.NewLine, methods.Select(m => m.ToFullString())) + " } }");
        var compilation = CSharpCompilation.Create(
            "FrameOwnership",
            [
                CSharpSyntaxTree.ParseText(File.ReadAllText(Path.Combine(deviceTests, "FabFrameCommit.cs"))),
                support,
                CSharpSyntaxTree.ParseText(Stubs),
            ],
            Net.Sdk.References,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
                nullableContextOptions: NullableContextOptions.Enable));
        using var stream = new MemoryStream();
        var emitted = compilation.Emit(stream);
        Assert.True(emitted.Success, string.Join(Environment.NewLine, emitted.Diagnostics));
        stream.Position = 0;
        var context = new AssemblyLoadContext(nameof(FabFrameCommitTests), isCollectible: true);
        try
        {
            var assembly = context.LoadFromStream(stream);
            var probe = assembly.GetType("Microsoft.AndroidX.Compose.DeviceTests.Probe", throwOnError: true)
                ?? throw new InvalidOperationException("Frame ownership probe missing.");
            var run = probe.GetMethod("Run")
                ?? throw new InvalidOperationException("Frame ownership probe entry missing.");
            var task = run.Invoke(null, [observerAlive, unregisters, completesFirst, callbackThrows]) as Task
                ?? throw new InvalidOperationException("Frame ownership probe did not return a task.");
            await task.WaitAsync(TimeSpan.FromSeconds(5));
        }
        finally
        {
            context.Unload();
        }
    }

    const string Stubs = """
        global using System;
        global using System.Threading;
        global using System.Threading.Tasks;
        namespace Java.Lang
        {
            public sealed class Runnable(Action body) : IDisposable
            {
                public int DisposeCount;
                public readonly TaskCompletionSource Disposed = new(TaskCreationOptions.RunContinuationsAsynchronously);
                public void Run()
                {
                    if (DisposeCount != 0) throw new InvalidOperationException("Native callback was disposed early.");
                    body();
                }
                public void Dispose()
                {
                    Interlocked.Increment(ref DisposeCount);
                    Disposed.TrySetResult();
                }
            }
        }
        namespace Android.Views
        {
            public sealed class ViewTreeObserver
            {
                public bool IsAlive;
                public bool Unregisters;
                public Java.Lang.Runnable? Callback;
                public void RegisterFrameCommitCallback(Java.Lang.Runnable callback) => Callback = callback;
                public bool UnregisterFrameCommitCallback(Java.Lang.Runnable callback) => Unregisters;
            }
        }
        namespace Android.Util
        {
            public static class Log
            {
                public static void Warn(string tag, string message) { }
                public static void Error(string tag, string message) => throw new InvalidOperationException(message);
            }
        }
        namespace Microsoft.AndroidX.Compose.DeviceTests
        {
            internal static class OperatingSystem
            {
                public static bool IsAndroidVersionAtLeast(int version) => true;
            }
            public static class Probe
            {
                public static async Task Run(bool alive, bool unregisters, bool completesFirst, bool callbackThrows)
                {
                    var observer = new Android.Views.ViewTreeObserver { IsAlive = alive, Unregisters = unregisters };
                    int calls = 0;
                    var failure = new InvalidOperationException("Deliberate callback failure");
                    var frame = new FabFrameCommit(observer, () =>
                    {
                        calls++;
                        if (callbackThrows) throw failure;
                    });
                    var callback = observer.Callback ?? throw new InvalidOperationException("Native callback missing.");
                    if (completesFirst) callback.Run();
                    else
                    {
                        using var cancellation = new CancellationTokenSource();
                        cancellation.Cancel();
                        try { await frame.Completion.WaitAsync(cancellation.Token); }
                        catch (OperationCanceledException) { }
                        Require(!frame.Completion.IsCompleted, "Managed cancellation completed native work.");
                    }
                    frame.Dispose();
                    if (completesFirst || alive && unregisters)
                    {
                        Require(callback.DisposeCount == 1, "Safe native release was not immediate.");
                        if (!completesFirst)
                        {
                            Require(frame.Completion.IsCanceled, "Unregistered callback waiter was not canceled.");
                            Require(calls == 0, "Canceled native callback ran.");
                        }
                    }
                    else
                    {
                        Require(callback.DisposeCount == 0, "In-flight native callback was disposed.");
                        callback.Run();
                        await callback.Disposed.Task;
                    }
                    if (completesFirst || !(alive && unregisters))
                    {
                        bool caught = false;
                        try { await frame.Completion; }
                        catch (InvalidOperationException error) when (ReferenceEquals(error, failure)) { caught = true; }
                        Require(caught == callbackThrows, "Callback failure was not propagated.");
                        Require(calls == 1, "Native callback count changed.");
                    }
                    frame.Dispose();
                    Require(callback.DisposeCount == 1, "Callback was disposed more than once.");
                }
                static void Require(bool condition, string message)
                {
                    if (!condition) throw new InvalidOperationException(message);
                }
            }
        }
        """;
}
