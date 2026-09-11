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

public class CollectionApiCompatibilityTests
{
    const string Usings = "using System; using System.Collections.Generic; using AndroidX.Compose;";
    const string Types = """
        namespace AndroidX.Compose {
            public class Modifier {}
            public class PaddingValues {}
            public class Arrangement {}
            public class LazyListState {}
            public class LazyGridState {}
            public class LazyStaggeredGridState {}
            public class PagerState {}
            public class GridCells {}
            public class StaggeredGridCells {}
        }
        """;

    [Theory]
    [InlineData("LazyColumn", "IReadOnlyList<T> items, Action<T> itemContent, Modifier? modifier = null, LazyListState? state = null, bool reverseLayout = false, PaddingValues? contentPadding = null, Arrangement? verticalArrangement = null")]
    [InlineData("LazyRow", "IReadOnlyList<T> items, Action<T> itemContent, Modifier? modifier = null, LazyListState? state = null, PaddingValues? contentPadding = null, Arrangement? horizontalArrangement = null")]
    [InlineData("LazyVerticalGrid", "GridCells columns, IReadOnlyList<T> items, Action<T> itemContent, Modifier? modifier = null, LazyGridState? state = null, PaddingValues? contentPadding = null, Arrangement? verticalArrangement = null, Arrangement? horizontalArrangement = null")]
    [InlineData("LazyHorizontalGrid", "GridCells rows, IReadOnlyList<T> items, Action<T> itemContent, Modifier? modifier = null, LazyGridState? state = null, PaddingValues? contentPadding = null")]
    [InlineData("LazyVerticalStaggeredGrid", "StaggeredGridCells columns, IReadOnlyList<T> items, Action<T> itemContent, Modifier? modifier = null, LazyStaggeredGridState? state = null, PaddingValues? contentPadding = null")]
    [InlineData("LazyHorizontalStaggeredGrid", "StaggeredGridCells rows, IReadOnlyList<T> items, Action<T> itemContent, Modifier? modifier = null, LazyStaggeredGridState? state = null, PaddingValues? contentPadding = null")]
    [InlineData("HorizontalPager", "IReadOnlyList<T> items, Action<T> itemContent, Modifier? modifier = null, PagerState? state = null, PaddingValues? contentPadding = null")]
    [InlineData("VerticalPager", "IReadOnlyList<T> items, Action<T> itemContent, Modifier? modifier = null, PagerState? state = null, PaddingValues? contentPadding = null")]
    public void OriginalCallsAndCompiledConsumersRemainCompatible(string name, string parameters)
    {
        string original = $"public static void {name}<T>({parameters}) {{ }}";
        var oldMethod = (MethodDeclarationSyntax)(SyntaxFactory.ParseMemberDeclaration(original)
            ?? throw new InvalidOperationException("Original method could not be parsed."));
        var args = oldMethod.ParameterList.Parameters.Select(p => p.Identifier.ValueText switch
        {
            "items" => "new int[] { 1 }",
            "itemContent" => "(Action<int>)(_ => {})",
            "reverseLayout" => "false",
            _ => "null",
        }).ToArray();
        int required = oldMethod.ParameterList.Parameters.Count(p => p.Default is null);
        string positional = string.Join(", ", args);
        string named = string.Join(", ", oldMethod.ParameterList.Parameters
            .Select((p, i) => $"{p.Identifier.ValueText}: {args[i]}").Reverse());
        string delegateTypes = string.Join(", ", oldMethod.ParameterList.Parameters
            .Select(p => p.Type?.ToString().Replace("<T>", "<int>", StringComparison.Ordinal)));
        string calls = $$"""
            public static class Consumer {
                public static void Run() {
                    Composables.{{name}}<int>({{positional}});
                    Composables.{{name}}<int>({{named}});
                    Composables.{{name}}<int>({{string.Join(", ", args.Take(required))}});
                    Composables.{{name}}<int>({{string.Join(", ", args.Take(required))}}, state: null);
                    Action<{{delegateTypes}}> method = Composables.{{name}}<int>;
                    method({{positional}});
                }
            }
            """;
        string contractName = $"CollectionContract_{name}";
        var oldContract = CompileContract(contractName, original);
        var oldReference = MetadataReference.CreateFromImage(oldContract.ToArray());
        using var consumer = Compile("OldConsumer", Usings + calls, oldReference);

        // Project the actual public declarations, removing only Android-dependent bodies/attributes.
        var currentMethods = ReadMethods(name);
        using var newContract = CompileContract(contractName, currentMethods);
        var currentReference = MetadataReference.CreateFromImage(newContract.ToArray());
        using var recompiled = Compile("RecompiledConsumer", Usings + calls, currentReference);
        using var keyed = Compile("KeyedConsumer", Usings + $$"""
            public static class KeyedConsumer {
                public static void Run() {
                    Composables.{{name}}<int>({{string.Join(", ", args.Take(required))}}, key: item => item);
                    Composables.{{name}}<int>({{positional}}, key: item => item);
                }
            }
            """, currentReference);

        var context = new AssemblyLoadContext(contractName, isCollectible: true);
        try
        {
            context.LoadFromStream(newContract);
            var assembly = context.LoadFromStream(consumer);
            var run = assembly.GetType("Consumer")?.GetMethod("Run", BindingFlags.Public | BindingFlags.Static)
                ?? throw new InvalidOperationException("Consumer entry point was not emitted.");
            run.Invoke(null, null);
        }
        finally
        {
            context.Unload();
            oldContract.Dispose();
        }
    }

    static string ReadMethods(string name, [CallerFilePath] string file = "")
    {
        string directory = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(file)
            ?? throw new InvalidOperationException("Test source directory unavailable."), "..", "Microsoft.AndroidX.Compose"));
        string[] files = ["Composables.Lazy.cs", "Composables.Pager.cs"];
        return string.Join("\n", files
            .SelectMany(fileName => CSharpSyntaxTree.ParseText(File.ReadAllText(Path.Combine(directory, fileName)))
                .GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>())
            .Where(m => m.Identifier.ValueText == name && m.Modifiers.Any(SyntaxKind.PublicKeyword))
            .Select(m => m.WithAttributeLists(default)
                .WithParameterList(m.ParameterList.WithParameters(SyntaxFactory.SeparatedList(
                    m.ParameterList.Parameters.Select(p => p.WithAttributeLists(default)))))
                .WithBody(SyntaxFactory.Block()).WithExpressionBody(null).WithSemicolonToken(default)
                .NormalizeWhitespace().ToFullString()));
    }

    static MemoryStream CompileContract(string name, string methods) =>
        Compile(name, Usings + Types + $"namespace AndroidX.Compose {{ public static class Composables {{ {methods} }} }}");

    static MemoryStream Compile(string name, string source, MetadataReference? contract = null)
    {
        var references = contract is null ? Net.Sdk.References : Net.Sdk.References.Add(contract);
        var compilation = CSharpCompilation.Create(name, [CSharpSyntaxTree.ParseText(source)], references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));
        var stream = new MemoryStream();
        var result = compilation.Emit(stream);
        Assert.True(result.Success, string.Join("\n", result.Diagnostics));
        stream.Position = 0;
        return stream;
    }
}
