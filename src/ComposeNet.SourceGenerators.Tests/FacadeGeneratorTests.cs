using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace ComposeNet.SourceGenerators.Tests;

/// <summary>
/// Tests for <see cref="ComposeFacadeGenerator"/>. The synthetic stubs
/// mirror enough of the Compose / ComposeNet shape (IComposer,
/// IFunctionN, IModifier, ComposableNode, ComposableContainer,
/// ComposableLambda0, ComposableLambdas, ComposeBridges,
/// RenderContext/ScopeKind) for the generated facade source to compile.
/// </summary>
public class FacadeGeneratorTests
{
    const string Stubs = """
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
        namespace Java.Lang
        {
            public class Object
            {
                public System.IntPtr Handle => default;
                public static T? GetObject<T>(System.IntPtr handle, Android.Runtime.JniHandleOwnership transfer) where T : class => default;
            }
            public sealed class Boolean : Object { public bool BooleanValue() => false; }
            public sealed class Float : Object { public float FloatValue() => 0f; }
        }
        namespace AndroidX.Compose.Runtime { public interface IComposer { } }
        namespace AndroidX.Compose.UI { public interface IModifier { } }
        namespace AndroidX.Compose.Material3
        {
            public sealed class ColorScheme
            {
                public long Primary { get; set; }
                public long Surface { get; set; }
                public long SecondaryContainer { get; set; }
            }
            public static class MaterialTheme
            {
                public static MaterialThemeImpl Instance => null!;
            }
            public sealed class MaterialThemeImpl
            {
                public ColorScheme GetColorScheme(AndroidX.Compose.Runtime.IComposer c, int n) => null!;
            }
        }
        namespace Kotlin.Jvm.Functions
        {
            public interface IFunction0 { }
            public interface IFunction1 { }
            public interface IFunction2 { }
            public interface IFunction3 { }
        }
        namespace ComposeNet
        {
            public enum ScopeKind { None, Row, Column }
            public static class RenderContext
            {
                public ref struct ScopeFrame { public void Dispose() { } }
                public static ScopeFrame PushScope(System.IntPtr scope, ScopeKind kind) => default;
                public static System.IntPtr CurrentScope => default;
            }
            public abstract class ComposableNode
            {
                internal abstract void Render(AndroidX.Compose.Runtime.IComposer composer);
                protected AndroidX.Compose.UI.IModifier? BuildModifier() => null;
            }
            public abstract class ComposableContainer : ComposableNode
            {
                protected void RenderChildren(AndroidX.Compose.Runtime.IComposer composer) { }
            }
            public sealed class ComposableLambda0 : Kotlin.Jvm.Functions.IFunction0
            {
                public ComposableLambda0(System.Action body) { }
            }
            public sealed class ComposableLambda1 : Kotlin.Jvm.Functions.IFunction1
            {
                public ComposableLambda1(System.Action<object?> body) { }
            }
            public static class ComposableLambdas
            {
                public static Kotlin.Jvm.Functions.IFunction2 Wrap2(
                    AndroidX.Compose.Runtime.IComposer composer,
                    System.Action<AndroidX.Compose.Runtime.IComposer> body,
                    [System.Runtime.CompilerServices.CallerLineNumber] int line = 0,
                    [System.Runtime.CompilerServices.CallerFilePath] string file = "") => null!;
                public static Kotlin.Jvm.Functions.IFunction3 Wrap3(
                    AndroidX.Compose.Runtime.IComposer composer,
                    System.Action<AndroidX.Compose.Runtime.IComposer> body,
                    [System.Runtime.CompilerServices.CallerLineNumber] int line = 0,
                    [System.Runtime.CompilerServices.CallerFilePath] string file = "") => null!;
                public static Kotlin.Jvm.Functions.IFunction3 Wrap3(
                    AndroidX.Compose.Runtime.IComposer composer,
                    System.Action<System.IntPtr, AndroidX.Compose.Runtime.IComposer> body,
                    [System.Runtime.CompilerServices.CallerLineNumber] int line = 0,
                    [System.Runtime.CompilerServices.CallerFilePath] string file = "") => null!;
            }
            public static partial class ComposeBridges
            {
                public static System.IntPtr ModifierHandle(AndroidX.Compose.UI.IModifier? m) => default;
                public static System.IntPtr PainterResource(int id, AndroidX.Compose.Runtime.IComposer composer) => default;
            }
        }
        """;

    static (Compilation Output, ImmutableArray<Diagnostic> Diags, string? Emitted) Run(string userCode, string facadeName)
    {
        var stubs = CSharpSyntaxTree.ParseText(Stubs);
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
                new ComposeBridgeGenerator(),
                new ComposeFacadeGenerator())
            .RunGeneratorsAndUpdateCompilation(compilation, out var output, out var diags);

        string? emitted = null;
        foreach (var tree in output.SyntaxTrees)
        {
            var path = tree.FilePath;
            if (path.Contains($"ComposeNet.Facade.{facadeName}.") && path.EndsWith(".g.cs", System.StringComparison.Ordinal))
            {
                emitted = tree.GetText().ToString();
                break;
            }
        }
        return (output, diags, emitted);
    }

    const string ButtonAttrs = """
        [assembly: ComposeDefaults("ButtonDefault",
            "!onClick", "modifier", "enabled", "shape", "colors",
            "elevation", "border", "contentPadding", "interactionSource", "!content")]
        """;

    const string ButtonSig = "(Lkotlin/jvm/functions/Function0;Landroidx/compose/ui/Modifier;ZLandroidx/compose/ui/graphics/Shape;Landroidx/compose/material3/ButtonColors;Landroidx/compose/material3/ButtonElevation;Landroidx/compose/foundation/BorderStroke;Landroidx/compose/foundation/layout/PaddingValues;Landroidx/compose/foundation/interaction/MutableInteractionSource;Lkotlin/jvm/functions/Function3;Landroidx/compose/runtime/Composer;II)V";

    [Fact]
    public void Button_GeneratesContainerWithOnClick()
    {
        var code = $$"""
            using AndroidX.Compose.Runtime;
            using AndroidX.Compose.UI;
            using ComposeNet;
            using Kotlin.Jvm.Functions;

            {{ButtonAttrs}}

            namespace ComposeNet
            {
                public static partial class ComposeBridges
                {
                    [ComposeBridge(Class="androidx/compose/material3/ButtonKt", JvmName="Button",
                                   Signature="{{ButtonSig}}", Defaults=typeof(ButtonDefault))]
                    [ComposeFacade]
                    public static partial void Button(IFunction0 onClick, IModifier? modifier,
                                                      IFunction3 content, IComposer composer);
                }
            }
            """;

        var (output, diags, emitted) = Run(code, "Button");
        Assert.Empty(diags.Where(d => d.Severity == DiagnosticSeverity.Error));
        Assert.NotNull(emitted);
        Assert.Contains("public sealed partial class Button : global::ComposeNet.ComposableContainer", emitted);
        Assert.Contains("readonly global::System.Action _onClick;", emitted);
        Assert.Contains("public Button(global::System.Action onClick)", emitted);
        Assert.Contains("var __onClick = new global::ComposeNet.ComposableLambda0(_onClick);", emitted);
        Assert.Contains("var __content = global::ComposeNet.ComposableLambdas.Wrap3(composer, c => RenderChildren(c));", emitted);
        Assert.Contains("global::ComposeNet.ComposeBridges.Button(__onClick, BuildModifier(), __content, composer);", emitted);

        var errors = output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
        Assert.Empty(errors);
    }

    [Fact]
    public void Card_GeneratesContainerNoCtor()
    {
        var code = """
            using AndroidX.Compose.Runtime;
            using AndroidX.Compose.UI;
            using ComposeNet;
            using Kotlin.Jvm.Functions;

            [assembly: ComposeDefaults("CardDefault",
                "modifier", "shape", "colors", "elevation", "border", "!content")]

            namespace ComposeNet
            {
                public static partial class ComposeBridges
                {
                    [ComposeBridge(Class="androidx/compose/material3/CardKt", JvmName="Card",
                                   Signature="(Landroidx/compose/ui/Modifier;Landroidx/compose/ui/graphics/Shape;Landroidx/compose/material3/CardColors;Landroidx/compose/material3/CardElevation;Landroidx/compose/foundation/BorderStroke;Lkotlin/jvm/functions/Function3;Landroidx/compose/runtime/Composer;II)V",
                                   Defaults=typeof(CardDefault))]
                    [ComposeFacade]
                    public static partial void Card(IModifier? modifier, IFunction3 content, IComposer composer);
                }
            }
            """;

        var (output, diags, emitted) = Run(code, "Card");
        Assert.Empty(diags.Where(d => d.Severity == DiagnosticSeverity.Error));
        Assert.NotNull(emitted);
        Assert.Contains("public sealed partial class Card : global::ComposeNet.ComposableContainer", emitted);
        Assert.DoesNotContain("public Card(", emitted);
        Assert.Contains("global::ComposeNet.ComposeBridges.Card(BuildModifier(), __content, composer);", emitted);

        var errors = output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
        Assert.Empty(errors);
    }

    [Fact]
    public void Text_LeafWithPrimitiveCtorParam()
    {
        var code = """
            using AndroidX.Compose.Runtime;
            using AndroidX.Compose.UI;
            using ComposeNet;
            using Kotlin.Jvm.Functions;

            [assembly: ComposeDefaults("TextDefault",
                "!text", "modifier", "color", "fontSize")]

            namespace ComposeNet
            {
                public static partial class ComposeBridges
                {
                    [ComposeBridge(Class="androidx/compose/material3/TextKt", JvmName="Text",
                                   Signature="(Ljava/lang/String;Landroidx/compose/ui/Modifier;JJLandroidx/compose/runtime/Composer;II)V",
                                   Defaults=typeof(TextDefault))]
                    [ComposeFacade]
                    public static partial void Text(string text, IModifier? modifier, IComposer composer);
                }
            }
            """;

        var (output, diags, emitted) = Run(code, "Text");
        Assert.Empty(diags.Where(d => d.Severity == DiagnosticSeverity.Error));
        Assert.NotNull(emitted);
        Assert.Contains("public sealed partial class Text : global::ComposeNet.ComposableNode", emitted);
        Assert.Contains("readonly string _text;", emitted);
        Assert.Contains("public Text(string text)", emitted);
        Assert.Contains("global::ComposeNet.ComposeBridges.Text(_text, BuildModifier(), composer);", emitted);

        var errors = output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
        Assert.Empty(errors);
    }

    [Fact]
    public void Surface_ContainerWithWrap2()
    {
        var code = """
            using AndroidX.Compose.Runtime;
            using AndroidX.Compose.UI;
            using ComposeNet;
            using Kotlin.Jvm.Functions;

            [assembly: ComposeDefaults("SurfaceDefault",
                "modifier", "shape", "color", "contentColor", "tonalElevation",
                "shadowElevation", "border", "!content")]

            namespace ComposeNet
            {
                public static partial class ComposeBridges
                {
                    [ComposeBridge(Class="androidx/compose/material3/SurfaceKt", JvmName="Surface-T9BRK9s",
                                   Signature="(Landroidx/compose/ui/Modifier;Landroidx/compose/ui/graphics/Shape;JJFFLandroidx/compose/foundation/BorderStroke;Lkotlin/jvm/functions/Function2;Landroidx/compose/runtime/Composer;II)V",
                                   Defaults=typeof(SurfaceDefault))]
                    [ComposeFacade]
                    public static partial void Surface(IModifier? modifier, IFunction2 content, IComposer composer);
                }
            }
            """;

        var (output, diags, emitted) = Run(code, "Surface");
        Assert.Empty(diags.Where(d => d.Severity == DiagnosticSeverity.Error));
        Assert.NotNull(emitted);
        Assert.Contains("var __content = global::ComposeNet.ComposableLambdas.Wrap2(composer, c => RenderChildren(c));", emitted);

        var errors = output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
        Assert.Empty(errors);
    }

    [Fact]
    public void ScopePublishingContainer_EmitsPushScope()
    {
        var code = """
            using AndroidX.Compose.Runtime;
            using AndroidX.Compose.UI;
            using ComposeNet;
            using Kotlin.Jvm.Functions;

            [assembly: ComposeDefaults("NavigationBarDefault",
                "modifier", "containerColor", "contentColor", "tonalElevation", "windowInsets", "!content")]

            namespace ComposeNet
            {
                public static partial class ComposeBridges
                {
                    [ComposeBridge(Class="androidx/compose/material3/NavigationBarKt", JvmName="NavigationBar-HsRjFt4",
                                   Signature="(Landroidx/compose/ui/Modifier;JJFLandroidx/compose/foundation/layout/WindowInsets;Lkotlin/jvm/functions/Function3;Landroidx/compose/runtime/Composer;II)V",
                                   Defaults=typeof(NavigationBarDefault))]
                    [ComposeFacade(Scope = "Row")]
                    public static partial void NavigationBar(IModifier? modifier, IFunction3 content, IComposer composer);
                }
            }
            """;

        var (output, diags, emitted) = Run(code, "NavigationBar");
        Assert.Empty(diags.Where(d => d.Severity == DiagnosticSeverity.Error));
        Assert.NotNull(emitted);
        Assert.Contains("Wrap3(composer, (__scope, c) =>", emitted);
        Assert.Contains("global::ComposeNet.RenderContext.PushScope(__scope, global::ComposeNet.ScopeKind.Row);", emitted);

        var errors = output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
        Assert.Empty(errors);
    }

    [Fact]
    public void UnsupportedCallback_EmitsCN3002()
    {
        var code = $$"""
            using AndroidX.Compose.Runtime;
            using AndroidX.Compose.UI;
            using ComposeNet;
            using Kotlin.Jvm.Functions;

            [assembly: ComposeDefaults("CheckboxDefault",
                "!checked", "!onCheckedChange", "modifier", "enabled", "colors", "interactionSource")]

            namespace ComposeNet
            {
                public static partial class ComposeBridges
                {
                    [ComposeBridge(Class="androidx/compose/material3/CheckboxKt", JvmName="Checkbox",
                                   Signature="(ZLkotlin/jvm/functions/Function1;Landroidx/compose/ui/Modifier;ZLandroidx/compose/material3/CheckboxColors;Landroidx/compose/foundation/interaction/MutableInteractionSource;Landroidx/compose/runtime/Composer;II)V",
                                   Defaults=typeof(CheckboxDefault))]
                    [ComposeFacade]
                    public static partial void Checkbox(bool @checked, IFunction1 onCheckedChange, IModifier? modifier, IComposer composer);
                }
            }
            """;

        var (_, diags, _) = Run(code, "Checkbox");
        var cn3002 = diags.Where(d => d.Id == "CN3002").ToArray();
        Assert.NotEmpty(cn3002);
    }

    [Fact]
    public void AlertDialog_MultiSlotWithDefaultsMask()
    {
        var code = """
            using AndroidX.Compose.Runtime;
            using AndroidX.Compose.UI;
            using ComposeNet;
            using Kotlin.Jvm.Functions;

            [assembly: ComposeDefaults("AlertDialogDefault",
                "!onDismissRequest", "!confirmButton", "modifier", "dismissButton", "icon", "title", "text",
                "shape", "containerColor", "iconContentColor", "titleContentColor", "textContentColor", "tonalElevation", "properties")]

            namespace ComposeNet
            {
                public static partial class ComposeBridges
                {
                    [ComposeBridge(Class="androidx/compose/material3/AndroidAlertDialog_androidKt", JvmName="AlertDialog-Oix01E0",
                                   Signature="(Lkotlin/jvm/functions/Function0;Lkotlin/jvm/functions/Function2;Landroidx/compose/ui/Modifier;Lkotlin/jvm/functions/Function2;Lkotlin/jvm/functions/Function2;Lkotlin/jvm/functions/Function2;Lkotlin/jvm/functions/Function2;Landroidx/compose/ui/graphics/Shape;JJJJFLandroidx/compose/ui/window/DialogProperties;Landroidx/compose/runtime/Composer;III)V",
                                   Defaults=typeof(AlertDialogDefault))]
                    [ComposeFacade]
                    public static partial void AlertDialog(IFunction0 onDismissRequest, IFunction2 confirmButton,
                        IModifier? modifier, IFunction2? dismissButton, IFunction2? icon, IFunction2? title, IFunction2? text,
                        int defaults, IComposer composer);
                }
            }
            """;

        var (output, diags, emitted) = Run(code, "AlertDialog");
        Assert.Empty(diags.Where(d => d.Severity == DiagnosticSeverity.Error));
        Assert.NotNull(emitted);
        // Leaf (not a container): required ConfirmButton property + null-check.
        Assert.Contains("public sealed partial class AlertDialog : global::ComposeNet.ComposableNode", emitted);
        Assert.Contains("public global::ComposeNet.ComposableNode? ConfirmButton { get; set; }", emitted);
        Assert.Contains("public global::ComposeNet.ComposableNode? DismissButton { get; set; }", emitted);
        Assert.Contains("if (ConfirmButton is null)", emitted);
        // OnDismissRequest is a System.Action ctor param.
        Assert.Contains("public AlertDialog(global::System.Action onDismissRequest)", emitted);
        // Auto-mask logic touches each enum member the slot bit corresponds to.
        Assert.Contains("int __defaults = (int)global::ComposeNet.AlertDialogDefault.All;", emitted);
        Assert.Contains("if (__modifier is not null) __defaults &= ~(int)global::ComposeNet.AlertDialogDefault.Modifier;", emitted);
        Assert.Contains("if (__dismissButton is not null) __defaults &= ~(int)global::ComposeNet.AlertDialogDefault.DismissButton;", emitted);

        var errors = output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
        Assert.Empty(errors);
    }

    [Fact]
    public void ScopeWithoutWrap3_EmitsCN3003()
    {
        var code = """
            using AndroidX.Compose.Runtime;
            using AndroidX.Compose.UI;
            using ComposeNet;
            using Kotlin.Jvm.Functions;

            [assembly: ComposeDefaults("SurfaceDefault",
                "modifier", "shape", "color", "contentColor", "tonalElevation",
                "shadowElevation", "border", "!content")]

            namespace ComposeNet
            {
                public static partial class ComposeBridges
                {
                    [ComposeBridge(Class="androidx/compose/material3/SurfaceKt", JvmName="Surface-T9BRK9s",
                                   Signature="(Landroidx/compose/ui/Modifier;Landroidx/compose/ui/graphics/Shape;JJFFLandroidx/compose/foundation/BorderStroke;Lkotlin/jvm/functions/Function2;Landroidx/compose/runtime/Composer;II)V",
                                   Defaults=typeof(SurfaceDefault))]
                    [ComposeFacade(Scope = "Row")]
                    public static partial void Surface(IModifier? modifier, IFunction2 content, IComposer composer);
                }
            }
            """;

        var (_, diags, _) = Run(code, "Surface");
        Assert.Contains(diags, d => d.Id == "CN3003");
    }

    [Fact]
    public void InvalidScopeValue_EmitsCN3003()
    {
        var code = """
            using AndroidX.Compose.Runtime;
            using AndroidX.Compose.UI;
            using ComposeNet;
            using Kotlin.Jvm.Functions;

            [assembly: ComposeDefaults("CardDefault",
                "modifier", "shape", "colors", "elevation", "border", "!content")]

            namespace ComposeNet
            {
                public static partial class ComposeBridges
                {
                    [ComposeBridge(Class="androidx/compose/material3/CardKt", JvmName="Card",
                                   Signature="(Landroidx/compose/ui/Modifier;Landroidx/compose/ui/graphics/Shape;Landroidx/compose/material3/CardColors;Landroidx/compose/material3/CardElevation;Landroidx/compose/foundation/BorderStroke;Lkotlin/jvm/functions/Function3;Landroidx/compose/runtime/Composer;II)V",
                                   Defaults=typeof(CardDefault))]
                    [ComposeFacade(Scope = "Diagonal")]
                    public static partial void Card(IModifier? modifier, IFunction3 content, IComposer composer);
                }
            }
            """;

        var (_, diags, _) = Run(code, "Card");
        Assert.Contains(diags, d => d.Id == "CN3003" && d.GetMessage().Contains("Diagonal"));
    }

    [Fact]
    public void WrongContainingType_EmitsCN3001()
    {
        var code = """
            using AndroidX.Compose.Runtime;
            using AndroidX.Compose.UI;
            using ComposeNet;
            using Kotlin.Jvm.Functions;

            namespace ComposeNet
            {
                public static partial class NotComposeBridges
                {
                    [ComposeFacade]
                    public static partial void Foo(IComposer composer);
                }
            }
            """;

        var (_, diags, _) = Run(code, "Foo");
        Assert.Contains(diags, d => d.Id == "CN3001");
    }

    [Fact]
    public void MissingComposeBridge_EmitsCN3004()
    {
        var code = """
            using AndroidX.Compose.Runtime;
            using AndroidX.Compose.UI;
            using ComposeNet;
            using Kotlin.Jvm.Functions;

            namespace ComposeNet
            {
                public static partial class ComposeBridges
                {
                    [ComposeFacade]
                    public static partial void Orphan(IModifier? modifier, IFunction3 content, IComposer composer);
                }
            }
            """;

        var (_, diags, _) = Run(code, "Orphan");
        Assert.Contains(diags, d => d.Id == "CN3004");
    }

    [Fact]
    public void ClassNameOverride_StillCallsBridgeMethodName()
    {
        var code = $$"""
            using AndroidX.Compose.Runtime;
            using AndroidX.Compose.UI;
            using ComposeNet;
            using Kotlin.Jvm.Functions;

            {{ButtonAttrs}}

            namespace ComposeNet
            {
                public static partial class ComposeBridges
                {
                    [ComposeBridge(Class="androidx/compose/material3/ButtonKt", JvmName="Button",
                                   Signature="{{ButtonSig}}", Defaults=typeof(ButtonDefault))]
                    [ComposeFacade(ClassName = "MyButton")]
                    public static partial void Button(IFunction0 onClick, IModifier? modifier,
                                                      IFunction3 content, IComposer composer);
                }
            }
            """;

        var (output, diags, emitted) = Run(code, "MyButton");
        Assert.Empty(diags.Where(d => d.Severity == DiagnosticSeverity.Error));
        Assert.NotNull(emitted);
        Assert.Contains("public sealed partial class MyButton", emitted);
        // Critical: bridge call must use the real bridge method name "Button",
        // not the override "MyButton" (which doesn't exist on ComposeBridges).
        Assert.Contains("global::ComposeNet.ComposeBridges.Button(", emitted);
        Assert.DoesNotContain("global::ComposeNet.ComposeBridges.MyButton(", emitted);

        var errors = output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
        Assert.Empty(errors);
    }

    // ---------------------------------------------------------------
    // Phase 2 — [Callback(typeof(T))] → Action<T> ctor
    // ---------------------------------------------------------------

    [Fact]
    public void Callback_BoolUnboxesViaJavaLangBoolean()
    {
        var code = """
            using AndroidX.Compose.Runtime;
            using AndroidX.Compose.UI;
            using ComposeNet;
            using Kotlin.Jvm.Functions;

            [assembly: ComposeDefaults("IconToggleButtonDefault",
                "!checked", "!onCheckedChange", "modifier", "enabled", "colors", "interactionSource", "!content")]

            namespace ComposeNet
            {
                public static partial class ComposeBridges
                {
                    [ComposeBridge(Class="androidx/compose/material3/IconButtonKt", JvmName="IconToggleButton",
                                   Signature="(ZLkotlin/jvm/functions/Function1;Landroidx/compose/ui/Modifier;ZLandroidx/compose/material3/IconToggleButtonColors;Landroidx/compose/foundation/interaction/MutableInteractionSource;Lkotlin/jvm/functions/Function2;Landroidx/compose/runtime/Composer;II)V",
                                   Defaults=typeof(IconToggleButtonDefault))]
                    [ComposeFacade]
                    public static partial void IconToggleButton(bool @checked, [Callback(typeof(bool))] IFunction1 onCheckedChange,
                        IModifier? modifier, IFunction2 content, IComposer composer);
                }
            }
            """;

        var (output, diags, emitted) = Run(code, "IconToggleButton");
        Assert.Empty(diags.Where(d => d.Severity == DiagnosticSeverity.Error));
        Assert.NotNull(emitted);
        Assert.Contains("readonly global::System.Action<bool> _onCheckedChange;", emitted);
        Assert.Contains("public IconToggleButton(bool @checked, global::System.Action<bool> onCheckedChange)", emitted);
        Assert.Contains("new global::ComposeNet.ComposableLambda1(v => _onCheckedChange(v is global::Java.Lang.Boolean __b && __b.BooleanValue()));", emitted);

        var errors = output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
        Assert.Empty(errors);
    }

    [Fact]
    public void Callback_StringUnboxesViaToString()
    {
        var code = """
            using AndroidX.Compose.Runtime;
            using AndroidX.Compose.UI;
            using ComposeNet;
            using Kotlin.Jvm.Functions;

            [assembly: ComposeDefaults("TextFieldDefault",
                "!value", "!onValueChange", "modifier")]

            namespace ComposeNet
            {
                public static partial class ComposeBridges
                {
                    [ComposeBridge(Class="androidx/compose/material3/TextFieldKt", JvmName="TextField",
                                   Signature="(Ljava/lang/String;Lkotlin/jvm/functions/Function1;Landroidx/compose/ui/Modifier;Landroidx/compose/runtime/Composer;II)V",
                                   Defaults=typeof(TextFieldDefault))]
                    [ComposeFacade]
                    public static partial void TextField(string value, [Callback(typeof(string))] IFunction1 onValueChange,
                        IModifier? modifier, IComposer composer);
                }
            }
            """;

        var (output, diags, emitted) = Run(code, "TextField");
        Assert.Empty(diags.Where(d => d.Severity == DiagnosticSeverity.Error));
        Assert.NotNull(emitted);
        Assert.Contains("readonly global::System.Action<string> _onValueChange;", emitted);
        Assert.Contains("public TextField(string value, global::System.Action<string> onValueChange)", emitted);
        Assert.Contains("new global::ComposeNet.ComposableLambda1(v => _onValueChange(v?.ToString() ?? string.Empty));", emitted);

        var errors = output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
        Assert.Empty(errors);
    }

    [Fact]
    public void Callback_UnsupportedTypeEmitsCN3005()
    {
        var code = """
            using AndroidX.Compose.Runtime;
            using AndroidX.Compose.UI;
            using ComposeNet;
            using Kotlin.Jvm.Functions;

            [assembly: ComposeDefaults("FooDefault", "!a", "!cb", "modifier")]

            namespace ComposeNet
            {
                public static partial class ComposeBridges
                {
                    [ComposeBridge(Class="x/Kt", JvmName="Foo",
                                   Signature="(ILkotlin/jvm/functions/Function1;Landroidx/compose/ui/Modifier;Landroidx/compose/runtime/Composer;II)V",
                                   Defaults=typeof(FooDefault))]
                    [ComposeFacade]
                    public static partial void Foo(int a, [Callback(typeof(int))] IFunction1 cb, IModifier? modifier, IComposer composer);
                }
            }
            """;

        var (_, diags, _) = Run(code, "Foo");
        Assert.Contains(diags, d => d.Id == "CN3005");
    }

    // ---------------------------------------------------------------
    // Phase 3 — named slots
    // ---------------------------------------------------------------

    [Fact]
    public void NamedSlots_RenameViaSlotAttribute()
    {
        var code = """
            using AndroidX.Compose.Runtime;
            using AndroidX.Compose.UI;
            using ComposeNet;
            using Kotlin.Jvm.Functions;

            [assembly: ComposeDefaults("ListItemDefault",
                "!headlineContent", "modifier", "overlineContent", "supportingContent",
                "leadingContent", "trailingContent", "colors", "tonalElevation", "shadowElevation")]

            namespace ComposeNet
            {
                public static partial class ComposeBridges
                {
                    [ComposeBridge(Class="androidx/compose/material3/ListItemKt", JvmName="ListItem",
                                   Signature="(Lkotlin/jvm/functions/Function2;Landroidx/compose/ui/Modifier;Lkotlin/jvm/functions/Function2;Lkotlin/jvm/functions/Function2;Lkotlin/jvm/functions/Function2;Lkotlin/jvm/functions/Function2;Landroidx/compose/material3/ListItemColors;FFLandroidx/compose/runtime/Composer;II)V",
                                   Defaults=typeof(ListItemDefault))]
                    [ComposeFacade]
                    public static partial void ListItem(
                        [Slot("Headline")] IFunction2 headlineContent,
                        IModifier? modifier,
                        [Slot("Overline")] IFunction2? overlineContent,
                        [Slot("Supporting")] IFunction2? supportingContent,
                        [Slot("Leading")] IFunction2? leadingContent,
                        [Slot("Trailing")] IFunction2? trailingContent,
                        int defaults, IComposer composer);
                }
            }
            """;

        var (output, diags, emitted) = Run(code, "ListItem");
        Assert.Empty(diags.Where(d => d.Severity == DiagnosticSeverity.Error));
        Assert.NotNull(emitted);
        Assert.Contains("public global::ComposeNet.ComposableNode? Headline { get; set; }", emitted);
        Assert.Contains("public global::ComposeNet.ComposableNode? Overline { get; set; }", emitted);
        Assert.Contains("public global::ComposeNet.ComposableNode? Trailing { get; set; }", emitted);
        Assert.Contains("if (Headline is null)", emitted);
        Assert.Contains("if (__overlineContent is not null) __defaults &= ~(int)global::ComposeNet.ListItemDefault.OverlineContent;", emitted);

        var errors = output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
        Assert.Empty(errors);
    }

    [Fact]
    public void BadgedBox_TwoRequiredFunction3Slots()
    {
        var code = """
            using AndroidX.Compose.Runtime;
            using AndroidX.Compose.UI;
            using ComposeNet;
            using Kotlin.Jvm.Functions;

            [assembly: ComposeDefaults("BadgedBoxDefault", "!badge", "modifier", "!content")]

            namespace ComposeNet
            {
                public static partial class ComposeBridges
                {
                    [ComposeBridge(Class="androidx/compose/material3/BadgeKt", JvmName="BadgedBox",
                                   Signature="(Lkotlin/jvm/functions/Function3;Landroidx/compose/ui/Modifier;Lkotlin/jvm/functions/Function3;Landroidx/compose/runtime/Composer;II)V",
                                   Defaults=typeof(BadgedBoxDefault))]
                    [ComposeFacade]
                    public static partial void BadgedBox(IFunction3 badge, IModifier? modifier, IFunction3 content,
                        int defaults, IComposer composer);
                }
            }
            """;

        var (output, diags, emitted) = Run(code, "BadgedBox");
        Assert.Empty(diags.Where(d => d.Severity == DiagnosticSeverity.Error));
        Assert.NotNull(emitted);
        // Two required Function3 slots → both surface as properties, not RenderChildren.
        Assert.Contains("public sealed partial class BadgedBox : global::ComposeNet.ComposableNode", emitted);
        Assert.Contains("public global::ComposeNet.ComposableNode? Badge { get; set; }", emitted);
        Assert.Contains("public global::ComposeNet.ComposableNode? Content { get; set; }", emitted);
        Assert.Contains("if (Badge is null)", emitted);
        Assert.Contains("if (Content is null)", emitted);

        var errors = output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
        Assert.Empty(errors);
    }

    // ---------------------------------------------------------------
    // Phase 6 — DefaultColorFromTheme
    // ---------------------------------------------------------------

    [Fact]
    public void DefaultColorFromTheme_EmitsContainerColorProperty()
    {
        var code = """
            using AndroidX.Compose.Runtime;
            using AndroidX.Compose.UI;
            using ComposeNet;
            using Kotlin.Jvm.Functions;

            [assembly: ComposeDefaults("DrawerSheetDefault", "!content", "drawerContainerColor")]

            namespace ComposeNet
            {
                public static partial class ComposeBridges
                {
                    [ComposeBridge(Class="androidx/compose/material3/NavigationDrawerKt", JvmName="ModalDrawerSheet-afqeVBk",
                                   Signature="(Lkotlin/jvm/functions/Function3;JLandroidx/compose/runtime/Composer;II)V",
                                   Defaults=typeof(DrawerSheetDefault))]
                    [ComposeFacade(DefaultColorFromTheme = "secondaryContainer")]
                    public static partial void ModalDrawerSheet(IFunction3 content, long drawerContainerColor, IComposer composer);
                }
            }
            """;

        var (output, diags, emitted) = Run(code, "ModalDrawerSheet");
        Assert.Empty(diags.Where(d => d.Severity == DiagnosticSeverity.Error));
        Assert.NotNull(emitted);
        Assert.Contains("public long ContainerColor { get; set; }", emitted);
        Assert.Contains("long __color = ContainerColor != 0L ? ContainerColor : global::AndroidX.Compose.Material3.MaterialTheme.Instance.GetColorScheme(composer, 0).SecondaryContainer;", emitted);
        Assert.Contains("global::ComposeNet.ComposeBridges.ModalDrawerSheet(__content, __color, composer);", emitted);

        var errors = output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
        Assert.Empty(errors);
    }

    [Fact]
    public void DefaultColorFromTheme_NoLongParamEmitsCN3007()
    {
        var code = """
            using AndroidX.Compose.Runtime;
            using AndroidX.Compose.UI;
            using ComposeNet;
            using Kotlin.Jvm.Functions;

            [assembly: ComposeDefaults("CardDefault", "modifier", "!content")]

            namespace ComposeNet
            {
                public static partial class ComposeBridges
                {
                    [ComposeBridge(Class="x/Kt", JvmName="Card",
                                   Signature="(Landroidx/compose/ui/Modifier;Lkotlin/jvm/functions/Function3;Landroidx/compose/runtime/Composer;II)V",
                                   Defaults=typeof(CardDefault))]
                    [ComposeFacade(DefaultColorFromTheme = "secondaryContainer")]
                    public static partial void Card(IModifier? modifier, IFunction3 content, IComposer composer);
                }
            }
            """;

        var (_, diags, _) = Run(code, "Card");
        Assert.Contains(diags, d => d.Id == "CN3007");
    }

    // ---------------------------------------------------------------
    // Phase 7 — PainterResource
    // ---------------------------------------------------------------

    [Fact]
    public void PainterResource_EmitsTryFinallyDeleteLocalRef()
    {
        var code = """
            using System;
            using AndroidX.Compose.Runtime;
            using AndroidX.Compose.UI;
            using ComposeNet;
            using Kotlin.Jvm.Functions;

            [assembly: ComposeDefaults("ImageDefault", "!painter", "contentDescription", "modifier")]

            namespace ComposeNet
            {
                public static partial class ComposeBridges
                {
                    [ComposeBridge(Class="androidx/compose/foundation/ImageKt", JvmName="Image",
                                   Signature="(Landroidx/compose/ui/graphics/painter/Painter;Ljava/lang/String;Landroidx/compose/ui/Modifier;Landroidx/compose/runtime/Composer;II)V",
                                   Defaults=typeof(ImageDefault))]
                    [ComposeFacade]
                    public static partial void Image([PainterResource] IntPtr painter,
                        string? contentDescription, IModifier? modifier,
                        int defaults, IComposer composer);
                }
            }
            """;

        var (output, diags, emitted) = Run(code, "Image");
        Assert.Empty(diags.Where(d => d.Severity == DiagnosticSeverity.Error));
        Assert.NotNull(emitted);
        Assert.Contains("public Image(int drawableResourceId", emitted);
        Assert.Contains("readonly int _drawableResourceId;", emitted);
        Assert.Contains("global::System.IntPtr __painterRef = global::ComposeNet.ComposeBridges.PainterResource(_drawableResourceId, composer);", emitted);
        Assert.Contains("try", emitted);
        Assert.Contains("finally", emitted);
        Assert.Contains("global::Android.Runtime.JNIEnv.DeleteLocalRef(__painterRef);", emitted);
        // Bridge call inside try block — uses __painterRef for painter arg.
        Assert.Contains("global::ComposeNet.ComposeBridges.Image(__painterRef", emitted);

        var errors = output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
        Assert.Empty(errors);
    }

    [Fact]
    public void ScopeReceiver_IntPtrEndingInScope_BindsToRenderContextCurrentScope()
    {
        var code = """
            using System;
            using AndroidX.Compose.Runtime;
            using AndroidX.Compose.UI;
            using ComposeNet;
            using Kotlin.Jvm.Functions;

            [assembly: ComposeDefaults("NavigationBarItemDefault",
                "!selected", "!onClick", "!icon", "modifier",
                "enabled", "label", "alwaysShowLabel", "colors", "interactionSource")]

            namespace ComposeNet
            {
                public static partial class ComposeBridges
                {
                    [ComposeBridge(Class="androidx/compose/material3/NavigationBarKt", JvmName="NavigationBarItem",
                                   Signature="(Landroidx/compose/foundation/layout/RowScope;ZLkotlin/jvm/functions/Function0;Lkotlin/jvm/functions/Function2;Landroidx/compose/ui/Modifier;ZLkotlin/jvm/functions/Function2;ZLandroidx/compose/material3/NavigationBarItemColors;Landroidx/compose/foundation/interaction/MutableInteractionSource;Landroidx/compose/runtime/Composer;II)V",
                                   Defaults=typeof(NavigationBarItemDefault))]
                    [ComposeFacade]
                    public static partial void NavigationBarItem(
                        IntPtr rowScope, bool selected, IFunction0 onClick,
                        IFunction2 icon, IModifier? modifier, IFunction2? label,
                        int defaults, IComposer composer);
                }
            }
            """;

        var (output, diags, emitted) = Run(code, "NavigationBarItem");
        Assert.Empty(diags.Where(d => d.Severity == DiagnosticSeverity.Error));
        Assert.NotNull(emitted);
        // ScopeReceiver is bound to RenderContext.CurrentScope (no ctor slot).
        Assert.Contains("global::ComposeNet.ComposeBridges.NavigationBarItem(global::ComposeNet.RenderContext.CurrentScope,", emitted);
        // Required Function2 (icon) becomes a named property (auto multi-slot).
        Assert.Contains("public global::ComposeNet.ComposableNode? Icon", emitted);
        // Optional Function2 (label) becomes a named property.
        Assert.Contains("public global::ComposeNet.ComposableNode? Label", emitted);
        // Ctor exposes only selected + onClick (rowScope is auto-bound).
        Assert.Contains("public NavigationBarItem(bool selected, global::System.Action onClick)", emitted);

        var errors = output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
        Assert.Empty(errors);
    }

    [Fact]
    public void Callback_OnNonFunction1Param_ReportsCN3006()
    {
        var code = """
            using System;
            using AndroidX.Compose.Runtime;
            using AndroidX.Compose.UI;
            using ComposeNet;
            using Kotlin.Jvm.Functions;

            namespace ComposeNet
            {
                public static partial class ComposeBridges
                {
                    [ComposeBridge(Class="x/Foo", JvmName="bar", Signature="()V")]
                    [ComposeFacade]
                    public static partial void Foo([Callback(typeof(bool))] IFunction0 onClick, IComposer composer);
                }
            }
            """;
        var (_, diags, _) = Run(code, "Foo");
        Assert.Contains(diags, d => d.Id == "CN3006" && d.GetMessage().Contains("[Callback]") && d.GetMessage().Contains("IFunction1"));
        Assert.DoesNotContain(diags, d => d.Id == "CN3005");
    }

    [Fact]
    public void MultiplePainterResource_ReportsCN3006()
    {
        var code = """
            using System;
            using AndroidX.Compose.Runtime;
            using AndroidX.Compose.UI;
            using ComposeNet;

            [assembly: ComposeDefaults("FooDefault", "!a", "!b")]

            namespace ComposeNet
            {
                public static partial class ComposeBridges
                {
                    [ComposeBridge(Class="x/Foo", JvmName="bar",
                                   Signature="(Landroidx/compose/ui/graphics/painter/Painter;Landroidx/compose/ui/graphics/painter/Painter;Landroidx/compose/runtime/Composer;II)V",
                                   Defaults=typeof(FooDefault))]
                    [ComposeFacade]
                    public static partial void Foo([PainterResource] IntPtr a, [PainterResource] IntPtr b,
                        int defaults, IComposer composer);
                }
            }
            """;
        var (_, diags, _) = Run(code, "Foo");
        Assert.Contains(diags, d => d.Id == "CN3006" && d.GetMessage().Contains("only be applied to one"));
    }

    [Fact]
    public void CallerDefaults_WithoutDefaultsEnum_ReportsCN3006()
    {
        var code = """
            using System;
            using AndroidX.Compose.Runtime;
            using AndroidX.Compose.UI;
            using ComposeNet;
            using Kotlin.Jvm.Functions;

            namespace ComposeNet
            {
                public static partial class ComposeBridges
                {
                    // Bridge has `int defaults` but no `Defaults = typeof(...)` on [ComposeBridge].
                    [ComposeBridge(Class="x/Foo", JvmName="bar",
                                   Signature="(Lkotlin/jvm/functions/Function2;Landroidx/compose/runtime/Composer;II)V")]
                    [ComposeFacade]
                    public static partial void Foo(IFunction2 content, int defaults, IComposer composer);
                }
            }
            """;
        var (_, diags, _) = Run(code, "Foo");
        Assert.Contains(diags, d => d.Id == "CN3006" && d.GetMessage().Contains("int defaults"));
    }
}
