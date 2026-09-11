using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace AndroidX.Compose.SourceGenerators.Tests;

public class CompositionKeyTests
{
    [Fact]
    public async Task RuntimeKeyExpressions_AreDeterministicAcrossProcesses()
    {
        string directory = BuildProbe();
        try
        {
            var first = await RunProbe(directory);
            var second = await RunProbe(directory, reverse: true);
            Assert.NotEqual(first.ProcessId, second.ProcessId);
            Assert.Equal(first.Keys.Count, second.Keys.Count);
            foreach (var (site, keys) in first.Keys)
                Assert.True(keys.SequenceEqual(second.Keys[site]),
                    $"Composition keys changed in a new process at {site}: " +
                    $"{string.Join(", ", keys)} != {string.Join(", ", second.Keys[site])}");
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task RuntimeKeyExpressions_PreservePositionAndTypeDiscrimination()
    {
        string directory = BuildProbe();
        try
        {
            var (_, keys) = await RunProbe(directory);
            string[] childSites = ["ComposableContainer.cs:0", "ComposableContainer.cs:1", "SegmentedButton.cs:0"];
            foreach (string site in childSites)
            {
                var values = keys[site];
                Assert.Equal(values[0], values[1]);
                Assert.Equal(values.Length - 1, values.Distinct().Count());
                Assert.Equal(keys["ComposableContainer.cs:0"], values);
            }
            Assert.Equal(keys["ComposableContentNode.cs:0"], keys["ComposableContentNode.cs:1"]);
            Assert.Equal(keys["ContentNodeAsChild"], keys["ComposableContentNode.cs:0"]);
            Assert.Equal(keys["SourceLocation"], keys["Navigation"]);
            Assert.Equal(-524125597, keys["SourceLocation"][0]);
            Assert.Equal(-1450593381, keys["ComposableContainer.cs:0"][6]);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    // Compile the actual arguments passed to Compose, not a copy of the hash algorithm.
    // Android integration is covered by SaveableProcessTestActivity.
    static string BuildProbe([CallerFilePath] string sourceFile = "")
    {
        string runtime = Path.Combine(
            Path.GetDirectoryName(Path.GetDirectoryName(sourceFile))
                ?? throw new InvalidOperationException("Cannot locate runtime sources beside the test project."),
            "Microsoft.AndroidX.Compose");
        var expressions = new Dictionary<string, string>();
        (string File, int Count)[] files =
        [
            ("ComposableContainer.cs", 2),
            ("ComposableContentNode.cs", 2),
            ("SegmentedButton.cs", 1),
        ];
        foreach (var (file, count) in files)
        {
            var root = CSharpSyntaxTree.ParseText(File.ReadAllText(Path.Combine(runtime, file))).GetRoot();
            var groups = root.DescendantNodes().OfType<InvocationExpressionSyntax>()
                .Where(call => call.Expression is MemberAccessExpressionSyntax member &&
                    member.Name.Identifier.ValueText == "StartReplaceableGroup").ToArray();
            Assert.Equal(count, groups.Length);
            for (int index = 0; index < groups.Length; index++)
                expressions.Add($"{file}:{index}",
                    groups[index].ArgumentList.Arguments[0].Expression.ToString()
                        .Replace("child.GetType()", "childType", StringComparison.Ordinal));
        }
        var lambdas = CSharpSyntaxTree.ParseText(
            File.ReadAllText(Path.Combine(runtime, "ComposableLambdas.cs"))).GetRoot();
        var navigation = lambdas.DescendantNodes().OfType<MethodDeclarationSyntax>()
            .Single(method => method.Identifier.ValueText == "InstantiateNavComposable");
        expressions.Add("Navigation", navigation.DescendantNodes().OfType<ArgumentSyntax>()
            .Single(argument => argument.NameColon?.Name.Identifier.ValueText == "key").Expression.ToString());
        expressions.Add("ContentNodeAsChild", expressions["ComposableContainer.cs:0"]);
        expressions.Add("SourceLocation", "SourceLocationKey.Compute(line, file)");

        string methods = string.Join(Environment.NewLine, expressions.Select((pair, index) =>
            $"static int Key{index}(int i, Type childType, int line, string file) => {pair.Value};"));
        string entries = string.Join(Environment.NewLine, expressions.Select((pair, index) =>
            pair.Key.StartsWith("ComposableContentNode", StringComparison.Ordinal) || pair.Key == "ContentNodeAsChild"
                ? $"keys.Add(\"{pair.Key}\", [Key{index}(0, typeof(ComposableContentNode), 42, \"Screen.cs\")]);"
                : $"keys.Add(\"{pair.Key}\", cases.Select(c => Key{index}(c.Index, c.Type, c.Index + 42, \"Screen.cs\")).ToArray());"));
        string source = $$"""
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Reflection;
            using System.Reflection.Emit;
            using System.Text.Json;
            using AndroidX.Compose;

            var firstAssembly = DefineType("FirstAssembly", "1.0.0.0");
            var secondAssembly = DefineType("SecondAssembly", "1.0.0.0");
            var secondVersion = DefineType("FirstAssembly", "2.0.0.0");
            (int Index, Type Type)[] cases =
            [
                (0, typeof(string)), (0, typeof(string)), (1, typeof(string)),
                (0, typeof(int)), (0, typeof(List<string>)), (0, typeof(List<int>)),
                (0, firstAssembly), (0, secondAssembly), (0, secondVersion),
                (0, typeof(List<>).MakeGenericType(firstAssembly)),
                (0, typeof(List<>).MakeGenericType(secondAssembly)),
                (0, typeof(Dictionary<string, List<int>>)),
                (0, typeof(Dictionary<int, List<string>>)),
                (0, typeof(int[])), (0, typeof(int[,])),
            ];
            bool reverse = args.Contains("--reverse");
            if (reverse)
            {
                System.Globalization.CultureInfo.CurrentCulture = new("tr-TR");
                Array.Reverse(cases);
            }
            var keys = new Dictionary<string, int[]>();
            {{entries}}
            if (reverse)
                foreach (var values in keys.Values)
                    Array.Reverse(values);
            Console.WriteLine(Environment.ProcessId);
            Console.WriteLine(JsonSerializer.Serialize(keys));
            {{methods}}
            static Type DefineType(string assemblyName, string version) =>
                AssemblyBuilder.DefineDynamicAssembly(
                    new AssemblyName(assemblyName) { Version = Version.Parse(version) },
                    AssemblyBuilderAccess.Run)
                .DefineDynamicModule("Module").DefineType("Same.Namespace.Node").CreateType()
                ?? throw new InvalidOperationException("Probe type was not created.");

            namespace AndroidX.Compose { internal sealed class ComposableContentNode; }
            """;
        List<SyntaxTree> trees =
        [
            CSharpSyntaxTree.ParseText("global using System;"),
            CSharpSyntaxTree.ParseText(source),
            CSharpSyntaxTree.ParseText(File.ReadAllText(Path.Combine(runtime, "SourceLocationKey.cs"))),
        ];
        string groupKey = Path.Combine(runtime, "CompositionGroupKey.cs");
        if (File.Exists(groupKey))
            trees.Add(CSharpSyntaxTree.ParseText(File.ReadAllText(groupKey)));
        var compilation = CSharpCompilation.Create("CompositionKeyProbe", trees,
            Net.Sdk.References, new CSharpCompilationOptions(OutputKind.ConsoleApplication,
                nullableContextOptions: NullableContextOptions.Enable, checkOverflow: true));
        using var output = new MemoryStream();
        var result = compilation.Emit(output);
        Assert.True(result.Success, string.Join(Environment.NewLine, result.Diagnostics));
        string directory = Path.Combine(Path.GetTempPath(), "CompositionKeyProbe-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        File.WriteAllBytes(Path.Combine(directory, "CompositionKeyProbe.dll"), output.ToArray());
        File.WriteAllText(Path.Combine(directory, "CompositionKeyProbe.runtimeconfig.json"),
            JsonSerializer.Serialize(new
            {
                runtimeOptions = new
                {
                    tfm = "net10.0",
                    framework = new { name = "Microsoft.NETCore.App", version = Environment.Version.ToString() },
                },
            }));
        return directory;
    }

    static async Task<(int ProcessId, Dictionary<string, int[]> Keys)> RunProbe(string directory, bool reverse = false)
    {
        var start = new ProcessStartInfo("dotnet")
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        start.ArgumentList.Add(Path.Combine(directory, "CompositionKeyProbe.dll"));
        if (reverse)
            start.ArgumentList.Add("--reverse");
        using var process = Process.Start(start)
            ?? throw new InvalidOperationException("Could not start the composition-key probe.");
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var output = process.StandardOutput.ReadToEndAsync(timeout.Token);
        var error = process.StandardError.ReadToEndAsync(timeout.Token);
        try
        {
            await process.WaitForExitAsync(timeout.Token);
            Assert.True(process.ExitCode == 0, await error);
            string[] lines = (await output).Split('\n', StringSplitOptions.RemoveEmptyEntries);
            Assert.Equal(2, lines.Length);
            return (int.Parse(lines[0]), JsonSerializer.Deserialize<Dictionary<string, int[]>>(lines[1])
                ?? throw new InvalidOperationException("Composition-key probe did not return keys."));
        }
        finally
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync();
            }
        }
    }
}
