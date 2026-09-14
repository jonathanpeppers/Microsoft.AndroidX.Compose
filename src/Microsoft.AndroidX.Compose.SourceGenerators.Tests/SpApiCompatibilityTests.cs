using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace AndroidX.Compose.SourceGenerators.Tests;

public class SpApiCompatibilityTests
{
    // Distinct sentinels verify overload selection, not Kotlin packing (covered on device).
    const string BindingStubs = """
        namespace AndroidX.Compose.UI.Unit {
            public static class TextUnitKt {
                public static long GetSp(int value) => 1000L + value;
                public static long GetSp(float value) => 2000L + (long)(value * 100);
            }
            public static class TextUnit {
                public static int CompareTo(long left, long right) => throw new NotSupportedException();
                public static long Times(long value, float scalar) => throw new NotSupportedException();
                public static long Div(long value, float scalar) => throw new NotSupportedException();
                public static long UnaryMinus(long value) => throw new NotSupportedException();
            }
        }
        """;

    const string OldContract = """
        namespace AndroidX.Compose {
            public readonly struct Sp {
                public long PackedValue { get; }
                public Sp(int sp) { PackedValue = 1000L + sp; }
                public Sp(long packedValue) { PackedValue = packedValue; }
                public static implicit operator Sp(int value) => new Sp(value);
            }
            public static class SpExtensions {
                public static Sp Sp(this int value) => new Sp(value);
            }
        }
        """;

    const string ExistingConsumer = """
        using System;
        using AndroidX.Compose;
        public static class Consumer {
            public static bool Run() {
                int amount = 16;
                long packed = 0x000000023F800000L;
                uint unsigned = 16;
                short small = 16;
                Sp converted = amount;
                Sp? nullable = amount;
                Func<int, Sp> extension = SpExtensions.Sp;
                return new Sp(amount).PackedValue == 1016L
                    && new Sp(sp: amount).PackedValue == 1016L
                    && converted.PackedValue == 1016L
                    && nullable.Value.PackedValue == 1016L
                    && new Sp(small).PackedValue == 1016L
                    && new Sp(unsigned).PackedValue == 16L
                    && new Sp(16L).PackedValue == 16L
                    && new Sp(default).PackedValue == 1000L
                    && ((Sp)16L).PackedValue == 1016L
                    && new Sp(packedValue: packed).PackedValue == packed
                    && amount.Sp().PackedValue == 1016L
                    && extension(16).PackedValue == 1016L;
            }
        }
        """;

    [Fact]
    public void IntegerAndPackedLongConsumersRemainSourceAndBinaryCompatible()
    {
        const string name = "SpContract";
        using var oldContract = Emit(Create(name, OldContract));
        using var oldConsumer = Emit(Create("OldSpConsumer", ExistingConsumer,
            MetadataReference.CreateFromImage(oldContract.ToArray())));
        using var current = Emit(Create(name, CurrentContract()));
        using var recompiled = Emit(Create("RecompiledSpConsumer", ExistingConsumer,
            MetadataReference.CreateFromImage(current.ToArray())));

        var context = new AssemblyLoadContext(name, isCollectible: true);
        try
        {
            context.LoadFromStream(current);
            MemoryStream[] consumers = [oldConsumer, recompiled];
            foreach (var consumer in consumers)
            {
                var run = context.LoadFromStream(consumer).GetType("Consumer")?.GetMethod("Run")
                    ?? throw new InvalidOperationException("Sp consumer entry point missing.");
                Assert.Equal(true, run.Invoke(null, null));
            }
        }
        finally
        {
            context.Unload();
        }
    }

    [Fact]
    public void FractionalCallsBindToFloatWithoutAddingImplicitAmountConversions()
    {
        using var contract = Emit(Create("SpContract", CurrentContract()));
        var reference = MetadataReference.CreateFromImage(contract.ToArray());
        const string calls = """
            using System;
            using AndroidX.Compose;
            public static class Consumer {
                public static bool Run() {
                    Func<float, Sp> extension = SpExtensions.Sp;
                    return new Sp(0.5f).PackedValue == 2050L
                        && new Sp(sp: -0.5f).PackedValue == 1950L
                        && 0.5f.Sp().PackedValue == 2050L
                        && extension(0.5f).PackedValue == 2050L;
                }
            }
            """;
        using var consumer = Emit(Create("FloatSpConsumer", calls, reference));
        var context = new AssemblyLoadContext("FloatSp", isCollectible: true);
        try
        {
            context.LoadFromStream(contract);
            var run = context.LoadFromStream(consumer).GetType("Consumer")?.GetMethod("Run")
                ?? throw new InvalidOperationException("Float Sp consumer entry point missing.");
            Assert.Equal(true, run.Invoke(null, null));
        }
        finally
        {
            context.Unload();
        }

        (string Expression, string Diagnostic)[] rejected =
            [("16L", "CS0266"), ("16UL", "CS0029"), ("0.5f", "CS0266"), ("0.5d", "CS0266")];
        foreach (var test in rejected)
        {
            var invalid = Create("InvalidConsumer",
                $"using AndroidX.Compose; class Consumer {{ Sp value = {test.Expression}; }}", reference);
            Assert.Contains(invalid.GetDiagnostics(), d => d.Severity == DiagnosticSeverity.Error && d.Id == test.Diagnostic);
        }
    }

    static string CurrentContract([CallerFilePath] string file = "")
    {
        string directory = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(file)
            ?? throw new InvalidOperationException("Test source directory unavailable."), "..", "Microsoft.AndroidX.Compose"));
        string[] files = ["Sp.cs", "SpExtensions.cs"];
        return BindingStubs + "\n" + string.Join("\n", files
            .Select(name => File.ReadAllText(Path.Combine(directory, name))
                .Replace("namespace AndroidX.Compose;", "namespace AndroidX.Compose {", StringComparison.Ordinal) + "\n}"));
    }

    static CSharpCompilation Create(string name, string source, MetadataReference? contract = null) =>
        CSharpCompilation.Create(name, [CSharpSyntaxTree.ParseText("global using System;\n" + source)],
            contract is null ? Net.Sdk.References : Net.Sdk.References.Add(contract),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));

    static MemoryStream Emit(CSharpCompilation compilation)
    {
        var stream = new MemoryStream();
        var result = compilation.Emit(stream);
        Assert.True(result.Success, string.Join("\n", result.Diagnostics));
        stream.Position = 0;
        return stream;
    }
}
