using System;
using System.IO;
using System.Linq;
using System.Runtime.Loader;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace AndroidX.Compose.SourceGenerators.Tests;

public class FlowOverflowNativeLifetimeTests
{
    [Fact]
    public void CompanionResolutionDisposesTemporaryPeersWithoutDisposingTheReturnedPeer()
    {
        var sourceDirectory = new DirectoryInfo(AppContext.BaseDirectory).Parent?.Parent?.Parent?.Parent
            ?? throw new InvalidOperationException("Could not locate the source directory.");
        string source = File.ReadAllText(Path.Combine(sourceDirectory.FullName,
            "Microsoft.AndroidX.Compose", "FlowOverflowNative.cs"));
        var method = CSharpSyntaxTree.ParseText(source).GetRoot().DescendantNodes()
            .OfType<MethodDeclarationSyntax>().Single(node => node.Identifier.ValueText == "GetCompanion");
        string actual = """
            using System;
            using System.Diagnostics.CodeAnalysis;
            using Android.Runtime;
            namespace AndroidX.Compose
            {
                public static partial class LifetimeProbe
                {
            """ + method + "}}";
        var compilation = CSharpCompilation.Create("FlowCompanionLifetime",
            [CSharpSyntaxTree.ParseText(actual), CSharpSyntaxTree.ParseText(Probe)],
            Net.Sdk.References, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
                nullableContextOptions: NullableContextOptions.Enable));
        using var image = new MemoryStream();
        var result = compilation.Emit(image);
        Assert.True(result.Success, string.Join(Environment.NewLine, result.Diagnostics));
        image.Position = 0;
        var context = new AssemblyLoadContext(nameof(FlowOverflowNativeLifetimeTests), isCollectible: true);
        try
        {
            var run = context.LoadFromStream(image).GetType("AndroidX.Compose.LifetimeProbe")?.GetMethod("Run")
                ?? throw new InvalidOperationException("Companion lifetime probe missing.");
            run.Invoke(null, null);
        }
        finally { context.Unload(); }
    }

    const string Probe = """
        using System;
        namespace Java.Lang
        {
            public class Object : IDisposable
            {
                public int Disposals;
                public void Dispose() => Disposals++;
            }
            public sealed class Class : Object
            {
                public static Class? Last;
                public static Class ForName(string name, bool initialize, object loader) => Last = new();
                public Field GetField(string name) => Field.Last = new();
            }
            public sealed class Field : Object
            {
                public static Field? Last;
                public Object? Get(Object? receiver) => Android.Runtime.Extensions.Peer;
            }
        }
        namespace Android.App
        {
            public static class Application { public static Context Context { get; } = new(); }
            public sealed class Context { public object ClassLoader { get; } = new(); }
        }
        namespace Android.Runtime
        {
            public static class Extensions
            {
                public static Java.Lang.Object? Peer, Result;
                public static Exception? Failure;
                public static T? JavaCast<T>(this Java.Lang.Object peer) where T : Java.Lang.Object
                {
                    if (Failure is { } failure) throw failure;
                    return Result as T;
                }
            }
        }
        namespace AndroidX.Compose
        {
            public sealed class CompanionPeer : Java.Lang.Object { }
            public static partial class LifetimeProbe
            {
                public static void Run()
                {
                    int[] modes = [0, 1, 2, 3];
                    foreach (int mode in modes)
                    {
                        var peer = new CompanionPeer();
                        var other = new CompanionPeer();
                        var failure = new InvalidOperationException("Deliberate binding cast failure");
                        Android.Runtime.Extensions.Peer = peer;
                        Android.Runtime.Extensions.Result = mode == 0 ? peer : mode == 1 ? other : null;
                        Android.Runtime.Extensions.Failure = mode == 3 ? failure : null;
                        bool caught = false;
                        try
                        {
                            var result = GetCompanion<CompanionPeer>("test.FlowOverflow");
                            Require(mode < 2, "Missing cast result was accepted.");
                            Require(ReferenceEquals(result, mode == 0 ? peer : other), "Returned peer changed.");
                            Require(result.Disposals == 0, "Returned singleton peer was disposed.");
                        }
                        catch (InvalidOperationException error)
                        {
                            caught = true;
                            if (mode == 3) Require(ReferenceEquals(error, failure), "Cast failure was replaced.");
                            else Require(mode == 2 && error.Message.Contains("wrong bound type"), "Unexpected failure.");
                        }
                        Require(caught == (mode >= 2), "Wrong success/failure path.");
                        Require(peer.Disposals == (mode == 0 ? 0 : 1), "Temporary peer cleanup is incorrect.");
                        Require(other.Disposals == 0, "Different returned peer was disposed.");
                        Require(Java.Lang.Class.Last?.Disposals == 1, "Reflected class wrapper leaked.");
                        Require(Java.Lang.Field.Last?.Disposals == 1, "Reflected field wrapper leaked.");
                    }
                }
                static void Require(bool condition, string message)
                {
                    if (!condition) throw new Exception(message);
                }
            }
        }
        """;
}
