using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace AndroidX.Compose.SourceGenerators.Tests;

public class ScaffoldApiCompatibilityTests
{
    const string Preamble = """
        #nullable enable
        using System;
        using AndroidX.Compose;
        using AndroidX.Compose.Runtime;
        namespace AndroidX.Compose.Runtime { public interface IComposer { } }
        namespace AndroidX.Compose {
            public class Composer : IComposer { }
            [AttributeUsage(AttributeTargets.Method)]
            public class ComposableAttribute : Attribute { }
            [AttributeUsage(AttributeTargets.Method)]
            public class GenerateImplicitComposableAttribute : Attribute { }
            [AttributeUsage(AttributeTargets.Parameter)]
            public class ComposableContentAttribute : Attribute { }
            public class Modifier { }
            public class PaddingValues { }
            public class WindowInsets { }
            public static class ComposableContext {
                public static IComposer Current { get; } = new Composer();
            }
            public static partial class Composables {
                public static WindowInsets? LastInsets { get; set; }
            }
        }
        """;

    const string Original = """
        [Composable, GenerateImplicitComposable]
        public static void Scaffold(
            IComposer composer,
            [ComposableContent] Action<PaddingValues, IComposer> content,
            Modifier? modifier = null,
            [ComposableContent] Action<IComposer>? topBar = null,
            [ComposableContent] Action<IComposer>? bottomBar = null,
            [ComposableContent] Action<IComposer>? snackbarHost = null,
            [ComposableContent] Action<IComposer>? floatingActionButton = null) { }
        """;

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void OriginalCallsMethodGroupsAndCompiledConsumersRemainCompatible(bool explicitComposer)
    {
        string prefix = explicitComposer ? "ComposableContext.Current, " : "";
        string callback = explicitComposer ? "Action<PaddingValues, IComposer>" : "Action<PaddingValues>";
        string slot = explicitComposer ? "Action<IComposer>" : "Action";
        string delegatePrefix = explicitComposer ? "IComposer, " : "";
        string setup = $"{callback} content = {(explicitComposer ? "(_, _) => { }" : "_ => { }")};";
        string arguments = $"{prefix}content, null, null, null, null, null";
        string calls = $$"""
            public static class Consumer {
                public static void Run() {
                    {{setup}}
                    Composables.Scaffold({{arguments}});
                    Composables.Scaffold({{prefix}}content);
                    Composables.Scaffold({{prefix}}content, null);
                    Composables.Scaffold({{prefix}}null, null);
                    Composables.Scaffold({{prefix}}content: content, floatingActionButton: null);
                    Composables.Scaffold({{prefix}}floatingActionButton: null, snackbarHost: null,
                        bottomBar: null, topBar: null, modifier: null, content: content);
                    Action<{{delegatePrefix}}{{callback}}, Modifier?, {{slot}}?, {{slot}}?, {{slot}}?, {{slot}}?> method = Composables.Scaffold;
                    method({{arguments}});
                    if (Composables.LastInsets != null) throw new Exception("Legacy call supplied insets.");
                }
            }
            """;
        string contractName = $"ScaffoldContract_{explicitComposer}";
        using var original = CompileContract(contractName, explicitComposer
            ? Original
            : Original.Replace("public static void", "internal static void", StringComparison.Ordinal));
        using var consumer = Compile("OldConsumer", "using System; using AndroidX.Compose; using AndroidX.Compose.Runtime;" + calls,
            MetadataReference.CreateFromImage(original.ToArray()));
        using var current = CompileContract(contractName, ReadCurrentMethods(explicitComposer));
        var currentReference = MetadataReference.CreateFromImage(current.ToArray());
        using var recompiled = Compile("RecompiledConsumer",
            "using System; using AndroidX.Compose; using AndroidX.Compose.Runtime;" + calls, currentReference);
        using var custom = Compile("CustomConsumer", $$"""
            using System; using AndroidX.Compose; using AndroidX.Compose.Runtime;
            public static class CustomConsumer {
                public static void Run() {
                    {{setup}}
                    var insets = new WindowInsets();
                    Composables.Scaffold({{prefix}}content, contentWindowInsets: insets);
                    if (!ReferenceEquals(insets, Composables.LastInsets)) throw new Exception("Insets not forwarded.");
                    Composables.Scaffold({{arguments}}, insets);
                    if (!ReferenceEquals(insets, Composables.LastInsets)) throw new Exception("Positional insets not forwarded.");
                    Composables.Scaffold({{prefix}}content, contentWindowInsets: null);
                    if (Composables.LastInsets != null) throw new Exception("Null not forwarded.");
                }
            }
            """, currentReference);
        var context = new AssemblyLoadContext(contractName, isCollectible: true);
        try
        {
            context.LoadFromStream(current);
            Invoke(context.LoadFromStream(consumer), "Consumer");
            Invoke(context.LoadFromStream(custom), "CustomConsumer");
        }
        finally
        {
            context.Unload();
        }
    }

    static void Invoke(Assembly assembly, string type)
    {
        var run = assembly.GetType(type)?.GetMethod("Run")
            ?? throw new InvalidOperationException("Consumer entry point missing.");
        run.Invoke(null, null);
    }

    static string ReadCurrentMethods(bool explicitComposer, [CallerFilePath] string file = "")
    {
        string source = Path.Combine(Path.GetDirectoryName(file)
            ?? throw new InvalidOperationException("Test directory missing."),
            "..", "Microsoft.AndroidX.Compose", "Composables.Handwritten.cs");
        return string.Join("\n", CSharpSyntaxTree.ParseText(File.ReadAllText(source))
            .GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>()
            .Where(m => m.Identifier.ValueText == "Scaffold")
            .Select(m =>
            {
                // Publish the internal explicit adapter in the synthetic contract too,
                // so its original method-group shape can be checked without Android.
                m = m.WithModifiers(SyntaxFactory.TokenList(
                    SyntaxFactory.Token(explicitComposer ? SyntaxKind.PublicKeyword : SyntaxKind.InternalKeyword),
                    SyntaxFactory.Token(SyntaxKind.StaticKeyword)));
                return (m.Body is null ? m : m.WithBody(SyntaxFactory.Block(
                    SyntaxFactory.ParseStatement("LastInsets = contentWindowInsets;"))))
                    .NormalizeWhitespace().ToFullString();
            }));
    }

    static MemoryStream CompileContract(string name, string methods)
    {
        var options = new CSharpParseOptions(LanguageVersion.Preview);
        var compilation = CSharpCompilation.Create(name,
            [CSharpSyntaxTree.ParseText(Preamble + $"namespace AndroidX.Compose {{ public static partial class Composables {{ {methods} }} }}", options)],
            Net.Sdk.References, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        CSharpGeneratorDriver.Create([new ImplicitComposableOverloadGenerator().AsSourceGenerator()], parseOptions: options)
            .RunGeneratorsAndUpdateCompilation(compilation, out var output, out var diagnostics);
        Assert.Empty(diagnostics);
        return Emit(output);
    }

    static MemoryStream Compile(string name, string source, MetadataReference contract) =>
        Emit(CSharpCompilation.Create(name, [CSharpSyntaxTree.ParseText(source)],
            Net.Sdk.References.Add(contract), new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)));

    static MemoryStream Emit(Compilation compilation)
    {
        var stream = new MemoryStream();
        var result = compilation.Emit(stream);
        Assert.True(result.Success, string.Join("\n", result.Diagnostics));
        stream.Position = 0;
        return stream;
    }
}
