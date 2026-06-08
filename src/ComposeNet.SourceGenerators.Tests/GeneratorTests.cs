using System.Collections.Immutable;
using System.Linq;
using ComposeNet.SourceGenerators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace ComposeNet.SourceGenerators.Tests;

/// <summary>
/// Runs <see cref="ComposeDefaultsGenerator"/> against synthetic
/// compilations that provide just enough of the Compose / Kotlin shape
/// for the generator to operate on. We assert the emitted source text
/// directly — no real Compose binding DLLs needed.
/// </summary>
public class GeneratorTests
{
    const string SharedHeader = """
        namespace AndroidX.Compose.Runtime { public interface IComposer { } }
        namespace Kotlin.Jvm.Functions
        {
            public interface IFunction0 { }
            public interface IFunction2 { }
            public interface IFunction3 { }
        }
        """;

    static (Compilation Output, ImmutableArray<Diagnostic> Diagnostics, string? Emitted) RunGenerator(string assemblyAttribute, string syntheticBindings)
    {
        var input = CSharpSyntaxTree.ParseText(assemblyAttribute);
        var bindings = CSharpSyntaxTree.ParseText(SharedHeader + "\n" + syntheticBindings);

        var compilation = CSharpCompilation.Create(
            "Test",
            new[] { input, bindings },
            references: Net.Sdk.References,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));

        var driver = CSharpGeneratorDriver.Create(new ComposeDefaultsGenerator())
            .RunGeneratorsAndUpdateCompilation(compilation, out var output, out var diags);

        string? emitted = null;
        foreach (var tree in output.SyntaxTrees)
        {
            var path = tree.FilePath;
            if (path.EndsWith(".g.cs", System.StringComparison.Ordinal) &&
                !path.EndsWith("ComposeDefaultsAttribute.g.cs", System.StringComparison.Ordinal))
            {
                emitted = tree.GetText().ToString();
                break;
            }
        }

        return (output, diags, emitted);
    }

    [Fact]
    public void HappyPath_EmitsNamedBitsAndAll()
    {
        var bindings = """
            namespace Foo
            {
                public static class FooKt
                {
                    public static void Foo(int alpha, string beta,
                        AndroidX.Compose.Runtime.IComposer _composer, int _changed, int _default) { }
                }
            }
            """;
        var attr = """
            [assembly: ComposeNet.ComposeDefaults<Foo.FooKt>("Foo", "FooDefault")]
            """;

        var (_, diags, emitted) = RunGenerator(attr, bindings);
        Assert.Empty(diags);
        Assert.NotNull(emitted);
        Assert.Contains("internal enum FooDefault", emitted);
        Assert.Contains("Alpha = 1 << 0,", emitted);
        Assert.Contains("Beta  = 1 << 1,", emitted);
        Assert.Contains("All = Alpha | Beta,", emitted);
        Assert.Contains("None = 0,", emitted);
    }

    [Fact]
    public void FunctionTypedParameters_AreSkipped_HolesPreserved()
    {
        var bindings = """
            namespace Foo
            {
                public static class ButtonKt
                {
                    public static void Button(
                        Kotlin.Jvm.Functions.IFunction0 onClick,
                        int alpha, int beta, int gamma, int delta,
                        Kotlin.Jvm.Functions.IFunction3 content,
                        AndroidX.Compose.Runtime.IComposer _composer, int _changed, int _default) { }
                }
            }
            """;
        var attr = """
            [assembly: ComposeNet.ComposeDefaults<Foo.ButtonKt>("Button", "ButtonDefault")]
            """;

        var (_, diags, emitted) = RunGenerator(attr, bindings);
        Assert.Empty(diags);
        Assert.NotNull(emitted);
        Assert.Contains("Alpha = 1 << 1,", emitted);
        Assert.Contains("Beta  = 1 << 2,", emitted);
        Assert.Contains("Gamma = 1 << 3,", emitted);
        Assert.Contains("Delta = 1 << 4,", emitted);
        Assert.DoesNotContain("OnClick =", emitted);
        Assert.Contains("// bit 0: onClick", emitted);
        Assert.Contains("// bit 5: content", emitted);
        Assert.Contains("All = Alpha | Beta | Gamma | Delta,", emitted);
    }

    [Fact]
    public void MangledKotlinName_IsResolvedByExactString()
    {
        var bindings = """
            namespace Foo
            {
                public static class TextKt
                {
                    public static void TextDashDashFoo(string text, int alpha,
                        AndroidX.Compose.Runtime.IComposer _composer, int _changed) { }
                }
            }
            """;
        var attr = """
            [assembly: ComposeNet.ComposeDefaults<Foo.TextKt>("TextDashDashFoo", "TextDefault")]
            """;

        var (_, diags, emitted) = RunGenerator(attr, bindings);
        Assert.Empty(diags);
        Assert.NotNull(emitted);
        Assert.Contains("Text  = 1 << 0,", emitted);
        Assert.Contains("Alpha = 1 << 1,", emitted);
    }

    [Fact]
    public void OverloadSelection_PicksLongest()
    {
        var bindings = """
            namespace Foo
            {
                public static class FooKt
                {
                    public static void Foo(int a,
                        AndroidX.Compose.Runtime.IComposer _composer, int _changed) { }
                    public static void Foo(int a, int b, int c, int d,
                        AndroidX.Compose.Runtime.IComposer _composer, int _changed, int _default) { }
                }
            }
            """;
        var attr = """
            [assembly: ComposeNet.ComposeDefaults<Foo.FooKt>("Foo", "FooDefault")]
            """;

        var (_, diags, emitted) = RunGenerator(attr, bindings);
        Assert.Empty(diags);
        Assert.NotNull(emitted);
        Assert.Contains("D = 1 << 3,", emitted);
        Assert.Contains("All = A | B | C | D,", emitted);
    }

    [Fact]
    public void MissingMethod_ReportsCN1001()
    {
        var bindings = """
            namespace Foo
            {
                public static class FooKt
                {
                    public static void Bar(int x,
                        AndroidX.Compose.Runtime.IComposer _composer, int _changed) { }
                }
            }
            """;
        var attr = """
            [assembly: ComposeNet.ComposeDefaults<Foo.FooKt>("Foo", "FooDefault")]
            """;

        var (_, diags, _) = RunGenerator(attr, bindings);
        Assert.Contains(diags, d => d.Id == "CN1001");
    }

    [Fact]
    public void NoComposer_ReportsCN1002()
    {
        var bindings = """
            namespace Foo
            {
                public static class FooKt
                {
                    public static void Foo(int x, int y) { }
                }
            }
            """;
        var attr = """
            [assembly: ComposeNet.ComposeDefaults<Foo.FooKt>("Foo", "FooDefault")]
            """;

        var (_, diags, _) = RunGenerator(attr, bindings);
        Assert.Contains(diags, d => d.Id == "CN1002");
    }

    [Fact]
    public void SyntheticParameterName_FallsBackToParamN()
    {
        var bindings = """
            namespace Foo
            {
                public static class FooKt
                {
                    public static void Foo(int alpha, int p1, int gamma,
                        AndroidX.Compose.Runtime.IComposer _composer, int _changed, int _default) { }
                }
            }
            """;
        var attr = """
            [assembly: ComposeNet.ComposeDefaults<Foo.FooKt>("Foo", "FooDefault")]
            """;

        var (_, diags, emitted) = RunGenerator(attr, bindings);
        Assert.Empty(diags);
        Assert.NotNull(emitted);
        Assert.Contains("Alpha  = 1 << 0,", emitted);
        Assert.Contains("Param1 = 1 << 1,", emitted);
        Assert.Contains("Gamma  = 1 << 2,", emitted);
        Assert.Contains("All = Alpha | Param1 | Gamma,", emitted);
    }

    [Fact]
    public void DeclarativeAttribute_EmitsBitsFromNamesList()
    {
        var attr = """
            [assembly: ComposeNet.ComposeDefaults("ButtonDefault",
                "!onClick", "modifier", "enabled", "shape", "!content")]
            """;

        var (_, diags, emitted) = RunGenerator(attr, "// nothing");
        Assert.Empty(diags);
        Assert.NotNull(emitted);
        Assert.Contains("internal enum ButtonDefault", emitted);
        // !onClick at bit 0 is consumed but no member emitted.
        Assert.DoesNotContain("OnClick =", emitted);
        Assert.Contains("// bit 0: onClick", emitted);
        Assert.Contains("Modifier = 1 << 1,", emitted);
        Assert.Contains("Enabled  = 1 << 2,", emitted);
        Assert.Contains("Shape    = 1 << 3,", emitted);
        // !content at bit 4 is consumed but no member emitted.
        Assert.DoesNotContain("Content =", emitted);
        Assert.Contains("// bit 4: content", emitted);
        Assert.Contains("All = Modifier | Enabled | Shape,", emitted);
    }

    [Fact]
    public void DeclarativeAttribute_RequiresNoBindingSymbols()
    {
        // No synthetic Kt class — proves the declarative path doesn't need IMethodSymbol.
        var attr = """
            [assembly: ComposeNet.ComposeDefaults("FooDefault", "alpha", "beta", "gamma")]
            """;

        var (_, diags, emitted) = RunGenerator(attr, "// nothing");
        Assert.Empty(diags);
        Assert.NotNull(emitted);
        Assert.Contains("Alpha = 1 << 0,", emitted);
        Assert.Contains("Beta  = 1 << 1,", emitted);
        Assert.Contains("Gamma = 1 << 2,", emitted);
        Assert.Contains("All = Alpha | Beta | Gamma,", emitted);
    }

    [Fact]
    public void AttributeIsAddedViaPostInit()
    {
        var (output, _, _) = RunGenerator("// nothing", "// nothing");
        var attrTree = output.SyntaxTrees.FirstOrDefault(t => t.FilePath.EndsWith("ComposeDefaultsAttribute.g.cs", System.StringComparison.Ordinal));
        Assert.NotNull(attrTree);
        Assert.Contains("ComposeDefaultsAttribute<T>", attrTree.GetText().ToString());
        Assert.Contains("AttributeTargets.Assembly", attrTree.GetText().ToString());
    }

    static string BuildDeclarativeAttribute(string enumName, int slotCount)
    {
        var names = string.Join(", ", Enumerable.Range(0, slotCount).Select(i => "\"slot" + i + "\""));
        return $"[assembly: ComposeNet.ComposeDefaults(\"{enumName}\", {names})]";
    }

    [Fact]
    public void Boundary_31Slots_StaysIntBackedAndOmitsSplitHelper()
    {
        var attr = BuildDeclarativeAttribute("ThirtyOneDefault", 31);

        var (_, diags, emitted) = RunGenerator(attr, "// nothing");
        Assert.Empty(diags);
        Assert.NotNull(emitted);
        // No `: long` base type — int-backed enum stays binary-compatible.
        Assert.Contains("internal enum ThirtyOneDefault", emitted);
        Assert.DoesNotContain(": long", emitted);
        // Bits use the int-style shift literal.
        Assert.Contains("Slot0  = 1 << 0,", emitted);
        Assert.Contains("Slot30 = 1 << 30,", emitted);
        Assert.DoesNotContain("1L <<", emitted);
        // No Extensions helper class.
        Assert.DoesNotContain("ThirtyOneDefaultExtensions", emitted);
        Assert.DoesNotContain(".Split(", emitted);
    }

    [Fact]
    public void Boundary_32Slots_SwitchesToLongBackedAndEmitsSplitHelper()
    {
        var attr = BuildDeclarativeAttribute("ThirtyTwoDefault", 32);

        var (_, diags, emitted) = RunGenerator(attr, "// nothing");
        Assert.Empty(diags);
        Assert.NotNull(emitted);
        Assert.Contains("internal enum ThirtyTwoDefault : long", emitted);
        // Every shift is now a long literal, including the lowest bit.
        Assert.Contains("Slot0  = 1L << 0,", emitted);
        Assert.Contains("Slot31 = 1L << 31,", emitted);
        Assert.DoesNotContain("1 << 0,", emitted);
        // Split helper is emitted.
        Assert.Contains("internal static class ThirtyTwoDefaultExtensions", emitted);
        Assert.Contains("public static (int Mask0, int Mask1) Split(this ThirtyTwoDefault value)", emitted);
        Assert.Contains("(int)((long)value & 0xFFFFFFFFL)", emitted);
        Assert.Contains("(int)(((long)value >> 32) & 0xFFFFFFFFL)", emitted);
    }

    [Fact]
    public void WideEnum_48Slots_LikeColorScheme_EmitsBit47AndSplit()
    {
        var attr = BuildDeclarativeAttribute("ColorSchemeDefault", 48);

        var (_, diags, emitted) = RunGenerator(attr, "// nothing");
        Assert.Empty(diags);
        Assert.NotNull(emitted);
        Assert.Contains("internal enum ColorSchemeDefault : long", emitted);
        Assert.Contains("Slot0  = 1L << 0,", emitted);
        Assert.Contains("Slot32 = 1L << 32,", emitted);
        Assert.Contains("Slot47 = 1L << 47,", emitted);
        Assert.Contains("internal static class ColorSchemeDefaultExtensions", emitted);
        Assert.Contains("Split(this ColorSchemeDefault value)", emitted);
    }

    [Fact]
    public void WideEnum_63Slots_EmitsHighestBitAsLongShift()
    {
        var attr = BuildDeclarativeAttribute("SixtyThreeDefault", 63);

        var (_, diags, emitted) = RunGenerator(attr, "// nothing");
        Assert.Empty(diags);
        Assert.NotNull(emitted);
        Assert.Contains("internal enum SixtyThreeDefault : long", emitted);
        Assert.Contains("Slot62 = 1L << 62,", emitted);
        Assert.Contains("internal static class SixtyThreeDefaultExtensions", emitted);
    }

    [Fact]
    public void SmallEnum_GoldenStringUnchangedFromPreIssue178()
    {
        // Golden-string check: prove the ≤ 31-bit code path is byte-for-byte
        // identical to what the generator emitted before long-mode landed.
        // If this breaks, the existing int-backed declarations across the
        // repo also change shape — that's a real review surface.
        var attr = """
            [assembly: ComposeNet.ComposeDefaults("SmallDefault", "alpha", "beta", "gamma")]
            """;

        var (_, diags, emitted) = RunGenerator(attr, "// nothing");
        Assert.Empty(diags);
        Assert.NotNull(emitted);

        var expected =
            "// <auto-generated/>\n" +
            "// Generated by ComposeNet.SourceGenerators from declarative names for 'SmallDefault'\n" +
            "// Bit positions match the Kotlin parameter index in the $default bitmask.\n" +
            "#nullable enable\n" +
            "namespace ComposeNet\n" +
            "{\n" +
            "    [global::System.Flags]\n" +
            "    internal enum SmallDefault\n" +
            "    {\n" +
            "        None = 0,\n" +
            "        Alpha = 1 << 0,\n" +
            "        Beta  = 1 << 1,\n" +
            "        Gamma = 1 << 2,\n" +
            "\n" +
            "        /// <summary>OR of every user-defaultable bit.</summary>\n" +
            "        All = Alpha | Beta | Gamma,\n" +
            "    }\n" +
            "}\n";

        Assert.Equal(expected.Replace("\n", System.Environment.NewLine), emitted);
    }

    [Fact]
    public void Split_RoundTripsThroughGeneratedHelper()
    {
        // Emit the generated assembly to memory, load it, and exercise
        // the Split extension via reflection. This catches any sign /
        // shift bug in the helper formula.
        var attr = BuildDeclarativeAttribute("RoundTripDefault", 48);
        var (output, diags, _) = RunGenerator(attr, "// nothing");
        Assert.Empty(diags);

        using var ms = new System.IO.MemoryStream();
        var emitResult = output.Emit(ms);
        Assert.True(
            emitResult.Success,
            "Emit failed: " + string.Join("\n", emitResult.Diagnostics));

        var asm = System.Reflection.Assembly.Load(ms.ToArray());
        var enumType = asm.GetType("ComposeNet.RoundTripDefault");
        Assert.NotNull(enumType);
        Assert.Equal(typeof(long), System.Enum.GetUnderlyingType(enumType!));

        var extType = asm.GetType("ComposeNet.RoundTripDefaultExtensions");
        Assert.NotNull(extType);
        var split = extType!.GetMethod(
            "Split",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        Assert.NotNull(split);

        // Bits chosen to exercise: lowest, the int sign bit (31), the
        // first slot in the high word (32), and a high-side slot (47).
        long bits = (1L << 0) | (1L << 31) | (1L << 32) | (1L << 47);
        var value = System.Enum.ToObject(enumType!, bits);

        var tuple = split!.Invoke(null, new[] { value });
        Assert.NotNull(tuple);
        var tupleType = tuple!.GetType();
        int mask0 = (int)tupleType.GetField("Item1")!.GetValue(tuple)!;
        int mask1 = (int)tupleType.GetField("Item2")!.GetValue(tuple)!;

        // Reassemble the original long and compare.
        long roundTripped = ((long)(uint)mask0) | (((long)(uint)mask1) << 32);
        Assert.Equal(bits, roundTripped);

        // And the individual halves match Kotlin's expectation: low
        // 32 bits → Mask0, high 32 bits → Mask1.
        Assert.Equal(unchecked((int)((1L << 0) | (1L << 31))), mask0);
        Assert.Equal((int)((1L << 0) | (1L << 15)), mask1);
    }
}
