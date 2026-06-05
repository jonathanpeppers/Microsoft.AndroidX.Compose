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
        }
        namespace AndroidX.Compose.Runtime { public interface IComposer { } }
        namespace AndroidX.Compose.UI { public interface IModifier { } }
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
    public void ManualDefaultsParam_EmitsCN3002()
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

        var (_, diags, _) = Run(code, "AlertDialog");
        var cn3002 = diags.Where(d => d.Id == "CN3002").ToArray();
        Assert.NotEmpty(cn3002);
        Assert.Contains(cn3002, d => d.GetMessage().Contains("defaults"));
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
}
