using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Microsoft.AndroidX.Compose.SourceGenerators.Tests;

public class CompanionGeneratorTests
{
    // Minimum Android/JNI stubs to make the user code & generated body compile.
    const string AndroidStubs = """
        namespace Android.Runtime
        {
            public static class JNIEnv
            {
                public static System.IntPtr FindClass(string name) => default;
                public static System.IntPtr GetMethodID(System.IntPtr cls, string name, string sig) => default;
                public static System.IntPtr GetStaticMethodID(System.IntPtr cls, string name, string sig) => default;
                public static System.IntPtr GetStaticFieldID(System.IntPtr cls, string name, string sig) => default;
                public static System.IntPtr GetStaticObjectField(System.IntPtr cls, System.IntPtr fid) => default;
                public static System.IntPtr NewGlobalRef(System.IntPtr h) => default;
                public static void DeleteLocalRef(System.IntPtr h) { }
                public static unsafe System.IntPtr CallObjectMethod(System.IntPtr inst, System.IntPtr m) => default;
                public static unsafe System.IntPtr CallStaticObjectMethod(System.IntPtr cls, System.IntPtr m, global::Android.Runtime.JValue* args) => default;
                public static int CallIntMethod(System.IntPtr inst, System.IntPtr m) => default;
            }
            public enum JniHandleOwnership { TransferLocalRef = 0 }
            public readonly struct JValue
            {
                public JValue(System.IntPtr v) { }
                public JValue(int v) { }
            }
        }
        namespace Java.Lang
        {
            public class Object
            {
                public System.IntPtr Handle => default;
                protected Object(System.IntPtr handle, global::Android.Runtime.JniHandleOwnership transfer) { }
                public Object() { }
            }
        }
        """;

    // Minimal attribute source — what Attributes.cs (post-init) would
    // normally provide. The test instantiates the companion generator in
    // isolation so we feed the attribute decls directly to the
    // compilation.
    const string AttributeSource = """
        namespace Microsoft.AndroidX.Compose
        {
            [global::System.AttributeUsage(global::System.AttributeTargets.Class, AllowMultiple = false)]
            internal sealed class ComposeCompanionAttribute : global::System.Attribute
            {
                public ComposeCompanionAttribute(string outerJniClass) { }
                public bool InlineClass { get; set; }
            }

            [global::System.AttributeUsage(global::System.AttributeTargets.Property, AllowMultiple = false)]
            internal sealed class ComposeCompanionGetterAttribute : global::System.Attribute
            {
                public ComposeCompanionGetterAttribute(string getterName) { }
                public string? ReturnDescriptor { get; set; }
            }
        }
        """;

    static (Compilation Output, ImmutableArray<Diagnostic> Diags, string? Emitted) Run(string userCode)
    {
        var parseOptions = new CSharpParseOptions(LanguageVersion.Preview);
        var stubs = CSharpSyntaxTree.ParseText(AndroidStubs, parseOptions);
        var attrs = CSharpSyntaxTree.ParseText(AttributeSource, parseOptions);
        var src = CSharpSyntaxTree.ParseText(userCode, parseOptions);

        var compilation = CSharpCompilation.Create(
            "Test",
            new[] { stubs, attrs, src },
            Net.Sdk.References,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
                allowUnsafe: true,
                nullableContextOptions: NullableContextOptions.Enable));

        var driver = CSharpGeneratorDriver.Create(
                new[] { new ComposeCompanionGenerator().AsSourceGenerator() },
                parseOptions: parseOptions)
            .RunGeneratorsAndUpdateCompilation(compilation, out var output, out var diags);

        string? emitted = null;
        foreach (var tree in output.SyntaxTrees)
        {
            var path = tree.FilePath;
            if (path.Contains(".Companion.") && path.EndsWith(".g.cs", System.StringComparison.Ordinal))
            {
                emitted = tree.GetText().ToString();
                break;
            }
        }
        return (output, diags, emitted);
    }

    [Fact]
    public void Simple_EmitsCompanionAccessorAndProperty()
    {
        var code = """
            using Microsoft.AndroidX.Compose;
            namespace Microsoft.AndroidX.Compose
            {
                [ComposeCompanion("androidx/compose/ui/text/font/FontWeight")]
                public sealed partial class FontWeight : Java.Lang.Object
                {
                    FontWeight(System.IntPtr h, global::Android.Runtime.JniHandleOwnership t) : base(h, t) { }

                    [ComposeCompanionGetter("getThin")]
                    public static partial FontWeight Thin { get; }

                    [ComposeCompanionGetter("getNormal")]
                    public static partial FontWeight Normal { get; }
                }
            }
            """;

        var (output, diags, emitted) = Run(code);
        Assert.Empty(diags.Where(d => d.Severity == DiagnosticSeverity.Error));
        Assert.NotNull(emitted);
        Assert.Contains("partial class FontWeight", emitted);
        // Companion accessor
        Assert.Contains("FindClass(\"androidx/compose/ui/text/font/FontWeight\")", emitted);
        Assert.Contains("GetStaticFieldID(__outer, \"Companion\", \"Landroidx/compose/ui/text/font/FontWeight$Companion;\")", emitted);
        Assert.Contains("NewGlobalRef(__local)", emitted);
        // Resolve helper
        Assert.Contains("ResolveSimple(string getterName, string returnDescriptor)", emitted);
        // Property bodies
        Assert.Contains("s_thin ??= ResolveSimple(\"getThin\", \"Landroidx/compose/ui/text/font/FontWeight;\")", emitted);
        Assert.Contains("s_normal ??= ResolveSimple(\"getNormal\", \"Landroidx/compose/ui/text/font/FontWeight;\")", emitted);
        // No inline-class plumbing for the simple case
        Assert.DoesNotContain("box-impl", emitted);

        // The whole thing must compile.
        var compileDiags = output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        Assert.Empty(compileDiags);
    }

    [Fact]
    public void CustomReturnDescriptor_PassesThroughToResolveSimple()
    {
        var code = """
            using Microsoft.AndroidX.Compose;
            namespace Microsoft.AndroidX.Compose
            {
                [ComposeCompanion("androidx/compose/ui/text/font/FontFamily")]
                public sealed partial class FontFamily : Java.Lang.Object
                {
                    FontFamily(System.IntPtr h, global::Android.Runtime.JniHandleOwnership t) : base(h, t) { }

                    [ComposeCompanionGetter("getSansSerif",
                                            ReturnDescriptor = "Landroidx/compose/ui/text/font/GenericFontFamily;")]
                    public static partial FontFamily SansSerif { get; }
                }
            }
            """;

        var (output, diags, emitted) = Run(code);
        Assert.Empty(diags.Where(d => d.Severity == DiagnosticSeverity.Error));
        Assert.NotNull(emitted);
        Assert.Contains("ResolveSimple(\"getSansSerif\", \"Landroidx/compose/ui/text/font/GenericFontFamily;\")", emitted);

        var compileDiags = output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        Assert.Empty(compileDiags);
    }

    [Fact]
    public void InlineClass_EmitsBoxImplBranch()
    {
        var code = """
            using Microsoft.AndroidX.Compose;
            namespace Microsoft.AndroidX.Compose
            {
                [ComposeCompanion("androidx/compose/ui/text/style/TextAlign", InlineClass = true)]
                public sealed partial class TextAlign : Java.Lang.Object
                {
                    TextAlign(System.IntPtr h, global::Android.Runtime.JniHandleOwnership t) : base(h, t) { }

                    [ComposeCompanionGetter("getCenter-e0LSkKk")]
                    public static partial TextAlign Center { get; }
                }
            }
            """;

        var (output, diags, emitted) = Run(code);
        Assert.Empty(diags.Where(d => d.Severity == DiagnosticSeverity.Error));
        Assert.NotNull(emitted);
        Assert.Contains("static unsafe TextAlign ResolveInline(string mangledGetter)", emitted);
        Assert.Contains("GetMethodID(__companionCls, mangledGetter, \"()I\")", emitted);
        Assert.Contains("GetStaticMethodID(__outerCls, \"box-impl\", \"(I)Landroidx/compose/ui/text/style/TextAlign;\")", emitted);
        Assert.Contains("s_center ??= ResolveInline(\"getCenter-e0LSkKk\")", emitted);
        // No simple-path helper when inline.
        Assert.DoesNotContain("ResolveSimple", emitted);

        var compileDiags = output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        Assert.Empty(compileDiags);
    }

    [Fact]
    public void NonPartialClass_ReportsCN4001()
    {
        var code = """
            using Microsoft.AndroidX.Compose;
            namespace Microsoft.AndroidX.Compose
            {
                [ComposeCompanion("foo/Bar")]
                public sealed class NotPartial : Java.Lang.Object
                {
                    NotPartial(System.IntPtr h, global::Android.Runtime.JniHandleOwnership t) : base(h, t) { }
                }
            }
            """;
        var (_, diags, _) = Run(code);
        Assert.Contains(diags, d => d.Id == "CN4001");
    }

    [Fact]
    public void EmptyOuter_ReportsCN4002()
    {
        var code = """
            using Microsoft.AndroidX.Compose;
            namespace Microsoft.AndroidX.Compose
            {
                [ComposeCompanion("")]
                public sealed partial class EmptyOuter : Java.Lang.Object
                {
                    EmptyOuter(System.IntPtr h, global::Android.Runtime.JniHandleOwnership t) : base(h, t) { }
                }
            }
            """;
        var (_, diags, _) = Run(code);
        Assert.Contains(diags, d => d.Id == "CN4002");
    }

    [Fact]
    public void GetterOnNonPartialProperty_ReportsCN4003()
    {
        var code = """
            using Microsoft.AndroidX.Compose;
            namespace Microsoft.AndroidX.Compose
            {
                [ComposeCompanion("foo/Bar")]
                public sealed partial class Host : Java.Lang.Object
                {
                    Host(System.IntPtr h, global::Android.Runtime.JniHandleOwnership t) : base(h, t) { }

                    [ComposeCompanionGetter("getThing")]
                    public static Host Thing => null!;
                }
            }
            """;
        var (_, diags, _) = Run(code);
        Assert.Contains(diags, d => d.Id == "CN4003");
    }

    [Fact]
    public void EmptyGetterName_ReportsCN4004()
    {
        var code = """
            using Microsoft.AndroidX.Compose;
            namespace Microsoft.AndroidX.Compose
            {
                [ComposeCompanion("foo/Bar")]
                public sealed partial class Host : Java.Lang.Object
                {
                    Host(System.IntPtr h, global::Android.Runtime.JniHandleOwnership t) : base(h, t) { }

                    [ComposeCompanionGetter("")]
                    public static partial Host Thing { get; }
                }
            }
            """;
        var (_, diags, _) = Run(code);
        Assert.Contains(diags, d => d.Id == "CN4004");
    }

    [Fact]
    public void OrphanedGetter_ReportsCN4005()
    {
        var code = """
            using Microsoft.AndroidX.Compose;
            namespace Microsoft.AndroidX.Compose
            {
                public sealed partial class NoHost : Java.Lang.Object
                {
                    NoHost(System.IntPtr h, global::Android.Runtime.JniHandleOwnership t) : base(h, t) { }

                    [ComposeCompanionGetter("getThing")]
                    public static partial NoHost Thing { get; }
                }
            }
            """;
        var (_, diags, _) = Run(code);
        Assert.Contains(diags, d => d.Id == "CN4005");
    }

    [Fact]
    public void ReturnTypeWithoutPeerCtor_ReportsCN4006()
    {
        var code = """
            using Microsoft.AndroidX.Compose;
            namespace Microsoft.AndroidX.Compose
            {
                public class NoCtor : Java.Lang.Object
                {
                    // No (IntPtr, JniHandleOwnership) ctor declared.
                }

                [ComposeCompanion("foo/Bar")]
                public sealed partial class Host : Java.Lang.Object
                {
                    Host(System.IntPtr h, global::Android.Runtime.JniHandleOwnership t) : base(h, t) { }

                    [ComposeCompanionGetter("getThing")]
                    public static partial NoCtor Thing { get; }
                }
            }
            """;
        var (_, diags, _) = Run(code);
        Assert.Contains(diags, d => d.Id == "CN4006");
    }

    [Fact]
    public void InlineClassWithReturnDescriptor_ReportsCN4007()
    {
        var code = """
            using Microsoft.AndroidX.Compose;
            namespace Microsoft.AndroidX.Compose
            {
                [ComposeCompanion("foo/Bar", InlineClass = true)]
                public sealed partial class Host : Java.Lang.Object
                {
                    Host(System.IntPtr h, global::Android.Runtime.JniHandleOwnership t) : base(h, t) { }

                    [ComposeCompanionGetter("getThing-mangled", ReturnDescriptor = "Lfoo/Baz;")]
                    public static partial Host Thing { get; }
                }
            }
            """;
        var (_, diags, _) = Run(code);
        Assert.Contains(diags, d => d.Id == "CN4007");
    }

    [Fact]
    public void MangledKotlinGetterName_PassedThroughVerbatim()
    {
        // Hyphens in Kotlin-mangled inline-class getter names must NOT be
        // rejected by the generator's name validation.
        var code = """
            using Microsoft.AndroidX.Compose;
            namespace Microsoft.AndroidX.Compose
            {
                [ComposeCompanion("androidx/compose/ui/text/font/FontStyle", InlineClass = true)]
                public sealed partial class FontStyle : Java.Lang.Object
                {
                    FontStyle(System.IntPtr h, global::Android.Runtime.JniHandleOwnership t) : base(h, t) { }

                    [ComposeCompanionGetter("getNormal-_-LCdwA")]
                    public static partial FontStyle Normal { get; }
                }
            }
            """;

        var (_, diags, emitted) = Run(code);
        Assert.Empty(diags.Where(d => d.Severity == DiagnosticSeverity.Error));
        Assert.NotNull(emitted);
        Assert.Contains("ResolveInline(\"getNormal-_-LCdwA\")", emitted);
    }
}
