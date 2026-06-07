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
            public enum JniHandleOwnership { TransferLocalRef = 0, DoNotTransfer = 1 }
            public interface IJavaObject
            {
                System.IntPtr Handle { get; }
            }
            public readonly struct JValue
            {
                public JValue(System.IntPtr v) { } public JValue(bool v) { } public JValue(int v) { }
                public JValue(long v) { } public JValue(float v) { } public JValue(double v) { }
            }
        }
        namespace Java.Lang
        {
            public class Object : Android.Runtime.IJavaObject
            {
                public System.IntPtr Handle => default;
                public static T? GetObject<T>(System.IntPtr handle, Android.Runtime.JniHandleOwnership transfer) where T : class => default;
            }
            public sealed class Boolean : Object { public bool BooleanValue() => false; }
            public sealed class Float : Object { public float FloatValue() => 0f; }
            public abstract class Enum : Object { }
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
            public readonly struct Dp
            {
                public Dp(float v) { Value = v; }
                public float Value { get; }
                public static float Pack(Dp? d) => d?.Value ?? 0f;
            }
            public readonly struct Color
            {
                public Color(long v) { PackedValue = (ulong)v; }
                public ulong PackedValue { get; }
                public static implicit operator long(Color c) => (long)c.PackedValue;
            }
            public readonly struct Sp
            {
                public Sp(float v) { Value = v; }
                public float Value { get; }
                public static long Pack(Sp? s) => 0L;
            }
            public readonly struct Em
            {
                public Em(float v) { Value = v; }
                public float Value { get; }
                public static long Pack(Em? e) => 0L;
            }
            public readonly struct TextOverflow
            {
                public TextOverflow(int v) { Value = v; }
                public int Value { get; }
                public static int Pack(TextOverflow? a) => 0;
            }
            public class FontWeight : Java.Lang.Object { }
            public class FontStyle : Java.Lang.Object { }
            public class FontFamily : Java.Lang.Object { }
            public class TextAlign : Java.Lang.Object { }
            public class TextDecoration : Java.Lang.Object { }
            public class Shape : Java.Lang.Object { }
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
    public void HybridContainer_RequiredFn3PlusNullableFn2_EmitsContainerWithNamedSlot()
    {
        var code = """
            using AndroidX.Compose.Runtime;
            using AndroidX.Compose.UI;
            using ComposeNet;
            using Kotlin.Jvm.Functions;

            [assembly: ComposeDefaults("BottomAppBarDefault",
                "!actions", "modifier", "floatingActionButton", "containerColor",
                "contentColor", "tonalElevation", "contentPadding", "windowInsets",
                "scrollBehavior")]

            namespace ComposeNet
            {
                public static partial class ComposeBridges
                {
                    [ComposeBridge(Class="androidx/compose/material3/AppBarKt", JvmName="BottomAppBar-qhFBPw4",
                                   Signature="(Lkotlin/jvm/functions/Function3;Landroidx/compose/ui/Modifier;Lkotlin/jvm/functions/Function2;JJFLandroidx/compose/foundation/layout/PaddingValues;Landroidx/compose/foundation/layout/WindowInsets;Landroidx/compose/material3/BottomAppBarScrollBehavior;Landroidx/compose/runtime/Composer;II)V",
                                   Defaults=typeof(BottomAppBarDefault))]
                    [ComposeFacade(Scope = "Row")]
                    public static partial void BottomAppBar(IFunction3 actions, IModifier? modifier, IFunction2? floatingActionButton, IComposer composer);
                }
            }
            """;

        var (output, diags, emitted) = Run(code, "BottomAppBar");
        Assert.Empty(diags.Where(d => d.Severity == DiagnosticSeverity.Error));
        Assert.NotNull(emitted);
        // Derives from ComposableContainer (container body), not ComposableNode.
        Assert.Contains(": global::ComposeNet.ComposableContainer", emitted);
        // Required Fn3 stays as container body: RenderChildren + PushScope(Row).
        Assert.Contains("Wrap3(composer, (__scope, c) =>", emitted);
        Assert.Contains("global::ComposeNet.RenderContext.PushScope(__scope, global::ComposeNet.ScopeKind.Row);", emitted);
        Assert.Contains("RenderChildren(c);", emitted);
        // Nullable Fn2 surfaces as a named property.
        Assert.Contains("public global::ComposeNet.ComposableNode? FloatingActionButton { get; set; }", emitted);

        var errors = output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
        Assert.Empty(errors);
    }

    [Fact]
    public void HybridContainer_WithoutScope_StillBehavesAsLeaf()
    {
        // Same shape as BottomAppBar but without [ComposeFacade(Scope=...)] —
        // the generator must NOT treat as hybrid; both Fn slots become
        // named properties (no RenderChildren, no ComposableContainer).
        var code = """
            using AndroidX.Compose.Runtime;
            using AndroidX.Compose.UI;
            using ComposeNet;
            using Kotlin.Jvm.Functions;

            [assembly: ComposeDefaults("FooBarDefault",
                "!actions", "modifier", "floatingActionButton")]

            namespace ComposeNet
            {
                public static partial class ComposeBridges
                {
                    [ComposeBridge(Class="x/y/Z", JvmName="FooBar",
                                   Signature="(Lkotlin/jvm/functions/Function3;Landroidx/compose/ui/Modifier;Lkotlin/jvm/functions/Function2;Landroidx/compose/runtime/Composer;II)V",
                                   Defaults=typeof(FooBarDefault))]
                    [ComposeFacade]
                    public static partial void FooBar(IFunction3 actions, IModifier? modifier, IFunction2? floatingActionButton, IComposer composer);
                }
            }
            """;

        var (_, diags, emitted) = Run(code, "FooBar");
        Assert.Empty(diags.Where(d => d.Severity == DiagnosticSeverity.Error));
        Assert.NotNull(emitted);
        // No Scope → leaf shape: ComposableNode base, no RenderChildren.
        Assert.Contains(": global::ComposeNet.ComposableNode", emitted);
        Assert.DoesNotContain("RenderChildren", emitted);
        Assert.Contains("public global::ComposeNet.ComposableNode? Actions { get; set; }", emitted);
        Assert.Contains("public global::ComposeNet.ComposableNode? FloatingActionButton { get; set; }", emitted);
    }

    [Fact]
    public void HybridContainer_ContainerTrueWithFn2Body_EmitsContainerWithNamedSlot()
    {
        // Mirrors the ModalWideNavigationRail shape (issue #121): a
        // non-`@Composable` IFunction2 body slot + a nullable IFunction2?
        // header slot. Container = true is required because there's no
        // IFunction3 body for the Scope path to latch onto. The facade
        // must still derive from ComposableContainer (collection-init
        // syntax), wrap children via Wrap2, and surface the nullable
        // Fn2 slot as a named property.
        var code = """
            using AndroidX.Compose.Runtime;
            using AndroidX.Compose.UI;
            using ComposeNet;
            using Kotlin.Jvm.Functions;

            [assembly: ComposeDefaults("RailDefault",
                "modifier", "header", "!content")]

            namespace ComposeNet
            {
                public static partial class ComposeBridges
                {
                    [ComposeBridge(Class="x/y/RailKt", JvmName="Rail",
                                   Signature="(Landroidx/compose/ui/Modifier;Lkotlin/jvm/functions/Function2;Lkotlin/jvm/functions/Function2;Landroidx/compose/runtime/Composer;II)V",
                                   Defaults=typeof(RailDefault))]
                    [ComposeFacade(Container = true)]
                    public static partial void Rail(
                        IModifier? modifier, IFunction2? header, IFunction2 content, IComposer composer);
                }
            }
            """;

        var (output, diags, emitted) = Run(code, "Rail");
        Assert.Empty(diags.Where(d => d.Severity == DiagnosticSeverity.Error));
        Assert.NotNull(emitted);
        // Derives from ComposableContainer so collection-init syntax works
        // ("new Rail { new WideNavigationRailItem(...), ... }").
        Assert.Contains(": global::ComposeNet.ComposableContainer", emitted);
        // Non-nullable IFunction2 body wraps children via Wrap2 (no
        // Fn3 / no PushScope — Container=true uses the Fn2 path).
        Assert.Contains("Wrap2(composer, c => RenderChildren(c))", emitted);
        Assert.DoesNotContain("PushScope", emitted);
        // Nullable IFunction2? slot surfaces as a named property, not a
        // ctor primitive.
        Assert.Contains("public global::ComposeNet.ComposableNode? Header { get; set; }", emitted);

        var errors = output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
        Assert.Empty(errors);
    }

    [Fact]
    public void JavaEnumPrimitive_SurfacedAsCtorParam()
    {
        // Simulates ToggleableState — a class derived from Java.Lang.Enum.
        // The generator must accept it as a primitive-like ctor slot.
        var code = """
            using AndroidX.Compose.Runtime;
            using AndroidX.Compose.UI;
            using ComposeNet;
            using Kotlin.Jvm.Functions;

            [assembly: ComposeDefaults("TriStateCheckboxDefault",
                "!state", "!onClick", "modifier", "enabled", "colors", "interactionSource")]

            namespace ComposeNet.Demo
            {
                public class FakeToggleableState : global::Java.Lang.Enum { }
            }

            namespace ComposeNet
            {
                public static partial class ComposeBridges
                {
                    [ComposeBridge(Class="x/y/Z", JvmName="TriStateCheckbox",
                                   Signature="(Landroidx/compose/ui/state/ToggleableState;Lkotlin/jvm/functions/Function0;Landroidx/compose/ui/Modifier;ZLandroidx/compose/material3/CheckboxColors;Landroidx/compose/foundation/interaction/MutableInteractionSource;Landroidx/compose/runtime/Composer;II)V",
                                   Defaults=typeof(TriStateCheckboxDefault))]
                    [ComposeFacade]
                    public static partial void TriStateCheckbox(global::ComposeNet.Demo.FakeToggleableState state, IFunction0 onClick, IModifier? modifier, IComposer composer);
                }
            }
            """;

        var (output, diags, emitted) = Run(code, "TriStateCheckbox");
        Assert.Empty(diags.Where(d => d.Severity == DiagnosticSeverity.Error));
        Assert.NotNull(emitted);
        // Java enum surfaces as a ctor field + ctor param + bridge arg pass-through.
        Assert.Contains("readonly global::ComposeNet.Demo.FakeToggleableState _state;", emitted);
        Assert.Contains("global::ComposeNet.Demo.FakeToggleableState state", emitted);
        Assert.Contains("ComposeBridges.TriStateCheckbox(_state,", emitted);

        // Sanity-check: the synthetic compilation must actually compile —
        // otherwise the generator could be matching against an
        // IErrorTypeSymbol's syntactic Name/Namespace rather than truly
        // exercising IsJavaEnum's inheritance walk.
        var compileErrors = output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
        Assert.Empty(compileErrors);
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
        Assert.Contains("public global::ComposeNet.Color ContainerColor { get; set; }", emitted);
        Assert.Contains("long __color = (long)ContainerColor != 0L ? (long)ContainerColor : global::AndroidX.Compose.Material3.MaterialTheme.Instance.GetColorScheme(composer, 0).SecondaryContainer;", emitted);
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

    [Fact]
    public void WrapperFacade_WithBodyAndDefaults_GeneratesFacade()
    {
        // Direct-binding "wrapper" facade: a partial method with a
        // hand-written body and no [ComposeBridge]. The wrapper takes
        // an `int defaults` (user-controlled mask) so the facade emits
        // the auto-mask logic and the wrapper just passes it through.
        var code = """
            using AndroidX.Compose.Runtime;
            using AndroidX.Compose.UI;
            using ComposeNet;
            using Kotlin.Jvm.Functions;

            [assembly: ComposeDefaults("BoxDefault", "modifier", "contentAlignment", "propagateMinConstraints", "!content")]

            namespace ComposeNet
            {
                public static partial class ComposeBridges
                {
                    [ComposeFacade(Defaults = typeof(BoxDefault))]
                    public static partial void Box(IModifier? modifier, IFunction3 content, int defaults, IComposer composer);

                    public static partial void Box(IModifier? modifier, IFunction3 content, int defaults, IComposer composer)
                    {
                        // Pretend this calls BoxKt.Box — body irrelevant to the facade generator.
                    }
                }
            }
            """;
        var (_, diags, emitted) = Run(code, "Box");
        Assert.Empty(diags.Where(d => d.Severity == DiagnosticSeverity.Error));
        Assert.NotNull(emitted);
        Assert.Contains("public sealed partial class Box : global::ComposeNet.ComposableContainer", emitted);
        Assert.Contains("global::ComposeNet.ComposeBridges.Box(", emitted);
        // Auto-mask emits because the wrapper takes `int defaults`.
        Assert.Contains("(int)global::ComposeNet.BoxDefault.All", emitted);
        Assert.Contains("if (__modifier is not null) __defaults &= ~(int)global::ComposeNet.BoxDefault.Modifier", emitted);
    }

    [Fact]
    public void WrapperFacade_NoDefaultsAttribute_GeneratesFacadeWithoutMask()
    {
        // Spacer-style wrapper: no $default in the bytecode, so no
        // Defaults enum either. Facade generator should still emit a
        // working facade — just without the auto-mask code.
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
                    public static partial void Spacer(IModifier? modifier, IComposer composer);

                    public static partial void Spacer(IModifier? modifier, IComposer composer) { }
                }
            }
            """;
        var (_, diags, emitted) = Run(code, "Spacer");
        Assert.Empty(diags.Where(d => d.Severity == DiagnosticSeverity.Error));
        Assert.NotNull(emitted);
        Assert.Contains("public sealed partial class Spacer : global::ComposeNet.ComposableNode", emitted);
        Assert.Contains("global::ComposeNet.ComposeBridges.Spacer(", emitted);
        Assert.DoesNotContain("__defaults", emitted);
    }

    [Fact]
    public void Facade_EnumCtorParameter_IsAcceptedAsPrimitive()
    {
        // TriStateCheckbox-style facade: the ctor takes an enum value
        // (ToggleableState) plus an Action onClick. Enums should be
        // classified as ctor primitives (TypeKind.Enum).
        var code = """
            using AndroidX.Compose.Runtime;
            using AndroidX.Compose.UI;
            using ComposeNet;
            using Kotlin.Jvm.Functions;

            namespace MyApp { public enum MyState { On, Off, Mixed } }

            [assembly: ComposeDefaults("TriDefault", "!state", "!onClick", "modifier")]

            namespace ComposeNet
            {
                public static partial class ComposeBridges
                {
                    [ComposeFacade(Defaults = typeof(TriDefault))]
                    public static partial void TriStateCheckbox(
                        MyApp.MyState state, IFunction0 onClick, IModifier? modifier, IComposer composer);

                    public static partial void TriStateCheckbox(
                        MyApp.MyState state, IFunction0 onClick, IModifier? modifier, IComposer composer) { }
                }
            }
            """;
        var (_, diags, emitted) = Run(code, "TriStateCheckbox");
        Assert.Empty(diags.Where(d => d.Severity == DiagnosticSeverity.Error));
        Assert.NotNull(emitted);
        Assert.Contains("public sealed partial class TriStateCheckbox : global::ComposeNet.ComposableNode", emitted);
        // Enum becomes a positional ctor primitive.
        Assert.Contains("readonly global::MyApp.MyState _state", emitted);
        Assert.Contains("global::MyApp.MyState state", emitted);
    }

    [Fact]
    public void WrapperFacade_TryReadFromEnum_HandlesBit31()
    {
        // Regression: bit 31 of an int $default mask is negative when
        // read as `int`. TryReadFromEnum must still decode it correctly.
        // Verified by placing Modifier at bit 31 so the IModifier?
        // auto-mask emits "& ~(int)WideDefault.Modifier" — without the
        // fix, TryReadFromEnum drops the Modifier slot (v <= 0 filter)
        // and the auto-mask clear is silently omitted.
        var code = """
            using AndroidX.Compose.Runtime;
            using AndroidX.Compose.UI;
            using ComposeNet;
            using Kotlin.Jvm.Functions;
            using System;

            namespace MyApp
            {
                [Flags]
                public enum WideDefault
                {
                    Other      = 1 << 0,
                    Modifier   = unchecked((int)0x80000000), // bit 31
                    All        = Other | Modifier,
                }
            }

            namespace ComposeNet
            {
                public static partial class ComposeBridges
                {
                    [ComposeFacade(Defaults = typeof(MyApp.WideDefault))]
                    public static partial void Wide(int other, IModifier? modifier, int defaults, IComposer composer);

                    public static partial void Wide(int other, IModifier? modifier, int defaults, IComposer composer) { }
                }
            }
            """;
        var (_, diags, emitted) = Run(code, "Wide");
        Assert.Empty(diags.Where(d => d.Severity == DiagnosticSeverity.Error));
        Assert.NotNull(emitted);
        // The auto-mask clear for the IModifier? slot proves bit 31 was
        // decoded — without it, this clear would be missing.
        Assert.True(
            emitted!.Contains("WideDefault.Modifier"),
            "Expected emitted facade to reference WideDefault.Modifier (bit 31 slot). Emitted:\n" + emitted);
    }

    // ─── Phase 4 — [StateHolder] state-holder facades ─────────────────

    /// <summary>Shared snippet stubbing a DatePicker-style state class.</summary>
    const string DatePickerStateStubs = """
        namespace AndroidX.Compose.Material3
        {
            public interface IDatePickerState { }
        }
        namespace ComposeNet
        {
            public sealed class DatePickerState
            {
                internal AndroidX.Compose.Material3.IDatePickerState? Jvm;
            }
        }
        """;

    const string DatePickerSig =
        "(Landroidx/compose/material3/DatePickerState;Landroidx/compose/ui/Modifier;Landroidx/compose/material3/DatePickerFormatter;Landroidx/compose/material3/DatePickerColors;Lkotlin/jvm/functions/Function2;Lkotlin/jvm/functions/Function2;ZLandroidx/compose/runtime/Composer;II)V";

    [Fact]
    public void DatePicker_StateHolder_GeneratesRememberRoundTrip()
    {
        var code = $$"""
            using AndroidX.Compose.Runtime;
            using AndroidX.Compose.UI;
            using ComposeNet;
            using Kotlin.Jvm.Functions;
            using System;

            [assembly: ComposeDefaults("DatePickerDefault",
                "!state", "modifier", "dateFormatter", "colors", "title", "headline", "showModeToggle")]

            {{DatePickerStateStubs}}

            namespace ComposeNet
            {
                public static partial class ComposeBridges
                {
                    [ComposeBridge(Class="androidx/compose/material3/DatePickerKt",
                                   JvmName="DatePicker",
                                   Signature="{{DatePickerSig}}",
                                   Defaults=typeof(DatePickerDefault))]
                    [ComposeFacade]
                    public static partial void DatePicker(
                        [StateHolder(Remember = nameof(RememberDatePickerState),
                                     StateType = typeof(DatePickerState))]
                        IntPtr state,
                        IModifier? modifier,
                        int defaults,
                        IComposer composer);

                    public static IntPtr RememberDatePickerState(IComposer composer) => default;
                }
            }
            """;

        var (output, diags, emitted) = Run(code, "DatePicker");
        Assert.Empty(diags.Where(d => d.Severity == DiagnosticSeverity.Error));
        Assert.NotNull(emitted);

        // Ctor: state slot LAST, with default = null. The field is
        // emitted writable (no `readonly`) so partials can override
        // it via init-only convenience setters.
        Assert.Contains("global::ComposeNet.DatePickerState? _state;", emitted);
        Assert.DoesNotContain("readonly global::ComposeNet.DatePickerState? _state;", emitted);
        Assert.Contains("public DatePicker(global::ComposeNet.DatePickerState? state = null)", emitted);

        // Render: Remember + .Jvm population.
        Assert.Contains(
            "var __state = global::ComposeNet.ComposeBridges.RememberDatePickerState(composer);",
            emitted);
        Assert.Contains("if (_state is not null && _state.Jvm is null)", emitted);
        Assert.Contains(
            "_state.Jvm = global::Java.Lang.Object.GetObject<global::AndroidX.Compose.Material3.IDatePickerState>(__state, global::Android.Runtime.JniHandleOwnership.DoNotTransfer)!;",
            emitted);

        // Bridge call uses __state in the IntPtr slot.
        Assert.Contains(
            "global::ComposeNet.ComposeBridges.DatePicker(__state, __modifier, __defaults, composer);",
            emitted);

        var errors = output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
        Assert.Empty(errors);
    }

    [Fact]
    public void StateHolder_OnNonIntPtr_FailsCN3009()
    {
        var code = $$"""
            using AndroidX.Compose.Runtime;
            using AndroidX.Compose.UI;
            using ComposeNet;
            using Kotlin.Jvm.Functions;

            [assembly: ComposeDefaults("DatePickerDefault",
                "!state", "modifier", "dateFormatter", "colors", "title", "headline", "showModeToggle")]

            {{DatePickerStateStubs}}

            namespace ComposeNet
            {
                public static partial class ComposeBridges
                {
                    [ComposeBridge(Class="x/y/DatePickerKt", JvmName="DatePicker",
                                   Signature="{{DatePickerSig}}",
                                   Defaults=typeof(DatePickerDefault))]
                    [ComposeFacade]
                    public static partial void DatePicker(
                        [StateHolder(Remember = nameof(RememberDatePickerState),
                                     StateType = typeof(DatePickerState))]
                        int state,
                        IModifier? modifier,
                        int defaults,
                        IComposer composer);

                    public static System.IntPtr RememberDatePickerState(IComposer composer) => default;
                }
            }
            """;

        var (_, diags, emitted) = Run(code, "DatePicker");
        Assert.Null(emitted);
        Assert.Contains(diags, d => d.Id == "CN3009"
            && d.GetMessage().Contains("must annotate an 'IntPtr' parameter"));
    }

    [Fact]
    public void StateHolder_MissingJvmField_FailsCN3009()
    {
        // Wrapper class with NO Jvm field — generator must reject.
        var code = $$"""
            using AndroidX.Compose.Runtime;
            using AndroidX.Compose.UI;
            using ComposeNet;
            using Kotlin.Jvm.Functions;
            using System;

            [assembly: ComposeDefaults("DatePickerDefault",
                "!state", "modifier", "dateFormatter", "colors", "title", "headline", "showModeToggle")]

            namespace AndroidX.Compose.Material3 { public interface IDatePickerState { } }
            namespace ComposeNet { public sealed class BadState { } }

            namespace ComposeNet
            {
                public static partial class ComposeBridges
                {
                    [ComposeBridge(Class="x/y/DatePickerKt", JvmName="DatePicker",
                                   Signature="{{DatePickerSig}}",
                                   Defaults=typeof(DatePickerDefault))]
                    [ComposeFacade]
                    public static partial void DatePicker(
                        [StateHolder(Remember = nameof(RememberDatePickerState),
                                     StateType = typeof(BadState))]
                        IntPtr state,
                        IModifier? modifier,
                        int defaults,
                        IComposer composer);

                    public static IntPtr RememberDatePickerState(IComposer composer) => default;
                }
            }
            """;

        var (_, diags, emitted) = Run(code, "DatePicker");
        Assert.Null(emitted);
        Assert.Contains(diags, d => d.Id == "CN3009"
            && d.GetMessage().Contains("no instance field named 'Jvm'"));
    }

    [Fact]
    public void StateHolder_NonSuppressedStateBit_ClearedByMask()
    {
        // If a future declaration forgets the "!" prefix and exposes
        // the state bit as an enum member, the StateHolder belt+suspenders
        // mask path MUST still clear it so the bridge sees "supplied".
        var code = $$"""
            using AndroidX.Compose.Runtime;
            using AndroidX.Compose.UI;
            using ComposeNet;
            using Kotlin.Jvm.Functions;
            using System;

            [assembly: ComposeDefaults("DatePickerDefault",
                "state", "modifier", "dateFormatter", "colors", "title", "headline", "showModeToggle")]

            {{DatePickerStateStubs}}

            namespace ComposeNet
            {
                public static partial class ComposeBridges
                {
                    [ComposeBridge(Class="x/y/DatePickerKt", JvmName="DatePicker",
                                   Signature="{{DatePickerSig}}",
                                   Defaults=typeof(DatePickerDefault))]
                    [ComposeFacade]
                    public static partial void DatePicker(
                        [StateHolder(Remember = nameof(RememberDatePickerState),
                                     StateType = typeof(DatePickerState))]
                        IntPtr state,
                        IModifier? modifier,
                        int defaults,
                        IComposer composer);

                    public static IntPtr RememberDatePickerState(IComposer composer) => default;
                }
            }
            """;

        var (_, diags, emitted) = Run(code, "DatePicker");
        Assert.Empty(diags.Where(d => d.Severity == DiagnosticSeverity.Error));
        Assert.NotNull(emitted);
        Assert.Contains(
            "__defaults &= ~(int)global::ComposeNet.DatePickerDefault.State;",
            emitted);
    }

    // ─── Phase 4b — parameterised Remember*State (TimePicker-style) ──

    /// <summary>Shared snippet stubbing a TimePicker-style state class
    /// with the Kotlin "initialX → live X" convention.</summary>
    const string TimePickerStateStubs = """
        namespace AndroidX.Compose.Material3
        {
            public interface ITimePickerState
            {
                int Hour { get; set; }
                int Minute { get; set; }
                bool Is24hour();
            }
        }
        namespace ComposeNet
        {
            public sealed class TimePickerState
            {
                internal AndroidX.Compose.Material3.ITimePickerState? Jvm;
                public TimePickerState(int initialHour = 12, int initialMinute = 0, bool is24Hour = true)
                {
                    InitialHour = initialHour;
                    InitialMinute = initialMinute;
                    InitialIs24Hour = is24Hour;
                }
                internal int InitialHour { get; }
                internal int InitialMinute { get; }
                internal bool InitialIs24Hour { get; }
                public int Hour => Jvm?.Hour ?? InitialHour;
                public int Minute => Jvm?.Minute ?? InitialMinute;
                public bool Is24Hour => Jvm?.Is24hour() ?? InitialIs24Hour;
            }
        }
        """;

    const string TimePickerSig =
        "(Landroidx/compose/material3/TimePickerState;Landroidx/compose/ui/Modifier;Landroidx/compose/material3/TimePickerColors;ILandroidx/compose/runtime/Composer;II)V";

    [Fact]
    public void TimePicker_ParameterisedStateHolder_GeneratesAutoCreateAndArgs()
    {
        var code = $$"""
            using AndroidX.Compose.Runtime;
            using AndroidX.Compose.UI;
            using ComposeNet;
            using Kotlin.Jvm.Functions;
            using System;

            [assembly: ComposeDefaults("TimePickerDefault",
                "!state", "modifier", "colors", "layoutType")]

            {{TimePickerStateStubs}}

            namespace ComposeNet
            {
                public static partial class ComposeBridges
                {
                    [ComposeBridge(Class="androidx/compose/material3/TimePickerKt",
                                   JvmName="TimePicker-mT9BvqQ",
                                   Signature="{{TimePickerSig}}",
                                   Defaults=typeof(TimePickerDefault))]
                    [ComposeFacade]
                    public static partial void TimePicker(
                        [StateHolder(Remember = nameof(RememberTimePickerState),
                                     StateType = typeof(TimePickerState))]
                        IntPtr state,
                        IModifier? modifier,
                        int defaults,
                        IComposer composer);

                    public static IntPtr RememberTimePickerState(int initialHour, int initialMinute,
                                                                 bool is24Hour, IComposer composer) => default;
                }
            }
            """;

        var (output, diags, emitted) = Run(code, "TimePicker");
        Assert.Empty(diags.Where(d => d.Severity == DiagnosticSeverity.Error));
        Assert.NotNull(emitted);

        // Ctor: parameterised StateHolder auto-creates the wrapper when
        // the caller passes null, so the field is guaranteed non-null
        // even though the ctor param keeps its = null default. The
        // field is emitted writable (no `readonly`) so partials can
        // override it via init-only convenience setters.
        Assert.Contains("global::ComposeNet.TimePickerState? _state;", emitted);
        Assert.DoesNotContain("readonly global::ComposeNet.TimePickerState? _state;", emitted);
        Assert.Contains("public TimePicker(global::ComposeNet.TimePickerState? state = null)", emitted);
        Assert.Contains("_state = state ?? new global::ComposeNet.TimePickerState();", emitted);

        // Render: Remember called with wrapper-sourced init args.
        // InitialHour/InitialMinute resolve via Pascal match;
        // is24Hour falls back to Is24Hour (live getter), which returns
        // InitialIs24Hour while Jvm is still null on first render.
        Assert.Contains(
            "var __state = global::ComposeNet.ComposeBridges.RememberTimePickerState(_state!.InitialHour, _state!.InitialMinute, _state!.Is24Hour, composer);",
            emitted);
        // Unguarded Jvm population — Phase 4b knows _state is non-null.
        Assert.Contains("if (_state.Jvm is null)", emitted);
        Assert.DoesNotContain("if (_state is not null && _state.Jvm is null)", emitted);
        Assert.Contains(
            "_state.Jvm = global::Java.Lang.Object.GetObject<global::AndroidX.Compose.Material3.ITimePickerState>(__state, global::Android.Runtime.JniHandleOwnership.DoNotTransfer)!;",
            emitted);

        // Bridge call uses __state in the IntPtr slot.
        Assert.Contains(
            "global::ComposeNet.ComposeBridges.TimePicker(__state, __modifier, __defaults, composer);",
            emitted);

        var errors = output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
        Assert.Empty(errors);
    }

    // ─── Phase 4c — shared-state caching (StateHolder.SharedState) ────

    [Fact]
    public void SharedState_Phase4b_GeneratesCachedHandleReuse()
    {
        // TimePicker / TimeInput share the same TimePickerState wrapper
        // across sibling facades. When the first facade renders it
        // calls Remember and binds Jvm; subsequent siblings must skip
        // the Remember call and reuse the cached handle.
        var code = $$"""
            using AndroidX.Compose.Runtime;
            using AndroidX.Compose.UI;
            using ComposeNet;
            using Kotlin.Jvm.Functions;
            using System;

            [assembly: ComposeDefaults("TimePickerDefault",
                "!state", "modifier", "colors", "layoutType")]

            {{TimePickerStateStubs}}

            namespace ComposeNet
            {
                public static partial class ComposeBridges
                {
                    [ComposeBridge(Class="androidx/compose/material3/TimePickerKt",
                                   JvmName="TimePicker-mT9BvqQ",
                                   Signature="{{TimePickerSig}}",
                                   Defaults=typeof(TimePickerDefault))]
                    [ComposeFacade]
                    public static partial void TimePicker(
                        [StateHolder(Remember = nameof(RememberTimePickerState),
                                     StateType = typeof(TimePickerState),
                                     SharedState = true)]
                        IntPtr state,
                        IModifier? modifier,
                        int defaults,
                        IComposer composer);

                    public static IntPtr RememberTimePickerState(int initialHour, int initialMinute,
                                                                 bool is24Hour, IComposer composer) => default;
                }
            }
            """;

        var (output, diags, emitted) = Run(code, "TimePicker");
        Assert.Empty(diags.Where(d => d.Severity == DiagnosticSeverity.Error));
        Assert.NotNull(emitted);

        // Declares a local IntPtr the bridge will consume.
        Assert.Contains("global::System.IntPtr __state;", emitted);

        // Cache-hit branch — Jvm already bound by a sibling.
        Assert.Contains("if (_state!.Jvm is not null)", emitted);
        Assert.Contains(
            "__state = ((global::Android.Runtime.IJavaObject)_state.Jvm!).Handle;",
            emitted);

        // Cache-miss branch — call Remember, populate Jvm so the next
        // sibling will hit the cached path.
        Assert.Contains(
            "__state = global::ComposeNet.ComposeBridges.RememberTimePickerState(_state!.InitialHour, _state!.InitialMinute, _state!.Is24Hour, composer);",
            emitted);
        // Phase 4b assigns unguarded (ctor auto-create guarantees non-null).
        Assert.Contains(
            "_state.Jvm = global::Java.Lang.Object.GetObject<global::AndroidX.Compose.Material3.ITimePickerState>(__state, global::Android.Runtime.JniHandleOwnership.DoNotTransfer)!;",
            emitted);

        // Must NOT emit the non-shared "always call Remember" preamble.
        Assert.DoesNotContain(
            "var __state = global::ComposeNet.ComposeBridges.RememberTimePickerState",
            emitted);

        // Bridge call still uses __state.
        Assert.Contains(
            "global::ComposeNet.ComposeBridges.TimePicker(__state, __modifier, __defaults, composer);",
            emitted);

        var errors = output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
        Assert.Empty(errors);
    }

    [Fact]
    public void SharedState_Phase4_GeneratesNullableCachedHandleReuse()
    {
        // Zero-user-param Remember (DatePicker-style). _state is
        // nullable because there's no auto-create. SharedState must
        // skip Remember when the caller supplied a wrapper with a
        // populated Jvm field.
        var code = $$"""
            using AndroidX.Compose.Runtime;
            using AndroidX.Compose.UI;
            using ComposeNet;
            using Kotlin.Jvm.Functions;
            using System;

            [assembly: ComposeDefaults("DatePickerDefault",
                "!state", "modifier", "dateFormatter", "colors", "title", "headline", "showModeToggle")]

            {{DatePickerStateStubs}}

            namespace ComposeNet
            {
                public static partial class ComposeBridges
                {
                    [ComposeBridge(Class="androidx/compose/material3/DatePickerKt",
                                   JvmName="DatePicker",
                                   Signature="{{DatePickerSig}}",
                                   Defaults=typeof(DatePickerDefault))]
                    [ComposeFacade]
                    public static partial void DatePicker(
                        [StateHolder(Remember = nameof(RememberDatePickerState),
                                     StateType = typeof(DatePickerState),
                                     SharedState = true)]
                        IntPtr state,
                        IModifier? modifier,
                        int defaults,
                        IComposer composer);

                    public static IntPtr RememberDatePickerState(IComposer composer) => default;
                }
            }
            """;

        var (output, diags, emitted) = Run(code, "DatePicker");
        Assert.Empty(diags.Where(d => d.Severity == DiagnosticSeverity.Error));
        Assert.NotNull(emitted);

        // Phase 4 field stays nullable (no auto-create in ctor). The
        // field is emitted writable (no `readonly`) so partials can
        // override it via init-only convenience setters.
        Assert.Contains("global::ComposeNet.DatePickerState? _state;", emitted);
        Assert.DoesNotContain("readonly global::ComposeNet.DatePickerState? _state;", emitted);
        Assert.DoesNotContain("_state = state ?? new global::ComposeNet.DatePickerState();", emitted);

        // Cache-hit branch — guarded with explicit null check on _state.
        Assert.Contains("if (_state is not null && _state.Jvm is not null)", emitted);
        Assert.Contains(
            "__state = ((global::Android.Runtime.IJavaObject)_state.Jvm).Handle;",
            emitted);

        // Cache-miss branch — Remember + null-guarded Jvm assignment.
        Assert.Contains(
            "__state = global::ComposeNet.ComposeBridges.RememberDatePickerState(composer);",
            emitted);
        Assert.Contains("if (_state is not null)", emitted);
        Assert.Contains(
            "_state.Jvm = global::Java.Lang.Object.GetObject<global::AndroidX.Compose.Material3.IDatePickerState>(__state, global::Android.Runtime.JniHandleOwnership.DoNotTransfer)!;",
            emitted);

        // Must NOT emit the non-shared "always call Remember" preamble.
        Assert.DoesNotContain(
            "var __state = global::ComposeNet.ComposeBridges.RememberDatePickerState",
            emitted);

        var errors = output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
        Assert.Empty(errors);
    }

    [Fact]
    public void SharedState_DefaultsFalse_FallsBackToNonSharedPreamble()
    {
        // Regression: when SharedState is omitted, the generator emits
        // the existing "always call Remember" preamble, not the cached
        // path. Guards against the SharedState branch being accidentally
        // promoted to the default behavior.
        var code = $$"""
            using AndroidX.Compose.Runtime;
            using AndroidX.Compose.UI;
            using ComposeNet;
            using Kotlin.Jvm.Functions;
            using System;

            [assembly: ComposeDefaults("TimePickerDefault",
                "!state", "modifier", "colors", "layoutType")]

            {{TimePickerStateStubs}}

            namespace ComposeNet
            {
                public static partial class ComposeBridges
                {
                    [ComposeBridge(Class="androidx/compose/material3/TimePickerKt",
                                   JvmName="TimePicker-mT9BvqQ",
                                   Signature="{{TimePickerSig}}",
                                   Defaults=typeof(TimePickerDefault))]
                    [ComposeFacade]
                    public static partial void TimePicker(
                        [StateHolder(Remember = nameof(RememberTimePickerState),
                                     StateType = typeof(TimePickerState))]
                        IntPtr state,
                        IModifier? modifier,
                        int defaults,
                        IComposer composer);

                    public static IntPtr RememberTimePickerState(int initialHour, int initialMinute,
                                                                 bool is24Hour, IComposer composer) => default;
                }
            }
            """;

        var (_, diags, emitted) = Run(code, "TimePicker");
        Assert.Empty(diags.Where(d => d.Severity == DiagnosticSeverity.Error));
        Assert.NotNull(emitted);

        // Non-shared shape: unconditional Remember call, no cache check.
        Assert.Contains(
            "var __state = global::ComposeNet.ComposeBridges.RememberTimePickerState(_state!.InitialHour, _state!.InitialMinute, _state!.Is24Hour, composer);",
            emitted);
        Assert.DoesNotContain("global::System.IntPtr __state;", emitted);
        Assert.DoesNotContain("if (_state!.Jvm is not null)", emitted);
        Assert.DoesNotContain("if (_state is not null && _state.Jvm is not null)", emitted);
    }

    [Fact]
    public void ParameterisedStateHolder_UnresolvedMember_FailsCN3009()
    {
        // RememberFooState takes 'mystery' which is not a member of
        // TimePickerState (and 'InitialMystery' doesn't exist either),
        // so the generator must reject.
        var code = $$"""
            using AndroidX.Compose.Runtime;
            using AndroidX.Compose.UI;
            using ComposeNet;
            using Kotlin.Jvm.Functions;
            using System;

            [assembly: ComposeDefaults("TimePickerDefault",
                "!state", "modifier", "colors", "layoutType")]

            {{TimePickerStateStubs}}

            namespace ComposeNet
            {
                public static partial class ComposeBridges
                {
                    [ComposeBridge(Class="x/y/TimePickerKt", JvmName="TimePicker-mT9BvqQ",
                                   Signature="{{TimePickerSig}}",
                                   Defaults=typeof(TimePickerDefault))]
                    [ComposeFacade]
                    public static partial void TimePicker(
                        [StateHolder(Remember = nameof(RememberTimePickerState),
                                     StateType = typeof(TimePickerState))]
                        IntPtr state,
                        IModifier? modifier,
                        int defaults,
                        IComposer composer);

                    public static IntPtr RememberTimePickerState(int mystery, IComposer composer) => default;
                }
            }
            """;

        var (_, diags, emitted) = Run(code, "TimePicker");
        Assert.Null(emitted);
        Assert.Contains(diags, d => d.Id == "CN3009"
            && d.GetMessage().Contains("cannot resolve Remember parameter 'mystery'")
            && d.GetMessage().Contains("InitialMystery"));
    }

    [Fact]
    public void ParameterisedStateHolder_NoParameterlessCtor_FailsCN3009()
    {
        // StateType only has an all-required-params ctor, so the
        // facade can't auto-create it. CN3009 fires.
        var code = $$"""
            using AndroidX.Compose.Runtime;
            using AndroidX.Compose.UI;
            using ComposeNet;
            using Kotlin.Jvm.Functions;
            using System;

            [assembly: ComposeDefaults("TimePickerDefault",
                "!state", "modifier", "colors", "layoutType")]
            namespace AndroidX.Compose.Material3 { public interface ITimePickerState { } }
            namespace ComposeNet
            {
                public sealed class TimePickerState
                {
                    internal AndroidX.Compose.Material3.ITimePickerState? Jvm;
                    public TimePickerState(int initialHour, int initialMinute, bool is24Hour) { }
                    public int InitialHour => 0;
                    public int InitialMinute => 0;
                    public bool Is24Hour => false;
                }
            }

            namespace ComposeNet
            {
                public static partial class ComposeBridges
                {
                    [ComposeBridge(Class="x/y/TimePickerKt", JvmName="TimePicker-mT9BvqQ",
                                   Signature="{{TimePickerSig}}",
                                   Defaults=typeof(TimePickerDefault))]
                    [ComposeFacade]
                    public static partial void TimePicker(
                        [StateHolder(Remember = nameof(RememberTimePickerState),
                                     StateType = typeof(TimePickerState))]
                        IntPtr state,
                        IModifier? modifier,
                        int defaults,
                        IComposer composer);

                    public static IntPtr RememberTimePickerState(int initialHour, int initialMinute,
                                                                 bool is24Hour, IComposer composer) => default;
                }
            }
            """;

        var (_, diags, emitted) = Run(code, "TimePicker");
        Assert.Null(emitted);
        Assert.Contains(diags, d => d.Id == "CN3009"
            && d.GetMessage().Contains("parameterised Remember requires StateType")
            && d.GetMessage().Contains("constructible with no arguments"));
    }

    [Fact]
    public void StateHolder_Phase4_StillEmitsGuardedJvmPopulation()
    {
        // Belt+suspenders: confirm the existing Phase 4 (zero-user-param
        // Remember) path still emits the nullable guard `if (_state is
        // not null && ...)`. Phase 4b's stricter unguarded path is only
        // for parameterised remembers.
        var code = $$"""
            using AndroidX.Compose.Runtime;
            using AndroidX.Compose.UI;
            using ComposeNet;
            using Kotlin.Jvm.Functions;
            using System;

            [assembly: ComposeDefaults("DatePickerDefault",
                "!state", "modifier", "dateFormatter", "colors", "title", "headline", "showModeToggle")]

            {{DatePickerStateStubs}}

            namespace ComposeNet
            {
                public static partial class ComposeBridges
                {
                    [ComposeBridge(Class="x/y/DatePickerKt", JvmName="DatePicker",
                                   Signature="{{DatePickerSig}}",
                                   Defaults=typeof(DatePickerDefault))]
                    [ComposeFacade]
                    public static partial void DatePicker(
                        [StateHolder(Remember = nameof(RememberDatePickerState),
                                     StateType = typeof(DatePickerState))]
                        IntPtr state,
                        IModifier? modifier,
                        int defaults,
                        IComposer composer);

                    public static IntPtr RememberDatePickerState(IComposer composer) => default;
                }
            }
            """;

        var (_, diags, emitted) = Run(code, "DatePicker");
        Assert.Empty(diags.Where(d => d.Severity == DiagnosticSeverity.Error));
        Assert.NotNull(emitted);
        Assert.Contains("if (_state is not null && _state.Jvm is null)", emitted);
        Assert.DoesNotContain("_state = state ?? new", emitted);
    }

    // ---------------------------------------------------------------
    // OptionalValue — Compose value-class types (Sp/Dp/Em/TextAlign)
    // and reference-typed wrappers (FontWeight/TextDecoration/Shape)
    // surface as nullable auto-properties on the generated facade.
    // ---------------------------------------------------------------

    [Fact]
    public void OptionalValue_SpEmitsNullableAutoProperty()
    {
        var code = """
            using AndroidX.Compose.Runtime;
            using AndroidX.Compose.UI;
            using ComposeNet;
            using Kotlin.Jvm.Functions;

            [assembly: ComposeDefaults("TextDefault",
                "!text", "modifier", "fontSize")]

            namespace ComposeNet
            {
                public static partial class ComposeBridges
                {
                    [ComposeBridge(Class="androidx/compose/material3/TextKt", JvmName="Text",
                                   Signature="(Ljava/lang/String;Landroidx/compose/ui/Modifier;JLandroidx/compose/runtime/Composer;II)V",
                                   Defaults=typeof(TextDefault))]
                    [ComposeFacade]
                    public static partial void Text(string text, IModifier? modifier, Sp? fontSize, IComposer composer);
                }
            }
            """;

        var (output, diags, emitted) = Run(code, "Text");
        Assert.Empty(diags.Where(d => d.Severity == DiagnosticSeverity.Error));
        Assert.NotNull(emitted);
        // Property emitted as nullable Sp.
        Assert.Contains("public global::ComposeNet.Sp? FontSize { get; set; }", emitted);
        // Bridge call passes property through (no ctor slot, no field).
        Assert.Contains("global::ComposeNet.ComposeBridges.Text(_text, BuildModifier(), FontSize, composer);", emitted);
        // Not a ctor parameter.
        Assert.DoesNotContain("Sp? fontSize", emitted);

        var errors = output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
        Assert.Empty(errors);
    }

    [Fact]
    public void OptionalValue_FontWeightEmitsNullableReferenceProperty()
    {
        var code = """
            using AndroidX.Compose.Runtime;
            using AndroidX.Compose.UI;
            using ComposeNet;
            using Kotlin.Jvm.Functions;

            [assembly: ComposeDefaults("TextDefault",
                "!text", "modifier", "fontWeight")]

            namespace ComposeNet
            {
                public static partial class ComposeBridges
                {
                    [ComposeBridge(Class="androidx/compose/material3/TextKt", JvmName="Text",
                                   Signature="(Ljava/lang/String;Landroidx/compose/ui/Modifier;Landroidx/compose/ui/text/font/FontWeight;Landroidx/compose/runtime/Composer;II)V",
                                   Defaults=typeof(TextDefault))]
                    [ComposeFacade]
                    public static partial void Text(string text, IModifier? modifier, FontWeight? fontWeight, IComposer composer);
                }
            }
            """;

        var (output, diags, emitted) = Run(code, "Text");
        Assert.Empty(diags.Where(d => d.Severity == DiagnosticSeverity.Error));
        Assert.NotNull(emitted);
        // Reference-typed wrapper surfaces as a nullable property.
        Assert.Contains("public global::ComposeNet.FontWeight? FontWeight { get; set; }", emitted);
        Assert.Contains("global::ComposeNet.ComposeBridges.Text(_text, BuildModifier(), FontWeight, composer);", emitted);

        var errors = output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
        Assert.Empty(errors);
    }

    [Fact]
    public void OptionalValue_NonNullableReferenceWrapperIsRejected()
    {
        // Non-nullable FontWeight (no `?`) must not classify as
        // OptionalValue: surfacing it as a nullable auto-property would
        // pass `null` straight to a bridge slot the caller declared as
        // non-nullable. CN3002 is expected.
        var code = """
            using AndroidX.Compose.Runtime;
            using AndroidX.Compose.UI;
            using ComposeNet;
            using Kotlin.Jvm.Functions;

            [assembly: ComposeDefaults("TextDefault", "!text", "modifier", "fontWeight")]

            namespace ComposeNet
            {
                public static partial class ComposeBridges
                {
                    [ComposeBridge(Class="androidx/compose/material3/TextKt", JvmName="Text",
                                   Signature="(Ljava/lang/String;Landroidx/compose/ui/Modifier;Landroidx/compose/ui/text/font/FontWeight;Landroidx/compose/runtime/Composer;II)V",
                                   Defaults=typeof(TextDefault))]
                    [ComposeFacade]
                    public static partial void Text(string text, IModifier? modifier, FontWeight fontWeight, IComposer composer);
                }
            }
            """;

        var (_, diags, _) = Run(code, "Text");
        Assert.Contains(diags, d => d.Id == "CN3002");
    }

    [Fact]
    public void OptionalValue_MultipleValueAndReferenceTypesCoexist()
    {
        var code = """
            using AndroidX.Compose.Runtime;
            using AndroidX.Compose.UI;
            using ComposeNet;
            using Kotlin.Jvm.Functions;

            [assembly: ComposeDefaults("TextDefault",
                "!text", "modifier", "fontSize", "fontWeight", "letterSpacing", "decoration", "lineHeight")]

            namespace ComposeNet
            {
                public static partial class ComposeBridges
                {
                    [ComposeBridge(Class="androidx/compose/material3/TextKt", JvmName="Text",
                                   Signature="(Ljava/lang/String;Landroidx/compose/ui/Modifier;JLandroidx/compose/ui/text/font/FontWeight;JLandroidx/compose/ui/text/style/TextDecoration;JLandroidx/compose/runtime/Composer;II)V",
                                   Defaults=typeof(TextDefault))]
                    [ComposeFacade]
                    public static partial void Text(
                        string text, IModifier? modifier,
                        Sp? fontSize, FontWeight? fontWeight,
                        Sp? letterSpacing, TextDecoration? decoration,
                        Sp? lineHeight,
                        IComposer composer);
                }
            }
            """;

        var (output, diags, emitted) = Run(code, "Text");
        Assert.Empty(diags.Where(d => d.Severity == DiagnosticSeverity.Error));
        Assert.NotNull(emitted);
        Assert.Contains("public global::ComposeNet.Sp? FontSize { get; set; }", emitted);
        Assert.Contains("public global::ComposeNet.FontWeight? FontWeight { get; set; }", emitted);
        Assert.Contains("public global::ComposeNet.Sp? LetterSpacing { get; set; }", emitted);
        Assert.Contains("public global::ComposeNet.TextDecoration? Decoration { get; set; }", emitted);
        Assert.Contains("public global::ComposeNet.Sp? LineHeight { get; set; }", emitted);
        // PascalCased property names flow through the bridge call.
        Assert.Contains(
            "global::ComposeNet.ComposeBridges.Text(_text, BuildModifier(), FontSize, FontWeight, LetterSpacing, Decoration, LineHeight, composer);",
            emitted);

        var errors = output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
        Assert.Empty(errors);
    }

    [Fact]
    public void OptionalValue_TextOverflowAndDpAreClassifiedAsValueTypes()
    {
        // TextOverflow is a non-nullable @JvmInline value class in
        // Compose source — it travels as packed `I`. Dp is also packed
        // (`F`). Both surface as nullable auto-properties.
        var code = """
            using AndroidX.Compose.Runtime;
            using AndroidX.Compose.UI;
            using ComposeNet;
            using Kotlin.Jvm.Functions;

            [assembly: ComposeDefaults("FooDefault",
                "!a", "overflow", "size")]

            namespace ComposeNet
            {
                public static partial class ComposeBridges
                {
                    [ComposeBridge(Class="x/Foo", JvmName="Foo",
                                   Signature="(Ljava/lang/String;IFLandroidx/compose/runtime/Composer;II)V",
                                   Defaults=typeof(FooDefault))]
                    [ComposeFacade]
                    public static partial void Foo(string a, TextOverflow? overflow, Dp? size, IComposer composer);
                }
            }
            """;

        var (output, diags, emitted) = Run(code, "Foo");
        Assert.Empty(diags.Where(d => d.Severity == DiagnosticSeverity.Error));
        Assert.NotNull(emitted);
        Assert.Contains("public global::ComposeNet.TextOverflow? Overflow { get; set; }", emitted);
        Assert.Contains("public global::ComposeNet.Dp? Size { get; set; }", emitted);

        var errors = output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
        Assert.Empty(errors);
    }

    [Fact]
    public void OptionalValue_ClearsBitInCallerProvidesDefaultsPath()
    {
        // With a user-declared `int defaults` param, the facade owns
        // the mask. OptionalValue properties must clear their bit when
        // the caller assigned a non-null value.
        var code = """
            using AndroidX.Compose.Runtime;
            using AndroidX.Compose.UI;
            using ComposeNet;
            using Kotlin.Jvm.Functions;

            [assembly: ComposeDefaults("BarDefault",
                "!a", "modifier", "fontSize", "fontWeight")]

            namespace ComposeNet
            {
                public static partial class ComposeBridges
                {
                    [ComposeBridge(Class="x/Bar", JvmName="Bar",
                                   Signature="(Ljava/lang/String;Landroidx/compose/ui/Modifier;JLandroidx/compose/ui/text/font/FontWeight;Landroidx/compose/runtime/Composer;II)V",
                                   Defaults=typeof(BarDefault))]
                    [ComposeFacade]
                    public static partial void Bar(
                        string a, IModifier? modifier,
                        Sp? fontSize, FontWeight? fontWeight,
                        int defaults, IComposer composer);
                }
            }
            """;

        var (output, diags, emitted) = Run(code, "Bar");
        Assert.Empty(diags.Where(d => d.Severity == DiagnosticSeverity.Error));
        Assert.NotNull(emitted);
        Assert.Contains(
            "if (FontSize is not null) __defaults &= ~(int)global::ComposeNet.BarDefault.FontSize;",
            emitted);
        Assert.Contains(
            "if (FontWeight is not null) __defaults &= ~(int)global::ComposeNet.BarDefault.FontWeight;",
            emitted);
        var errors = output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
        Assert.Empty(errors);
    }

    [Fact]
    public void OptionalValue_NullablePrimitivesEmitNullableAutoProperties()
    {
        // bool? / int? / long? params surface as Optional auto-properties
        // — null leaves the Kotlin default in place via the auto-mask
        // bit, a value clears the bit and lowers to the JNI primitive
        // slot.
        var code = """
            using AndroidX.Compose.Runtime;
            using AndroidX.Compose.UI;
            using ComposeNet;
            using Kotlin.Jvm.Functions;

            [assembly: ComposeDefaults("PrimDefault",
                "!text", "modifier", "softWrap", "maxLines", "color")]

            namespace ComposeNet
            {
                public static partial class ComposeBridges
                {
                    [ComposeBridge(Class="x/Y", JvmName="F",
                                   Signature="(Ljava/lang/String;Landroidx/compose/ui/Modifier;ZIJLandroidx/compose/runtime/Composer;II)V",
                                   Defaults=typeof(PrimDefault))]
                    [ComposeFacade]
                    public static partial void F(
                        string text, IModifier? modifier,
                        bool? softWrap, int? maxLines, long? color,
                        IComposer composer);
                }
            }
            """;

        var (output, diags, emitted) = Run(code, "F");
        Assert.Empty(diags.Where(d => d.Severity == DiagnosticSeverity.Error));
        Assert.NotNull(emitted);
        // Each nullable primitive surfaces as an auto-property.
        Assert.Contains("public bool? SoftWrap { get; set; }", emitted);
        Assert.Contains("public int? MaxLines { get; set; }", emitted);
        Assert.Contains("public long? Color { get; set; }", emitted);
        // The bridge call passes the property names through.
        Assert.Contains("global::ComposeNet.ComposeBridges.F(_text, BuildModifier(), SoftWrap, MaxLines, Color, composer);", emitted);
        // Not surfaced as ctor parameters.
        Assert.DoesNotContain("bool? softWrap", emitted);
        Assert.DoesNotContain("int? maxLines", emitted);
        Assert.DoesNotContain("long? color", emitted);

        var errors = output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
        Assert.Empty(errors);
    }

    // ─── [ConfirmStateChange] — per-instance JNI veto adapter ─────────

    /// <summary>Shared snippet stubbing a DrawerState-style state class
    /// plus the convention-named JCW adapter
    /// (<c>&lt;TName&gt;ConfirmStateChange</c>) the generator looks up
    /// from a <c>[ConfirmStateChange(typeof(T))]</c> attribute.</summary>
    const string DrawerStateStubs = """
        namespace AndroidX.Compose.Material3
        {
            public enum DrawerValue { Closed, Open }
            public interface IDrawerState { }
        }
        namespace ComposeNet
        {
            public sealed class DrawerStateHolder
            {
                internal AndroidX.Compose.Material3.IDrawerState? Jvm;
                public AndroidX.Compose.Material3.DrawerValue InitialValue { get; }
                public DrawerStateHolder() { }
            }
            public sealed class DrawerValueConfirmStateChange : Kotlin.Jvm.Functions.IFunction1
            {
                public System.Func<AndroidX.Compose.Material3.DrawerValue, bool>? Callback { get; set; }
            }
        }
        """;

    const string DrawerSig =
        "(Lkotlin/jvm/functions/Function2;Landroidx/compose/ui/Modifier;Landroidx/compose/material3/DrawerState;ZJLkotlin/jvm/functions/Function2;Landroidx/compose/runtime/Composer;II)V";

    [Fact]
    public void ConfirmStateChange_GeneratesAdapterFieldPropertyAndPreamble()
    {
        // Happy path: a Remember bridge takes an IFunction1?
        // `confirmStateChange` param annotated with
        // [ConfirmStateChange(typeof(DrawerValue))]. The facade should:
        //   - allocate a single readonly adapter field
        //   - expose a Func<DrawerValue, bool>? ConfirmStateChange prop
        //   - assign Callback = ConfirmStateChange in the Render
        //     preamble BEFORE calling Remember
        //   - forward _confirmStateChangeAdapter to the Remember bridge
        //     as the IFunction1? slot
        var code = $$"""
            using AndroidX.Compose.Runtime;
            using AndroidX.Compose.UI;
            using ComposeNet;
            using Kotlin.Jvm.Functions;
            using System;

            [assembly: ComposeDefaults("DrawerDefault",
                "!drawerContent", "modifier", "!drawerState", "gesturesEnabled", "scrimColor", "!content")]

            {{DrawerStateStubs}}

            namespace ComposeNet
            {
                public static partial class ComposeBridges
                {
                    [ComposeBridge(Class="androidx/compose/material3/NavigationDrawerKt",
                                   JvmName="ModalNavigationDrawer",
                                   Signature="{{DrawerSig}}",
                                   Defaults=typeof(DrawerDefault))]
                    [ComposeFacade]
                    public static partial void Drawer(
                        IFunction2 drawerContent,
                        IModifier? modifier,
                        [StateHolder(Remember = nameof(RememberDrawerState),
                                     StateType = typeof(DrawerStateHolder))]
                        IntPtr drawerState,
                        IFunction2 content,
                        int defaults,
                        IComposer composer);

                    public static IntPtr RememberDrawerState(
                        AndroidX.Compose.Material3.DrawerValue initialValue,
                        [ConfirmStateChange(typeof(AndroidX.Compose.Material3.DrawerValue))]
                        IFunction1? confirmStateChange,
                        IComposer composer) => default;
                }
            }
            """;

        var (output, diags, emitted) = Run(code, "Drawer");
        Assert.Empty(diags.Where(d => d.Severity == DiagnosticSeverity.Error));
        Assert.NotNull(emitted);

        // (a) adapter field allocated once.
        Assert.Contains(
            "readonly global::ComposeNet.DrawerValueConfirmStateChange _confirmStateChangeAdapter = new global::ComposeNet.DrawerValueConfirmStateChange();",
            emitted);
        // (b) ConfirmStateChange property surfaces with the right type.
        Assert.Contains(
            "public global::System.Func<global::AndroidX.Compose.Material3.DrawerValue, bool>? ConfirmStateChange { get; set; }",
            emitted);
        // (c) Render preamble assigns the user delegate into the adapter.
        Assert.Contains(
            "_confirmStateChangeAdapter.Callback = ConfirmStateChange;",
            emitted);
        // (d) Remember call forwards the adapter into the IFunction1? slot.
        Assert.Contains(
            "global::ComposeNet.ComposeBridges.RememberDrawerState(_drawerState!.InitialValue, _confirmStateChangeAdapter, composer)",
            emitted);

        var errors = output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
        Assert.Empty(errors);
    }

    [Fact]
    public void ConfirmStateChange_OnNonIFunction1_FailsCN3011()
    {
        // Put [ConfirmStateChange] on a primitive param — generator
        // must reject with CN3011 because the slot is meant for an
        // IFunction1 veto adapter.
        var code = $$"""
            using AndroidX.Compose.Runtime;
            using AndroidX.Compose.UI;
            using ComposeNet;
            using Kotlin.Jvm.Functions;
            using System;

            [assembly: ComposeDefaults("DrawerDefault",
                "!drawerContent", "modifier", "!drawerState", "gesturesEnabled", "scrimColor", "!content")]

            {{DrawerStateStubs}}

            namespace ComposeNet
            {
                public static partial class ComposeBridges
                {
                    [ComposeBridge(Class="androidx/compose/material3/NavigationDrawerKt",
                                   JvmName="ModalNavigationDrawer",
                                   Signature="{{DrawerSig}}",
                                   Defaults=typeof(DrawerDefault))]
                    [ComposeFacade]
                    public static partial void Drawer(
                        IFunction2 drawerContent,
                        IModifier? modifier,
                        [StateHolder(Remember = nameof(RememberDrawerState),
                                     StateType = typeof(DrawerStateHolder))]
                        IntPtr drawerState,
                        IFunction2 content,
                        int defaults,
                        IComposer composer);

                    public static IntPtr RememberDrawerState(
                        AndroidX.Compose.Material3.DrawerValue initialValue,
                        [ConfirmStateChange(typeof(AndroidX.Compose.Material3.DrawerValue))]
                        bool confirmStateChange,
                        IComposer composer) => default;
                }
            }
            """;

        var (_, diags, emitted) = Run(code, "Drawer");
        Assert.Null(emitted);
        Assert.Contains(diags, d => d.Id == "CN3011"
            && d.GetMessage().Contains("requires a Kotlin.Jvm.Functions.IFunction1"));
    }

    [Fact]
    public void ConfirmStateChange_MissingConventionAdapter_FailsCN3011()
    {
        // No `ComposeNet.<TName>ConfirmStateChange` class in scope and
        // no explicit AdapterType — generator must report CN3011 with
        // a message naming the missing convention class.
        var code = $$"""
            using AndroidX.Compose.Runtime;
            using AndroidX.Compose.UI;
            using ComposeNet;
            using Kotlin.Jvm.Functions;
            using System;

            [assembly: ComposeDefaults("DrawerDefault",
                "!drawerContent", "modifier", "!drawerState", "gesturesEnabled", "scrimColor", "!content")]

            namespace AndroidX.Compose.Material3
            {
                public enum DrawerValue { Closed, Open }
                public interface IDrawerState { }
            }
            namespace ComposeNet
            {
                public sealed class DrawerStateHolder
                {
                    internal AndroidX.Compose.Material3.IDrawerState? Jvm;
                    public AndroidX.Compose.Material3.DrawerValue InitialValue { get; }
                    public DrawerStateHolder() { }
                }
                // NOTE: no `DrawerValueConfirmStateChange` class declared.
            }

            namespace ComposeNet
            {
                public static partial class ComposeBridges
                {
                    [ComposeBridge(Class="androidx/compose/material3/NavigationDrawerKt",
                                   JvmName="ModalNavigationDrawer",
                                   Signature="{{DrawerSig}}",
                                   Defaults=typeof(DrawerDefault))]
                    [ComposeFacade]
                    public static partial void Drawer(
                        IFunction2 drawerContent,
                        IModifier? modifier,
                        [StateHolder(Remember = nameof(RememberDrawerState),
                                     StateType = typeof(DrawerStateHolder))]
                        IntPtr drawerState,
                        IFunction2 content,
                        int defaults,
                        IComposer composer);

                    public static IntPtr RememberDrawerState(
                        AndroidX.Compose.Material3.DrawerValue initialValue,
                        [ConfirmStateChange(typeof(AndroidX.Compose.Material3.DrawerValue))]
                        IFunction1? confirmStateChange,
                        IComposer composer) => default;
                }
            }
            """;

        var (_, diags, emitted) = Run(code, "Drawer");
        Assert.Null(emitted);
        Assert.Contains(diags, d => d.Id == "CN3011"
            && d.GetMessage().Contains("ComposeNet.DrawerValueConfirmStateChange"));
    }

    // ---------------------------------------------------------------
    // Branching — BranchOn / AlternateBridge (CN3010). The primary
    // bridge is the smaller one; the alternate is a strict superset
    // adding exactly one optional slot. The facade exposes the extra
    // slot as a nullable property and routes to the alternate bridge
    // when that property is non-null.
    // ---------------------------------------------------------------

    const string BranchPrimaryAttrs = """
        [assembly: ComposeDefaults("BarDefault",
            "!title", "modifier", "navigationIcon", "actions")]
        [assembly: ComposeDefaults("BarSubtitleDefault",
            "!title", "!subtitle", "modifier", "navigationIcon", "actions")]
        """;

    const string BranchBridges = """
        namespace ComposeNet
        {
            public static partial class ComposeBridges
            {
                [ComposeBridge(Class="x/Y", JvmName="Bar",
                               Signature="(Lkotlin/jvm/functions/Function2;Landroidx/compose/ui/Modifier;Lkotlin/jvm/functions/Function2;Lkotlin/jvm/functions/Function3;Landroidx/compose/runtime/Composer;II)V",
                               Defaults=typeof(BarDefault))]
                [ComposeFacade(BranchOn="Subtitle", AlternateBridge=nameof(BarWithSubtitle))]
                public static partial void Bar(
                    IFunction2  title,
                    IModifier?  modifier,
                    IFunction2? navigationIcon,
                    IFunction3? actions,
                    int         defaults,
                    IComposer   composer);

                [ComposeBridge(Class="x/Y", JvmName="BarWithSubtitle",
                               Signature="(Lkotlin/jvm/functions/Function2;Lkotlin/jvm/functions/Function2;Landroidx/compose/ui/Modifier;Lkotlin/jvm/functions/Function2;Lkotlin/jvm/functions/Function3;Landroidx/compose/runtime/Composer;II)V",
                               Defaults=typeof(BarSubtitleDefault))]
                public static partial void BarWithSubtitle(
                    IFunction2  title,
                    IFunction2  subtitle,
                    IModifier?  modifier,
                    IFunction2? navigationIcon,
                    IFunction3? actions,
                    int         defaults,
                    IComposer   composer);
            }
        }
        """;

    [Fact]
    public void Branching_EmitsIfElseWithPerBranchMasks()
    {
        var code = $$"""
            using AndroidX.Compose.Runtime;
            using AndroidX.Compose.UI;
            using ComposeNet;
            using Kotlin.Jvm.Functions;

            {{BranchPrimaryAttrs}}

            {{BranchBridges}}
            """;

        var (output, diags, emitted) = Run(code, "Bar");
        Assert.Empty(diags.Where(d => d.Severity == DiagnosticSeverity.Error));
        Assert.NotNull(emitted);

        // The facade exposes Subtitle (PascalCased BranchOn) plus the
        // three shared optional slots as nullable ComposableNode?
        // properties — and Title is required.
        Assert.Contains("public global::ComposeNet.ComposableNode? Title { get; set; }", emitted);
        Assert.Contains("public global::ComposeNet.ComposableNode? Subtitle { get; set; }", emitted);
        Assert.Contains("public global::ComposeNet.ComposableNode? NavigationIcon { get; set; }", emitted);
        Assert.Contains("public global::ComposeNet.ComposableNode? Actions { get; set; }", emitted);

        // Modifier is hoisted (both branches need it for the mask).
        Assert.Contains("var __modifier = BuildModifier();", emitted);

        // The branched slot's wrapper lives INSIDE the if-branch, not
        // at the top alongside the shared wrappers.
        var subtitleWrapIndex = emitted!.IndexOf("var __subtitle =", System.StringComparison.Ordinal);
        var ifCondIndex = emitted.IndexOf("if (Subtitle is not null)", System.StringComparison.Ordinal);
        Assert.True(ifCondIndex > 0, "expected `if (Subtitle is not null)`");
        Assert.True(subtitleWrapIndex > ifCondIndex, "subtitle wrapper must appear inside the if-branch");

        // Per-branch mask + call: alternate uses BarSubtitleDefault,
        // primary uses BarDefault.
        Assert.Contains("(int)global::ComposeNet.BarSubtitleDefault.All;", emitted);
        Assert.Contains("(int)global::ComposeNet.BarDefault.All;", emitted);

        // Alt branch calls the alternate bridge with the alt's actual
        // parameter order (title, subtitle, modifier, nav, actions).
        Assert.Contains("global::ComposeNet.ComposeBridges.BarWithSubtitle(__title, __subtitle, __modifier, __navigationIcon, __actions, __defaults, composer);", emitted);

        // Primary branch calls the primary bridge (no subtitle).
        Assert.Contains("global::ComposeNet.ComposeBridges.Bar(__title, __modifier, __navigationIcon, __actions, __defaults, composer);", emitted);

        var errors = output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
        Assert.Empty(errors);
    }

    [Fact]
    public void Branching_WalksAlternateParamOrder()
    {
        // Alternate bridge puts the extra slot in a DIFFERENT position
        // (after modifier, not after title). The emitter must walk
        // each bridge's actual parameter list — not the slots list —
        // to keep argument order correct.
        const string AltAttrs = """
            [assembly: ComposeDefaults("BarDefault",
                "!title", "modifier", "navigationIcon", "actions")]
            [assembly: ComposeDefaults("BarFlexDefault",
                "!title", "modifier", "subtitle", "navigationIcon", "actions")]
            """;

        var code = $$"""
            using AndroidX.Compose.Runtime;
            using AndroidX.Compose.UI;
            using ComposeNet;
            using Kotlin.Jvm.Functions;

            {{AltAttrs}}

            namespace ComposeNet
            {
                public static partial class ComposeBridges
                {
                    [ComposeBridge(Class="x/Y", JvmName="Bar",
                                   Signature="(Lkotlin/jvm/functions/Function2;Landroidx/compose/ui/Modifier;Lkotlin/jvm/functions/Function2;Lkotlin/jvm/functions/Function3;Landroidx/compose/runtime/Composer;II)V",
                                   Defaults=typeof(BarDefault))]
                    [ComposeFacade(BranchOn="Subtitle", AlternateBridge=nameof(BarFlex))]
                    public static partial void Bar(
                        IFunction2  title,
                        IModifier?  modifier,
                        IFunction2? navigationIcon,
                        IFunction3? actions,
                        int         defaults,
                        IComposer   composer);

                    [ComposeBridge(Class="x/Y", JvmName="BarFlex",
                                   Signature="(Lkotlin/jvm/functions/Function2;Landroidx/compose/ui/Modifier;Lkotlin/jvm/functions/Function2;Lkotlin/jvm/functions/Function2;Lkotlin/jvm/functions/Function3;Landroidx/compose/runtime/Composer;III)V",
                                   Defaults=typeof(BarFlexDefault))]
                    public static partial void BarFlex(
                        IFunction2  title,
                        IModifier?  modifier,
                        IFunction2? subtitle,
                        IFunction2? navigationIcon,
                        IFunction3? actions,
                        int         defaults,
                        IComposer   composer);
                }
            }
            """;

        var (output, diags, emitted) = Run(code, "Bar");
        Assert.Empty(diags.Where(d => d.Severity == DiagnosticSeverity.Error));
        Assert.NotNull(emitted);

        // Alt branch walks BarFlex's order: title, modifier, subtitle,
        // navigationIcon, actions — NOT the slots-list order.
        Assert.Contains("global::ComposeNet.ComposeBridges.BarFlex(__title, __modifier, __subtitle, __navigationIcon, __actions, __defaults, composer);", emitted);
        // The Subtitle bit IS in BarFlexDefault (not `!`-suppressed),
        // so the alt-branch mask clears it for the supplied slot.
        Assert.Contains("__defaults &= ~(int)global::ComposeNet.BarFlexDefault.Subtitle;", emitted);

        var errors = output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
        Assert.Empty(errors);
    }

    [Fact]
    public void Branching_MissingAlternate_EmitsCN3010()
    {
        var code = """
            using AndroidX.Compose.Runtime;
            using AndroidX.Compose.UI;
            using ComposeNet;
            using Kotlin.Jvm.Functions;

            [assembly: ComposeDefaults("BarDefault",
                "!title", "modifier", "navigationIcon", "actions")]

            namespace ComposeNet
            {
                public static partial class ComposeBridges
                {
                    [ComposeBridge(Class="x/Y", JvmName="Bar",
                                   Signature="(Lkotlin/jvm/functions/Function2;Landroidx/compose/ui/Modifier;Lkotlin/jvm/functions/Function2;Lkotlin/jvm/functions/Function3;Landroidx/compose/runtime/Composer;II)V",
                                   Defaults=typeof(BarDefault))]
                    [ComposeFacade(BranchOn="Subtitle", AlternateBridge="BarWithSubtitleMissing")]
                    public static partial void Bar(
                        IFunction2  title,
                        IModifier?  modifier,
                        IFunction2? navigationIcon,
                        IFunction3? actions,
                        int         defaults,
                        IComposer   composer);
                }
            }
            """;

        var (_, diags, _) = Run(code, "Bar");
        Assert.Contains(diags, d => d.Id == "CN3010" && d.GetMessage().Contains("BarWithSubtitleMissing"));
    }

    [Fact]
    public void Branching_OnlyOneOfBranchOnAlternateBridge_EmitsCN3010()
    {
        // BranchOn without AlternateBridge.
        var code = """
            using AndroidX.Compose.Runtime;
            using AndroidX.Compose.UI;
            using ComposeNet;
            using Kotlin.Jvm.Functions;

            [assembly: ComposeDefaults("BarDefault",
                "!title", "modifier", "navigationIcon", "actions")]

            namespace ComposeNet
            {
                public static partial class ComposeBridges
                {
                    [ComposeBridge(Class="x/Y", JvmName="Bar",
                                   Signature="(Lkotlin/jvm/functions/Function2;Landroidx/compose/ui/Modifier;Lkotlin/jvm/functions/Function2;Lkotlin/jvm/functions/Function3;Landroidx/compose/runtime/Composer;II)V",
                                   Defaults=typeof(BarDefault))]
                    [ComposeFacade(BranchOn="Subtitle")]
                    public static partial void Bar(
                        IFunction2  title,
                        IModifier?  modifier,
                        IFunction2? navigationIcon,
                        IFunction3? actions,
                        int         defaults,
                        IComposer   composer);
                }
            }
            """;

        var (_, diags, _) = Run(code, "Bar");
        Assert.Contains(diags, d => d.Id == "CN3010" && d.GetMessage().Contains("BranchOn and AlternateBridge"));
    }

    [Fact]
    public void Branching_BranchOnMismatch_EmitsCN3010()
    {
        // Extra param is `subtitle` (Pascal "Subtitle"), but BranchOn
        // names a different property — should fail with CN3010.
        var code = """
            using AndroidX.Compose.Runtime;
            using AndroidX.Compose.UI;
            using ComposeNet;
            using Kotlin.Jvm.Functions;

            [assembly: ComposeDefaults("BarDefault",
                "!title", "modifier", "navigationIcon", "actions")]
            [assembly: ComposeDefaults("BarSubtitleDefault",
                "!title", "!subtitle", "modifier", "navigationIcon", "actions")]

            namespace ComposeNet
            {
                public static partial class ComposeBridges
                {
                    [ComposeBridge(Class="x/Y", JvmName="Bar",
                                   Signature="(Lkotlin/jvm/functions/Function2;Landroidx/compose/ui/Modifier;Lkotlin/jvm/functions/Function2;Lkotlin/jvm/functions/Function3;Landroidx/compose/runtime/Composer;II)V",
                                   Defaults=typeof(BarDefault))]
                    [ComposeFacade(BranchOn="Caption", AlternateBridge=nameof(BarWithSubtitle))]
                    public static partial void Bar(
                        IFunction2  title,
                        IModifier?  modifier,
                        IFunction2? navigationIcon,
                        IFunction3? actions,
                        int         defaults,
                        IComposer   composer);

                    [ComposeBridge(Class="x/Y", JvmName="BarWithSubtitle",
                                   Signature="(Lkotlin/jvm/functions/Function2;Lkotlin/jvm/functions/Function2;Landroidx/compose/ui/Modifier;Lkotlin/jvm/functions/Function2;Lkotlin/jvm/functions/Function3;Landroidx/compose/runtime/Composer;II)V",
                                   Defaults=typeof(BarSubtitleDefault))]
                    public static partial void BarWithSubtitle(
                        IFunction2  title,
                        IFunction2  subtitle,
                        IModifier?  modifier,
                        IFunction2? navigationIcon,
                        IFunction3? actions,
                        int         defaults,
                        IComposer   composer);
                }
            }
            """;

        var (_, diags, _) = Run(code, "Bar");
        Assert.Contains(diags, d => d.Id == "CN3010" && d.GetMessage().Contains("Caption"));
    }

    [Fact]
    public void Branching_AlternateHasMissingPrimaryParam_EmitsCN3010()
    {
        // Alternate is missing `actions`, so it's not a strict
        // superset; CN3010 should fire.
        var code = """
            using AndroidX.Compose.Runtime;
            using AndroidX.Compose.UI;
            using ComposeNet;
            using Kotlin.Jvm.Functions;

            [assembly: ComposeDefaults("BarDefault",
                "!title", "modifier", "navigationIcon", "actions")]
            [assembly: ComposeDefaults("BarBadDefault",
                "!title", "!subtitle", "modifier", "navigationIcon")]

            namespace ComposeNet
            {
                public static partial class ComposeBridges
                {
                    [ComposeBridge(Class="x/Y", JvmName="Bar",
                                   Signature="(Lkotlin/jvm/functions/Function2;Landroidx/compose/ui/Modifier;Lkotlin/jvm/functions/Function2;Lkotlin/jvm/functions/Function3;Landroidx/compose/runtime/Composer;II)V",
                                   Defaults=typeof(BarDefault))]
                    [ComposeFacade(BranchOn="Subtitle", AlternateBridge=nameof(BarBad))]
                    public static partial void Bar(
                        IFunction2  title,
                        IModifier?  modifier,
                        IFunction2? navigationIcon,
                        IFunction3? actions,
                        int         defaults,
                        IComposer   composer);

                    [ComposeBridge(Class="x/Y", JvmName="BarBad",
                                   Signature="(Lkotlin/jvm/functions/Function2;Lkotlin/jvm/functions/Function2;Landroidx/compose/ui/Modifier;Lkotlin/jvm/functions/Function2;Landroidx/compose/runtime/Composer;II)V",
                                   Defaults=typeof(BarBadDefault))]
                    public static partial void BarBad(
                        IFunction2  title,
                        IFunction2  subtitle,
                        IModifier?  modifier,
                        IFunction2? navigationIcon,
                        int         defaults,
                        IComposer   composer);
                }
            }
            """;

        var (_, diags, _) = Run(code, "Bar");
        Assert.Contains(diags, d => d.Id == "CN3010" && d.GetMessage().Contains("actions"));
    }

    [Fact]
    public void Branching_PrimaryMissingDefaultsParam_EmitsCN3010()
    {
        var code = """
            using AndroidX.Compose.Runtime;
            using AndroidX.Compose.UI;
            using ComposeNet;
            using Kotlin.Jvm.Functions;

            [assembly: ComposeDefaults("BarSubtitleDefault",
                "!title", "!subtitle", "modifier", "navigationIcon", "actions")]

            namespace ComposeNet
            {
                public static partial class ComposeBridges
                {
                    [ComposeBridge(Class="x/Y", JvmName="Bar",
                                   Signature="(Lkotlin/jvm/functions/Function2;Landroidx/compose/ui/Modifier;Lkotlin/jvm/functions/Function2;Lkotlin/jvm/functions/Function3;Landroidx/compose/runtime/Composer;I)V")]
                    [ComposeFacade(BranchOn="Subtitle", AlternateBridge=nameof(BarWithSubtitle))]
                    public static partial void Bar(
                        IFunction2  title,
                        IModifier?  modifier,
                        IFunction2? navigationIcon,
                        IFunction3? actions,
                        IComposer   composer);

                    [ComposeBridge(Class="x/Y", JvmName="BarWithSubtitle",
                                   Signature="(Lkotlin/jvm/functions/Function2;Lkotlin/jvm/functions/Function2;Landroidx/compose/ui/Modifier;Lkotlin/jvm/functions/Function2;Lkotlin/jvm/functions/Function3;Landroidx/compose/runtime/Composer;II)V",
                                   Defaults=typeof(BarSubtitleDefault))]
                    public static partial void BarWithSubtitle(
                        IFunction2  title,
                        IFunction2  subtitle,
                        IModifier?  modifier,
                        IFunction2? navigationIcon,
                        IFunction3? actions,
                        int         defaults,
                        IComposer   composer);
                }
            }
            """;

        var (_, diags, _) = Run(code, "Bar");
        Assert.Contains(diags, d => d.Id == "CN3010" && d.GetMessage().Contains("'int defaults'"));
    }
}