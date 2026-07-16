using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace AndroidX.Compose.SourceGenerators.Tests;

/// <summary>
/// Tests for <see cref="ComposeFacadeGenerator"/>. The synthetic stubs
/// mirror enough of the Compose / AndroidX.Compose shape (IComposer,
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
                public static unsafe void CallStaticVoidMethod(System.IntPtr cls, System.IntPtr m, global::Android.Runtime.JValue* args) { }
                public static unsafe void CallVoidMethod(System.IntPtr inst, System.IntPtr m, global::Android.Runtime.JValue* args) { }
                public static unsafe System.IntPtr CallStaticObjectMethod(System.IntPtr cls, System.IntPtr m, global::Android.Runtime.JValue* args) => default;
                public static unsafe System.IntPtr CallObjectMethod(System.IntPtr inst, System.IntPtr m, global::Android.Runtime.JValue* args) => default;
                public static unsafe System.IntPtr NewObject(System.IntPtr cls, System.IntPtr m, global::Android.Runtime.JValue* args) => default;
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
            public class Object : global::Android.Runtime.IJavaObject
            {
                public System.IntPtr Handle => default;
                public static T? GetObject<T>(System.IntPtr handle, global::Android.Runtime.JniHandleOwnership transfer) where T : class => default;
            }
            public sealed class Boolean : Object { public bool BooleanValue() => false; }
            public sealed class Float : Object { public float FloatValue() => 0f; }
            public abstract class Enum : Object { }
        }
        namespace AndroidX.Compose.Runtime { public interface IComposer { } }
        namespace AndroidX.Compose.UI { public interface IModifier { } }
        namespace AndroidX.Compose.UI.Graphics.Painter
        {
            public abstract class Painter : Java.Lang.Object { }
        }
        namespace AndroidX.Compose.UI.Graphics.Vector
        {
            public sealed class ImageVector : Java.Lang.Object { }
        }
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
                public ColorScheme GetColorScheme(global::AndroidX.Compose.Runtime.IComposer c, int n) => null!;
            }
            public sealed class ButtonColors : Java.Lang.Object { }
        }
        namespace AndroidX.Compose.UI.Text
        {
            public sealed class TextStyle : Java.Lang.Object { }
        }
        namespace AndroidX.Compose.UI.Text.Input
        {
            public interface IVisualTransformation { }
        }
        namespace AndroidX.Compose.Foundation.Text
        {
            public sealed class KeyboardOptions : Java.Lang.Object { }
        }
        namespace Kotlin.Jvm.Functions
        {
            public interface IFunction0 { }
            public interface IFunction1 { }
            public interface IFunction2 { }
            public interface IFunction3 { }
            public interface IFunction4 { }
        }
        namespace AndroidX.Compose
        {
            [System.AttributeUsage(System.AttributeTargets.Method)]
            public sealed class ComposableAttribute : System.Attribute { }
            [System.AttributeUsage(System.AttributeTargets.Method)]
            internal sealed class ComposableDirectTargetAttribute(
                System.Type containingType, string methodName) : System.Attribute { }
            [System.AttributeUsage(System.AttributeTargets.Parameter)]
            public sealed class ComposableContentAttribute : System.Attribute { }
            public static class ComposableContext
            {
                public static global::AndroidX.Compose.Runtime.IComposer Current => throw new System.NotImplementedException();
            }
            public enum ScopeKind { None, Row, Column, Box, Other }
            public static class RenderContext
            {
                public ref struct ScopeFrame { public void Dispose() { } }
                public ref struct RowFrame { public void SetIndex(int i) { } public void Dispose() { } }
                public static ScopeFrame PushScope(System.IntPtr scope, ScopeKind kind) => default;
                public static RowFrame PushRow(int count) => default;
                public static System.IntPtr CurrentScope => default;
                public static int CurrentRowChildIndex => default;
                public static int CurrentRowChildCount => default;
            }
            public readonly struct Dp
            {
                public Dp(float v) { Value = v; }
                public float Value { get; }
                internal static float Pack(Dp? d) => d?.Value ?? 0f;
            }
            public readonly struct Color
            {
                public long ToPacked() => default;
            }
            public readonly struct Sp
            {
                public Sp(float v) { Value = v; }
                public float Value { get; }
                internal static long Pack(Sp? s) => 0L;
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
                internal static int Pack(TextOverflow? a) => 0;
            }
            public class FontWeight : Java.Lang.Object { }
            public class FontStyle : Java.Lang.Object { }
            public class FontFamily : Java.Lang.Object { }
            public class TextAlign : Java.Lang.Object { }
            public class TextDecoration : Java.Lang.Object { }
            public class Shape : Java.Lang.Object { }
            public class PaddingValues : Java.Lang.Object { }
            public class Alignment : Java.Lang.Object { }
            public class ContentScale : Java.Lang.Object { }
            public sealed class Modifier
            {
                internal object StructuralKey => new();
                internal global::AndroidX.Compose.UI.IModifier? Build() => null;
            }
            public abstract class ComposableNode
            {
                public Modifier? Modifier { get; set; }
                public abstract void Render(global::AndroidX.Compose.Runtime.IComposer composer);
                public void Render() { }
                protected global::AndroidX.Compose.UI.IModifier? BuildModifier() => null;
                internal object? BuildModifierStructuralKey() => null;
            }
            public abstract class ComposableContainer : ComposableNode
            {
                public void Add(ComposableNode node) { }
                protected void RenderChildren(global::AndroidX.Compose.Runtime.IComposer composer) { }
                private protected void RenderChildrenIndexed(global::AndroidX.Compose.Runtime.IComposer composer) { }
            }
            internal sealed class ComposableContentNode : ComposableNode
            {
                public ComposableContentNode(System.Action<global::AndroidX.Compose.Runtime.IComposer> body) { }
                public override void Render(global::AndroidX.Compose.Runtime.IComposer composer) { }
                internal static void RenderDirect(global::AndroidX.Compose.Runtime.IComposer composer,
                    System.Action<global::AndroidX.Compose.Runtime.IComposer> body, bool indexed) { }
                internal static void RenderDirect(global::AndroidX.Compose.Runtime.IComposer composer,
                    System.Action body, bool indexed) { }
            }
            public static partial class Composables { }
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
                    global::AndroidX.Compose.Runtime.IComposer composer,
                    System.Action<global::AndroidX.Compose.Runtime.IComposer> body,
                    [System.Runtime.CompilerServices.CallerLineNumber] int line = 0,
                    [System.Runtime.CompilerServices.CallerFilePath] string file = "") => null!;
                public static Kotlin.Jvm.Functions.IFunction3 Wrap3(
                    global::AndroidX.Compose.Runtime.IComposer composer,
                    System.Action<global::AndroidX.Compose.Runtime.IComposer> body,
                    [System.Runtime.CompilerServices.CallerLineNumber] int line = 0,
                    [System.Runtime.CompilerServices.CallerFilePath] string file = "") => null!;
                public static Kotlin.Jvm.Functions.IFunction3 Wrap3(
                    global::AndroidX.Compose.Runtime.IComposer composer,
                    System.Action<System.IntPtr, global::AndroidX.Compose.Runtime.IComposer> body,
                    [System.Runtime.CompilerServices.CallerLineNumber] int line = 0,
                    [System.Runtime.CompilerServices.CallerFilePath] string file = "") => null!;
            }
            public static partial class ComposeBridges
            {
                public static System.IntPtr ModifierHandle(global::AndroidX.Compose.UI.IModifier? m) => default;
                public static System.IntPtr PainterResource(int id, global::AndroidX.Compose.Runtime.IComposer composer) => default;
            }
            public enum ChangedBits { Uncertain = 0, Same = 1, Different = 2, Static = 4 }
            public static class ComposeExtensions
            {
                public static int DiffSlotShift(int paramIndex) => 1 + paramIndex * 3;
                public static int DiffSlot<T>(this global::AndroidX.Compose.Runtime.IComposer composer, T value, int bitOffset,
                    [System.Runtime.CompilerServices.CallerLineNumber] int line = 0,
                    [System.Runtime.CompilerServices.CallerFilePath] string file = "") => 0;
                internal static Kotlin.Jvm.Functions.IFunction0 RememberAction(
                    this global::AndroidX.Compose.Runtime.IComposer composer, System.Action action,
                    [System.Runtime.CompilerServices.CallerLineNumber] int line = 0,
                    [System.Runtime.CompilerServices.CallerFilePath] string file = "") => null!;
                internal static Kotlin.Jvm.Functions.IFunction1 RememberAction(
                    this global::AndroidX.Compose.Runtime.IComposer composer, System.Action<global::Java.Lang.Object?> action,
                    [System.Runtime.CompilerServices.CallerLineNumber] int line = 0,
                    [System.Runtime.CompilerServices.CallerFilePath] string file = "") => null!;
                public static T Remember<T>(
                    this global::AndroidX.Compose.Runtime.IComposer composer, System.Func<T> factory,
                    [System.Runtime.CompilerServices.CallerLineNumber] int line = 0,
                    [System.Runtime.CompilerServices.CallerFilePath] string file = "") => factory();
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
            if (path.Contains($"AndroidX.Compose.Facade.{facadeName}.") && path.EndsWith(".g.cs", System.StringComparison.Ordinal))
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
            using global::AndroidX.Compose.Runtime;
            using global::AndroidX.Compose.UI;
            using AndroidX.Compose;
            using Kotlin.Jvm.Functions;

            {{ButtonAttrs}}

            namespace AndroidX.Compose
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
        Assert.Contains("public sealed partial class Button : global::AndroidX.Compose.ComposableContainer", emitted);
        Assert.Contains("internal static void Button(global::AndroidX.Compose.Runtime.IComposer composer, global::System.Action onClick, [global::AndroidX.Compose.ComposableContentAttribute] global::System.Action<global::AndroidX.Compose.Runtime.IComposer> content", emitted);
        Assert.Contains("internal static void Button_PrimaryResource_Explicit(global::AndroidX.Compose.Runtime.IComposer __composer", emitted);
        Assert.Contains(
            "var __content = global::AndroidX.Compose.ComposableLambdas.Wrap3(__composer, c => global::AndroidX.Compose.ComposableContentNode.RenderDirect(c, content, false));",
            emitted);
        Assert.DoesNotContain("var node = new global::AndroidX.Compose.Button", emitted);
        Assert.Contains("readonly global::System.Action _onClick;", emitted);
        Assert.Contains("public Button(global::System.Action onClick)", emitted);
        Assert.Contains("var __onClick = composer.RememberAction(_onClick);", emitted);
        Assert.Contains("var __content = global::AndroidX.Compose.ComposableLambdas.Wrap3(composer, c => RenderChildren(c));", emitted);
        Assert.Contains("global::AndroidX.Compose.ComposeBridges.Button(__onClick, BuildModifier(), __content, composer);", emitted);

        var errors = output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
        Assert.Empty(errors);
    }

    [Fact]
    public void ImplicitComposer_EmitsComposerlessContentOverload()
    {
        var code = $$"""
            using global::AndroidX.Compose.Runtime;
            using global::AndroidX.Compose.UI;
            using AndroidX.Compose;
            using Kotlin.Jvm.Functions;

            {{ButtonAttrs}}

            namespace AndroidX.Compose
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
        Assert.Contains(
            "[global::AndroidX.Compose.ComposableContentAttribute] global::System.Action content",
            emitted);
        Assert.Contains(
            "public static void Button(global::System.Action onClick, [global::AndroidX.Compose.ComposableContentAttribute] global::System.Action content",
            emitted);
        Assert.Contains(
            "var __content = global::AndroidX.Compose.ComposableLambdas.Wrap3(__composer, c => global::AndroidX.Compose.ComposableContentNode.RenderDirect(c, content, false));",
            emitted);
        Assert.Contains(
            "global::System.ArgumentNullException.ThrowIfNull(content);",
            emitted);
        Assert.Contains(
            "Button_PrimaryResource_Implicit(global::AndroidX.Compose.ComposableContext.Current",
            emitted);
        Assert.Contains(
            "[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]",
            emitted);
        Assert.Contains(
            "public static void Button_PrimaryResource_Implicit(global::AndroidX.Compose.Runtime.IComposer __composer",
            emitted);
        Assert.DoesNotContain("var node = new global::AndroidX.Compose.Button", emitted);
        Assert.DoesNotContain("node.Render(", emitted);
        Assert.Empty(output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error));
    }

    [Fact]
    public void DirectDefaultsMask_UsesSurfacedParameterOrderAcrossSlotKinds()
    {
        var code = """
            using global::AndroidX.Compose.Runtime;
            using global::AndroidX.Compose.UI;
            using AndroidX.Compose;
            using Kotlin.Jvm.Functions;

            [assembly: ComposeDefaults("WidgetDefault",
                "!text", "modifier", "enabled", "icon", "fontSize", "!content")]

            namespace AndroidX.Compose
            {
                public static partial class ComposeBridges
                {
                    [ComposeBridge(Class="my/pkg/WidgetKt", JvmName="Widget",
                                   Signature="(Ljava/lang/String;Landroidx/compose/ui/Modifier;ZLkotlin/jvm/functions/Function2;FLkotlin/jvm/functions/Function2;Landroidx/compose/runtime/Composer;II)V",
                                   Defaults=typeof(WidgetDefault))]
                    [ComposeFacade]
                    public static partial void Widget(
                        string text,
                        IModifier? modifier,
                        [FacadeDefault(true)] bool enabled,
                        IFunction2? icon,
                        Dp? fontSize,
                        IFunction2 content,
                        IComposer composer);
                }
            }
            """;

        var (output, diags, emitted) = Run(code, "Widget");

        Assert.Empty(diags.Where(d => d.Severity == DiagnosticSeverity.Error));
        Assert.NotNull(emitted);
        Assert.Contains(
            "if ((__omittedArguments & 0x4UL) == 0) __defaults &= ~global::AndroidX.Compose.WidgetDefault.Enabled;",
            emitted);
        Assert.Contains(
            "if ((__omittedArguments & 0x8UL) == 0) __defaults &= ~global::AndroidX.Compose.WidgetDefault.Modifier;",
            emitted);
        Assert.Contains(
            "if ((__omittedArguments & 0x10UL) == 0) __defaults &= ~global::AndroidX.Compose.WidgetDefault.Icon;",
            emitted);
        Assert.Contains(
            "if ((__omittedArguments & 0x20UL) == 0) __defaults &= ~global::AndroidX.Compose.WidgetDefault.FontSize;",
            emitted);
        Assert.Contains(
            "global::AndroidX.Compose.ComposeBridges.WidgetExplicitDefaults(text, __modifier, enabled, __icon, fontSize, __content, (int)__defaults, __composer);",
            emitted);
        Assert.Empty(output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error));
    }

    [Fact]
    public void DirectDefaultsMask_WideAutoMaskUsesExplicitTwoMaskEntry()
    {
        var names = string.Join(", ", Enumerable.Range(0, 33).Select(i => $"\"slot{i}\""));
        var parameters = string.Join(", ", Enumerable.Range(0, 33).Select(i => $"int? slot{i}"));
        var signature = new string('I', 33)
            + "Landroidx/compose/runtime/Composer;"
            + new string('I', 6);
        var code = $$"""
            using global::AndroidX.Compose.Runtime;
            using AndroidX.Compose;

            [assembly: ComposeDefaults("WideDefault", {{names}})]

            namespace AndroidX.Compose
            {
                public static partial class ComposeBridges
                {
                    [ComposeBridge(Class="x/WideKt", JvmName="Wide",
                                   Signature="({{signature}})V",
                                   Defaults=typeof(WideDefault))]
                    [ComposeFacade]
                    public static partial void Wide({{parameters}}, IComposer composer);
                }
            }
            """;

        var (output, diags, emitted) = Run(code, "Wide");

        Assert.Empty(diags.Where(d => d.Severity == DiagnosticSeverity.Error));
        Assert.NotNull(emitted);
        Assert.Contains("if ((__omittedArguments & 0x1UL) == 0) __defaults &= ~global::AndroidX.Compose.WideDefault.Slot0;", emitted);
        Assert.Contains("if ((__omittedArguments & 0x80000000UL) == 0) __defaults &= ~global::AndroidX.Compose.WideDefault.Slot31;", emitted);
        Assert.Contains("if ((__omittedArguments & 0x100000000UL) == 0) __defaults &= ~global::AndroidX.Compose.WideDefault.Slot32;", emitted);
        Assert.Contains("var (__defaultsMask0, __defaultsMask1) = __defaults.Split();", emitted);
        Assert.Contains("global::AndroidX.Compose.ComposeBridges.WideExplicitDefaults(", emitted);
        Assert.Contains("__defaultsMask0, __defaultsMask1, __composer);", emitted);
        Assert.Empty(output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error));
    }

    [Fact]
    public void TextCatalogFallback_PreservesNullableDefaultsWithoutInterceptor()
    {
        var code = """
            using global::AndroidX.Compose.Runtime;
            using global::AndroidX.Compose.UI;
            using AndroidX.Compose;

            [assembly: ComposeDefaults("TextDefault",
                "!text", "modifier", "color", "fontSize", "fontStyle",
                "fontWeight", "fontFamily", "letterSpacing", "decoration", "align",
                "lineHeight", "overflow", "softWrap", "maxLines", "minLines",
                "onTextLayout", "style")]

            namespace AndroidX.Compose
            {
                public static partial class ComposeBridges
                {
                    [ComposeBridge(
                        Class = "androidx/compose/material3/TextKt",
                        JvmName = "Text--4IGK_g",
                        Signature = "(Ljava/lang/String;Landroidx/compose/ui/Modifier;JJLandroidx/compose/ui/text/font/FontStyle;Landroidx/compose/ui/text/font/FontWeight;Landroidx/compose/ui/text/font/FontFamily;JLandroidx/compose/ui/text/style/TextDecoration;Landroidx/compose/ui/text/style/TextAlign;JIZIILkotlin/jvm/functions/Function1;Landroidx/compose/ui/text/TextStyle;Landroidx/compose/runtime/Composer;III)V",
                        Defaults = typeof(TextDefault))]
                    [ComposeFacade]
                    public static partial void Text(
                        string text,
                        IModifier? modifier,
                        Color? color,
                        Sp? fontSize,
                        FontStyle? fontStyle,
                        FontWeight? fontWeight,
                        FontFamily? fontFamily,
                        Sp? letterSpacing,
                        TextDecoration? decoration,
                        TextAlign? align,
                        Sp? lineHeight,
                        TextOverflow? overflow,
                        bool? softWrap,
                        int? maxLines,
                        int? minLines,
                        IComposer composer,
                        int _changed = 0);
                }
            }
            """;

        var (output, diags, emitted) = Run(code, "Text");

        Assert.Empty(diags.Where(d => d.Severity == DiagnosticSeverity.Error));
        Assert.NotNull(emitted);
        Assert.Contains("(modifier is null ? 0x2UL : 0UL)", emitted);
        Assert.Contains("(fontStyle is null ? 0x10UL : 0UL)", emitted);
        Assert.Contains("(minLines is null ? 0x4000UL : 0UL)", emitted);
        Assert.DoesNotContain("minLines, 0UL, 0);", emitted);
        Assert.Contains("var __defaults = global::AndroidX.Compose.TextDefault.All;", emitted);
        Assert.Contains("int __changed = 0;", emitted);
        Assert.DoesNotContain("int __changed = __omittedArguments == 0 ? __directChanged & 0b1 : 0;", emitted);
        Assert.Contains("global::AndroidX.Compose.ComposeBridges.TextExplicitDefaults(", emitted);

        var defaultsType = output.GetTypeByMetadataName("AndroidX.Compose.TextDefault");
        var all = defaultsType?.GetMembers("All").OfType<IFieldSymbol>().Single();
        Assert.Equal(131070, all?.ConstantValue);

        var bridge = output.SyntaxTrees.Single(tree =>
            tree.FilePath.EndsWith("ComposeBridges.Text.g.cs", System.StringComparison.Ordinal))
            .GetText().ToString();
        Assert.Contains("args[18] = new global::Android.Runtime.JValue(_changed);", bridge);
        Assert.Contains("args[19] = new global::Android.Runtime.JValue(0);", bridge);
        Assert.Contains("args[20] = new global::Android.Runtime.JValue(defaults);", bridge);
        Assert.Empty(output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error));
    }

    [Fact]
    public void Card_GeneratesContainerNoCtor()
    {
        var code = """
            using global::AndroidX.Compose.Runtime;
            using global::AndroidX.Compose.UI;
            using AndroidX.Compose;
            using Kotlin.Jvm.Functions;

            [assembly: ComposeDefaults("CardDefault",
                "modifier", "shape", "colors", "elevation", "border", "!content")]

            namespace AndroidX.Compose
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
        Assert.Contains("public sealed partial class Card : global::AndroidX.Compose.ComposableContainer", emitted);
        Assert.DoesNotContain("public Card(", emitted);
        Assert.Contains("global::AndroidX.Compose.ComposeBridges.Card(BuildModifier(), __content, composer);", emitted);

        var errors = output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
        Assert.Empty(errors);
    }

    [Fact]
    public void Text_LeafWithPrimitiveCtorParam()
    {
        var code = """
            using global::AndroidX.Compose.Runtime;
            using global::AndroidX.Compose.UI;
            using AndroidX.Compose;
            using Kotlin.Jvm.Functions;

            [assembly: ComposeDefaults("TextDefault",
                "!text", "modifier", "color", "fontSize")]

            namespace AndroidX.Compose
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
        Assert.Contains("public sealed partial class Text : global::AndroidX.Compose.ComposableNode", emitted);
        Assert.Contains("readonly string _text;", emitted);
        Assert.Contains("public Text(string text)", emitted);
        Assert.Contains("global::AndroidX.Compose.ComposeBridges.Text(_text, BuildModifier(), composer);", emitted);

        var errors = output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
        Assert.Empty(errors);
    }

    [Theory]
    [InlineData("bool",   "true",                     "true")]
    [InlineData("bool",   "false",                    "false")]
    [InlineData("int",    "42",                       "42")]
    [InlineData("long",   "100L",                     "100L")]
    [InlineData("float",  "1.5f",                     "1.5f")]
    [InlineData("double", "2.5",                      "2.5")]
    [InlineData("string", "\"hello\"",                "\"hello\"")]
    [InlineData("string?", "null",                    "null")]
    public void PrimitiveCtorParam_HonoursFacadeDefaultAttribute(string type, string sourceDefault, string emittedDefault)
    {
        // The facade owns the public C# default. The bridge parameter and
        // trailing Composer both remain required.
        var code = $$"""
            using global::AndroidX.Compose.Runtime;
            using global::AndroidX.Compose.UI;
            using AndroidX.Compose;
            using Kotlin.Jvm.Functions;

            [assembly: ComposeDefaults("WidgetDefault",
                "modifier", "value")]

            namespace AndroidX.Compose
            {
                public static partial class ComposeBridges
                {
                    [ComposeBridge(Class="my/pkg/WidgetKt", JvmName="Widget",
                                   Signature="(Landroidx/compose/ui/Modifier;ILandroidx/compose/runtime/Composer;II)V",
                                   Defaults=typeof(WidgetDefault))]
                    [ComposeFacade]
                    public static partial void Widget(IModifier? modifier, [FacadeDefault({{sourceDefault}})] {{type}} value, IComposer composer);
                }
            }
            """;

        var (output, diags, emitted) = Run(code, "Widget");
        Assert.Empty(diags.Where(d => d.Severity == DiagnosticSeverity.Error));
        Assert.NotNull(emitted);
        // Ctor surfaces the user param with the propagated default.
        Assert.Contains($"public Widget({type} value = {emittedDefault})", emitted);
    }

    [Fact]
    public void PrimitiveCtorParam_WithoutDefault_StaysRequired()
    {
        // Regression guard: when the bridge declares a primitive WITHOUT
        // an explicit default, the generated facade ctor must keep it
        // required (no trailing `=`). Mirrors Text_LeafWithPrimitiveCtorParam
        // but asserted on the negative — the literal " = " (with spaces
        // around equals) does not appear inside the ctor parameter list.
        var code = """
            using global::AndroidX.Compose.Runtime;
            using global::AndroidX.Compose.UI;
            using AndroidX.Compose;
            using Kotlin.Jvm.Functions;

            [assembly: ComposeDefaults("WidgetDefault",
                "modifier", "!value")]

            namespace AndroidX.Compose
            {
                public static partial class ComposeBridges
                {
                    [ComposeBridge(Class="my/pkg/WidgetKt", JvmName="Widget",
                                   Signature="(Landroidx/compose/ui/Modifier;ILandroidx/compose/runtime/Composer;II)V",
                                   Defaults=typeof(WidgetDefault))]
                    [ComposeFacade]
                    public static partial void Widget(IModifier? modifier, int value, IComposer composer);
                }
            }
            """;

        var (output, diags, emitted) = Run(code, "Widget");
        Assert.Empty(diags.Where(d => d.Severity == DiagnosticSeverity.Error));
        Assert.NotNull(emitted);
        Assert.Contains("public Widget(int value)", emitted);
        Assert.DoesNotContain("public Widget(int value = ", emitted);
    }

    [Fact]
    public void PrimitiveCtorParam_DefaultedSortsAfterRequired()
    {
        // Bridge declares: required `string text`, then defaulted
        // `bool enabled = true`. Without re-sorting the ctor params
        // would be `(string text, bool enabled = true)` which is OK
        // here, but the test also asserts the defaulted slot stays
        // before the (always-defaulted) StateHolder. Since we have no
        // StateHolder in this test, just confirm the two surface in
        // declaration order with the trailing default.
        var code = """
            using global::AndroidX.Compose.Runtime;
            using global::AndroidX.Compose.UI;
            using AndroidX.Compose;
            using Kotlin.Jvm.Functions;

            [assembly: ComposeDefaults("LabelDefault",
                "!text", "modifier", "enabled")]

            namespace AndroidX.Compose
            {
                public static partial class ComposeBridges
                {
                    [ComposeBridge(Class="my/pkg/LabelKt", JvmName="Label",
                                   Signature="(Ljava/lang/String;Landroidx/compose/ui/Modifier;ZLandroidx/compose/runtime/Composer;II)V",
                                   Defaults=typeof(LabelDefault))]
                    [ComposeFacade]
                    public static partial void Label(string text, IModifier? modifier, bool enabled = true, IComposer composer = null!);
                }
            }
            """;

        var (output, diags, emitted) = Run(code, "Label");
        Assert.Empty(diags.Where(d => d.Severity == DiagnosticSeverity.Error));
        Assert.NotNull(emitted);
        // Required `text` first; defaulted `enabled` last with the propagated default.
        Assert.Contains("public Label(string text, bool enabled = true)", emitted);
        // Both ctor params surface as readonly backing fields.
        Assert.Contains("readonly string _text;", emitted);
        Assert.Contains("readonly bool _enabled;", emitted);
    }

    [Fact]
    public void Surface_ContainerWithWrap2()
    {
        var code = """
            using global::AndroidX.Compose.Runtime;
            using global::AndroidX.Compose.UI;
            using AndroidX.Compose;
            using Kotlin.Jvm.Functions;

            [assembly: ComposeDefaults("SurfaceDefault",
                "modifier", "shape", "color", "contentColor", "tonalElevation",
                "shadowElevation", "border", "!content")]

            namespace AndroidX.Compose
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
        Assert.Contains("var __content = global::AndroidX.Compose.ComposableLambdas.Wrap2(composer, c => RenderChildren(c));", emitted);

        var errors = output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
        Assert.Empty(errors);
    }

    [Fact]
    public void HybridContainer_RequiredFn3PlusNullableFn2_EmitsContainerWithNamedSlot()
    {
        var code = """
            using global::AndroidX.Compose.Runtime;
            using global::AndroidX.Compose.UI;
            using AndroidX.Compose;
            using Kotlin.Jvm.Functions;

            [assembly: ComposeDefaults("BottomAppBarDefault",
                "!actions", "modifier", "floatingActionButton", "containerColor",
                "contentColor", "tonalElevation", "contentPadding", "windowInsets",
                "scrollBehavior")]

            namespace AndroidX.Compose
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
        Assert.Contains(": global::AndroidX.Compose.ComposableContainer", emitted);
        // Required Fn3 stays as container body: RenderChildren + PushScope(Row).
        Assert.Contains("Wrap3(composer, (__scope, c) =>", emitted);
        Assert.Contains("global::AndroidX.Compose.RenderContext.PushScope(__scope, global::AndroidX.Compose.ScopeKind.Row);", emitted);
        Assert.Contains("RenderChildren(c);", emitted);
        // Nullable Fn2 surfaces as a named property.
        Assert.Contains("public global::AndroidX.Compose.ComposableNode? FloatingActionButton { get; set; }", emitted);

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
            using global::AndroidX.Compose.Runtime;
            using global::AndroidX.Compose.UI;
            using AndroidX.Compose;
            using Kotlin.Jvm.Functions;

            [assembly: ComposeDefaults("FooBarDefault",
                "!actions", "modifier", "floatingActionButton")]

            namespace AndroidX.Compose
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
        Assert.Contains(": global::AndroidX.Compose.ComposableNode", emitted);
        Assert.DoesNotContain("RenderChildren", emitted);
        Assert.Contains("public global::AndroidX.Compose.ComposableNode? Actions { get; set; }", emitted);
        Assert.Contains("public global::AndroidX.Compose.ComposableNode? FloatingActionButton { get; set; }", emitted);
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
            using global::AndroidX.Compose.Runtime;
            using global::AndroidX.Compose.UI;
            using AndroidX.Compose;
            using Kotlin.Jvm.Functions;

            [assembly: ComposeDefaults("RailDefault",
                "modifier", "header", "!content")]

            namespace AndroidX.Compose
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
        Assert.Contains(": global::AndroidX.Compose.ComposableContainer", emitted);
        // Non-nullable IFunction2 body wraps children via Wrap2 (no
        // Fn3 / no PushScope — Container=true uses the Fn2 path).
        Assert.Contains("Wrap2(composer, c => RenderChildren(c))", emitted);
        Assert.DoesNotContain("PushScope", emitted);
        // Nullable IFunction2? slot surfaces as a named property, not a
        // ctor primitive.
        Assert.Contains("public global::AndroidX.Compose.ComposableNode? Header { get; set; }", emitted);

        var errors = output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
        Assert.Empty(errors);
    }

    [Fact]
    public void JavaEnumPrimitive_SurfacedAsCtorParam()
    {
        // Simulates ToggleableState — a class derived from Java.Lang.Enum.
        // The generator must accept it as a primitive-like ctor slot.
        var code = """
            using global::AndroidX.Compose.Runtime;
            using global::AndroidX.Compose.UI;
            using AndroidX.Compose;
            using Kotlin.Jvm.Functions;

            [assembly: ComposeDefaults("TriStateCheckboxDefault",
                "!state", "!onClick", "modifier", "enabled", "colors", "interactionSource")]

            namespace AndroidX.Compose.Demo
            {
                public class FakeToggleableState : global::Java.Lang.Enum { }
            }

            namespace AndroidX.Compose
            {
                public static partial class ComposeBridges
                {
                    [ComposeBridge(Class="x/y/Z", JvmName="TriStateCheckbox",
                                   Signature="(Landroidx/compose/ui/state/ToggleableState;Lkotlin/jvm/functions/Function0;Landroidx/compose/ui/Modifier;ZLandroidx/compose/material3/CheckboxColors;Landroidx/compose/foundation/interaction/MutableInteractionSource;Landroidx/compose/runtime/Composer;II)V",
                                   Defaults=typeof(TriStateCheckboxDefault))]
                    [ComposeFacade]
                    public static partial void TriStateCheckbox(global::AndroidX.Compose.Demo.FakeToggleableState state, IFunction0 onClick, IModifier? modifier, IComposer composer);
                }
            }
            """;

        var (output, diags, emitted) = Run(code, "TriStateCheckbox");
        Assert.Empty(diags.Where(d => d.Severity == DiagnosticSeverity.Error));
        Assert.NotNull(emitted);
        // Java enum surfaces as a ctor field + ctor param + bridge arg pass-through.
        Assert.Contains("readonly global::AndroidX.Compose.Demo.FakeToggleableState _state;", emitted);
        Assert.Contains("global::AndroidX.Compose.Demo.FakeToggleableState state", emitted);
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
            using global::AndroidX.Compose.Runtime;
            using global::AndroidX.Compose.UI;
            using AndroidX.Compose;
            using Kotlin.Jvm.Functions;

            [assembly: ComposeDefaults("NavigationBarDefault",
                "modifier", "containerColor", "contentColor", "tonalElevation", "windowInsets", "!content")]

            namespace AndroidX.Compose
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
        Assert.Contains("global::AndroidX.Compose.RenderContext.PushScope(__scope, global::AndroidX.Compose.ScopeKind.Row);", emitted);
        Assert.Contains("Wrap3(__composer, (__scope, c) =>", emitted);
        Assert.Contains("global::AndroidX.Compose.ComposableContentNode.RenderDirect(c, content, false);", emitted);
        Assert.DoesNotContain("var node = new global::AndroidX.Compose.NavigationBar", emitted);

        var errors = output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
        Assert.Empty(errors);
    }

    [Fact]
    public void AmbiguousCallback_EmitsCN3013()
    {
        var code = $$"""
            using global::AndroidX.Compose.Runtime;
            using global::AndroidX.Compose.UI;
            using AndroidX.Compose;
            using Kotlin.Jvm.Functions;

            [assembly: ComposeDefaults("CheckboxDefault",
                "!checked", "!onCheckedChange", "modifier", "enabled", "colors", "interactionSource")]

            namespace AndroidX.Compose
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
        Assert.Contains(diags, d => d.Id == "CN3013");
    }

    [Fact]
    public void AlertDialog_MultiSlotWithDefaultsMask()
    {
        var code = """
            using global::AndroidX.Compose.Runtime;
            using global::AndroidX.Compose.UI;
            using AndroidX.Compose;
            using Kotlin.Jvm.Functions;

            [assembly: ComposeDefaults("AlertDialogDefault",
                "!onDismissRequest", "!confirmButton", "modifier", "dismissButton", "icon", "title", "text",
                "shape", "containerColor", "iconContentColor", "titleContentColor", "textContentColor", "tonalElevation", "properties")]

            namespace AndroidX.Compose
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
        Assert.Contains("public sealed partial class AlertDialog : global::AndroidX.Compose.ComposableNode", emitted);
        Assert.Contains("/// <summary>Required composable slot for Kotlin's <c>confirmButton</c> parameter.</summary>", emitted);
        Assert.Contains("public required global::AndroidX.Compose.ComposableNode ConfirmButton { get; set; }", emitted);
        Assert.Contains("/// <summary>Optional composable slot for Kotlin's <c>dismissButton</c> parameter.</summary>", emitted);
        Assert.Contains("public global::AndroidX.Compose.ComposableNode? DismissButton { get; set; }", emitted);
        Assert.DoesNotContain("public required global::AndroidX.Compose.ComposableNode DismissButton", emitted);
        Assert.Contains("if (ConfirmButton is null)", emitted);
        // OnDismissRequest is a System.Action ctor param.
        Assert.Contains("public AlertDialog(global::System.Action onDismissRequest)", emitted);
        // Auto-mask logic touches each enum member the slot bit corresponds to.
        Assert.Contains("int __defaults = (int)global::AndroidX.Compose.AlertDialogDefault.All;", emitted);
        Assert.Contains("if (__modifier is not null) __defaults &= ~(int)global::AndroidX.Compose.AlertDialogDefault.Modifier;", emitted);
        Assert.Contains("if (__dismissButton is not null) __defaults &= ~(int)global::AndroidX.Compose.AlertDialogDefault.DismissButton;", emitted);
        Assert.Contains("var __confirmButton = global::AndroidX.Compose.ComposableLambdas.Wrap2(__composer, confirmButton);", emitted);
        Assert.Contains("global::AndroidX.Compose.ComposeBridges.AlertDialog(__onDismissRequest, __confirmButton, __modifier, __dismissButton, __icon, __title, __text, (int)__defaults, __composer);", emitted);
        Assert.DoesNotContain("var node = new global::AndroidX.Compose.AlertDialog", emitted);

        var errors = output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
        Assert.Empty(errors);
    }

    [Fact]
    public void ScopeWithoutWrap3_EmitsCN3003()
    {
        var code = """
            using global::AndroidX.Compose.Runtime;
            using global::AndroidX.Compose.UI;
            using AndroidX.Compose;
            using Kotlin.Jvm.Functions;

            [assembly: ComposeDefaults("SurfaceDefault",
                "modifier", "shape", "color", "contentColor", "tonalElevation",
                "shadowElevation", "border", "!content")]

            namespace AndroidX.Compose
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
            using global::AndroidX.Compose.Runtime;
            using global::AndroidX.Compose.UI;
            using AndroidX.Compose;
            using Kotlin.Jvm.Functions;

            [assembly: ComposeDefaults("CardDefault",
                "modifier", "shape", "colors", "elevation", "border", "!content")]

            namespace AndroidX.Compose
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
    public void IndexedChildrenContainer_EmitsRenderChildrenIndexed()
    {
        var code = """
            using global::AndroidX.Compose.Runtime;
            using global::AndroidX.Compose.UI;
            using AndroidX.Compose;
            using Kotlin.Jvm.Functions;

            [assembly: ComposeDefaults("SegmentedButtonRowDefault",
                "modifier", "space", "!content")]

            namespace AndroidX.Compose
            {
                public static partial class ComposeBridges
                {
                    [ComposeBridge(Class="androidx/compose/material3/SegmentedButtonKt", JvmName="SingleChoiceSegmentedButtonRow",
                                   Signature="(Landroidx/compose/ui/Modifier;FLkotlin/jvm/functions/Function3;Landroidx/compose/runtime/Composer;II)V",
                                   Defaults=typeof(SegmentedButtonRowDefault))]
                    [ComposeFacade(Scope = "Other", IndexedChildren = true)]
                    public static partial void SingleChoiceSegmentedButtonRow(IModifier? modifier, IFunction3 content, IComposer composer);
                }
            }
            """;

        var (output, diags, emitted) = Run(code, "SingleChoiceSegmentedButtonRow");
        Assert.Empty(diags.Where(d => d.Severity == DiagnosticSeverity.Error));
        Assert.NotNull(emitted);
        // Scope still pushed.
        Assert.Contains("Wrap3(composer, (__scope, c) =>", emitted);
        Assert.Contains("global::AndroidX.Compose.RenderContext.PushScope(__scope, global::AndroidX.Compose.ScopeKind.Other);", emitted);
        // Per-child indexed loop instead of plain RenderChildren.
        Assert.Contains("RenderChildrenIndexed(c);", emitted);
        Assert.DoesNotContain("RenderChildren(c);", emitted);
        Assert.Contains("global::AndroidX.Compose.ComposableContentNode.RenderDirect(c, content, true);", emitted);
        Assert.DoesNotContain("var node = new global::AndroidX.Compose.SingleChoiceSegmentedButtonRow", emitted);

        var errors = output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
        Assert.Empty(errors);
    }

    [Fact]
    public void IndexedChildrenOnLeaf_EmitsCN3006()
    {
        // Leaf bridge with no IFunction2/IFunction3 body — IndexedChildren has nowhere to apply.
        var code = """
            using global::AndroidX.Compose.Runtime;
            using global::AndroidX.Compose.UI;
            using AndroidX.Compose;
            using Kotlin.Jvm.Functions;

            [assembly: ComposeDefaults("SpacerDefault", "modifier")]

            namespace AndroidX.Compose
            {
                public static partial class ComposeBridges
                {
                    [ComposeBridge(Class="androidx/compose/foundation/layout/SpacerKt", JvmName="Spacer",
                                   Signature="(Landroidx/compose/ui/Modifier;Landroidx/compose/runtime/Composer;II)V",
                                   Defaults=typeof(SpacerDefault))]
                    [ComposeFacade(IndexedChildren = true)]
                    public static partial void IndexedSpacer(IModifier? modifier, IComposer composer);
                }
            }
            """;

        var (_, diags, _) = Run(code, "IndexedSpacer");
        Assert.Contains(diags, d => d.Id == "CN3006" && d.GetMessage().Contains("IndexedChildren"));
    }

    [Fact]
    public void WrongContainingType_EmitsCN3001()
    {
        var code = """
            using global::AndroidX.Compose.Runtime;
            using global::AndroidX.Compose.UI;
            using AndroidX.Compose;
            using Kotlin.Jvm.Functions;

            namespace AndroidX.Compose
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
            using global::AndroidX.Compose.Runtime;
            using global::AndroidX.Compose.UI;
            using AndroidX.Compose;
            using Kotlin.Jvm.Functions;

            namespace AndroidX.Compose
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
            using global::AndroidX.Compose.Runtime;
            using global::AndroidX.Compose.UI;
            using AndroidX.Compose;
            using Kotlin.Jvm.Functions;

            {{ButtonAttrs}}

            namespace AndroidX.Compose
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
        Assert.Contains("global::AndroidX.Compose.ComposeBridges.Button(", emitted);
        Assert.DoesNotContain("global::AndroidX.Compose.ComposeBridges.MyButton(", emitted);

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
            using global::AndroidX.Compose.Runtime;
            using global::AndroidX.Compose.UI;
            using AndroidX.Compose;
            using Kotlin.Jvm.Functions;

            [assembly: ComposeDefaults("IconToggleButtonDefault",
                "!checked", "!onCheckedChange", "modifier", "enabled", "colors", "interactionSource", "!content")]

            namespace AndroidX.Compose
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
        Assert.Contains("composer.RememberAction(v => _onCheckedChange(v is global::Java.Lang.Boolean __b && __b.BooleanValue()));", emitted);

        var errors = output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
        Assert.Empty(errors);
    }

    [Fact]
    public void Callback_StringUnboxesViaToString()
    {
        var code = """
            using global::AndroidX.Compose.Runtime;
            using global::AndroidX.Compose.UI;
            using AndroidX.Compose;
            using Kotlin.Jvm.Functions;

            [assembly: ComposeDefaults("TextFieldDefault",
                "!value", "!onValueChange", "modifier")]

            namespace AndroidX.Compose
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
        Assert.Contains("composer.RememberAction(v => _onValueChange(v?.ToString() ?? string.Empty));", emitted);
        Assert.Contains("global::AndroidX.Compose.ComposeBridges.TextFieldExplicitDefaults(value, __onValueChange, __modifier, (int)__defaults, __composer);", emitted);

        var errors = output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
        Assert.Empty(errors);
    }

    [Fact]
    public void Callback_UnsupportedTypeEmitsCN3005()
    {
        var code = """
            using global::AndroidX.Compose.Runtime;
            using global::AndroidX.Compose.UI;
            using AndroidX.Compose;
            using Kotlin.Jvm.Functions;

            [assembly: ComposeDefaults("FooDefault", "!a", "!cb", "modifier")]

            namespace AndroidX.Compose
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
            using global::AndroidX.Compose.Runtime;
            using global::AndroidX.Compose.UI;
            using AndroidX.Compose;
            using Kotlin.Jvm.Functions;

            [assembly: ComposeDefaults("ListItemDefault",
                "!headlineContent", "modifier", "overlineContent", "supportingContent",
                "leadingContent", "trailingContent", "colors", "tonalElevation", "shadowElevation")]

            namespace AndroidX.Compose
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
        Assert.Contains("public required global::AndroidX.Compose.ComposableNode Headline { get; set; }", emitted);
        Assert.Contains("public global::AndroidX.Compose.ComposableNode? Overline { get; set; }", emitted);
        Assert.Contains("public global::AndroidX.Compose.ComposableNode? Trailing { get; set; }", emitted);
        Assert.Contains("if (Headline is null)", emitted);
        Assert.Contains("if (__overlineContent is not null) __defaults &= ~(int)global::AndroidX.Compose.ListItemDefault.OverlineContent;", emitted);
        Assert.Contains("var __headlineContent = global::AndroidX.Compose.ComposableLambdas.Wrap2(__composer, headline);", emitted);
        Assert.Contains("var __overlineContent = overline is null ? null : global::AndroidX.Compose.ComposableLambdas.Wrap2(__composer, overline);", emitted);
        Assert.DoesNotContain("var node = new global::AndroidX.Compose.ListItem", emitted);

        var errors = output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
        Assert.Empty(errors);
    }

    [Fact]
    public void BadgedBox_TwoRequiredFunction3Slots()
    {
        var code = """
            using global::AndroidX.Compose.Runtime;
            using global::AndroidX.Compose.UI;
            using AndroidX.Compose;
            using Kotlin.Jvm.Functions;

            [assembly: ComposeDefaults("BadgedBoxDefault", "!badge", "modifier", "!content")]

            namespace AndroidX.Compose
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
        Assert.Contains("public sealed partial class BadgedBox : global::AndroidX.Compose.ComposableNode", emitted);
        Assert.Contains("public required global::AndroidX.Compose.ComposableNode Badge { get; set; }", emitted);
        Assert.Contains("public required global::AndroidX.Compose.ComposableNode Content { get; set; }", emitted);
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
            using global::AndroidX.Compose.Runtime;
            using global::AndroidX.Compose.UI;
            using AndroidX.Compose;
            using Kotlin.Jvm.Functions;

            [assembly: ComposeDefaults("DrawerSheetDefault", "!content", "drawerContainerColor")]

            namespace AndroidX.Compose
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
        Assert.Contains("public global::AndroidX.Compose.Color ContainerColor { get; set; }", emitted);
        Assert.Contains("long __color = ContainerColor.ToPacked() != 0L ? ContainerColor.ToPacked() : global::AndroidX.Compose.Material3.MaterialTheme.Instance.GetColorScheme(composer, 0).SecondaryContainer;", emitted);
        Assert.Contains("global::AndroidX.Compose.ComposeBridges.ModalDrawerSheet(__content, __color, composer);", emitted);
        Assert.Contains("long __color = containerColor.ToPacked() != 0L ? containerColor.ToPacked() : global::AndroidX.Compose.Material3.MaterialTheme.Instance.GetColorScheme(__composer, 0).SecondaryContainer;", emitted);
        Assert.Contains("global::AndroidX.Compose.ComposeBridges.ModalDrawerSheetExplicitDefaults(__content, __color, (int)__defaults, __composer);", emitted);

        var errors = output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
        Assert.Empty(errors);
    }

    [Fact]
    public void Changed_ThemeColorDiffsResolvedFallback()
    {
        var code = """
            using global::AndroidX.Compose.Runtime;
            using global::AndroidX.Compose.UI;
            using AndroidX.Compose;
            using Kotlin.Jvm.Functions;

            [assembly: ComposeDefaults("DrawerSheetDefault", "!content", "drawerContainerColor")]

            namespace AndroidX.Compose
            {
                public static partial class ComposeBridges
                {
                    [ComposeBridge(Class="androidx/compose/material3/NavigationDrawerKt",
                                   JvmName="ModalDrawerSheet-afqeVBk",
                                   Signature="(Lkotlin/jvm/functions/Function3;JLandroidx/compose/runtime/Composer;II)V",
                                   Defaults=typeof(DrawerSheetDefault))]
                    [ComposeFacade(DefaultColorFromTheme = "secondaryContainer")]
                    public static partial void ModalDrawerSheet(
                        IFunction3 content,
                        long drawerContainerColor,
                        IComposer composer,
                        int _changed = 0);
                }
            }
            """;

        var (output, diags, emitted) = Run(code, "ModalDrawerSheet");

        Assert.Empty(diags.Where(d => d.Severity == DiagnosticSeverity.Error));
        Assert.NotNull(emitted);
        Assert.Contains("__changed |= __composer.DiffSlot(__color, 4);", emitted);
        Assert.DoesNotContain("__changed |= ((__directChanged >> 4)", emitted);
        Assert.Empty(output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error));
    }

    [Fact]
    public void DefaultColorFromTheme_NoLongParamEmitsCN3007()
    {
        var code = """
            using global::AndroidX.Compose.Runtime;
            using global::AndroidX.Compose.UI;
            using AndroidX.Compose;
            using Kotlin.Jvm.Functions;

            [assembly: ComposeDefaults("CardDefault", "modifier", "!content")]

            namespace AndroidX.Compose
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
    public void PainterResource_EmitsTypedPainterPeer()
    {
        var code = """
            using System;
            using global::AndroidX.Compose.Runtime;
            using global::AndroidX.Compose.UI;
            using global::AndroidX.Compose.UI.Graphics.Painter;
            using AndroidX.Compose;
            using Kotlin.Jvm.Functions;

            [assembly: ComposeDefaults("ImageDefault", "!painter", "contentDescription", "modifier")]

            namespace AndroidX.Compose
            {
                public static partial class ComposeBridges
                {
                    [ComposeBridge(Class="androidx/compose/foundation/ImageKt", JvmName="Image",
                                   Signature="(Landroidx/compose/ui/graphics/painter/Painter;Ljava/lang/String;Landroidx/compose/ui/Modifier;Landroidx/compose/runtime/Composer;II)V",
                                   Defaults=typeof(ImageDefault))]
                    [ComposeFacade]
                    public static partial void Image([PainterResource] Painter painter,
                        string? contentDescription, IModifier? modifier,
                        int defaults, IComposer composer);
                }
            }
            """;

        var (output, diags, emitted) = Run(code, "Image");
        Assert.Empty(diags.Where(d => d.Severity == DiagnosticSeverity.Error));
        Assert.NotNull(emitted);

        // Both ctor overloads: id-based (default) and pre-resolved Painter.
        Assert.Contains("public Image(int drawableResourceId", emitted);
        Assert.Contains("public Image(global::AndroidX.Compose.UI.Graphics.Painter.Painter painter", emitted);

        // Sibling backing fields. Exactly one is populated per instance.
        Assert.Contains("readonly int _drawableResourceId;", emitted);
        Assert.Contains("readonly global::AndroidX.Compose.UI.Graphics.Painter.Painter? _painter;", emitted);

        // Painter ctor null-guards and stores into `_painter`.
        Assert.Contains("_painter = painter ?? throw new global::System.ArgumentNullException(nameof(painter));", emitted);

        // Render preamble: caller-owned Painter is forwarded directly;
        // resource-id path wraps the local ref into a managed peer via
        // TransferLocalRef (consumes the local ref — no DeleteLocalRef
        // needed) so the bridge's auto-emitted GC.KeepAlive covers both
        // shapes uniformly.
        Assert.Contains("global::AndroidX.Compose.UI.Graphics.Painter.Painter __painterPeer;", emitted);
        Assert.Contains("if (_painter is not null)", emitted);
        Assert.Contains("__painterPeer = _painter;", emitted);
        Assert.Contains("__painterRef = global::AndroidX.Compose.ComposeBridges.PainterResource(_drawableResourceId, composer);", emitted);
        Assert.Contains("__painterPeer = global::Java.Lang.Object.GetObject<global::AndroidX.Compose.UI.Graphics.Painter.Painter>(", emitted);
        Assert.Contains("__painterRef, global::Android.Runtime.JniHandleOwnership.TransferLocalRef)!;", emitted);

        // No facade-side try/finally + DeleteLocalRef + GC.KeepAlive
        // anymore — pushed down to the bridge (auto-emits KeepAlive on
        // typed Painter param) / TransferLocalRef (consumes local ref).
        Assert.DoesNotContain("__painterOwned", emitted);
        Assert.DoesNotContain("JNIEnv.DeleteLocalRef(__painterRef)", emitted);
        Assert.DoesNotContain("global::System.GC.KeepAlive(_painter);", emitted);

        // Bridge call passes typed __painterPeer for the painter arg.
        Assert.Contains("global::AndroidX.Compose.ComposeBridges.Image(__painterPeer", emitted);
        Assert.Contains("internal static void Image(global::AndroidX.Compose.Runtime.IComposer composer, global::AndroidX.Compose.UI.Graphics.Painter.Painter painter", emitted);
        Assert.Contains("var __painterRef = global::AndroidX.Compose.ComposeBridges.PainterResource(drawableResourceId, __composer);", emitted);
        Assert.Contains("var __painterPeer = painter;", emitted);
        Assert.DoesNotContain("var node = new global::AndroidX.Compose.Image", emitted);

        var errors = output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
        Assert.Empty(errors);
    }

    [Fact]
    public void ScopeReceiver_IntPtrEndingInScope_BindsToRenderContextCurrentScope()
    {
        var code = """
            using System;
            using global::AndroidX.Compose.Runtime;
            using global::AndroidX.Compose.UI;
            using AndroidX.Compose;
            using Kotlin.Jvm.Functions;

            [assembly: ComposeDefaults("NavigationBarItemDefault",
                "!selected", "!onClick", "!icon", "modifier",
                "enabled", "label", "alwaysShowLabel", "colors", "interactionSource")]

            namespace AndroidX.Compose
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
        Assert.Contains("global::AndroidX.Compose.ComposeBridges.NavigationBarItem(global::AndroidX.Compose.RenderContext.CurrentScope,", emitted);
        // Required Function2 (icon) becomes a named property (auto multi-slot).
        Assert.Contains("public global::AndroidX.Compose.ComposableNode? Icon", emitted);
        // Optional Function2 (label) becomes a named property.
        Assert.Contains("public global::AndroidX.Compose.ComposableNode? Label", emitted);
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
            using global::AndroidX.Compose.Runtime;
            using global::AndroidX.Compose.UI;
            using AndroidX.Compose;
            using Kotlin.Jvm.Functions;

            namespace AndroidX.Compose
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
            using global::AndroidX.Compose.Runtime;
            using global::AndroidX.Compose.UI;
            using global::AndroidX.Compose.UI.Graphics.Painter;
            using AndroidX.Compose;

            [assembly: ComposeDefaults("FooDefault", "!a", "!b")]

            namespace AndroidX.Compose
            {
                public static partial class ComposeBridges
                {
                    [ComposeBridge(Class="x/Foo", JvmName="bar",
                                   Signature="(Landroidx/compose/ui/graphics/painter/Painter;Landroidx/compose/ui/graphics/painter/Painter;Landroidx/compose/runtime/Composer;II)V",
                                   Defaults=typeof(FooDefault))]
                    [ComposeFacade]
                    public static partial void Foo([PainterResource] Painter a, [PainterResource] Painter b,
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
            using global::AndroidX.Compose.Runtime;
            using global::AndroidX.Compose.UI;
            using AndroidX.Compose;
            using Kotlin.Jvm.Functions;

            namespace AndroidX.Compose
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
            using global::AndroidX.Compose.Runtime;
            using global::AndroidX.Compose.UI;
            using AndroidX.Compose;
            using Kotlin.Jvm.Functions;

            [assembly: ComposeDefaults("BoxDefault", "modifier", "contentAlignment", "propagateMinConstraints", "!content")]

            namespace AndroidX.Compose
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
        Assert.Contains("public sealed partial class Box : global::AndroidX.Compose.ComposableContainer", emitted);
        Assert.Contains("global::AndroidX.Compose.ComposeBridges.Box(", emitted);
        // Auto-mask emits because the wrapper takes `int defaults`.
        Assert.Contains("(int)global::AndroidX.Compose.BoxDefault.All", emitted);
        Assert.Contains("if (__modifier is not null) __defaults &= ~(int)global::AndroidX.Compose.BoxDefault.Modifier", emitted);
    }

    [Fact]
    public void WrapperFacade_NoDefaultsAttribute_GeneratesFacadeWithoutMask()
    {
        // Spacer-style wrapper: no $default in the bytecode, so no
        // Defaults enum either. Facade generator should still emit a
        // working facade — just without the auto-mask code.
        var code = """
            using global::AndroidX.Compose.Runtime;
            using global::AndroidX.Compose.UI;
            using AndroidX.Compose;
            using Kotlin.Jvm.Functions;

            namespace AndroidX.Compose
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
        Assert.Contains("public sealed partial class Spacer : global::AndroidX.Compose.ComposableNode", emitted);
        Assert.Contains("global::AndroidX.Compose.ComposeBridges.Spacer(", emitted);
        Assert.DoesNotContain("__defaults", emitted);
    }

    [Fact]
    public void Facade_EnumCtorParameter_IsAcceptedAsPrimitive()
    {
        // TriStateCheckbox-style facade: the ctor takes an enum value
        // (ToggleableState) plus an Action onClick. Enums should be
        // classified as ctor primitives (TypeKind.Enum).
        var code = """
            using global::AndroidX.Compose.Runtime;
            using global::AndroidX.Compose.UI;
            using AndroidX.Compose;
            using Kotlin.Jvm.Functions;

            namespace MyApp { public enum MyState { On, Off, Mixed } }

            [assembly: ComposeDefaults("TriDefault", "!state", "!onClick", "modifier")]

            namespace AndroidX.Compose
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
        Assert.Contains("public sealed partial class TriStateCheckbox : global::AndroidX.Compose.ComposableNode", emitted);
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
            using global::AndroidX.Compose.Runtime;
            using global::AndroidX.Compose.UI;
            using AndroidX.Compose;
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

            namespace AndroidX.Compose
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

    [Fact]
    public void WrapperFacade_ThirtyTwoSlots_UsesUncheckedSingleMask()
    {
        var names = string.Join(", ", Enumerable.Range(0, 32).Select(i => $"\"slot{i}\""));
        var parameters = string.Join(", ", Enumerable.Range(0, 32).Select(i => $"int slot{i}"));
        var code = $$"""
            using global::AndroidX.Compose.Runtime;
            using AndroidX.Compose;

            [assembly: ComposeDefaults("ThirtyTwoDefault", {{names}})]

            namespace AndroidX.Compose
            {
                public static partial class ComposeBridges
                {
                    [ComposeFacade(Defaults = typeof(ThirtyTwoDefault))]
                    public static partial void ThirtyTwo(
                        {{parameters}}, int defaults, IComposer composer);

                    public static partial void ThirtyTwo(
                        {{parameters}}, int defaults, IComposer composer) { }
                }
            }
            """;

        var (output, diags, emitted) = Run(code, "ThirtyTwo");

        Assert.Empty(diags.Where(d => d.Severity == DiagnosticSeverity.Error));
        Assert.NotNull(emitted);
        Assert.Contains(
            "int __defaults = unchecked((int)global::AndroidX.Compose.ThirtyTwoDefault.All);",
            emitted);
        Assert.Empty(output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error));
    }

    // ─── Phase 4 — [StateHolder] state-holder facades ─────────────────

    /// <summary>Shared snippet stubbing a DatePicker-style state class.</summary>
    const string DatePickerStateStubs = """
        namespace AndroidX.Compose.Material3
        {
            public interface IDatePickerState { }
        }
        namespace AndroidX.Compose
        {
            public sealed class DatePickerState
            {
                internal global::AndroidX.Compose.Material3.IDatePickerState? Jvm;
            }
        }
        """;

    const string DatePickerSig =
        "(Landroidx/compose/material3/DatePickerState;Landroidx/compose/ui/Modifier;Landroidx/compose/material3/DatePickerFormatter;Landroidx/compose/material3/DatePickerColors;Lkotlin/jvm/functions/Function2;Lkotlin/jvm/functions/Function2;ZLandroidx/compose/runtime/Composer;II)V";

    [Fact]
    public void DatePicker_StateHolder_GeneratesRememberRoundTrip()
    {
        var code = $$"""
            using global::AndroidX.Compose.Runtime;
            using global::AndroidX.Compose.UI;
            using AndroidX.Compose;
            using Kotlin.Jvm.Functions;
            using System;

            [assembly: ComposeDefaults("DatePickerDefault",
                "!state", "modifier", "dateFormatter", "colors", "title", "headline", "showModeToggle")]

            {{DatePickerStateStubs}}

            namespace AndroidX.Compose
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
        Assert.Contains("global::AndroidX.Compose.DatePickerState? _state;", emitted);
        Assert.DoesNotContain("readonly global::AndroidX.Compose.DatePickerState? _state;", emitted);
        Assert.Contains("public DatePicker(global::AndroidX.Compose.DatePickerState? state = null)", emitted);

        // Render: Remember + .Jvm population.
        Assert.Contains(
            "var __state = global::AndroidX.Compose.ComposeBridges.RememberDatePickerState(composer);",
            emitted);
        Assert.Contains("if (_state is not null && _state.Jvm is null)", emitted);
        Assert.Contains(
            "_state.Jvm = global::Java.Lang.Object.GetObject<global::AndroidX.Compose.Material3.IDatePickerState>(__state, global::Android.Runtime.JniHandleOwnership.DoNotTransfer)!;",
            emitted);

        // Bridge call uses __state in the IntPtr slot.
        Assert.Contains(
            "global::AndroidX.Compose.ComposeBridges.DatePicker(__state, __modifier, __defaults, composer);",
            emitted);

        var errors = output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
        Assert.Empty(errors);
    }

    [Fact]
    public void StateHolder_SortsBeforeDefaultedPrimitive_PreservesPositionalBinding()
    {
        // Regression for PR #240: when a bridge mixes a [StateHolder] slot
        // with a defaulted-primitive slot (e.g. `bool showModeToggle = true`)
        // the generated ctor MUST emit StateHolder first, defaulted-primitive
        // second — otherwise `new Foo(stateHolder)` positional call sites
        // (Jetnews/Jetchat sample apps) silently rebind to the bool and the
        // compiler complains "cannot convert from StateHolder to bool".
        var code = $$"""
            using global::AndroidX.Compose.Runtime;
            using global::AndroidX.Compose.UI;
            using AndroidX.Compose;
            using Kotlin.Jvm.Functions;
            using System;

            [assembly: ComposeDefaults("DatePickerDefault",
                "!state", "modifier", "dateFormatter", "colors", "title", "headline", "showModeToggle")]

            {{DatePickerStateStubs}}

            namespace AndroidX.Compose
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
                        bool showModeToggle = true,
                        int defaults = 0,
                        IComposer composer = null!);

                    public static IntPtr RememberDatePickerState(IComposer composer) => default;
                }
            }
            """;

        var (output, diags, emitted) = Run(code, "DatePicker");
        Assert.Empty(diags.Where(d => d.Severity == DiagnosticSeverity.Error));
        Assert.NotNull(emitted);

        // The whole point: StateHolder FIRST, primitive SECOND. Any other
        // ordering breaks positional `new DatePicker(myState)` calls.
        Assert.Contains(
            "public DatePicker(global::AndroidX.Compose.DatePickerState? state = null, bool showModeToggle = true)",
            emitted);

        var errors = output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
        Assert.Empty(errors);
    }

    [Fact]
    public void StateHolder_OnNonIntPtr_FailsCN3009()
    {
        var code = $$"""
            using global::AndroidX.Compose.Runtime;
            using global::AndroidX.Compose.UI;
            using AndroidX.Compose;
            using Kotlin.Jvm.Functions;

            [assembly: ComposeDefaults("DatePickerDefault",
                "!state", "modifier", "dateFormatter", "colors", "title", "headline", "showModeToggle")]

            {{DatePickerStateStubs}}

            namespace AndroidX.Compose
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
            using global::AndroidX.Compose.Runtime;
            using global::AndroidX.Compose.UI;
            using AndroidX.Compose;
            using Kotlin.Jvm.Functions;
            using System;

            [assembly: ComposeDefaults("DatePickerDefault",
                "!state", "modifier", "dateFormatter", "colors", "title", "headline", "showModeToggle")]

            namespace AndroidX.Compose.Material3 { public interface IDatePickerState { } }
            namespace AndroidX.Compose { public sealed class BadState { } }

            namespace AndroidX.Compose
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
            using global::AndroidX.Compose.Runtime;
            using global::AndroidX.Compose.UI;
            using AndroidX.Compose;
            using Kotlin.Jvm.Functions;
            using System;

            [assembly: ComposeDefaults("DatePickerDefault",
                "state", "modifier", "dateFormatter", "colors", "title", "headline", "showModeToggle")]

            {{DatePickerStateStubs}}

            namespace AndroidX.Compose
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
            "__defaults &= ~(int)global::AndroidX.Compose.DatePickerDefault.State;",
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
        namespace AndroidX.Compose
        {
            public sealed class TimePickerState
            {
                internal global::AndroidX.Compose.Material3.ITimePickerState? Jvm;
                public TimePickerState(int initialHour = 12, int initialMinute = 0, bool is24Hour = true)
                {
                    InitialHour = initialHour;
                    InitialMinute = initialMinute;
                    InitialIs24Hour = is24Hour;
                }
                internal int InitialHour { get; }
                internal int InitialMinute { get; }
                internal bool InitialIs24Hour { get; }
                internal int RememberHour => InitialHour;
                internal int RememberMinute => InitialMinute;
                internal void BindJvm(global::AndroidX.Compose.Material3.ITimePickerState jvm) => Jvm = jvm;
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
            using global::AndroidX.Compose.Runtime;
            using global::AndroidX.Compose.UI;
            using AndroidX.Compose;
            using Kotlin.Jvm.Functions;
            using System;

            [assembly: ComposeDefaults("TimePickerDefault",
                "!state", "modifier", "colors", "layoutType")]

            {{TimePickerStateStubs}}

            namespace AndroidX.Compose
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

                    public static IntPtr RememberTimePickerState(int rememberHour, int rememberMinute,
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
        Assert.Contains("global::AndroidX.Compose.TimePickerState? _state;", emitted);
        Assert.DoesNotContain("readonly global::AndroidX.Compose.TimePickerState? _state;", emitted);
        Assert.Contains("public TimePicker(global::AndroidX.Compose.TimePickerState? state = null)", emitted);
        Assert.Contains("_state = state ?? new global::AndroidX.Compose.TimePickerState();", emitted);

        // Render: Remember called with wrapper-sourced pending args.
        // RememberHour/RememberMinute resolve via Pascal match;
        // is24Hour falls back to Is24Hour (live getter), which returns
        // InitialIs24Hour while Jvm is still null on first render.
        Assert.Contains(
            "var __state = global::AndroidX.Compose.ComposeBridges.RememberTimePickerState(_state!.RememberHour, _state!.RememberMinute, _state!.Is24Hour, composer);",
            emitted);
        // Unguarded Jvm population — Phase 4b knows _state is non-null.
        Assert.Contains("if (_state.Jvm is null)", emitted);
        Assert.DoesNotContain("if (_state is not null && _state.Jvm is null)", emitted);
        Assert.Contains(
            "_state.Jvm = global::Java.Lang.Object.GetObject<global::AndroidX.Compose.Material3.ITimePickerState>(__state, global::Android.Runtime.JniHandleOwnership.DoNotTransfer)!;",
            emitted);

        // Bridge call uses __state in the IntPtr slot.
        Assert.Contains(
            "global::AndroidX.Compose.ComposeBridges.TimePicker(__state, __modifier, __defaults, composer);",
            emitted);

        var errors = output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
        Assert.Empty(errors);
    }

    [Fact]
    public void ComposableMethod_StateHolderBeforeFacadeDefaultPrimitive_EmitsValidOptionalParameters()
    {
        var code = $$"""
            using global::AndroidX.Compose.Runtime;
            using global::AndroidX.Compose.UI;
            using AndroidX.Compose;
            using System;

            [assembly: ComposeDefaults("PickerDefault",
                "!state", "modifier", "showModeToggle")]

            {{TimePickerStateStubs}}

            namespace AndroidX.Compose
            {
                public static partial class ComposeBridges
                {
                    [ComposeBridge(Class="x/Picker", JvmName="Picker",
                                   Signature="(Landroidx/compose/material3/TimePickerState;Landroidx/compose/ui/Modifier;ZLandroidx/compose/runtime/Composer;II)V",
                                   Defaults=typeof(PickerDefault))]
                    [ComposeFacade]
                    public static partial void Picker(
                        [StateHolder(Remember = nameof(RememberTimePickerState),
                                     StateType = typeof(TimePickerState))]
                        IntPtr state,
                        IModifier? modifier,
                        [FacadeDefault(true)] bool showModeToggle,
                        int defaults,
                        IComposer composer);

                    public static IntPtr RememberTimePickerState(
                        int initialHour, int initialMinute, bool is24Hour,
                        IComposer composer) => default;
                }
            }
            """;

        var (output, diags, emitted) = Run(code, "Picker");

        Assert.Empty(diags.Where(d => d.Severity == DiagnosticSeverity.Error));
        Assert.NotNull(emitted);
        Assert.Contains(
            "internal static void Picker(global::AndroidX.Compose.Runtime.IComposer composer, global::AndroidX.Compose.TimePickerState? state = null, bool showModeToggle = true, global::AndroidX.Compose.Modifier? modifier = null)",
            emitted);
        Assert.Empty(output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error));
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
            using global::AndroidX.Compose.Runtime;
            using global::AndroidX.Compose.UI;
            using AndroidX.Compose;
            using Kotlin.Jvm.Functions;
            using System;

            [assembly: ComposeDefaults("TimePickerDefault",
                "!state", "modifier", "colors", "layoutType")]

            {{TimePickerStateStubs}}

            namespace AndroidX.Compose
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
                                     Bind = nameof(TimePickerState.BindJvm),
                                     SharedState = true)]
                        IntPtr state,
                        IModifier? modifier,
                        int defaults,
                        IComposer composer);

                    public static IntPtr RememberTimePickerState(int rememberHour, int rememberMinute,
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
            "__state = global::AndroidX.Compose.ComposeBridges.RememberTimePickerState(_state!.RememberHour, _state!.RememberMinute, _state!.Is24Hour, composer);",
            emitted);
        // Phase 4b assigns unguarded (ctor auto-create guarantees non-null).
        Assert.Contains(
            "_state.BindJvm(",
            emitted);
        Assert.Contains(
            "global::Java.Lang.Object.GetObject<global::AndroidX.Compose.Material3.ITimePickerState>(__state, global::Android.Runtime.JniHandleOwnership.DoNotTransfer)",
            emitted);

        // Must NOT emit the non-shared "always call Remember" preamble.
        Assert.DoesNotContain(
            "var __state = global::AndroidX.Compose.ComposeBridges.RememberTimePickerState",
            emitted);

        // Bridge call still uses __state.
        Assert.Contains(
            "global::AndroidX.Compose.ComposeBridges.TimePicker(__state, __modifier, __defaults, composer);",
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
            using global::AndroidX.Compose.Runtime;
            using global::AndroidX.Compose.UI;
            using AndroidX.Compose;
            using Kotlin.Jvm.Functions;
            using System;

            [assembly: ComposeDefaults("DatePickerDefault",
                "!state", "modifier", "dateFormatter", "colors", "title", "headline", "showModeToggle")]

            {{DatePickerStateStubs}}

            namespace AndroidX.Compose
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
        Assert.Contains("global::AndroidX.Compose.DatePickerState? _state;", emitted);
        Assert.DoesNotContain("readonly global::AndroidX.Compose.DatePickerState? _state;", emitted);
        Assert.DoesNotContain("_state = state ?? new global::AndroidX.Compose.DatePickerState();", emitted);

        // Cache-hit branch — guarded with explicit null check on _state.
        Assert.Contains("if (_state is not null && _state.Jvm is not null)", emitted);
        Assert.Contains(
            "__state = ((global::Android.Runtime.IJavaObject)_state.Jvm).Handle;",
            emitted);

        // Cache-miss branch — Remember + null-guarded Jvm assignment.
        Assert.Contains(
            "__state = global::AndroidX.Compose.ComposeBridges.RememberDatePickerState(composer);",
            emitted);
        Assert.Contains("if (_state is not null)", emitted);
        Assert.Contains(
            "_state.Jvm = global::Java.Lang.Object.GetObject<global::AndroidX.Compose.Material3.IDatePickerState>(__state, global::Android.Runtime.JniHandleOwnership.DoNotTransfer)!;",
            emitted);

        // Must NOT emit the non-shared "always call Remember" preamble.
        Assert.DoesNotContain(
            "var __state = global::AndroidX.Compose.ComposeBridges.RememberDatePickerState",
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
            using global::AndroidX.Compose.Runtime;
            using global::AndroidX.Compose.UI;
            using AndroidX.Compose;
            using Kotlin.Jvm.Functions;
            using System;

            [assembly: ComposeDefaults("TimePickerDefault",
                "!state", "modifier", "colors", "layoutType")]

            {{TimePickerStateStubs}}

            namespace AndroidX.Compose
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
            "var __state = global::AndroidX.Compose.ComposeBridges.RememberTimePickerState(_state!.InitialHour, _state!.InitialMinute, _state!.Is24Hour, composer);",
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
            using global::AndroidX.Compose.Runtime;
            using global::AndroidX.Compose.UI;
            using AndroidX.Compose;
            using Kotlin.Jvm.Functions;
            using System;

            [assembly: ComposeDefaults("TimePickerDefault",
                "!state", "modifier", "colors", "layoutType")]

            {{TimePickerStateStubs}}

            namespace AndroidX.Compose
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
    public void ParameterisedStateHolder_IncompatibleMemberType_FailsCN3009()
    {
        var code = $$"""
            using global::AndroidX.Compose.Runtime;
            using global::AndroidX.Compose.UI;
            using AndroidX.Compose;
            using System;

            [assembly: ComposeDefaults("TimePickerDefault",
                "!state", "modifier", "colors", "layoutType")]

            namespace AndroidX.Compose.Material3
            {
                public interface ITimePickerState { }
            }

            namespace AndroidX.Compose
            {
                public sealed class BadState
                {
                    internal global::AndroidX.Compose.Material3.ITimePickerState? Jvm;
                    public string InitialHour => "12";
                }

                public static partial class ComposeBridges
                {
                    [ComposeBridge(Class="x/y/TimePickerKt", JvmName="TimePicker-mT9BvqQ",
                                   Signature="{{TimePickerSig}}",
                                   Defaults=typeof(TimePickerDefault))]
                    [ComposeFacade]
                    public static partial void TimePicker(
                        [StateHolder(Remember = nameof(RememberTimePickerState),
                                     StateType = typeof(BadState))]
                        IntPtr state,
                        IModifier? modifier,
                        int defaults,
                        IComposer composer);

                    public static IntPtr RememberTimePickerState(int initialHour, IComposer composer) => default;
                }
            }
            """;

        var (_, diags, emitted) = Run(code, "TimePicker");
        Assert.Null(emitted);
        Assert.Contains(diags, d => d.Id == "CN3009"
            && d.GetMessage().Contains("member 'InitialHour' has type 'string'")
            && d.GetMessage().Contains("not implicitly convertible")
            && d.GetMessage().Contains("parameter 'initialHour' of type 'int'"));
    }

    [Fact]
    public void ParameterisedStateHolder_NoParameterlessCtor_FailsCN3009()
    {
        // StateType only has an all-required-params ctor, so the
        // facade can't auto-create it. CN3009 fires.
        var code = $$"""
            using global::AndroidX.Compose.Runtime;
            using global::AndroidX.Compose.UI;
            using AndroidX.Compose;
            using Kotlin.Jvm.Functions;
            using System;

            [assembly: ComposeDefaults("TimePickerDefault",
                "!state", "modifier", "colors", "layoutType")]
            namespace AndroidX.Compose.Material3 { public interface ITimePickerState { } }
            namespace AndroidX.Compose
            {
                public sealed class TimePickerState
                {
                    internal global::AndroidX.Compose.Material3.ITimePickerState? Jvm;
                    public TimePickerState(int initialHour, int initialMinute, bool is24Hour) { }
                    public int InitialHour => 0;
                    public int InitialMinute => 0;
                    public bool Is24Hour => false;
                }
            }

            namespace AndroidX.Compose
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
            using global::AndroidX.Compose.Runtime;
            using global::AndroidX.Compose.UI;
            using AndroidX.Compose;
            using Kotlin.Jvm.Functions;
            using System;

            [assembly: ComposeDefaults("DatePickerDefault",
                "!state", "modifier", "dateFormatter", "colors", "title", "headline", "showModeToggle")]

            {{DatePickerStateStubs}}

            namespace AndroidX.Compose
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
            using global::AndroidX.Compose.Runtime;
            using global::AndroidX.Compose.UI;
            using AndroidX.Compose;
            using Kotlin.Jvm.Functions;

            [assembly: ComposeDefaults("TextDefault",
                "!text", "modifier", "fontSize")]

            namespace AndroidX.Compose
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
        Assert.Contains("public global::AndroidX.Compose.Sp? FontSize { get; set; }", emitted);
        // Bridge call passes property through (no ctor slot, no field).
        Assert.Contains("global::AndroidX.Compose.ComposeBridges.Text(_text, BuildModifier(), FontSize, composer);", emitted);
        // Not a tree-facade ctor parameter; composable method exposes it directly.
        Assert.Contains("public Text(string text)", emitted);
        Assert.Contains("global::AndroidX.Compose.Sp? fontSize = null", emitted);

        var errors = output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
        Assert.Empty(errors);
    }

    [Fact]
    public void OptionalValue_FontWeightEmitsNullableReferenceProperty()
    {
        var code = """
            using global::AndroidX.Compose.Runtime;
            using global::AndroidX.Compose.UI;
            using AndroidX.Compose;
            using Kotlin.Jvm.Functions;

            [assembly: ComposeDefaults("TextDefault",
                "!text", "modifier", "fontWeight")]

            namespace AndroidX.Compose
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
        Assert.Contains("public global::AndroidX.Compose.FontWeight? FontWeight { get; set; }", emitted);
        Assert.Contains("global::AndroidX.Compose.ComposeBridges.Text(_text, BuildModifier(), FontWeight, composer);", emitted);

        var errors = output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
        Assert.Empty(errors);
    }

    [Fact]
    public void OptionalValue_ShapeEmitsNullableReferenceProperty()
    {
        // Regression: Shape? on a Card-style facade should classify as
        // OptionalValue, surface as a `Shape? Shape { get; set; }`
        // auto-property, and forward through the bridge call positionally.
        // Auto-mask must clear CardDefault.Shape when the caller assigns.
        var code = """
            using global::AndroidX.Compose.Runtime;
            using global::AndroidX.Compose.UI;
            using AndroidX.Compose;
            using Kotlin.Jvm.Functions;

            [assembly: ComposeDefaults("CardDefault",
                "modifier", "shape", "!content")]

            namespace AndroidX.Compose
            {
                public static partial class ComposeBridges
                {
                    [ComposeBridge(Class="androidx/compose/material3/CardKt", JvmName="Card",
                                   Signature="(Landroidx/compose/ui/Modifier;Landroidx/compose/ui/graphics/Shape;Lkotlin/jvm/functions/Function3;Landroidx/compose/runtime/Composer;II)V",
                                   Defaults=typeof(CardDefault))]
                    [ComposeFacade]
                    public static partial void Card(IModifier? modifier, Shape? shape, IFunction3 content, IComposer composer);
                }
            }
            """;

        var (output, diags, emitted) = Run(code, "Card");
        Assert.Empty(diags.Where(d => d.Severity == DiagnosticSeverity.Error));
        Assert.NotNull(emitted);
        Assert.Contains("public global::AndroidX.Compose.Shape? Shape { get; set; }", emitted);
        // The bridge call forwards `Shape` positionally — between BuildModifier() and __content.
        Assert.Contains("global::AndroidX.Compose.ComposeBridges.Card(BuildModifier(), Shape, __content, composer);", emitted);

        var errors = output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
        Assert.Empty(errors);
    }

    [Fact]
    public void OptionalValue_PaddingValuesEmitsNullableReferenceProperty()
    {
        var code = """
            using global::AndroidX.Compose.Runtime;
            using global::AndroidX.Compose.UI;
            using AndroidX.Compose;
            using Kotlin.Jvm.Functions;

            [assembly: ComposeDefaults("TextDefault",
                "!text", "modifier", "contentPadding")]

            namespace AndroidX.Compose
            {
                public static partial class ComposeBridges
                {
                    [ComposeBridge(Class="androidx/compose/material3/TextKt", JvmName="Text",
                                   Signature="(Ljava/lang/String;Landroidx/compose/ui/Modifier;Landroidx/compose/foundation/layout/PaddingValues;Landroidx/compose/runtime/Composer;II)V",
                                   Defaults=typeof(TextDefault))]
                    [ComposeFacade]
                    public static partial void Text(string text, IModifier? modifier, PaddingValues? contentPadding, IComposer composer);
                }
            }
            """;

        var (output, diags, emitted) = Run(code, "Text");
        Assert.Empty(diags.Where(d => d.Severity == DiagnosticSeverity.Error));
        Assert.NotNull(emitted);
        // Reference-typed wrapper surfaces as a nullable property.
        Assert.Contains("public global::AndroidX.Compose.PaddingValues? ContentPadding { get; set; }", emitted);
        Assert.Contains("global::AndroidX.Compose.ComposeBridges.Text(_text, BuildModifier(), ContentPadding, composer);", emitted);

        var errors = output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
        Assert.Empty(errors);
    }

    [Fact]
    public void OptionalValue_ButtonColorsEmitsNullableReferenceProperty()
    {
        // Phase 2 MAUI: ButtonColors? on the Button bridge surfaces as
        // a nullable property and forwards positionally. MAUI's
        // ButtonHandler builds one from MAUI Primary and assigns it
        // so the M3 default container color is overridden.
        var code = """
            using global::AndroidX.Compose.Runtime;
            using global::AndroidX.Compose.UI;
            using AndroidX.Compose;
            using AndroidX.Compose.Material3;
            using Kotlin.Jvm.Functions;

            [assembly: ComposeDefaults("ButtonDefault",
                "!onClick", "modifier", "colors", "!content")]

            namespace AndroidX.Compose
            {
                public static partial class ComposeBridges
                {
                    [ComposeBridge(Class="androidx/compose/material3/ButtonKt", JvmName="Button",
                                   Signature="(Lkotlin/jvm/functions/Function0;Landroidx/compose/ui/Modifier;Landroidx/compose/material3/ButtonColors;Lkotlin/jvm/functions/Function3;Landroidx/compose/runtime/Composer;II)V",
                                   Defaults=typeof(ButtonDefault))]
                    [ComposeFacade]
                    public static partial void Button(IFunction0 onClick, IModifier? modifier, ButtonColors? colors, IFunction3 content, IComposer composer);
                }
            }
            """;

        var (output, diags, emitted) = Run(code, "Button");
        Assert.Empty(diags.Where(d => d.Severity == DiagnosticSeverity.Error));
        Assert.NotNull(emitted);
        Assert.Contains("public global::AndroidX.Compose.Material3.ButtonColors? Colors { get; set; }", emitted);
        Assert.Contains("global::AndroidX.Compose.ComposeBridges.Button(__onClick, BuildModifier(), Colors, __content, composer);", emitted);

        var errors = output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
        Assert.Empty(errors);
    }

    [Fact]
    public void OptionalValue_TextStyleEmitsNullableReferenceProperty()
    {
        // Phase 2 MAUI: TextStyle? on the TextField bridge surfaces
        // as a nullable property and forwards positionally. MAUI's
        // EntryHandler builds one from MAUI TextColor/Font and assigns.
        var code = """
            using global::AndroidX.Compose.Runtime;
            using global::AndroidX.Compose.UI;
            using AndroidX.Compose;
            using AndroidX.Compose.UI.Text;
            using Kotlin.Jvm.Functions;

            [assembly: ComposeDefaults("TfDefault",
                "!value", "modifier", "textStyle")]

            namespace AndroidX.Compose
            {
                public static partial class ComposeBridges
                {
                    [ComposeBridge(Class="androidx/compose/material3/TextFieldKt", JvmName="Tf",
                                   Signature="(Ljava/lang/String;Landroidx/compose/ui/Modifier;Landroidx/compose/ui/text/TextStyle;Landroidx/compose/runtime/Composer;II)V",
                                   Defaults=typeof(TfDefault))]
                    [ComposeFacade]
                    public static partial void Tf(string value, IModifier? modifier, TextStyle? textStyle, IComposer composer);
                }
            }
            """;

        var (output, diags, emitted) = Run(code, "Tf");
        Assert.Empty(diags.Where(d => d.Severity == DiagnosticSeverity.Error));
        Assert.NotNull(emitted);
        Assert.Contains("public global::AndroidX.Compose.UI.Text.TextStyle? TextStyle { get; set; }", emitted);
        Assert.Contains("global::AndroidX.Compose.ComposeBridges.Tf(_value, BuildModifier(), TextStyle, composer);", emitted);

        var errors = output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
        Assert.Empty(errors);
    }

    [Fact]
    public void OptionalValue_VisualTransformationEmitsNullableReferenceProperty()
    {
        // Phase 2 MAUI: IVisualTransformation? on the TextField bridge
        // surfaces as a nullable property and forwards positionally.
        // MAUI's EntryHandler routes IsPassword to PasswordVisualTransformation.
        var code = """
            using global::AndroidX.Compose.Runtime;
            using global::AndroidX.Compose.UI;
            using AndroidX.Compose;
            using AndroidX.Compose.UI.Text.Input;
            using Kotlin.Jvm.Functions;

            [assembly: ComposeDefaults("TfDefault",
                "!value", "modifier", "visualTransformation")]

            namespace AndroidX.Compose
            {
                public static partial class ComposeBridges
                {
                    [ComposeBridge(Class="androidx/compose/material3/TextFieldKt", JvmName="Tf",
                                   Signature="(Ljava/lang/String;Landroidx/compose/ui/Modifier;Landroidx/compose/ui/text/input/VisualTransformation;Landroidx/compose/runtime/Composer;II)V",
                                   Defaults=typeof(TfDefault))]
                    [ComposeFacade]
                    public static partial void Tf(string value, IModifier? modifier, IVisualTransformation? visualTransformation, IComposer composer);
                }
            }
            """;

        var (output, diags, emitted) = Run(code, "Tf");
        Assert.Empty(diags.Where(d => d.Severity == DiagnosticSeverity.Error));
        Assert.NotNull(emitted);
        Assert.Contains("public global::AndroidX.Compose.UI.Text.Input.IVisualTransformation? VisualTransformation { get; set; }", emitted);
        Assert.Contains("global::AndroidX.Compose.ComposeBridges.Tf(_value, BuildModifier(), VisualTransformation, composer);", emitted);

        var errors = output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
        Assert.Empty(errors);
    }

    [Fact]
    public void OptionalValue_KeyboardOptionsEmitsNullableReferenceProperty()
    {
        // Phase 2 MAUI: KeyboardOptions? on the TextField bridge
        // surfaces as a nullable property and forwards positionally.
        // MAUI's EntryHandler maps MAUI Keyboard enum to KeyboardType
        // via KeyboardOptions.Default.Copy.
        var code = """
            using global::AndroidX.Compose.Runtime;
            using global::AndroidX.Compose.UI;
            using AndroidX.Compose;
            using AndroidX.Compose.Foundation.Text;
            using Kotlin.Jvm.Functions;

            [assembly: ComposeDefaults("TfDefault",
                "!value", "modifier", "keyboardOptions")]

            namespace AndroidX.Compose
            {
                public static partial class ComposeBridges
                {
                    [ComposeBridge(Class="androidx/compose/material3/TextFieldKt", JvmName="Tf",
                                   Signature="(Ljava/lang/String;Landroidx/compose/ui/Modifier;Landroidx/compose/foundation/text/KeyboardOptions;Landroidx/compose/runtime/Composer;II)V",
                                   Defaults=typeof(TfDefault))]
                    [ComposeFacade]
                    public static partial void Tf(string value, IModifier? modifier, KeyboardOptions? keyboardOptions, IComposer composer);
                }
            }
            """;

        var (output, diags, emitted) = Run(code, "Tf");
        Assert.Empty(diags.Where(d => d.Severity == DiagnosticSeverity.Error));
        Assert.NotNull(emitted);
        Assert.Contains("public global::AndroidX.Compose.Foundation.Text.KeyboardOptions? KeyboardOptions { get; set; }", emitted);
        Assert.Contains("global::AndroidX.Compose.ComposeBridges.Tf(_value, BuildModifier(), KeyboardOptions, composer);", emitted);

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
            using global::AndroidX.Compose.Runtime;
            using global::AndroidX.Compose.UI;
            using AndroidX.Compose;
            using Kotlin.Jvm.Functions;

            [assembly: ComposeDefaults("TextDefault", "!text", "modifier", "fontWeight")]

            namespace AndroidX.Compose
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
            using global::AndroidX.Compose.Runtime;
            using global::AndroidX.Compose.UI;
            using AndroidX.Compose;
            using Kotlin.Jvm.Functions;

            [assembly: ComposeDefaults("TextDefault",
                "!text", "modifier", "fontSize", "fontWeight", "letterSpacing", "decoration", "lineHeight")]

            namespace AndroidX.Compose
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
        Assert.Contains("public global::AndroidX.Compose.Sp? FontSize { get; set; }", emitted);
        Assert.Contains("public global::AndroidX.Compose.FontWeight? FontWeight { get; set; }", emitted);
        Assert.Contains("public global::AndroidX.Compose.Sp? LetterSpacing { get; set; }", emitted);
        Assert.Contains("public global::AndroidX.Compose.TextDecoration? Decoration { get; set; }", emitted);
        Assert.Contains("public global::AndroidX.Compose.Sp? LineHeight { get; set; }", emitted);
        // PascalCased property names flow through the bridge call.
        Assert.Contains(
            "global::AndroidX.Compose.ComposeBridges.Text(_text, BuildModifier(), FontSize, FontWeight, LetterSpacing, Decoration, LineHeight, composer);",
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
            using global::AndroidX.Compose.Runtime;
            using global::AndroidX.Compose.UI;
            using AndroidX.Compose;
            using Kotlin.Jvm.Functions;

            [assembly: ComposeDefaults("FooDefault",
                "!a", "overflow", "size")]

            namespace AndroidX.Compose
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
        Assert.Contains("public global::AndroidX.Compose.TextOverflow? Overflow { get; set; }", emitted);
        Assert.Contains("public global::AndroidX.Compose.Dp? Size { get; set; }", emitted);

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
            using global::AndroidX.Compose.Runtime;
            using global::AndroidX.Compose.UI;
            using AndroidX.Compose;
            using Kotlin.Jvm.Functions;

            [assembly: ComposeDefaults("BarDefault",
                "!a", "modifier", "fontSize", "fontWeight")]

            namespace AndroidX.Compose
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
            "if (FontSize is not null) __defaults &= ~(int)global::AndroidX.Compose.BarDefault.FontSize;",
            emitted);
        Assert.Contains(
            "if (FontWeight is not null) __defaults &= ~(int)global::AndroidX.Compose.BarDefault.FontWeight;",
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
            using global::AndroidX.Compose.Runtime;
            using global::AndroidX.Compose.UI;
            using AndroidX.Compose;
            using Kotlin.Jvm.Functions;

            [assembly: ComposeDefaults("PrimDefault",
                "!text", "modifier", "softWrap", "maxLines", "color")]

            namespace AndroidX.Compose
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
        Assert.Contains("global::AndroidX.Compose.ComposeBridges.F(_text, BuildModifier(), SoftWrap, MaxLines, Color, composer);", emitted);
        // Not surfaced as tree-facade ctor parameters; composable method exposes
        // each optional property directly.
        Assert.Contains("public F(string text)", emitted);
        Assert.Contains("bool? softWrap = null", emitted);
        Assert.Contains("int? maxLines = null", emitted);
        Assert.Contains("long? color = null", emitted);

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
        namespace AndroidX.Compose
        {
            public sealed class DrawerStateHolder
            {
                internal global::AndroidX.Compose.Material3.IDrawerState? Jvm;
                public global::AndroidX.Compose.Material3.DrawerValue InitialValue { get; }
                public DrawerStateHolder() { }
            }
            internal sealed class DrawerValueConfirmStateChange : Kotlin.Jvm.Functions.IFunction1
            {
                internal DrawerValueConfirmStateChange() { }
                internal System.Func<global::AndroidX.Compose.Material3.DrawerValue, bool>? Callback { get; set; }
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
            using global::AndroidX.Compose.Runtime;
            using global::AndroidX.Compose.UI;
            using AndroidX.Compose;
            using Kotlin.Jvm.Functions;
            using System;

            [assembly: ComposeDefaults("DrawerDefault",
                "!drawerContent", "modifier", "!drawerState", "gesturesEnabled", "scrimColor", "!content")]

            {{DrawerStateStubs}}

            namespace AndroidX.Compose
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
                        global::AndroidX.Compose.Material3.DrawerValue initialValue,
                        [ConfirmStateChange(typeof(global::AndroidX.Compose.Material3.DrawerValue))]
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
            "readonly global::AndroidX.Compose.DrawerValueConfirmStateChange _confirmStateChangeAdapter = new global::AndroidX.Compose.DrawerValueConfirmStateChange();",
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
            "global::AndroidX.Compose.ComposeBridges.RememberDrawerState(_drawerState!.InitialValue, _confirmStateChangeAdapter, composer)",
            emitted);
        Assert.Contains(
            "var __confirmStateChangeAdapter = __composer.Remember(static () => new global::AndroidX.Compose.DrawerValueConfirmStateChange());",
            emitted);
        Assert.Contains("__confirmStateChangeAdapter.Callback = confirmStateChange;", emitted);
        Assert.Contains(
            "global::AndroidX.Compose.ComposeBridges.RememberDrawerState(__drawerStateHolder.InitialValue, __confirmStateChangeAdapter, __composer)",
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
            using global::AndroidX.Compose.Runtime;
            using global::AndroidX.Compose.UI;
            using AndroidX.Compose;
            using Kotlin.Jvm.Functions;
            using System;

            [assembly: ComposeDefaults("DrawerDefault",
                "!drawerContent", "modifier", "!drawerState", "gesturesEnabled", "scrimColor", "!content")]

            {{DrawerStateStubs}}

            namespace AndroidX.Compose
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
                        global::AndroidX.Compose.Material3.DrawerValue initialValue,
                        [ConfirmStateChange(typeof(global::AndroidX.Compose.Material3.DrawerValue))]
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
        // No `AndroidX.Compose.<TName>ConfirmStateChange` class in scope and
        // no explicit AdapterType — generator must report CN3011 with
        // a message naming the missing convention class.
        var code = $$"""
            using global::AndroidX.Compose.Runtime;
            using global::AndroidX.Compose.UI;
            using AndroidX.Compose;
            using Kotlin.Jvm.Functions;
            using System;

            [assembly: ComposeDefaults("DrawerDefault",
                "!drawerContent", "modifier", "!drawerState", "gesturesEnabled", "scrimColor", "!content")]

            namespace AndroidX.Compose.Material3
            {
                public enum DrawerValue { Closed, Open }
                public interface IDrawerState { }
            }
            namespace AndroidX.Compose
            {
                public sealed class DrawerStateHolder
                {
                    internal global::AndroidX.Compose.Material3.IDrawerState? Jvm;
                    public global::AndroidX.Compose.Material3.DrawerValue InitialValue { get; }
                    public DrawerStateHolder() { }
                }
                // NOTE: no `DrawerValueConfirmStateChange` class declared.
            }

            namespace AndroidX.Compose
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
                        global::AndroidX.Compose.Material3.DrawerValue initialValue,
                        [ConfirmStateChange(typeof(global::AndroidX.Compose.Material3.DrawerValue))]
                        IFunction1? confirmStateChange,
                        IComposer composer) => default;
                }
            }
            """;

        var (_, diags, emitted) = Run(code, "Drawer");
        Assert.Null(emitted);
        Assert.Contains(diags, d => d.Id == "CN3011"
            && d.GetMessage().Contains("AndroidX.Compose.DrawerValueConfirmStateChange"));
    }

    [Fact]
    public void ConfirmStateChange_PlusParameterisedRemember_PlusSharedState_ComposesAllThree()
    {
        // ModalBottomSheet shape: Phase 4b parameterised Remember
        // (skipPartiallyExpanded) + Phase 4c SharedState (cached Jvm
        // reuse) + Phase 10 ConfirmStateChange veto adapter, with a
        // PropertyName override (ConfirmValueChange instead of the
        // default ConfirmStateChange). Verifies the three features
        // compose without conflict.
        var code = $$"""
            using global::AndroidX.Compose.Runtime;
            using global::AndroidX.Compose.UI;
            using AndroidX.Compose;
            using Kotlin.Jvm.Functions;
            using System;

            [assembly: ComposeDefaults("ModalBottomSheetDefault",
                "!onDismissRequest", "modifier", "!sheetState", "dragHandle", "shape", "!content")]

            namespace AndroidX.Compose.Material3
            {
                public enum SheetValue { Hidden, Expanded, PartiallyExpanded }
                public interface ISheetState { }
            }
            namespace AndroidX.Compose
            {
                public sealed class SheetStateHolder
                {
                    internal global::AndroidX.Compose.Material3.ISheetState? Jvm;
                    public bool SkipPartiallyExpanded { get; }
                    public SheetStateHolder(bool skipPartiallyExpanded = false) { }
                }
                internal sealed class SheetValueConfirmStateChange : Kotlin.Jvm.Functions.IFunction1
                {
                    internal SheetValueConfirmStateChange() { }
                    internal System.Func<global::AndroidX.Compose.Material3.SheetValue, bool>? Callback { get; set; }
                }
            }

            namespace AndroidX.Compose
            {
                public static partial class ComposeBridges
                {
                    [ComposeBridge(Class="androidx/compose/material3/ModalBottomSheet_androidKt",
                                   JvmName="ModalBottomSheet-dYc4hso",
                                   Signature="(Lkotlin/jvm/functions/Function0;Landroidx/compose/ui/Modifier;Landroidx/compose/material3/SheetState;Lkotlin/jvm/functions/Function2;Landroidx/compose/ui/graphics/Shape;Lkotlin/jvm/functions/Function3;Landroidx/compose/runtime/Composer;II)V",
                                   Defaults=typeof(ModalBottomSheetDefault))]
                    [ComposeFacade(Scope = "Column")]
                    public static partial void ModalBottomSheet(
                        IFunction0 onDismissRequest,
                        IModifier? modifier,
                        [StateHolder(Remember = nameof(RememberSheetState),
                                     StateType = typeof(SheetStateHolder),
                                     SharedState = true)]
                        IntPtr sheetState,
                        IFunction2? dragHandle,
                        global::AndroidX.Compose.Shape? shape,
                        IFunction3 content,
                        int defaults,
                        IComposer composer);

                    public static IntPtr RememberSheetState(
                        bool skipPartiallyExpanded,
                        [ConfirmStateChange(typeof(global::AndroidX.Compose.Material3.SheetValue),
                            AdapterType = typeof(SheetValueConfirmStateChange),
                            PropertyName = "ConfirmValueChange")]
                        IFunction1 confirmValueChange,
                        IComposer composer) => default;
                }
            }
            """;

        var (output, diags, emitted) = Run(code, "ModalBottomSheet");
        Assert.Empty(diags.Where(d => d.Severity == DiagnosticSeverity.Error));
        Assert.NotNull(emitted);

        // (a) Per-instance veto adapter field allocated once.
        Assert.Contains(
            "readonly global::AndroidX.Compose.SheetValueConfirmStateChange _confirmValueChangeAdapter = new global::AndroidX.Compose.SheetValueConfirmStateChange();",
            emitted);
        // (b) PropertyName override surfaces as ConfirmValueChange (not
        //     the default ConfirmStateChange).
        Assert.Contains(
            "public global::System.Func<global::AndroidX.Compose.Material3.SheetValue, bool>? ConfirmValueChange { get; set; }",
            emitted);
        Assert.DoesNotContain("public global::System.Func<global::AndroidX.Compose.Material3.SheetValue, bool>? ConfirmStateChange", emitted);
        // (c) Render preamble assigns the user delegate into the adapter.
        Assert.Contains("_confirmValueChangeAdapter.Callback = ConfirmValueChange;", emitted);
        // (d) SharedState cache-hit branch — Jvm already bound.
        Assert.Contains("if (_sheetState!.Jvm is not null)", emitted);
        // (e) Cache-miss branch calls Remember with SkipPartiallyExpanded
        //     resolved from the wrapper member AND the per-instance JCW
        //     adapter forwarded as the IFunction1 slot.
        Assert.Contains(
            "global::AndroidX.Compose.ComposeBridges.RememberSheetState(_sheetState!.SkipPartiallyExpanded, _confirmValueChangeAdapter, composer)",
            emitted);

        var errors = output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
        Assert.Empty(errors);
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
        namespace AndroidX.Compose
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
                    IComposer   composer);
            }
        }
        """;

    [Fact]
    public void Branching_EmitsIfElseWithPerBranchMasks()
    {
        var code = $$"""
            using global::AndroidX.Compose.Runtime;
            using global::AndroidX.Compose.UI;
            using AndroidX.Compose;
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
        Assert.Contains("public required global::AndroidX.Compose.ComposableNode Title { get; set; }", emitted);
        Assert.Contains("public global::AndroidX.Compose.ComposableNode? Subtitle { get; set; }", emitted);
        Assert.Contains("public global::AndroidX.Compose.ComposableNode? NavigationIcon { get; set; }", emitted);
        Assert.Contains("public global::AndroidX.Compose.ComposableNode? Actions { get; set; }", emitted);

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
        Assert.Contains("(int)global::AndroidX.Compose.BarSubtitleDefault.All;", emitted);
        Assert.Contains("(int)global::AndroidX.Compose.BarDefault.All;", emitted);
        Assert.Contains("if (subtitle is not null)", emitted);
        Assert.Contains("var __subtitleContent = subtitle ?? throw new global::System.InvalidOperationException", emitted);
        Assert.Contains(
            "var __subtitle = global::AndroidX.Compose.ComposableLambdas.Wrap2(composer, c => Subtitle!.Render(c));",
            emitted);
        Assert.Contains(
            "var __subtitle = global::AndroidX.Compose.ComposableLambdas.Wrap2(__composer, __subtitleContent);",
            emitted);
        Assert.DoesNotContain("var node = new global::AndroidX.Compose.Bar", emitted);

        // Alt branch calls the alternate bridge with the alt's actual
        // parameter order (title, subtitle, modifier, nav, actions).
        Assert.Contains("global::AndroidX.Compose.ComposeBridges.BarWithSubtitleExplicitDefaults(__title, __subtitle, __modifier, __navigationIcon, __actions, __defaults, composer);", emitted);

        // Primary branch calls the primary bridge (no subtitle).
        Assert.Contains("global::AndroidX.Compose.ComposeBridges.BarExplicitDefaults(__title, __modifier, __navigationIcon, __actions, __defaults, composer);", emitted);

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
            using global::AndroidX.Compose.Runtime;
            using global::AndroidX.Compose.UI;
            using AndroidX.Compose;
            using Kotlin.Jvm.Functions;

            {{AltAttrs}}

            namespace AndroidX.Compose
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
        Assert.Contains("global::AndroidX.Compose.ComposeBridges.BarFlex(__title, __modifier, __subtitle, __navigationIcon, __actions, __defaults, composer);", emitted);
        // The Subtitle bit IS in BarFlexDefault (not `!`-suppressed),
        // so the alt-branch mask clears it for the supplied slot.
        Assert.Contains("__defaults &= ~(int)global::AndroidX.Compose.BarFlexDefault.Subtitle;", emitted);

        var errors = output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
        Assert.Empty(errors);
    }

    [Fact]
    public void Branching_MissingAlternate_EmitsCN3010()
    {
        var code = """
            using global::AndroidX.Compose.Runtime;
            using global::AndroidX.Compose.UI;
            using AndroidX.Compose;
            using Kotlin.Jvm.Functions;

            [assembly: ComposeDefaults("BarDefault",
                "!title", "modifier", "navigationIcon", "actions")]

            namespace AndroidX.Compose
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
            using global::AndroidX.Compose.Runtime;
            using global::AndroidX.Compose.UI;
            using AndroidX.Compose;
            using Kotlin.Jvm.Functions;

            [assembly: ComposeDefaults("BarDefault",
                "!title", "modifier", "navigationIcon", "actions")]

            namespace AndroidX.Compose
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
            using global::AndroidX.Compose.Runtime;
            using global::AndroidX.Compose.UI;
            using AndroidX.Compose;
            using Kotlin.Jvm.Functions;

            [assembly: ComposeDefaults("BarDefault",
                "!title", "modifier", "navigationIcon", "actions")]
            [assembly: ComposeDefaults("BarSubtitleDefault",
                "!title", "!subtitle", "modifier", "navigationIcon", "actions")]

            namespace AndroidX.Compose
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
            using global::AndroidX.Compose.Runtime;
            using global::AndroidX.Compose.UI;
            using AndroidX.Compose;
            using Kotlin.Jvm.Functions;

            [assembly: ComposeDefaults("BarDefault",
                "!title", "modifier", "navigationIcon", "actions")]
            [assembly: ComposeDefaults("BarBadDefault",
                "!title", "!subtitle", "modifier", "navigationIcon")]

            namespace AndroidX.Compose
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
            using global::AndroidX.Compose.Runtime;
            using global::AndroidX.Compose.UI;
            using AndroidX.Compose;
            using Kotlin.Jvm.Functions;

            [assembly: ComposeDefaults("BarSubtitleDefault",
                "!title", "!subtitle", "modifier", "navigationIcon", "actions")]

            namespace AndroidX.Compose
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
        Assert.Contains(diags, d => d.Id == "CN3010" && d.GetMessage().Contains("defaults metadata"));
    }

    [Fact]
    public void Image_OptionalAlignmentContentScaleAlphaEmitProperties()
    {
        // Issue #145: ContentScale, Alignment, and Alpha should surface
        // as OptionalValue properties (nullable = "use Kotlin default"),
        // not ctor params, and the auto-mask should clear each bit only
        // when the corresponding property is supplied.
        var code = """
            using global::AndroidX.Compose.Runtime;
            using global::AndroidX.Compose.UI;
            using AndroidX.Compose;
            using Kotlin.Jvm.Functions;

            [assembly: ComposeDefaults("ImageDefault",
                "!painter", "contentDescription", "modifier",
                "alignment", "contentScale", "alpha", "colorFilter")]

            namespace AndroidX.Compose
            {
                public static partial class ComposeBridges
                {
                    [ComposeBridge(
                        Class="androidx/compose/foundation/ImageKt",
                        JvmName="Image",
                        Signature="(Landroidx/compose/ui/graphics/painter/Painter;Ljava/lang/String;Landroidx/compose/ui/Modifier;Landroidx/compose/ui/Alignment;Landroidx/compose/ui/layout/ContentScale;FLandroidx/compose/ui/graphics/ColorFilter;Landroidx/compose/runtime/Composer;II)V",
                        Defaults=typeof(ImageDefault))]
                    [ComposeFacade]
                    public static partial void Image(
                        [PainterResource] global::AndroidX.Compose.UI.Graphics.Painter.Painter painter,
                        string? contentDescription,
                        IModifier? modifier,
                        Alignment? alignment,
                        ContentScale? contentScale,
                        float? alpha,
                        int defaults,
                        IComposer composer);
                }
            }
            """;

        var (output, diags, emitted) = Run(code, "Image");
        Assert.Empty(diags.Where(d => d.Severity == DiagnosticSeverity.Error));
        Assert.NotNull(emitted);

        // The three new slots surface as nullable auto-properties, not ctor params.
        Assert.Contains("public global::AndroidX.Compose.Alignment? Alignment { get; set; }", emitted);
        Assert.Contains("public global::AndroidX.Compose.ContentScale? ContentScale { get; set; }", emitted);
        Assert.Contains("public float? Alpha { get; set; }", emitted);

        // Auto-mask clears each bit only when the property is non-null.
        Assert.Contains("if (Alignment is not null) __defaults &= ~(int)global::AndroidX.Compose.ImageDefault.Alignment;", emitted);
        Assert.Contains("if (ContentScale is not null) __defaults &= ~(int)global::AndroidX.Compose.ImageDefault.ContentScale;", emitted);
        Assert.Contains("if (Alpha is not null) __defaults &= ~(int)global::AndroidX.Compose.ImageDefault.Alpha;", emitted);

        // Bridge call forwards each property directly.
        Assert.Contains("global::AndroidX.Compose.ComposeBridges.Image(", emitted);
        Assert.Contains(", Alignment, ContentScale, Alpha, __defaults, composer);", emitted);

        var errors = output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
        Assert.Empty(errors);
    }

    // ---------------------------------------------------------------
    // Phase 11 — SecondaryCtor / SecondaryDefaults (CN3012). The
    // primary bridge is paired with a sibling on ComposeBridges whose
    // one unique reference-type parameter becomes an additional
    // ctor on the generated facade; Render branches on whether the
    // discriminator field is non-null.
    // ---------------------------------------------------------------

    const string SecondaryAttrs = """
        [assembly: ComposeDefaults("IconPainterDefault",
            "!painter", "contentDescription", "modifier", "tint")]
        [assembly: ComposeDefaults("IconDefault",
            "imageVector", "contentDescription", "modifier", "tint")]
        """;

    const string SecondaryBridges = """
        namespace AndroidX.Compose
        {
            public static partial class ComposeBridges
            {
                [ComposeBridge(Class="x/IconKt", JvmName="Icon-painter",
                               Signature="(Landroidx/compose/ui/graphics/painter/Painter;Ljava/lang/String;Landroidx/compose/ui/Modifier;JLandroidx/compose/runtime/Composer;II)V",
                               Defaults=typeof(IconPainterDefault))]
                [ComposeFacade(ClassName="Icon", SecondaryCtor=nameof(IconImageVector), SecondaryDefaults=typeof(IconDefault))]
                public static partial void IconPainter(
                    [PainterResource] global::AndroidX.Compose.UI.Graphics.Painter.Painter painter,
                    string?    contentDescription,
                    IModifier? modifier,
                    long       tint,
                    int        defaults,
                    IComposer  composer);

                [ComposeBridge(Class="x/IconKt", JvmName="Icon-vector",
                               Signature="(Landroidx/compose/ui/graphics/vector/ImageVector;Ljava/lang/String;Landroidx/compose/ui/Modifier;JLandroidx/compose/runtime/Composer;II)V",
                               Defaults=typeof(IconDefault))]
                public static partial void IconImageVector(
                    global::AndroidX.Compose.UI.Graphics.Vector.ImageVector imageVector,
                    string?    contentDescription,
                    IModifier? modifier,
                    long       tint,
                    IComposer  composer);
            }
        }
        """;

    [Fact]
    public void Secondary_EmitsExtraCtorAndDispatchBranch()
    {
        var code = $$"""
            using global::AndroidX.Compose.Runtime;
            using global::AndroidX.Compose.UI;
            using AndroidX.Compose;
            using Kotlin.Jvm.Functions;

            {{SecondaryAttrs}}

            {{SecondaryBridges}}
            """;

        var (output, diags, emitted) = Run(code, "Icon");
        Assert.Empty(diags.Where(d => d.Severity == DiagnosticSeverity.Error));
        Assert.NotNull(emitted);

        // ClassName override.
        Assert.Contains("public sealed partial class Icon : global::AndroidX.Compose.ComposableNode", emitted);

        // Discriminator field — nullable backing field for the
        // secondary-only parameter.
        Assert.Contains("readonly global::AndroidX.Compose.UI.Graphics.Vector.ImageVector? _imageVector;", emitted);

        // Both primary and secondary ctors exist. Primary has Painter
        // (via [PainterResource] → also exposes int drawableResourceId
        // ctor); secondary takes ImageVector.
        Assert.Contains("public Icon(global::AndroidX.Compose.UI.Graphics.Vector.ImageVector imageVector,", emitted);
        Assert.Contains("public Icon(int drawableResourceId,", emitted);
        Assert.Contains("public Icon(global::AndroidX.Compose.UI.Graphics.Painter.Painter painter,", emitted);

        // Render dispatch: secondary branch runs first, with renamed
        // locals (__secModifier / __secDefaults) to avoid CS0136
        // shadowing of the primary path's __modifier / __defaults.
        Assert.Contains("if (_imageVector is not null)", emitted);
        Assert.Contains("var __secModifier = BuildModifier();", emitted);
        Assert.Contains("int __secDefaults = (int)global::AndroidX.Compose.IconDefault.All;", emitted);

        // The discriminator's own enum bit is cleared.
        Assert.Contains("__secDefaults &= ~(int)global::AndroidX.Compose.IconDefault.ImageVector;", emitted);

        // The secondary call passes the field (with `!`) and the
        // shared slot expressions in the secondary's parameter order.
        Assert.Contains("global::AndroidX.Compose.ComposeBridges.IconImageVectorExplicitDefaults(_imageVector!,", emitted);
        Assert.Contains(", __secDefaults, composer);", emitted);
        Assert.Contains("internal static void Icon(global::AndroidX.Compose.Runtime.IComposer composer, global::AndroidX.Compose.UI.Graphics.Painter.Painter painter,", emitted);
        Assert.Contains("internal static void Icon(global::AndroidX.Compose.Runtime.IComposer composer, global::AndroidX.Compose.UI.Graphics.Vector.ImageVector imageVector,", emitted);
        Assert.Contains("global::AndroidX.Compose.ComposeBridges.IconImageVectorExplicitDefaults(imageVector, contentDescription, __modifier, tint, (int)__defaults, __composer);", emitted);
        Assert.DoesNotContain("var node = new global::AndroidX.Compose.Icon", emitted);

        // Early return so the primary body doesn't run.
        Assert.Contains("return;", emitted);

        var errors = output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
        Assert.Empty(errors);
    }

    [Fact]
    public void Secondary_LambdaRoutesUseStableSharedLowering()
    {
        var code = """
            using global::AndroidX.Compose.Runtime;
            using AndroidX.Compose;
            using Kotlin.Jvm.Functions;

            [assembly: ComposeDefaults("PrimaryDefault", "!painter", "!onClick", "label", "!content")]
            [assembly: ComposeDefaults("SecondaryDefault", "imageVector", "!onClick", "label", "!content")]

            namespace AndroidX.Compose
            {
                public static partial class ComposeBridges
                {
                    [ComposeBridge(
                        Class="x/IconKt",
                        JvmName="Icon-painter",
                        Signature="(Landroidx/compose/ui/graphics/painter/Painter;Lkotlin/jvm/functions/Function0;Lkotlin/jvm/functions/Function2;Lkotlin/jvm/functions/Function3;Landroidx/compose/runtime/Composer;II)V",
                        Defaults=typeof(PrimaryDefault))]
                    [ComposeFacade(
                        ClassName="StableIcon",
                        Scope="Row",
                        SecondaryCtor=nameof(IconImageVector),
                        SecondaryDefaults=typeof(SecondaryDefault))]
                    public static partial void IconPainter(
                        [PainterResource] global::AndroidX.Compose.UI.Graphics.Painter.Painter painter,
                        IFunction0 onClick,
                        IFunction2? label,
                        IFunction3 content,
                        int defaults,
                        IComposer composer);

                    [ComposeBridge(
                        Class="x/IconKt",
                        JvmName="Icon-vector",
                        Signature="(Landroidx/compose/ui/graphics/vector/ImageVector;Lkotlin/jvm/functions/Function0;Lkotlin/jvm/functions/Function2;Lkotlin/jvm/functions/Function3;Landroidx/compose/runtime/Composer;II)V",
                        Defaults=typeof(SecondaryDefault))]
                    public static partial void IconImageVector(
                        global::AndroidX.Compose.UI.Graphics.Vector.ImageVector imageVector,
                        IFunction0 onClick,
                        IFunction2? label,
                        IFunction3 content,
                        IComposer composer);
                }
            }
            """;

        var (output, diags, emitted) = Run(code, "StableIcon");

        Assert.Empty(diags.Where(d => d.Severity == DiagnosticSeverity.Error));
        Assert.NotNull(emitted);
        Assert.Contains(
            "var __secLabel = Label is null ? null : global::AndroidX.Compose.ComposableLambdas.Wrap2(composer, c => Label.Render(c));",
            emitted);
        Assert.Contains("var __secContent = global::AndroidX.Compose.ComposableLambdas.Wrap3(composer,", emitted);
        Assert.Contains("var __onClick = __composer.RememberAction(onClick);", emitted);
        Assert.Contains(
            "var __label = label is null ? null : global::AndroidX.Compose.ComposableLambdas.Wrap2(__composer, label);",
            emitted);
        Assert.Contains(
            "var __content = global::AndroidX.Compose.ComposableLambdas.Wrap3(__composer,",
            emitted);
        Assert.Empty(output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error));
    }

    [Fact]
    public void Branching_DirectPainterOverloadUsesPainterRouteInBothBranches()
    {
        var code = """
            using global::AndroidX.Compose.Runtime;
            using global::AndroidX.Compose.UI;
            using AndroidX.Compose;
            using Kotlin.Jvm.Functions;

            [assembly: ComposeDefaults("PrimaryDefault",
                "!painter", "!title", "modifier")]
            [assembly: ComposeDefaults("AlternateDefault",
                "!painter", "!title", "!subtitle", "modifier")]

            namespace AndroidX.Compose
            {
                public static partial class ComposeBridges
                {
                    [ComposeBridge(Class="x/Y", JvmName="Primary",
                                   Signature="(Landroidx/compose/ui/graphics/painter/Painter;Lkotlin/jvm/functions/Function2;Landroidx/compose/ui/Modifier;Landroidx/compose/runtime/Composer;II)V",
                                   Defaults=typeof(PrimaryDefault))]
                    [ComposeFacade(BranchOn="Subtitle", AlternateBridge=nameof(Alternate))]
                    public static partial void BranchPainter(
                        [PainterResource] global::AndroidX.Compose.UI.Graphics.Painter.Painter painter,
                        IFunction2 title,
                        IModifier? modifier,
                        int defaults,
                        IComposer composer,
                        int _changed = 0);

                    [ComposeBridge(Class="x/Y", JvmName="Alternate",
                                   Signature="(Landroidx/compose/ui/graphics/painter/Painter;Lkotlin/jvm/functions/Function2;Lkotlin/jvm/functions/Function2;Landroidx/compose/ui/Modifier;Landroidx/compose/runtime/Composer;II)V",
                                   Defaults=typeof(AlternateDefault))]
                    public static partial void Alternate(
                        global::AndroidX.Compose.UI.Graphics.Painter.Painter painter,
                        IFunction2 title,
                        IFunction2 subtitle,
                        IModifier? modifier,
                        int defaults,
                        IComposer composer,
                        int _changed = 0);
                }
            }
            """;

        var (output, diags, emitted) = Run(code, "BranchPainter");
        Assert.Empty(diags.Where(d => d.Severity == DiagnosticSeverity.Error));
        Assert.NotNull(emitted);
        Assert.Contains("internal static void BranchPainter(global::AndroidX.Compose.Runtime.IComposer composer, global::AndroidX.Compose.UI.Graphics.Painter.Painter painter,", emitted);
        Assert.Contains("__changed |= ((__directChanged >> 1) & 0b111) << 1;", emitted);
        Assert.Matches(
            @"if \(\(__omittedArguments & 0x4UL\) == 0\)\r?\n\s+__changed \|= __composer\.DiffSlot\(__modifierKey, 10\);",
            emitted);
        Assert.Matches(
            @"if \(\(__omittedArguments & 0x4UL\) == 0\)\r?\n\s+__changed \|= __composer\.DiffSlot\(__modifierKey, 7\);",
            emitted);

        var errors = output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
        Assert.Empty(errors);
    }

    [Fact]
    public void Secondary_DirectLoweringPreservesSharedPainterThemeScopeAndIndexedContent()
    {
        var code = """
            using global::AndroidX.Compose.Runtime;
            using global::AndroidX.Compose.UI;
            using global::AndroidX.Compose.UI.Graphics.Painter;
            using AndroidX.Compose;
            using Kotlin.Jvm.Functions;

            [assembly: ComposeDefaults("PrimaryDefault",
                "!painter", "!mode", "!content", "containerColor")]
            [assembly: ComposeDefaults("SecondaryDefault",
                "!imageVector", "!painter", "!content", "containerColor")]

            namespace AndroidX.Compose
            {
                public static partial class ComposeBridges
                {
                    [ComposeBridge(Class="x/Kt", JvmName="Primary",
                                   Signature="(Landroidx/compose/ui/graphics/painter/Painter;ILkotlin/jvm/functions/Function3;JLandroidx/compose/runtime/Composer;II)V",
                                   Defaults=typeof(PrimaryDefault))]
                    [ComposeFacade(
                        ClassName="Combined",
                        Scope="Row",
                        IndexedChildren=true,
                        DefaultColorFromTheme="secondaryContainer",
                        SecondaryCtor=nameof(Secondary),
                        SecondaryDefaults=typeof(SecondaryDefault))]
                    public static partial void Primary(
                        [PainterResource] Painter painter,
                        int mode,
                        IFunction3 content,
                        long containerColor,
                        int defaults,
                        IComposer composer);

                    public static void Secondary(
                        global::AndroidX.Compose.UI.Graphics.Vector.ImageVector imageVector,
                        Painter painter,
                        IFunction3 content,
                        long containerColor,
                        int defaults,
                        IComposer composer) { }
                }
            }
            """;

        var (output, diags, emitted) = Run(code, "Combined");

        Assert.Empty(diags.Where(d => d.Severity == DiagnosticSeverity.Error));
        Assert.NotNull(emitted);
        Assert.Contains("internal static void Combined(global::AndroidX.Compose.Runtime.IComposer composer, global::AndroidX.Compose.UI.Graphics.Vector.ImageVector imageVector, int drawableResourceId,", emitted);
        Assert.Contains("global::AndroidX.Compose.RenderContext.PushScope(__scope, global::AndroidX.Compose.ScopeKind.Row);", emitted);
        Assert.Contains("global::AndroidX.Compose.ComposableContentNode.RenderDirect(c, content, true);", emitted);
        Assert.Contains("long __color = containerColor.ToPacked() != 0L ? containerColor.ToPacked()", emitted);
        Assert.Contains("var __painterRef = global::AndroidX.Compose.ComposeBridges.PainterResource(drawableResourceId, __composer);", emitted);
        Assert.Contains("global::AndroidX.Compose.ComposeBridges.Secondary(imageVector, __painterPeer, __content, __color, (int)__defaults, __composer);", emitted);
        Assert.DoesNotContain("var node = new global::AndroidX.Compose.Combined", emitted);
        Assert.Empty(output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error));
    }

    [Fact]
    public void Secondary_MissingDefaultsParam_EmitsCN3012()
    {
        // Secondary bridge has no trailing `int defaults` — rejected.
        var code = $$"""
            using global::AndroidX.Compose.Runtime;
            using global::AndroidX.Compose.UI;
            using AndroidX.Compose;
            using Kotlin.Jvm.Functions;

            [assembly: ComposeDefaults("IconPainterDefault",
                "!painter", "contentDescription", "modifier", "tint")]
            [assembly: ComposeDefaults("IconDefault",
                "imageVector", "contentDescription", "modifier", "tint")]

            namespace AndroidX.Compose
            {
                public static partial class ComposeBridges
                {
                    [ComposeBridge(Class="x/IconKt", JvmName="Icon-painter",
                                   Signature="(Landroidx/compose/ui/graphics/painter/Painter;Ljava/lang/String;Landroidx/compose/ui/Modifier;JLandroidx/compose/runtime/Composer;II)V",
                                   Defaults=typeof(IconPainterDefault))]
                    [ComposeFacade(ClassName="Icon", SecondaryCtor=nameof(IconImageVector), SecondaryDefaults=typeof(IconDefault))]
                    public static partial void IconPainter(
                        [PainterResource] global::AndroidX.Compose.UI.Graphics.Painter.Painter painter,
                        string?    contentDescription,
                        IModifier? modifier,
                        long       tint,
                        int        defaults,
                        IComposer  composer);

                    public static void IconImageVector(
                        global::AndroidX.Compose.UI.Graphics.Vector.ImageVector imageVector,
                        string?    contentDescription,
                        IModifier? modifier,
                        long       tint,
                        IComposer  composer) { }
                }
            }
            """;
        var (_, diags, _) = Run(code, "Icon");
        Assert.Contains(diags, d => d.Id == "CN3012" && d.Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    public void Secondary_OnlyOneOfSecondaryCtorSecondaryDefaults_EmitsCN3012()
    {
        // SecondaryCtor without SecondaryDefaults — rejected.
        var code = $$"""
            using global::AndroidX.Compose.Runtime;
            using global::AndroidX.Compose.UI;
            using AndroidX.Compose;
            using Kotlin.Jvm.Functions;

            [assembly: ComposeDefaults("IconPainterDefault",
                "!painter", "contentDescription", "modifier", "tint")]

            namespace AndroidX.Compose
            {
                public static partial class ComposeBridges
                {
                    [ComposeBridge(Class="x/IconKt", JvmName="Icon-painter",
                                   Signature="(Landroidx/compose/ui/graphics/painter/Painter;Ljava/lang/String;Landroidx/compose/ui/Modifier;JLandroidx/compose/runtime/Composer;II)V",
                                   Defaults=typeof(IconPainterDefault))]
                    [ComposeFacade(ClassName="Icon", SecondaryCtor=nameof(IconImageVector))]
                    public static partial void IconPainter(
                        [PainterResource] global::AndroidX.Compose.UI.Graphics.Painter.Painter painter,
                        string?    contentDescription,
                        IModifier? modifier,
                        long       tint,
                        int        defaults,
                        IComposer  composer);

                    public static void IconImageVector(
                        global::AndroidX.Compose.UI.Graphics.Vector.ImageVector imageVector,
                        string?    contentDescription,
                        IModifier? modifier,
                        long       tint,
                        int        defaults,
                        IComposer  composer) { }
                }
            }
            """;
        var (_, diags, _) = Run(code, "Icon");
        Assert.Contains(diags, d => d.Id == "CN3012" && d.Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    public void Secondary_OnlySecondaryDefaultsWithoutSecondaryCtor_EmitsCN3012()
    {
        // SecondaryDefaults set without SecondaryCtor — rejected. Mirror
        // of Secondary_OnlyOneOfSecondaryCtorSecondaryDefaults_EmitsCN3012;
        // covers the inverse direction (a typo or refactor that drops the
        // SecondaryCtor name should fail loudly, not silently no-op).
        var code = $$"""
            using global::AndroidX.Compose.Runtime;
            using global::AndroidX.Compose.UI;
            using AndroidX.Compose;
            using Kotlin.Jvm.Functions;

            [assembly: ComposeDefaults("IconPainterDefault",
                "!painter", "contentDescription", "modifier", "tint")]
            [assembly: ComposeDefaults("IconDefault",
                "!imageVector", "contentDescription", "modifier", "tint")]

            namespace AndroidX.Compose
            {
                public static partial class ComposeBridges
                {
                    [ComposeBridge(Class="x/IconKt", JvmName="Icon-painter",
                                   Signature="(Landroidx/compose/ui/graphics/painter/Painter;Ljava/lang/String;Landroidx/compose/ui/Modifier;JLandroidx/compose/runtime/Composer;II)V",
                                   Defaults=typeof(IconPainterDefault))]
                    [ComposeFacade(ClassName="Icon", SecondaryDefaults=typeof(IconDefault))]
                    public static partial void IconPainter(
                        [PainterResource] global::AndroidX.Compose.UI.Graphics.Painter.Painter painter,
                        string?    contentDescription,
                        IModifier? modifier,
                        long?      tint,
                        int        defaults,
                        IComposer  composer);
                }
            }
            """;
        var (_, diags, _) = Run(code, "Icon");
        Assert.Contains(diags, d => d.Id == "CN3012" && d.Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    public void Changed_TrailingChangedParam_EmitsMaskAndNamedComposerArg()
    {
        // Phase X — the new $changed plumbing. When the bridge declares
        // an optional trailing `int _changed = 0`, the facade generator:
        //   * hoists __modifier (so it's available regardless of slot
        //     classification — the modifier slot itself is left at
        //     Uncertain in the mask, conservatively),
        //   * routes Action callbacks through composer.RememberAction
        //     so the JCW peer's JNI handle stays identity-stable
        //     across renders (its $changed bit reads Static),
        //   * computes a per-slot __changed mask, ORing
        //     ChangedBits.Static for content/onClick slots, and
        //     leaving 0 for the modifier slot,
        //   * passes `composer:` and `_changed:` as named args so the
        //     trailing optional doesn't reorder against composer.
        var code = $$"""
            using global::AndroidX.Compose.Runtime;
            using global::AndroidX.Compose.UI;
            using AndroidX.Compose;
            using Kotlin.Jvm.Functions;

            {{ButtonAttrs}}

            namespace AndroidX.Compose
            {
                public static partial class ComposeBridges
                {
                    [ComposeBridge(Class="androidx/compose/material3/ButtonKt", JvmName="Button",
                                   Signature="{{ButtonSig}}", Defaults=typeof(ButtonDefault))]
                    [ComposeFacade]
                    public static partial void Button(IFunction0 onClick, IModifier? modifier,
                                                      IFunction3 content, IComposer composer, int _changed = 0);
                }
            }
            """;

        var (output, diags, emitted) = Run(code, "Button");
        Assert.Empty(diags.Where(d => d.Severity == DiagnosticSeverity.Error));
        Assert.NotNull(emitted);

        // RememberAction stabilizes the onClick wrapper.
        Assert.Contains("var __onClick = composer.RememberAction(_onClick);", emitted);
        // __modifier is hoisted up front (so the rest of Render can
        // reference it), AND its structural key is captured first
        // (BuildModifier mutates side-channels).
        Assert.Contains("var __modifierKey = BuildModifierStructuralKey();", emitted);
        Assert.Contains("var __modifier = BuildModifier();", emitted);
        // __changed mask is computed.
        Assert.Contains("int __changed = 0;", emitted);
        // onClick contributes Static at bit 1 (param 0).
        Assert.Contains("__changed |= (int)global::AndroidX.Compose.ChangedBits.Static << global::AndroidX.Compose.ComposeExtensions.DiffSlotShift(0);", emitted);
        // modifier (param 1) contributes a real DiffSlot on the
        // captured key — bit 4.
        Assert.Contains("__changed |= composer.DiffSlot(__modifierKey, global::AndroidX.Compose.ComposeExtensions.DiffSlotShift(1));", emitted);
        // content is Kotlin param 9 even though omitted defaults leave it
        // third in the C# bridge declaration.
        Assert.Contains("__changed |= (int)global::AndroidX.Compose.ChangedBits.Static << global::AndroidX.Compose.ComposeExtensions.DiffSlotShift(9);", emitted);
        // Bridge call uses named composer + _changed args.
        Assert.Contains("composer: composer, _changed: __changed", emitted);

        var errors = output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
        Assert.Empty(errors);
    }

    [Fact]
    public void Changed_ReorderedOptionalPrimitive_UsesKotlinDefaultSlotPosition()
    {
        var code = """
            using global::AndroidX.Compose.Runtime;
            using global::AndroidX.Compose.UI;
            using AndroidX.Compose;
            using Kotlin.Jvm.Functions;

            [assembly: ComposeDefaults("WidgetDefault",
                "!value", "modifier", "enabled", "!content")]

            namespace AndroidX.Compose
            {
                public static partial class ComposeBridges
                {
                    [ComposeBridge(Class="my/pkg/WidgetKt", JvmName="Widget",
                                   Signature="(ILandroidx/compose/ui/Modifier;ZLkotlin/jvm/functions/Function2;Landroidx/compose/runtime/Composer;II)V",
                                   Defaults=typeof(WidgetDefault))]
                    [ComposeFacade]
                    public static partial void Widget(
                        int value, IModifier? modifier, IFunction2 content,
                        [FacadeDefault(true)] bool enabled, IComposer composer, int _changed = 0);
                }
            }
            """;

        var (output, diags, emitted) = Run(code, "Widget");
        Assert.Empty(diags.Where(d => d.Severity == DiagnosticSeverity.Error));
        Assert.NotNull(emitted);
        Assert.Contains("__changed |= composer.DiffSlot(_value, global::AndroidX.Compose.ComposeExtensions.DiffSlotShift(0));", emitted);
        Assert.Contains("__changed |= composer.DiffSlot(__modifierKey, global::AndroidX.Compose.ComposeExtensions.DiffSlotShift(1));", emitted);
        Assert.Contains("__changed |= composer.DiffSlot(_enabled, global::AndroidX.Compose.ComposeExtensions.DiffSlotShift(2));", emitted);
        Assert.Contains("__changed |= (int)global::AndroidX.Compose.ChangedBits.Static << global::AndroidX.Compose.ComposeExtensions.DiffSlotShift(3);", emitted);
        Assert.Contains("__changed |= __composer.DiffSlot(__modifierKey, 4);", emitted);
        Assert.Contains("int __changed = __omittedArguments == 0 ? __directChanged & 0b1 : 0;", emitted);
        Assert.Matches(
            @"if \(\(__omittedArguments & 0x8UL\) == 0\)\r?\n\s+__changed \|= __composer\.DiffSlot\(__modifierKey, 4\);",
            emitted);
        Assert.Matches(
            @"if \(\(__omittedArguments & 0x4UL\) == 0\)\r?\n\s+__changed \|= \(\(__directChanged >> 7\) & 0b111\) << 7;",
            emitted);
        Assert.Contains("__changed |= ((__directChanged >> 1) & 0b111) << 1;", emitted);
        Assert.Contains("__changed |= ((__directChanged >> 4) & 0b111) << 10;", emitted);

        var errors = output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
        Assert.Empty(errors);
    }

    [Fact]
    public void Changed_NoChangedParam_EmitsLegacyPositionalCall()
    {
        // Bridges without the trailing `_changed` param still emit the
        // legacy positional bridge call and no __changed local. Event
        // callbacks nevertheless retain stable JNI identity.
        var code = $$"""
            using global::AndroidX.Compose.Runtime;
            using global::AndroidX.Compose.UI;
            using AndroidX.Compose;
            using Kotlin.Jvm.Functions;

            {{ButtonAttrs}}

            namespace AndroidX.Compose
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
        Assert.DoesNotContain("__changed", emitted);
        Assert.Contains("var __onClick = composer.RememberAction(_onClick);", emitted);
        Assert.Contains("var __onClick = __composer.RememberAction(onClick);", emitted);
        Assert.Contains(
            "var __content = global::AndroidX.Compose.ComposableLambdas.Wrap3(__composer, c => global::AndroidX.Compose.ComposableContentNode.RenderDirect(c, content, false));",
            emitted);
        Assert.Contains("global::AndroidX.Compose.ComposeBridges.Button(__onClick, BuildModifier(), __content, composer);", emitted);

        var errors = output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
        Assert.Empty(errors);
    }

    // ─── Per-phase $changed mask pin tests ─────────────────────────────
    // One test per [ComposeFacade] phase that asserts the emitted
    // __changed plumbing matches the param-classification table in
    // .github/copilot-instructions.md. Without these, future generator
    // changes silently regress the new emission.

    [Fact]
    public void Changed_Phase2_CallbackContributesStaticAtParamBit()
    {
        // Phase 2 — IFunction1 + [Callback]. RememberAction stabilizes
        // the JCW; the mask reads Static.
        var code = """
            using global::AndroidX.Compose.Runtime;
            using global::AndroidX.Compose.UI;
            using AndroidX.Compose;
            using Kotlin.Jvm.Functions;

            [assembly: ComposeDefaults("TextFieldDefault",
                "!value", "!onValueChange", "modifier")]

            namespace AndroidX.Compose
            {
                public static partial class ComposeBridges
                {
                    [ComposeBridge(Class="androidx/compose/material3/TextFieldKt", JvmName="TextField",
                                   Signature="(Ljava/lang/String;Lkotlin/jvm/functions/Function1;Landroidx/compose/ui/Modifier;Landroidx/compose/runtime/Composer;II)V",
                                   Defaults=typeof(TextFieldDefault))]
                    [ComposeFacade]
                    public static partial void TextField(string value, [Callback(typeof(string))] IFunction1 onValueChange,
                        IModifier? modifier, IComposer composer, int _changed = 0);
                }
            }
            """;

        var (output, diags, emitted) = Run(code, "TextField");
        Assert.Empty(diags.Where(d => d.Severity == DiagnosticSeverity.Error));
        Assert.NotNull(emitted);

        // RememberAction wraps the Action<string> callback (param 1, bit 4).
        Assert.Contains("composer.RememberAction", emitted);
        Assert.Contains("int __changed = 0;", emitted);
        // Param 0 (value, primitive string) → DiffSlot at bit 1.
        Assert.Contains("__changed |= composer.DiffSlot(_value, global::AndroidX.Compose.ComposeExtensions.DiffSlotShift(0));", emitted);
        // Param 1 (callback) → Static at bit 4.
        Assert.Contains("__changed |= (int)global::AndroidX.Compose.ChangedBits.Static << global::AndroidX.Compose.ComposeExtensions.DiffSlotShift(1);", emitted);
        // Param 2 (modifier) → DiffSlot on __modifierKey at bit 7.
        Assert.Contains("__changed |= composer.DiffSlot(__modifierKey, global::AndroidX.Compose.ComposeExtensions.DiffSlotShift(2));", emitted);
        Assert.Contains("composer: composer, _changed: __changed", emitted);

        var errors = output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
        Assert.Empty(errors);
    }

    [Fact]
    public void Changed_Phase3_MultiSlotNamedSlotsContributeDiffSlot()
    {
        // Phase 3 — multi-slot leaf (AlertDialog). Required IFunction0
        // → RememberAction → Static. Required IFunction2 (confirmButton)
        // → multi-slot promotes it to a NamedFunction2; nullable
        // IFunction2? slots are NamedFunction2 too. All named slots
        // diff against their property identity.
        var code = """
            using global::AndroidX.Compose.Runtime;
            using global::AndroidX.Compose.UI;
            using AndroidX.Compose;
            using Kotlin.Jvm.Functions;

            [assembly: ComposeDefaults("AlertDialogDefault",
                "!onDismissRequest", "!confirmButton", "modifier", "dismissButton", "icon", "title", "text",
                "shape", "containerColor", "iconContentColor", "titleContentColor", "textContentColor", "tonalElevation", "properties")]

            namespace AndroidX.Compose
            {
                public static partial class ComposeBridges
                {
                    [ComposeBridge(Class="androidx/compose/material3/AndroidAlertDialog_androidKt", JvmName="AlertDialog-Oix01E0",
                                   Signature="(Lkotlin/jvm/functions/Function0;Lkotlin/jvm/functions/Function2;Landroidx/compose/ui/Modifier;Lkotlin/jvm/functions/Function2;Lkotlin/jvm/functions/Function2;Lkotlin/jvm/functions/Function2;Lkotlin/jvm/functions/Function2;Landroidx/compose/ui/graphics/Shape;JJJJFLandroidx/compose/ui/window/DialogProperties;Landroidx/compose/runtime/Composer;III)V",
                                   Defaults=typeof(AlertDialogDefault))]
                    [ComposeFacade]
                    public static partial void AlertDialog(IFunction0 onDismissRequest, IFunction2 confirmButton,
                        IModifier? modifier, IFunction2? dismissButton, IFunction2? icon, IFunction2? title, IFunction2? text,
                        int defaults, IComposer composer, int _changed = 0);
                }
            }
            """;

        var (output, diags, emitted) = Run(code, "AlertDialog");
        Assert.Empty(diags.Where(d => d.Severity == DiagnosticSeverity.Error));
        Assert.NotNull(emitted);

        Assert.Contains("int __changed = 0;", emitted);
        // onDismissRequest (param 0) → Static via RememberAction at bit 1.
        Assert.Contains("__changed |= (int)global::AndroidX.Compose.ChangedBits.Static << global::AndroidX.Compose.ComposeExtensions.DiffSlotShift(0);", emitted);
        // confirmButton (param 1, RequiredFunction2 — wrapped via Wrap2 → identity-stable) → Static at bit 4.
        Assert.Contains("__changed |= (int)global::AndroidX.Compose.ChangedBits.Static << global::AndroidX.Compose.ComposeExtensions.DiffSlotShift(1);", emitted);
        // modifier (param 2) → DiffSlot on __modifierKey at bit 7.
        Assert.Contains("__changed |= composer.DiffSlot(__modifierKey, global::AndroidX.Compose.ComposeExtensions.DiffSlotShift(2));", emitted);
        // dismissButton (param 3, NamedFunction2 nullable) → DiffSlot on the DismissButton property at bit 10.
        Assert.Contains("__changed |= composer.DiffSlot<object?>(DismissButton, global::AndroidX.Compose.ComposeExtensions.DiffSlotShift(3));", emitted);
        // icon (param 4) → bit 13.
        Assert.Contains("__changed |= composer.DiffSlot<object?>(Icon, global::AndroidX.Compose.ComposeExtensions.DiffSlotShift(4));", emitted);
        // title (param 5) → bit 16.
        Assert.Contains("__changed |= composer.DiffSlot<object?>(Title, global::AndroidX.Compose.ComposeExtensions.DiffSlotShift(5));", emitted);
        // text (param 6) → bit 19.
        Assert.Contains("__changed |= composer.DiffSlot<object?>(Text, global::AndroidX.Compose.ComposeExtensions.DiffSlotShift(6));", emitted);
        Assert.Contains("composer: composer, _changed: __changed", emitted);

        var errors = output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
        Assert.Empty(errors);
    }

    [Fact]
    public void Changed_Phase4_StateHolderDiffsAgainstResolvedJvmHandle()
    {
        // Phase 4 — [StateHolder]. The DiffSlot reads the resolved
        // __<param> local (the IntPtr value or wrapper), not the
        // user-supplied wrapper field.
        var code = $$"""
            using global::AndroidX.Compose.Runtime;
            using global::AndroidX.Compose.UI;
            using AndroidX.Compose;
            using Kotlin.Jvm.Functions;
            using System;

            [assembly: ComposeDefaults("DatePickerDefault",
                "!state", "modifier", "dateFormatter", "colors", "title", "headline", "showModeToggle")]

            {{DatePickerStateStubs}}

            namespace AndroidX.Compose
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
                        IComposer composer,
                        int _changed = 0);

                    public static IntPtr RememberDatePickerState(IComposer composer) => default;
                }
            }
            """;

        var (output, diags, emitted) = Run(code, "DatePicker");
        Assert.Empty(diags.Where(d => d.Severity == DiagnosticSeverity.Error));
        Assert.NotNull(emitted);

        Assert.Contains("int __changed = 0;", emitted);
        // state (param 0) → DiffSlot on __state (the resolved IntPtr/peer) at bit 1.
        Assert.Contains("__changed |= composer.DiffSlot(__state, global::AndroidX.Compose.ComposeExtensions.DiffSlotShift(0));", emitted);
        // modifier (param 1) → bit 4.
        Assert.Contains("__changed |= composer.DiffSlot(__modifierKey, global::AndroidX.Compose.ComposeExtensions.DiffSlotShift(1));", emitted);
        Assert.Contains("__changed |= __composer.DiffSlot(__state, 1);", emitted);
        Assert.Contains("__changed |= __composer.DiffSlot(__modifierKey, 4);", emitted);
        Assert.Contains("composer: composer, _changed: __changed", emitted);

        var errors = output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
        Assert.Empty(errors);
    }

    [Fact]
    public void Changed_Phase7_PainterResourceDiffsAgainstDrawableId()
    {
        // Phase 7 — [PainterResource]. The DiffSlot reads
        // _drawableResourceId (the cheap value-type id), not the
        // resolved Painter peer (which gets a fresh JNI handle each
        // render).
        var code = """
            using System;
            using global::AndroidX.Compose.Runtime;
            using global::AndroidX.Compose.UI;
            using global::AndroidX.Compose.UI.Graphics.Painter;
            using AndroidX.Compose;
            using Kotlin.Jvm.Functions;

            [assembly: ComposeDefaults("ImageDefault", "!painter", "contentDescription", "modifier")]

            namespace AndroidX.Compose
            {
                public static partial class ComposeBridges
                {
                    [ComposeBridge(Class="androidx/compose/foundation/ImageKt", JvmName="Image",
                                   Signature="(Landroidx/compose/ui/graphics/painter/Painter;Ljava/lang/String;Landroidx/compose/ui/Modifier;Landroidx/compose/runtime/Composer;II)V",
                                   Defaults=typeof(ImageDefault))]
                    [ComposeFacade]
                    public static partial void Image([PainterResource] Painter painter,
                        string? contentDescription, IModifier? modifier,
                        int defaults, IComposer composer, int _changed = 0);
                }
            }
            """;

        var (output, diags, emitted) = Run(code, "Image");
        Assert.Empty(diags.Where(d => d.Severity == DiagnosticSeverity.Error));
        Assert.NotNull(emitted);

        Assert.Contains("int __changed = 0;", emitted);
        // painter (param 0) → DiffSlot on _drawableResourceId at bit 1.
        Assert.Contains("__changed |= composer.DiffSlot(_drawableResourceId, global::AndroidX.Compose.ComposeExtensions.DiffSlotShift(0));", emitted);
        // contentDescription (param 1, primitive string) → bit 4.
        Assert.Contains("__changed |= composer.DiffSlot(_contentDescription, global::AndroidX.Compose.ComposeExtensions.DiffSlotShift(1));", emitted);
        // modifier (param 2) → bit 7.
        Assert.Contains("__changed |= composer.DiffSlot(__modifierKey, global::AndroidX.Compose.ComposeExtensions.DiffSlotShift(2));", emitted);
        Assert.Contains("composer: composer, _changed: __changed", emitted);

        var errors = output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
        Assert.Empty(errors);
    }

    [Fact]
    public void Changed_Phase8_WrapperPassthroughEmitsMaskWhenChangedDeclared()
    {
        // Phase 8 — wrapper-passthrough Box. Hand-written body delegates
        // to a bound BoxKt.Box. When the wrapper's signature opts in
        // with `int _changed = 0`, the facade emits the mask and threads
        // it through.
        var code = """
            using global::AndroidX.Compose.Runtime;
            using global::AndroidX.Compose.UI;
            using AndroidX.Compose;
            using Kotlin.Jvm.Functions;

            [assembly: ComposeDefaults("BoxDefault", "modifier", "contentAlignment", "propagateMinConstraints", "!content")]

            namespace AndroidX.Compose
            {
                public static partial class ComposeBridges
                {
                    [ComposeFacade(Defaults = typeof(BoxDefault))]
                    public static partial void Box(IModifier? modifier, IFunction3 content, int defaults, IComposer composer, int _changed = 0);

                    public static partial void Box(IModifier? modifier, IFunction3 content, int defaults, IComposer composer, int _changed = 0)
                    {
                        // body irrelevant to the facade generator
                    }
                }
            }
            """;

        var (output, diags, emitted) = Run(code, "Box");
        Assert.Empty(diags.Where(d => d.Severity == DiagnosticSeverity.Error));
        Assert.NotNull(emitted);

        Assert.Contains("int __changed = 0;", emitted);
        // modifier (param 0) → __modifierKey at bit 1.
        Assert.Contains("__changed |= composer.DiffSlot(__modifierKey, global::AndroidX.Compose.ComposeExtensions.DiffSlotShift(0));", emitted);
        // content is Kotlin slot 3, even though C# moves it before optional primitives.
        Assert.Contains("__changed |= (int)global::AndroidX.Compose.ChangedBits.Static << global::AndroidX.Compose.ComposeExtensions.DiffSlotShift(3);", emitted);
        // Bridge call uses named composer + _changed args.
        Assert.Contains("composer: composer, _changed: __changed", emitted);

        var errors = output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
        Assert.Empty(errors);
    }

    [Fact]
    public void Changed_Phase9_BranchingEmitsPerBranchMask()
    {
        // Phase 9 — branching. Both bridges declare the trailing
        // `_changed` opt-in. The facade emits __changed masks INSIDE
        // each branch (because the per-param bit positions differ
        // between primary and alternate when the alt has an extra
        // slot inserted in a different position) and threads the
        // mask through both bridge calls.
        const string Attrs = """
            [assembly: ComposeDefaults("BarDefault",
                "!title", "modifier", "navigationIcon", "actions")]
            [assembly: ComposeDefaults("BarSubtitleDefault",
                "!title", "!subtitle", "modifier", "navigationIcon", "actions")]
            """;

        var code = $$"""
            using global::AndroidX.Compose.Runtime;
            using global::AndroidX.Compose.UI;
            using AndroidX.Compose;
            using Kotlin.Jvm.Functions;

            {{Attrs}}

            namespace AndroidX.Compose
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
                        IComposer   composer,
                        int         _changed = 0);

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
                        IComposer   composer,
                        int         _changed = 0);
                }
            }
            """;

        var (output, diags, emitted) = Run(code, "Bar");
        Assert.Empty(diags.Where(d => d.Severity == DiagnosticSeverity.Error));
        Assert.NotNull(emitted);

        // __changed mask declared and threaded through both branches.
        Assert.Contains("int __changed = 0;", emitted);
        Assert.Contains("composer: composer, _changed: __changed", emitted);
        // Both bridge calls use the named-arg form.
        Assert.Contains("global::AndroidX.Compose.ComposeBridges.BarWithSubtitle(", emitted);
        Assert.Contains("global::AndroidX.Compose.ComposeBridges.Bar(", emitted);

        var errors = output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
        Assert.Empty(errors);
    }

    [Fact]
    public void Changed_Phase11_SecondaryCtorThreadsMaskThroughBothBridges()
    {
        // Phase 11 — secondary ctor (Icon). Primary bridge has
        // [PainterResource]; secondary takes ImageVector. When both
        // declare `int _changed = 0` the facade emits __changed in
        // both dispatch paths.
        const string Attrs = """
            [assembly: ComposeDefaults("IconPainterDefault",
                "!painter", "contentDescription", "modifier", "tint")]
            [assembly: ComposeDefaults("IconDefault",
                "imageVector", "contentDescription", "modifier", "tint")]
            """;

        var code = $$"""
            using global::AndroidX.Compose.Runtime;
            using global::AndroidX.Compose.UI;
            using AndroidX.Compose;
            using Kotlin.Jvm.Functions;

            {{Attrs}}

            namespace AndroidX.Compose
            {
                public static partial class ComposeBridges
                {
                    [ComposeBridge(Class="x/IconKt", JvmName="Icon-painter",
                                   Signature="(Landroidx/compose/ui/graphics/painter/Painter;Ljava/lang/String;Landroidx/compose/ui/Modifier;JLandroidx/compose/runtime/Composer;II)V",
                                   Defaults=typeof(IconPainterDefault))]
                    [ComposeFacade(ClassName="Icon", SecondaryCtor=nameof(IconImageVector), SecondaryDefaults=typeof(IconDefault))]
                    public static partial void IconPainter(
                        [PainterResource] global::AndroidX.Compose.UI.Graphics.Painter.Painter painter,
                        string?    contentDescription,
                        IModifier? modifier,
                        long       tint,
                        int        defaults,
                        IComposer  composer,
                        int        _changed = 0);

                    public static void IconImageVector(
                        global::AndroidX.Compose.UI.Graphics.Vector.ImageVector imageVector,
                        string?    contentDescription,
                        IModifier? modifier,
                        long       tint,
                        int        defaults,
                        IComposer  composer,
                        int        _changed = 0) { }
                }
            }
            """;

        var (output, diags, emitted) = Run(code, "Icon");
        Assert.Empty(diags.Where(d => d.Severity == DiagnosticSeverity.Error));
        Assert.NotNull(emitted);

        // The facade threads a __changed mask through both dispatch
        // paths (primary + secondary).
        Assert.Contains("int __changed = 0;", emitted);
        Assert.Contains("composer: composer, _changed: __changed", emitted);
        // Both bridge calls present.
        Assert.Contains("global::AndroidX.Compose.ComposeBridges.IconPainter(", emitted);
        Assert.Contains("global::AndroidX.Compose.ComposeBridges.IconImageVector(", emitted);

        var errors = output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
        Assert.Empty(errors);
    }

    [Theory]
    [InlineData("", "IFunction1", "CN3013")]
    [InlineData("", "IFunction4", "CN3013")]
    [InlineData("[DeferredComposableContent] ", "IFunction4", "CN3002")]
    [InlineData("[RawCallback] ", "IFunction1", "CN3002")]
    [InlineData("[Callback(typeof(string)), RawCallback] ", "IFunction1", "CN3013")]
    public void LambdaExecutionMode_RejectsAmbiguousOrFacadeUnsupportedShapes(
        string attribute,
        string functionType,
        string expectedDiagnostic)
    {
        string signatureType = functionType == "IFunction1"
            ? "Function1"
            : "Function4";
        var code = $$"""
            using global::AndroidX.Compose.Runtime;
            using AndroidX.Compose;
            using Kotlin.Jvm.Functions;

            namespace AndroidX.Compose
            {
                public static partial class ComposeBridges
                {
                    [ComposeBridge(
                        Class="x/WidgetKt",
                        JvmName="Widget",
                        Signature="(Lkotlin/jvm/functions/{{signatureType}};Landroidx/compose/runtime/Composer;I)V")]
                    [ComposeFacade]
                    public static partial void Widget(
                        {{attribute}}{{functionType}} callback,
                        IComposer composer);
                }
            }
            """;

        var (_, diags, emitted) = Run(code, "Widget");

        Assert.Contains(diags, d => d.Id == expectedDiagnostic);
        Assert.Null(emitted);
    }
}