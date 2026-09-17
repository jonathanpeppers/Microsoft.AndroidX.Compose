using Kotlin.Jvm.Functions;
using AndroidX.Compose.UI.Graphics;
using AndroidX.Compose.UI.Graphics.Drawscope;
using AndroidX.Compose.UI.Text;
using BoundBrush = AndroidX.Compose.UI.Graphics.Brush;

namespace AndroidX.Compose;

internal static partial class ComposeBridges
{
    [ComposeBridge(
        Class = "androidx/compose/ui/text/TextPainterKt",
        JvmName = "drawText-d8-rzKo",
        Signature = "(Landroidx/compose/ui/graphics/drawscope/DrawScope;" +
                    "Landroidx/compose/ui/text/TextLayoutResult;JJF" +
                    "Landroidx/compose/ui/graphics/Shadow;" +
                    "Landroidx/compose/ui/text/style/TextDecoration;" +
                    "Landroidx/compose/ui/graphics/drawscope/DrawStyle;I)V")]
    internal static partial void DrawTextColor(
        IDrawScope scope,
        TextLayoutResult textLayout,
        long color,
        long topLeft,
        float alpha,
        Shadow? shadow,
        TextDecoration? decoration,
        DrawStyle? style,
        int blendMode);

    [ComposeBridge(
        Class = "androidx/compose/ui/text/TextPainterKt",
        JvmName = "drawText-LVfH_YU",
        Signature = "(Landroidx/compose/ui/graphics/drawscope/DrawScope;" +
                    "Landroidx/compose/ui/text/TextLayoutResult;" +
                    "Landroidx/compose/ui/graphics/Brush;JF" +
                    "Landroidx/compose/ui/graphics/Shadow;" +
                    "Landroidx/compose/ui/text/style/TextDecoration;" +
                    "Landroidx/compose/ui/graphics/drawscope/DrawStyle;I)V")]
    internal static partial void DrawTextBrush(
        IDrawScope scope,
        TextLayoutResult textLayout,
        BoundBrush brush,
        long topLeft,
        float alpha,
        Shadow? shadow,
        TextDecoration? decoration,
        DrawStyle? style,
        int blendMode);

    [ComposeBridge(
        Instance = true,
        Class = "androidx/compose/ui/draw/CacheDrawScope",
        JvmName = "getSize-NH-jbRc",
        Signature = "()J")]
    internal static partial long CacheDrawScopeSize(IntPtr scope);

    [ComposeBridge(
        Instance = true,
        Class = "androidx/compose/ui/draw/CacheDrawScope",
        JvmName = "onDrawBehind",
        Signature = "(Lkotlin/jvm/functions/Function1;)Landroidx/compose/ui/draw/DrawResult;")]
    internal static partial IntPtr CacheDrawScopeOnDrawBehind(
        IntPtr scope,
        IFunction1 draw);

    [ComposeBridge(
        Instance = true,
        Class = "androidx/compose/ui/draw/CacheDrawScope",
        JvmName = "onDrawWithContent",
        Signature = "(Lkotlin/jvm/functions/Function1;)Landroidx/compose/ui/draw/DrawResult;")]
    internal static partial IntPtr CacheDrawScopeOnDrawWithContent(
        IntPtr scope,
        IFunction1 draw);
}
