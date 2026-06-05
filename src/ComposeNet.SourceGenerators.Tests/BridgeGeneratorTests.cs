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
                public static unsafe System.IntPtr NewObject(System.IntPtr cls, System.IntPtr m, Android.Runtime.JValue* args) => default;
            }
            public enum JniHandleOwnership { TransferLocalRef = 0 }
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
        namespace Java.Lang
        {
            public class Object
            {
                public System.IntPtr Handle => default;
                public static T? GetObject<T>(System.IntPtr handle, Android.Runtime.JniHandleOwnership transfer) where T : class => default;
            }
        }
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
            public readonly record struct Dp(float Value)
            {
                public static float Pack(Dp? value) => value?.Value ?? 0f;
            }
            public readonly record struct Sp(float Value)
            {
                public static long Pack(Sp? value) => default;
            }
            public readonly record struct Em(float Value)
            {
                public static long Pack(Em? value) => default;
            }
            public readonly record struct TextAlign(int Value)
            {
                public static int Pack(TextAlign? value) => value?.Value ?? 0;
            }
            public sealed class Shape : Java.Lang.Object { }
            public sealed class FontWeight : Java.Lang.Object { }
            public sealed class TextDecoration : Java.Lang.Object { }
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
    public void DefaultsOmittedButSignatureHasDefault_ReportsCN2005()
    {
        // Signature has a $default slot (2 trailing Is for 1 user param)
        // but [ComposeBridge] omits Defaults. Without validation the old
        // generator would have treated $default as another $changed slot.
        var code = """
            using AndroidX.Compose.Runtime;
            using ComposeNet;

            namespace ComposeNet
            {
                public static partial class ComposeBridges
                {
                    [ComposeBridge(
                        Class = "x/Y",
                        JvmName = "Foo",
                        Signature = "(ZLandroidx/compose/runtime/Composer;II)V")]
                    public static partial void Foo(bool flag, IComposer composer);
                }
            }
            """;

        var (_, diags, _) = Run(code);
        Assert.Contains(diags, d => d.Id == "CN2005");
    }

    [Fact]
    public void DefaultsProvidedButSignatureHasNoDefault_ReportsCN2005()
    {
        // Signature has only $changed (1 trailing I for 1 user param) so
        // [ComposeBridge] must not specify Defaults.
        var code = """
            using AndroidX.Compose.Runtime;
            using ComposeNet;

            [assembly: ComposeDefaults("FooDefault", "id")]

            namespace ComposeNet
            {
                public static partial class ComposeBridges
                {
                    [ComposeBridge(
                        Class = "x/Y",
                        JvmName = "Foo",
                        Signature = "(ILandroidx/compose/runtime/Composer;I)V",
                        Defaults = typeof(FooDefault))]
                    public static partial void Foo(int id, IComposer composer);
                }
            }
            """;

        var (_, diags, _) = Run(code);
        Assert.Contains(diags, d => d.Id == "CN2005");
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

    [Fact]
    public void ReservedKeywordParam_IsAtSignEscaped()
    {
        // Kotlin-side parameter named `checked` collides with a C# reserved
        // keyword. The generator must emit `bool @checked` in the partial
        // method signature and reference `@checked` in the body so the result
        // compiles.
        var code = """
            using AndroidX.Compose.Runtime;
            using ComposeNet;
            using Kotlin.Jvm.Functions;

            [assembly: ComposeDefaults("CheckedThingDefault",
                "!checked", "modifier")]

            namespace ComposeNet
            {
                public static partial class ComposeBridges
                {
                    [ComposeBridge(
                        Class = "x/Y",
                        JvmName = "Foo",
                        Signature = "(ZLandroidx/compose/ui/Modifier;Landroidx/compose/runtime/Composer;II)V",
                        Defaults = typeof(CheckedThingDefault))]
                    public static partial void Foo(bool @checked, IModifier? modifier, IComposer composer);
                }
            }
            """;

        var (output, diags, emitted) = Run(code);
        Assert.Empty(diags.Where(d => d.Severity == DiagnosticSeverity.Error));
        Assert.NotNull(emitted);
        Assert.Contains("bool @checked", emitted);
        Assert.Contains("new global::Android.Runtime.JValue(@checked)", emitted);

        var errors = output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
        Assert.Empty(errors);
    }

    [Fact]
    public void ExtensionWithDefault_BackgroundShape_OneReceiverPrimitiveAndNullableRef()
    {
        // Mirrors androidx.compose.foundation.BackgroundKt.background-bw27NRU$default —
        // a non-@Composable Modifier extension. Tail is `I L<marker>`,
        // no Composer. The marker is always passed null.
        var code = """
            using ComposeNet;

            [assembly: ComposeDefaults("ModifierBackgroundDefault", "!color", "shape")]

            namespace ComposeNet
            {
                public static partial class ComposeBridges
                {
                    [ComposeBridge(
                        Class = "androidx/compose/foundation/BackgroundKt",
                        JvmName = "background-bw27NRU$default",
                        Signature = "(Landroidx/compose/ui/Modifier;JLandroidx/compose/ui/graphics/Shape;ILjava/lang/Object;)Landroidx/compose/ui/Modifier;",
                        Defaults = typeof(ModifierBackgroundDefault))]
                    public static partial System.IntPtr ModifierBackground(System.IntPtr modifier, long color, System.IntPtr? shape);
                }
            }
            """;

        var (output, diags, emitted) = Run(code);
        Assert.Empty(diags.Where(d => d.Severity == DiagnosticSeverity.Error));
        Assert.NotNull(emitted);
        Assert.Contains("FindClass(\"androidx/compose/foundation/BackgroundKt\")", emitted);
        Assert.Contains("\"background-bw27NRU$default\"", emitted);

        // Receiver at args[0], color at args[1], shape at args[2], $default at args[3], marker at args[4].
        Assert.Contains("global::Android.Runtime.JValue[5]", emitted);
        Assert.Contains("args[0] = new global::Android.Runtime.JValue(modifier)", emitted);
        Assert.Contains("args[1] = new global::Android.Runtime.JValue(color)", emitted);
        Assert.Contains("args[3] = new global::Android.Runtime.JValue(defaults)", emitted);
        Assert.Contains("args[4] = new global::Android.Runtime.JValue(global::System.IntPtr.Zero)", emitted);
        Assert.DoesNotContain("args[5]", emitted);

        // No Composer arg, no $changed, no GC.KeepAlive(composer).
        Assert.DoesNotContain("Composer", emitted);
        Assert.DoesNotContain("KeepAlive(composer)", emitted);

        // Auto-mask still emitted: shape is a nullable bit.
        Assert.Contains("(int)global::ComposeNet.ModifierBackgroundDefault.All", emitted);
        Assert.Contains("ModifierBackgroundDefault.Shape", emitted);
        // IntPtr? is treated as nullable: bit is cleared only when the
        // value is non-null. Otherwise Kotlin sees IntPtr.Zero as a
        // user-supplied value and never falls back to its default.
        Assert.Contains("if (shape is not null) defaults &= ~(int)global::ComposeNet.ModifierBackgroundDefault.Shape", emitted);

        // Non-void return.
        Assert.Contains("return global::Android.Runtime.JNIEnv.CallStaticObjectMethod(", emitted);

        var errors = output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
        Assert.Empty(errors);
    }

    [Fact]
    public void ExtensionWithDefault_BorderShape_TwoPrimitivesAndNullableRef()
    {
        // Mirrors androidx.compose.foundation.BorderKt.border-xT4_qwU$default —
        // Modifier extension with width (F), color (J), shape (Shape).
        var code = """
            using ComposeNet;

            [assembly: ComposeDefaults("ModifierBorderDefault", "!width", "!color", "shape")]

            namespace ComposeNet
            {
                public static partial class ComposeBridges
                {
                    [ComposeBridge(
                        Class = "androidx/compose/foundation/BorderKt",
                        JvmName = "border-xT4_qwU$default",
                        Signature = "(Landroidx/compose/ui/Modifier;FJLandroidx/compose/ui/graphics/Shape;ILjava/lang/Object;)Landroidx/compose/ui/Modifier;",
                        Defaults = typeof(ModifierBorderDefault))]
                    public static partial System.IntPtr ModifierBorder(System.IntPtr modifier, float width, long color, System.IntPtr? shape);
                }
            }
            """;

        var (output, diags, emitted) = Run(code);
        Assert.Empty(diags.Where(d => d.Severity == DiagnosticSeverity.Error));
        Assert.NotNull(emitted);

        // Receiver, width, color, shape, $default, marker = 6 slots.
        Assert.Contains("global::Android.Runtime.JValue[6]", emitted);
        Assert.Contains("args[0] = new global::Android.Runtime.JValue(modifier)", emitted);
        Assert.Contains("args[1] = new global::Android.Runtime.JValue(width)", emitted);
        Assert.Contains("args[2] = new global::Android.Runtime.JValue(color)", emitted);
        Assert.Contains("args[4] = new global::Android.Runtime.JValue(defaults)", emitted);
        Assert.Contains("args[5] = new global::Android.Runtime.JValue(global::System.IntPtr.Zero)", emitted);

        var errors = output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
        Assert.Empty(errors);
    }

    [Fact]
    public void ExtensionWithDefault_ClickableShape_PrimitiveStringRefAndRequiredFunction()
    {
        // Mirrors androidx.compose.foundation.ClickableKt.clickable-XHw0xAI$default —
        // Modifier extension: enabled (Z), onClickLabel (String, nullable),
        // role (Role, nullable), onClick (Function0, required).
        var code = """
            using ComposeNet;
            using Kotlin.Jvm.Functions;

            [assembly: ComposeDefaults("ModifierClickableDefault",
                "enabled", "onClickLabel", "role", "!onClick")]

            namespace ComposeNet
            {
                public static partial class ComposeBridges
                {
                    [ComposeBridge(
                        Class = "androidx/compose/foundation/ClickableKt",
                        JvmName = "clickable-XHw0xAI$default",
                        Signature = "(Landroidx/compose/ui/Modifier;ZLjava/lang/String;Landroidx/compose/ui/semantics/Role;Lkotlin/jvm/functions/Function0;ILjava/lang/Object;)Landroidx/compose/ui/Modifier;",
                        Defaults = typeof(ModifierClickableDefault))]
                    public static partial System.IntPtr ModifierClickable(
                        System.IntPtr modifier, bool enabled, string? onClickLabel,
                        System.IntPtr? role, IFunction0 onClick);
                }
            }
            """;

        var (output, diags, emitted) = Run(code);
        Assert.Empty(diags.Where(d => d.Severity == DiagnosticSeverity.Error));
        Assert.NotNull(emitted);

        // Receiver, enabled, onClickLabel, role, onClick, $default, marker = 7.
        Assert.Contains("global::Android.Runtime.JValue[7]", emitted);
        Assert.Contains("args[0] = new global::Android.Runtime.JValue(modifier)", emitted);
        Assert.Contains("args[1] = new global::Android.Runtime.JValue(enabled)", emitted);
        Assert.Contains("args[5] = new global::Android.Runtime.JValue(defaults)", emitted);
        Assert.Contains("args[6] = new global::Android.Runtime.JValue(global::System.IntPtr.Zero)", emitted);

        // String parameter hoisted into NewString/__ref.
        Assert.Contains("NewString(onClickLabel)", emitted);
        Assert.Contains("__ref_onClickLabel", emitted);
        Assert.Contains("DeleteLocalRef(__ref_onClickLabel)", emitted);

        // onClick (required Function0) gets KeepAlive; composer not present.
        Assert.Contains("global::System.GC.KeepAlive(onClick);", emitted);
        Assert.DoesNotContain("KeepAlive(composer)", emitted);

        // No Composer in emitted output at all.
        Assert.DoesNotContain("Composer", emitted);

        var errors = output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
        Assert.Empty(errors);
    }

    [Fact]
    public void ExtensionWithDefault_CallerProvidesDefaults_SuppressesAutoMask()
    {
        // Same Background shape but the C# stub takes `int defaults` so
        // the auto-mask logic is suppressed.
        var code = """
            using ComposeNet;

            [assembly: ComposeDefaults("ModifierBackgroundDefault", "!color", "shape")]

            namespace ComposeNet
            {
                public static partial class ComposeBridges
                {
                    [ComposeBridge(
                        Class = "androidx/compose/foundation/BackgroundKt",
                        JvmName = "background-bw27NRU$default",
                        Signature = "(Landroidx/compose/ui/Modifier;JLandroidx/compose/ui/graphics/Shape;ILjava/lang/Object;)Landroidx/compose/ui/Modifier;",
                        Defaults = typeof(ModifierBackgroundDefault))]
                    public static partial System.IntPtr ModifierBackground(
                        System.IntPtr modifier, long color, System.IntPtr shape, int defaults);
                }
            }
            """;

        var (_, diags, emitted) = Run(code);
        Assert.Empty(diags.Where(d => d.Severity == DiagnosticSeverity.Error));
        Assert.NotNull(emitted);
        Assert.DoesNotContain("(int)global::ComposeNet.ModifierBackgroundDefault.All", emitted);
        Assert.Contains("args[3] = new global::Android.Runtime.JValue(defaults)", emitted);
        Assert.Contains("args[4] = new global::Android.Runtime.JValue(global::System.IntPtr.Zero)", emitted);
    }

    [Fact]
    public void PlainStatic_ReceiverPlusPrimitive()
    {
        // Mirrors androidx.compose.foundation.layout.SizeKt.fillMaxWidth —
        // plain Kotlin static extension on Modifier, no Composer, no $default.
        var code = """
            using ComposeNet;

            namespace ComposeNet
            {
                public static partial class ComposeBridges
                {
                    [ComposeBridge(
                        Class = "androidx/compose/foundation/layout/SizeKt",
                        JvmName = "fillMaxWidth",
                        Signature = "(Landroidx/compose/ui/Modifier;F)Landroidx/compose/ui/Modifier;")]
                    public static partial System.IntPtr ModifierFillMaxWidth(System.IntPtr modifier, float fraction);
                }
            }
            """;

        var (output, diags, emitted) = Run(code);
        Assert.Empty(diags.Where(d => d.Severity == DiagnosticSeverity.Error));
        Assert.NotNull(emitted);
        Assert.Contains("FindClass(\"androidx/compose/foundation/layout/SizeKt\")", emitted);
        Assert.Contains("GetStaticMethodID(s_ModifierFillMaxWidth_class, \"fillMaxWidth\"", emitted);

        // Receiver at args[0], fraction at args[1] — 2 slots total.
        Assert.Contains("global::Android.Runtime.JValue[2]", emitted);
        Assert.Contains("args[0] = new global::Android.Runtime.JValue(modifier)", emitted);
        Assert.Contains("args[1] = new global::Android.Runtime.JValue(fraction)", emitted);
        Assert.DoesNotContain("args[2]", emitted);

        // No bitmask, no $default slot, no marker, no Composer.
        Assert.DoesNotContain(".All;", emitted);
        Assert.DoesNotContain("new global::Android.Runtime.JValue(defaults)", emitted);
        Assert.DoesNotContain("new global::Android.Runtime.JValue(global::System.IntPtr.Zero)", emitted);
        Assert.DoesNotContain("Composer", emitted);
        Assert.DoesNotContain("KeepAlive(composer)", emitted);

        Assert.Contains("return global::Android.Runtime.JNIEnv.CallStaticObjectMethod(", emitted);

        var errors = output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
        Assert.Empty(errors);
    }

    [Fact]
    public void PlainStatic_ReceiverOnly()
    {
        // Mirrors androidx.compose.foundation.layout.WindowInsetsPadding_androidKt.safeDrawingPadding —
        // Modifier extension with no other parameters.
        var code = """
            using ComposeNet;

            namespace ComposeNet
            {
                public static partial class ComposeBridges
                {
                    [ComposeBridge(
                        Class = "androidx/compose/foundation/layout/WindowInsetsPadding_androidKt",
                        JvmName = "safeDrawingPadding",
                        Signature = "(Landroidx/compose/ui/Modifier;)Landroidx/compose/ui/Modifier;")]
                    public static partial System.IntPtr ModifierSafeDrawingPadding(System.IntPtr modifier);
                }
            }
            """;

        var (output, diags, emitted) = Run(code);
        Assert.Empty(diags.Where(d => d.Severity == DiagnosticSeverity.Error));
        Assert.NotNull(emitted);
        Assert.Contains("global::Android.Runtime.JValue[1]", emitted);
        Assert.Contains("args[0] = new global::Android.Runtime.JValue(modifier)", emitted);
        Assert.DoesNotContain("args[1]", emitted);
        Assert.DoesNotContain("Composer", emitted);
        Assert.DoesNotContain("new global::Android.Runtime.JValue(defaults)", emitted);
        Assert.Contains("return global::Android.Runtime.JNIEnv.CallStaticObjectMethod(", emitted);

        var errors = output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
        Assert.Empty(errors);
    }

    [Fact]
    public void PlainStatic_FourPrimitives()
    {
        // Mirrors androidx.compose.foundation.layout.PaddingKt.padding-qDBjuR0 —
        // Modifier extension with four Dp values.
        var code = """
            using ComposeNet;

            namespace ComposeNet
            {
                public static partial class ComposeBridges
                {
                    [ComposeBridge(
                        Class = "androidx/compose/foundation/layout/PaddingKt",
                        JvmName = "padding-qDBjuR0",
                        Signature = "(Landroidx/compose/ui/Modifier;FFFF)Landroidx/compose/ui/Modifier;")]
                    public static partial System.IntPtr ModifierPaddingLTRB(
                        System.IntPtr modifier, float start, float top, float end, float bottom);
                }
            }
            """;

        var (output, diags, emitted) = Run(code);
        Assert.Empty(diags.Where(d => d.Severity == DiagnosticSeverity.Error));
        Assert.NotNull(emitted);
        Assert.Contains("\"padding-qDBjuR0\"", emitted);
        Assert.Contains("global::Android.Runtime.JValue[5]", emitted);
        Assert.Contains("args[0] = new global::Android.Runtime.JValue(modifier)", emitted);
        Assert.Contains("args[1] = new global::Android.Runtime.JValue(start)", emitted);
        Assert.Contains("args[2] = new global::Android.Runtime.JValue(top)", emitted);
        Assert.Contains("args[3] = new global::Android.Runtime.JValue(end)", emitted);
        Assert.Contains("args[4] = new global::Android.Runtime.JValue(bottom)", emitted);
        Assert.DoesNotContain("args[5]", emitted);
        Assert.DoesNotContain("Composer", emitted);

        var errors = output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
        Assert.Empty(errors);
    }

    [Fact]
    public void PlainStatic_NoReceiver_PrimitiveInObjectOut()
    {
        // Mirrors androidx.compose.foundation.shape.RoundedCornerShapeKt.RoundedCornerShape —
        // plain static call with no leading receiver, returns an object.
        var code = """
            using ComposeNet;

            namespace ComposeNet
            {
                public static partial class ComposeBridges
                {
                    [ComposeBridge(
                        Class = "androidx/compose/foundation/shape/RoundedCornerShapeKt",
                        JvmName = "RoundedCornerShape-0680j_4",
                        Signature = "(F)Landroidx/compose/foundation/shape/RoundedCornerShape;")]
                    public static partial System.IntPtr RoundedCornerShape(float dp);
                }
            }
            """;

        var (output, diags, emitted) = Run(code);
        Assert.Empty(diags.Where(d => d.Severity == DiagnosticSeverity.Error));
        Assert.NotNull(emitted);
        Assert.Contains("FindClass(\"androidx/compose/foundation/shape/RoundedCornerShapeKt\")", emitted);
        Assert.Contains("\"RoundedCornerShape-0680j_4\"", emitted);

        // No receiver — dp goes into args[0].
        Assert.Contains("global::Android.Runtime.JValue[1]", emitted);
        Assert.Contains("args[0] = new global::Android.Runtime.JValue(dp)", emitted);
        Assert.DoesNotContain("args[1]", emitted);

        // No Composer, no $default, no marker.
        Assert.DoesNotContain("Composer", emitted);
        Assert.DoesNotContain("new global::Android.Runtime.JValue(defaults)", emitted);
        Assert.DoesNotContain("new global::Android.Runtime.JValue(global::System.IntPtr.Zero)", emitted);

        Assert.Contains("return global::Android.Runtime.JNIEnv.CallStaticObjectMethod(", emitted);

        var errors = output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
        Assert.Empty(errors);
    }

    [Fact]
    public void PlainStatic_DefaultsSpecified_ReportsCN2005()
    {
        // Plain-static shape rejects Defaults via the existing CN2005
        // "signature has no $default slot" check.
        var code = """
            using ComposeNet;

            [assembly: ComposeDefaults("FooDefault", "dp")]

            namespace ComposeNet
            {
                public static partial class ComposeBridges
                {
                    [ComposeBridge(
                        Class = "x/Y",
                        JvmName = "Foo",
                        Signature = "(F)Landroidx/compose/foundation/shape/RoundedCornerShape;",
                        Defaults = typeof(FooDefault))]
                    public static partial System.IntPtr Foo(float dp);
                }
            }
            """;

        var (_, diags, _) = Run(code);
        Assert.Contains(diags, d => d.Id == "CN2005");
    }

    [Fact]
    public void Constructor_GeneratesNewObjectAndGetObjectWrap()
    {
        // Mirrors androidx.compose.foundation.lazy.grid.GridCells$Adaptive(Dp) —
        // a stripped Kotlin ctor whose single Dp parameter compiles down to F.
        var code = """
            using ComposeNet;

            namespace ComposeNet
            {
                public interface IGridCells { }
                public class GridCellsAdaptiveImpl : global::Java.Lang.Object, IGridCells { }

                public static partial class ComposeBridges
                {
                    [ComposeBridge(
                        Class = "androidx/compose/foundation/lazy/grid/GridCells$Adaptive",
                        JvmName = "<init>",
                        Signature = "(F)V")]
                    internal static partial GridCellsAdaptiveImpl GridCellsAdaptive(float minSizeDp);
                }
            }
            """;

        var (output, diags, emitted) = Run(code);
        Assert.Empty(diags.Where(d => d.Severity == DiagnosticSeverity.Error));
        Assert.NotNull(emitted);

        // Class lookup unchanged; method ID uses GetMethodID with "<init>".
        Assert.Contains("FindClass(\"androidx/compose/foundation/lazy/grid/GridCells$Adaptive\")", emitted);
        Assert.Contains("GetMethodID(", emitted);
        Assert.Contains("\"<init>\"", emitted);
        Assert.DoesNotContain("GetStaticMethodID", emitted);

        // Single arg slot for the Dp value; no Composer, no $default.
        Assert.Contains("global::Android.Runtime.JValue[1]", emitted);
        Assert.Contains("args[0] = new global::Android.Runtime.JValue(minSizeDp)", emitted);
        Assert.DoesNotContain("args[1]", emitted);
        Assert.DoesNotContain("Composer", emitted);
        Assert.DoesNotContain("new global::Android.Runtime.JValue(defaults)", emitted);

        // NewObject + GetObject<T> wrap with TransferLocalRef.
        Assert.Contains("global::Android.Runtime.JNIEnv.NewObject(", emitted);
        Assert.Contains("global::Java.Lang.Object.GetObject<global::ComposeNet.GridCellsAdaptiveImpl>(", emitted);
        Assert.Contains("global::Android.Runtime.JniHandleOwnership.TransferLocalRef", emitted);
        Assert.DoesNotContain("CallStaticObjectMethod", emitted);
        Assert.DoesNotContain("CallStaticVoidMethod", emitted);

        var errors = output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
        Assert.Empty(errors);
    }

    [Fact]
    public void Constructor_VoidReturn_ReportsCN2006()
    {
        var code = """
            using ComposeNet;

            namespace ComposeNet
            {
                public static partial class ComposeBridges
                {
                    [ComposeBridge(Class = "x/Y", JvmName = "<init>", Signature = "(F)V")]
                    internal static partial void Bad(float v);
                }
            }
            """;
        var (_, diags, _) = Run(code);
        Assert.Contains(diags, d => d.Id == "CN2006");
    }

    [Fact]
    public void Constructor_WithComposer_ReportsCN2006()
    {
        var code = """
            using ComposeNet;

            namespace ComposeNet
            {
                public class Thing : global::Java.Lang.Object { }
                public static partial class ComposeBridges
                {
                    [ComposeBridge(Class = "x/Y", JvmName = "<init>", Signature = "(F)V")]
                    internal static partial Thing Bad(float v, global::AndroidX.Compose.Runtime.IComposer composer);
                }
            }
            """;
        var (_, diags, _) = Run(code);
        Assert.Contains(diags, d => d.Id == "CN2006");
    }

    [Fact]
    public void Constructor_WithDefaults_ReportsCN2006()
    {
        var code = """
            using ComposeNet;

            [assembly: ComposeDefaults("ThingDefault", "v")]

            namespace ComposeNet
            {
                public class Thing : global::Java.Lang.Object { }
                public static partial class ComposeBridges
                {
                    [ComposeBridge(Class = "x/Y", JvmName = "<init>", Signature = "(F)V", Defaults = typeof(ThingDefault))]
                    internal static partial Thing Bad(float v);
                }
            }
            """;
        var (_, diags, _) = Run(code);
        Assert.Contains(diags, d => d.Id == "CN2006");
    }

    [Fact]
    public void Constructor_NonVoidSignature_ReportsCN2006()
    {
        // JVM ctors must return V at the bytecode level.
        var code = """
            using ComposeNet;

            namespace ComposeNet
            {
                public class Thing : global::Java.Lang.Object { }
                public static partial class ComposeBridges
                {
                    [ComposeBridge(Class = "x/Y", JvmName = "<init>", Signature = "(F)Lx/Y;")]
                    internal static partial Thing Bad(float v);
                }
            }
            """;
        var (_, diags, _) = Run(code);
        Assert.Contains(diags, d => d.Id == "CN2006");
    }

    // ---------- Compose value type recognition (Dp/Sp/Em/TextAlign) ----------

    [Fact]
    public void ValueType_Dp_LowersToPackHelper()
    {
        // Border-shaped bridge: (Modifier, Dp width, long color, Shape).
        // Color is a Compose `@JvmInline value class Color(val value: ULong)`
        // and the binding already surfaces it as a `long`, so it stays
        // a raw `long` slot — only Dp and Shape go through the value-type
        // / nullable-ref lowering.
        var code = """
            using ComposeNet;

            [assembly: ComposeDefaults("BorderDefault", "width", "!color", "shape")]

            namespace ComposeNet
            {
                public static partial class ComposeBridges
                {
                    [ComposeBridge(
                        Class = "androidx/compose/foundation/BorderKt",
                        JvmName = "border-xT4_qwU$default",
                        Signature = "(Landroidx/compose/ui/Modifier;FJLandroidx/compose/ui/graphics/Shape;ILjava/lang/Object;)Landroidx/compose/ui/Modifier;",
                        Defaults = typeof(BorderDefault))]
                    public static partial System.IntPtr Border(System.IntPtr modifier, Dp? width, long color, Shape? shape);
                }
            }
            """;

        var (_, diags, emitted) = Run(code);
        Assert.Empty(diags.Where(d => d.Severity == DiagnosticSeverity.Error));
        Assert.NotNull(emitted);
        Assert.Contains("global::ComposeNet.Dp.Pack(width)", emitted);
        // Shape? is a Java.Lang.Object subclass → existing nullable-ref path.
        Assert.Contains("shape is null ? global::System.IntPtr.Zero : ((global::Java.Lang.Object)shape).Handle", emitted);
        // Width and shape get the auto-mask null check; color is '!'d and
        // always passed as a raw long, so no enum member for it.
        Assert.Contains("if (width is not null) defaults &= ~(int)global::ComposeNet.BorderDefault.Width", emitted);
        Assert.DoesNotContain("BorderDefault.Color", emitted);
        Assert.Contains("if (shape is not null) defaults &= ~(int)global::ComposeNet.BorderDefault.Shape", emitted);
    }

    [Fact]
    public void ValueType_SpEmTextAlign_LowerCorrectly()
    {
        // @Composable bridge: (Sp fontSize, Em letterSpacing, TextAlign align, Composer).
        // JNI: 3 user slots (J/J/I) + Composer (L) + 1 $changed (I) + 1 $default (I).
        var code = """
            using ComposeNet;
            using AndroidX.Compose.Runtime;

            [assembly: ComposeDefaults("FontDefault", "fontSize", "letterSpacing", "align")]

            namespace ComposeNet
            {
                public static partial class ComposeBridges
                {
                    [ComposeBridge(
                        Class = "x/Y",
                        JvmName = "f",
                        Signature = "(JJILandroidx/compose/runtime/Composer;II)V",
                        Defaults = typeof(FontDefault))]
                    public static partial void F(Sp? fontSize, Em? letterSpacing, TextAlign? align, IComposer composer);
                }
            }
            """;

        var (_, diags, emitted) = Run(code);
        Assert.Empty(diags.Where(d => d.Severity == DiagnosticSeverity.Error));
        Assert.NotNull(emitted);
        Assert.Contains("global::ComposeNet.Sp.Pack(fontSize)", emitted);
        Assert.Contains("global::ComposeNet.Em.Pack(letterSpacing)", emitted);
        Assert.Contains("global::ComposeNet.TextAlign.Pack(align)", emitted);
    }

    [Fact]
    public void ValueType_OnNoDefaultBridge_ReportsCN2007()
    {
        // No $default slot → the auto-mask logic that backs value
        // types can't fire, so the generator must reject up front.
        var code = """
            using ComposeNet;

            namespace ComposeNet
            {
                public static partial class ComposeBridges
                {
                    [ComposeBridge(
                        Class = "x/Y",
                        JvmName = "f",
                        Signature = "(F)V")]
                    public static partial void F(Dp? width);
                }
            }
            """;

        var (_, diags, _) = Run(code);
        Assert.Contains(diags, d => d.Id == "CN2007");
    }

    [Fact]
    public void RefType_FontWeight_GoesThroughGenericNullableRefPath()
    {
        // FontWeight subclasses Java.Lang.Object — it should hit the
        // existing reference-type code path, not the value-type
        // registry. The auto-mask still clears the bit when non-null.
        // @Composable bridge: (FontWeight, Composer) → 1 user slot + 1
        // $changed + 1 $default.
        var code = """
            using ComposeNet;
            using AndroidX.Compose.Runtime;

            [assembly: ComposeDefaults("WeightedDefault", "weight")]

            namespace ComposeNet
            {
                public static partial class ComposeBridges
                {
                    [ComposeBridge(
                        Class = "x/Y",
                        JvmName = "f",
                        Signature = "(Landroidx/compose/ui/text/font/FontWeight;Landroidx/compose/runtime/Composer;II)V",
                        Defaults = typeof(WeightedDefault))]
                    public static partial void F(FontWeight? weight, IComposer composer);
                }
            }
            """;

        var (_, diags, emitted) = Run(code);
        Assert.Empty(diags.Where(d => d.Severity == DiagnosticSeverity.Error));
        Assert.NotNull(emitted);
        Assert.Contains("weight is null ? global::System.IntPtr.Zero : ((global::Java.Lang.Object)weight).Handle", emitted);
        Assert.Contains("if (weight is not null) defaults &= ~(int)global::ComposeNet.WeightedDefault.Weight", emitted);
    }
}
