using System.Collections.Immutable;
using System.Linq;
using ComposeNet.SourceGenerators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace ComposeNet.SourceGenerators.Tests;

public class BridgeGeneratorTests
{
    const string AndroidStubs = """
        namespace Android.Runtime
        {
            public static class JNIEnv
            {
                public static System.IntPtr FindClass(string name) => default;
                public static System.IntPtr GetStaticMethodID(System.IntPtr cls, string name, string sig) => default;
                public static System.IntPtr GetMethodID(System.IntPtr cls, string name, string sig) => default;
                public static System.IntPtr GetStaticFieldID(System.IntPtr cls, string name, string sig) => default;
                public static System.IntPtr GetStaticObjectField(System.IntPtr cls, System.IntPtr fid) => default;
                public static System.IntPtr NewGlobalRef(System.IntPtr h) => default;
                public static System.IntPtr NewString(string s) => default;
                public static void DeleteLocalRef(System.IntPtr h) { }
                public static unsafe void CallStaticVoidMethod(System.IntPtr cls, System.IntPtr m, Android.Runtime.JValue* args) { }
                public static unsafe void CallVoidMethod(System.IntPtr inst, System.IntPtr m, Android.Runtime.JValue* args) { }
                public static unsafe System.IntPtr CallStaticObjectMethod(System.IntPtr cls, System.IntPtr m, Android.Runtime.JValue* args) => default;
                public static unsafe System.IntPtr CallObjectMethod(System.IntPtr inst, System.IntPtr m, Android.Runtime.JValue* args) => default;
            }
            public readonly struct JValue
            {
                public JValue(System.IntPtr v) { } public JValue(bool v) { } public JValue(int v) { }
                public JValue(long v) { } public JValue(float v) { } public JValue(double v) { }
            }
        }
        namespace Java.Interop
        {
            public readonly struct JValue
            {
                public JValue(System.IntPtr v) { } public JValue(bool v) { } public JValue(int v) { }
                public JValue(long v) { } public JValue(float v) { } public JValue(double v) { }
            }
        }
        namespace Java.Lang { public class Object { public System.IntPtr Handle => default; } }
        namespace AndroidX.Compose.Runtime { public interface IComposer { } }
        namespace Kotlin.Jvm.Functions
        {
            public interface IFunction0 { }
            public interface IFunction1 { }
            public interface IFunction2 { }
            public interface IFunction3 { }
        }
        namespace ComposeNet
        {
            public interface IModifier { }
            public static partial class ComposeBridges
            {
                public static System.IntPtr ModifierHandle(IModifier? m) => default;
            }
        }
        """;

    static (Compilation Output, ImmutableArray<Diagnostic> Diags, string? Emitted) Run(string userCode)
    {
        var stubs = CSharpSyntaxTree.ParseText(AndroidStubs);
        var src = CSharpSyntaxTree.ParseText(userCode);

        var compilation = CSharpCompilation.Create(
            "Test",
            new[] { stubs, src },
            Net.Sdk.References,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
                allowUnsafe: true,
                nullableContextOptions: NullableContextOptions.Enable));

        var driver = CSharpGeneratorDriver.Create(
                new ComposeDefaultsGenerator(),
                new ComposeBridgeGenerator())
            .RunGeneratorsAndUpdateCompilation(compilation, out var output, out var diags);

        string? emitted = null;
        foreach (var tree in output.SyntaxTrees)
        {
            var path = tree.FilePath;
            if (path.Contains("ComposeBridges.") && path.EndsWith(".g.cs", System.StringComparison.Ordinal))
            {
                emitted = tree.GetText().ToString();
                break;
            }
        }
        return (output, diags, emitted);
    }

    [Fact]
    public void Button_GeneratesFullBody()
    {
        var code = """
            using AndroidX.Compose.Runtime;
            using ComposeNet;
            using Kotlin.Jvm.Functions;

            [assembly: ComposeDefaults("ButtonDefault",
                "!onClick", "modifier", "enabled", "shape", "colors",
                "elevation", "border", "contentPadding", "interactionSource", "!content")]

            namespace ComposeNet
            {
                public static partial class ComposeBridges
                {
                    [ComposeBridge(
                        Class = "androidx/compose/material3/ButtonKt",
                        JvmName = "Button",
                        Signature = "(Lkotlin/jvm/functions/Function0;Landroidx/compose/ui/Modifier;ZLandroidx/compose/ui/graphics/Shape;Landroidx/compose/material3/ButtonColors;Landroidx/compose/material3/ButtonElevation;Landroidx/compose/foundation/BorderStroke;Landroidx/compose/foundation/layout/PaddingValues;Landroidx/compose/foundation/interaction/MutableInteractionSource;Lkotlin/jvm/functions/Function3;Landroidx/compose/runtime/Composer;II)V",
                        Defaults = typeof(ButtonDefault))]
                    public static partial void Button(IFunction0 onClick, IModifier? modifier,
                                                      IFunction3 content, IComposer composer);
                }
            }
            """;

        var (output, diags, emitted) = Run(code);
        Assert.Empty(diags.Where(d => d.Severity == DiagnosticSeverity.Error));
        Assert.NotNull(emitted);
        Assert.Contains("FindClass(\"androidx/compose/material3/ButtonKt\")", emitted);
        Assert.Contains("GetStaticMethodID(s_Button_class, \"Button\"", emitted);
        Assert.Contains("(int)global::ComposeNet.ButtonDefault.All", emitted);
        Assert.Contains("if (modifier is not null) defaults &= ~(int)global::ComposeNet.ButtonDefault.Modifier;", emitted);
        Assert.Contains("ModifierHandle(modifier)", emitted);
        Assert.Contains("global::System.GC.KeepAlive(onClick);", emitted);
        Assert.Contains("global::System.GC.KeepAlive(content);", emitted);
        Assert.Contains("global::System.GC.KeepAlive(composer);", emitted);
        Assert.Contains("CallStaticVoidMethod(s_Button_class", emitted);

        // Whole compilation including the generated body must compile cleanly.
        var compileDiags = output.GetDiagnostics();
        var errors = compileDiags.Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
        Assert.Empty(errors);
    }

    [Fact]
    public void MangledJvmName_IsLiteralString()
    {
        var code = """
            using AndroidX.Compose.Runtime;
            using ComposeNet;
            using Kotlin.Jvm.Functions;

            [assembly: ComposeDefaults("SurfaceDefault",
                "modifier", "shape", "color", "contentColor", "tonalElevation",
                "shadowElevation", "border", "!content")]

            namespace ComposeNet
            {
                public static partial class ComposeBridges
                {
                    [ComposeBridge(
                        Class = "androidx/compose/material3/SurfaceKt",
                        JvmName = "Surface-T9BRK9s",
                        Signature = "(Landroidx/compose/ui/Modifier;Landroidx/compose/ui/graphics/Shape;JJFFLandroidx/compose/foundation/BorderStroke;Lkotlin/jvm/functions/Function2;Landroidx/compose/runtime/Composer;II)V",
                        Defaults = typeof(SurfaceDefault))]
                    public static partial void Surface(IModifier? modifier, IFunction2 content, IComposer composer);
                }
            }
            """;

        var (_, diags, emitted) = Run(code);
        Assert.Empty(diags.Where(d => d.Severity == DiagnosticSeverity.Error));
        Assert.NotNull(emitted);
        Assert.Contains("\"Surface-T9BRK9s\"", emitted);
    }

    [Fact]
    public void StringParam_NewStringAndDeleteLocalRef()
    {
        var code = """
            using AndroidX.Compose.Runtime;
            using ComposeNet;
            using Kotlin.Jvm.Functions;

            [assembly: ComposeDefaults("TextDefault",
                "!text", "modifier", "color", "fontSize", "fontStyle",
                "fontWeight", "fontFamily", "letterSpacing", "decoration", "align",
                "lineHeight", "overflow", "softWrap", "maxLines", "minLines",
                "onTextLayout", "style")]

            namespace ComposeNet
            {
                public static partial class ComposeBridges
                {
                    [ComposeBridge(
                        Class = "androidx/compose/material3/TextKt",
                        JvmName = "Text--4IGK_g",
                        Signature = "(Ljava/lang/String;Landroidx/compose/ui/Modifier;JJLandroidx/compose/ui/text/font/FontStyle;Landroidx/compose/ui/text/font/FontWeight;Landroidx/compose/ui/text/font/FontFamily;JLandroidx/compose/ui/text/style/TextDecoration;Landroidx/compose/ui/text/style/TextAlign;JIZIILkotlin/jvm/functions/Function1;Landroidx/compose/ui/text/TextStyle;Landroidx/compose/runtime/Composer;III)V",
                        Defaults = typeof(TextDefault))]
                    public static partial void Text(string text, IModifier? modifier, IComposer composer);
                }
            }
            """;

        var (_, diags, emitted) = Run(code);
        Assert.Empty(diags.Where(d => d.Severity == DiagnosticSeverity.Error));
        Assert.NotNull(emitted);
        Assert.Contains("NewString(text)", emitted);
        Assert.Contains("DeleteLocalRef(__ref_text)", emitted);
        Assert.Contains("__ref_text", emitted);
    }

    [Fact]
    public void CallerProvidesDefaults_SuppressesAutoMask()
    {
        var code = """
            using AndroidX.Compose.Runtime;
            using ComposeNet;
            using Kotlin.Jvm.Functions;

            [assembly: ComposeDefaults("AlertDialogDefault",
                "!onDismissRequest", "!confirmButton", "modifier", "dismissButton",
                "icon", "title", "text", "shape", "containerColor", "iconContentColor",
                "titleContentColor", "textContentColor", "tonalElevation", "properties")]

            namespace ComposeNet
            {
                public static partial class ComposeBridges
                {
                    [ComposeBridge(
                        Class = "androidx/compose/material3/AndroidAlertDialog_androidKt",
                        JvmName = "AlertDialog-Oix01E0",
                        Signature = "(Lkotlin/jvm/functions/Function0;Lkotlin/jvm/functions/Function2;Landroidx/compose/ui/Modifier;Lkotlin/jvm/functions/Function2;Lkotlin/jvm/functions/Function2;Lkotlin/jvm/functions/Function2;Lkotlin/jvm/functions/Function2;Landroidx/compose/ui/graphics/Shape;JJJJFLandroidx/compose/ui/window/DialogProperties;Landroidx/compose/runtime/Composer;III)V",
                        Defaults = typeof(AlertDialogDefault))]
                    public static partial void AlertDialog(
                        IFunction0 onDismissRequest, IFunction2 confirmButton,
                        IModifier? modifier, IFunction2? dismissButton, IFunction2? icon,
                        IFunction2? title, IFunction2? text, int defaults, IComposer composer);
                }
            }
            """;

        var (_, diags, emitted) = Run(code);
        Assert.Empty(diags.Where(d => d.Severity == DiagnosticSeverity.Error));
        Assert.NotNull(emitted);
        Assert.DoesNotContain("(int)global::ComposeNet.AlertDialogDefault.All", emitted);
        Assert.Contains("args[", emitted);
        Assert.Contains("] = new global::Android.Runtime.JValue(defaults)", emitted);
    }

    [Fact]
    public void ExtensionReceiver_PlacedAtArgZero()
    {
        var code = """
            using AndroidX.Compose.Runtime;
            using ComposeNet;
            using Kotlin.Jvm.Functions;

            [assembly: ComposeDefaults("NavigationBarItemDefault",
                "!selected", "!onClick", "!icon", "modifier", "enabled", "label",
                "alwaysShowLabel", "colors", "interactionSource")]

            namespace ComposeNet
            {
                public static partial class ComposeBridges
                {
                    [ComposeBridge(
                        Class = "androidx/compose/material3/NavigationBarKt",
                        JvmName = "NavigationBarItem",
                        Signature = "(Landroidx/compose/foundation/layout/RowScope;ZLkotlin/jvm/functions/Function0;Lkotlin/jvm/functions/Function2;Landroidx/compose/ui/Modifier;ZLkotlin/jvm/functions/Function2;ZLandroidx/compose/material3/NavigationBarItemColors;Landroidx/compose/foundation/interaction/MutableInteractionSource;Landroidx/compose/runtime/Composer;II)V",
                        Defaults = typeof(NavigationBarItemDefault))]
                    public static partial void NavigationBarItem(
                        System.IntPtr rowScope, bool selected, IFunction0 onClick, IFunction2 icon,
                        IModifier? modifier, IFunction2? label, int defaults, IComposer composer);
                }
            }
            """;

        var (_, diags, emitted) = Run(code);
        Assert.Empty(diags.Where(d => d.Severity == DiagnosticSeverity.Error));
        Assert.NotNull(emitted);
        Assert.Contains("args[0] = new global::Android.Runtime.JValue(rowScope)", emitted);
        Assert.Contains("args[1] = new global::Android.Runtime.JValue(selected)", emitted);
    }

    [Fact]
    public void MalformedSignature_ReportsCN2004()
    {
        var code = """
            using AndroidX.Compose.Runtime;
            using ComposeNet;
            using Kotlin.Jvm.Functions;

            [assembly: ComposeDefaults("FooDefault", "modifier")]

            namespace ComposeNet
            {
                public static partial class ComposeBridges
                {
                    [ComposeBridge(
                        Class = "x/Y",
                        JvmName = "Foo",
                        Signature = "garbage",
                        Defaults = typeof(FooDefault))]
                    public static partial void Foo(IModifier? modifier, IComposer composer);
                }
            }
            """;

        var (_, diags, _) = Run(code);
        Assert.Contains(diags, d => d.Id == "CN2004");
    }

    [Fact]
    public void UnknownParameter_ReportsCN2003()
    {
        var code = """
            using AndroidX.Compose.Runtime;
            using ComposeNet;
            using Kotlin.Jvm.Functions;

            [assembly: ComposeDefaults("FooDefault", "modifier")]

            namespace ComposeNet
            {
                public static partial class ComposeBridges
                {
                    [ComposeBridge(
                        Class = "x/Y",
                        JvmName = "Foo",
                        Signature = "(Landroidx/compose/ui/Modifier;Landroidx/compose/runtime/Composer;II)V",
                        Defaults = typeof(FooDefault))]
                    public static partial void Foo(IModifier? wrongName, IComposer composer);
                }
            }
            """;

        var (_, diags, _) = Run(code);
        Assert.Contains(diags, d => d.Id == "CN2003");
    }

    [Fact]
    public void NonVoidReturn_EmitsCallStaticObjectMethodReturn()
    {
        var code = """
            using AndroidX.Compose.Runtime;
            using ComposeNet;
            using Kotlin.Jvm.Functions;

            [assembly: ComposeDefaults("RememberDatePickerStateDefault",
                "initialSelectedDateMillis", "initialDisplayedMonthMillis", "yearRange",
                "initialDisplayMode", "selectableDates")]

            namespace ComposeNet
            {
                public static partial class ComposeBridges
                {
                    [ComposeBridge(
                        Class = "androidx/compose/material3/DatePickerKt",
                        JvmName = "rememberDatePickerState-EU0dCGE",
                        Signature = "(Ljava/lang/Long;Ljava/lang/Long;Lkotlin/ranges/IntRange;ILandroidx/compose/material3/SelectableDates;Landroidx/compose/runtime/Composer;II)Landroidx/compose/material3/DatePickerState;",
                        Defaults = typeof(RememberDatePickerStateDefault))]
                    public static partial System.IntPtr RememberDatePickerState(int defaults, IComposer composer);
                }
            }
            """;

        var (output, diags, emitted) = Run(code);
        Assert.Empty(diags.Where(d => d.Severity == DiagnosticSeverity.Error));
        Assert.NotNull(emitted);
        Assert.Contains("return global::Android.Runtime.JNIEnv.CallStaticObjectMethod(", emitted);

        var errors = output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
        Assert.Empty(errors);
    }

    [Fact]
    public void NoDefaults_RequiredOnlyParameters_OmitsBitmaskAndDefaultSlot()
    {
        // Mirrors androidx.compose.ui.res.PainterResources_androidKt.painterResource —
        // a @Composable with one required `int id` parameter, no defaultable
        // params, so the Kotlin codegen emits only $changed (no $default).
        var code = """
            using AndroidX.Compose.Runtime;
            using ComposeNet;

            namespace ComposeNet
            {
                public static partial class ComposeBridges
                {
                    [ComposeBridge(
                        Class = "androidx/compose/ui/res/PainterResources_androidKt",
                        JvmName = "painterResource",
                        Signature = "(ILandroidx/compose/runtime/Composer;I)Landroidx/compose/ui/graphics/painter/Painter;")]
                    public static partial System.IntPtr PainterResource(int id, IComposer composer);
                }
            }
            """;

        var (output, diags, emitted) = Run(code);
        Assert.Empty(diags.Where(d => d.Severity == DiagnosticSeverity.Error));
        Assert.NotNull(emitted);
        Assert.Contains("FindClass(\"androidx/compose/ui/res/PainterResources_androidKt\")", emitted);
        Assert.Contains("GetStaticMethodID(s_PainterResource_class, \"painterResource\"", emitted);

        // No bitmask: no `int defaults = ...All`, no $default JValue, no enum reference.
        Assert.DoesNotContain(".All;", emitted);
        Assert.DoesNotContain("new global::Android.Runtime.JValue(defaults)", emitted);

        // The user param goes into slot 0; composer into slot 1; one $changed at slot 2; no further slots.
        Assert.Contains("global::Android.Runtime.JValue[3]", emitted);
        Assert.Contains("args[0] = new global::Android.Runtime.JValue(id)", emitted);
        Assert.Contains("args[2] = new global::Android.Runtime.JValue(0);", emitted);
        Assert.DoesNotContain("args[3]", emitted);

        // Non-void returns are still wrapped in try/finally.
        Assert.Contains("return global::Android.Runtime.JNIEnv.CallStaticObjectMethod(", emitted);
        Assert.Contains("global::System.GC.KeepAlive(composer);", emitted);

        var errors = output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
        Assert.Empty(errors);
    }

    [Fact]
    public void InstanceField_EmitsGlobalRefAndCallObjectMethod()
    {
        var code = """
            using AndroidX.Compose.Runtime;
            using ComposeNet;
            using Kotlin.Jvm.Functions;

            [assembly: ComposeDefaults("RememberPlainTooltipPositionProviderDefault", "spacing")]

            namespace ComposeNet
            {
                public static partial class ComposeBridges
                {
                    [ComposeBridge(
                        Class = "androidx/compose/material3/TooltipDefaults",
                        InstanceField = "INSTANCE",
                        JvmName = "rememberPlainTooltipPositionProvider-kHDZbjc",
                        Signature = "(FLandroidx/compose/runtime/Composer;II)Landroidx/compose/ui/window/PopupPositionProvider;",
                        Defaults = typeof(RememberPlainTooltipPositionProviderDefault))]
                    public static partial System.IntPtr RememberPlainTooltipPositionProvider(int defaults, IComposer composer);
                }
            }
            """;

        var (output, diags, emitted) = Run(code);
        Assert.Empty(diags.Where(d => d.Severity == DiagnosticSeverity.Error));
        Assert.NotNull(emitted);
        Assert.Contains("GetStaticFieldID(", emitted);
        Assert.Contains("NewGlobalRef(__local)", emitted);
        Assert.Contains("DeleteLocalRef(__local)", emitted);
        Assert.Contains("GetMethodID(", emitted);
        Assert.Contains("return global::Android.Runtime.JNIEnv.CallObjectMethod(s_RememberPlainTooltipPositionProvider_instance,", emitted);

        var errors = output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
        Assert.Empty(errors);
    }
}
