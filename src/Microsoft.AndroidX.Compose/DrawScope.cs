using AndroidX.Compose.UI.Graphics.Drawscope;
using AndroidX.Compose.UI.Unit;
using BoundBrush = AndroidX.Compose.UI.Graphics.Brush;
using BoundCanvas = AndroidX.Compose.UI.Graphics.ICanvas;
using BoundColorFilter = AndroidX.Compose.UI.Graphics.ColorFilter;
using BoundDrawContext = AndroidX.Compose.UI.Graphics.Drawscope.IDrawContext;
using BoundDrawStyle = AndroidX.Compose.UI.Graphics.Drawscope.DrawStyle;
using BoundImageBitmap = AndroidX.Compose.UI.Graphics.IImageBitmap;
using BoundOutline = AndroidX.Compose.UI.Graphics.Outline;
using BoundShadow = AndroidX.Compose.UI.Graphics.Shadow;
using BoundTextLayoutResult = AndroidX.Compose.UI.Text.TextLayoutResult;
using PaintFactory = AndroidX.Compose.UI.Graphics.AndroidPaint_androidKt;
using NativeCanvasApi = AndroidX.Compose.UI.Graphics.AndroidCanvas_androidKt;

namespace AndroidX.Compose;

/// <summary>
/// Managed view of the Compose <c>DrawScope</c> supplied to Canvas and drawing
/// modifier callbacks.
/// </summary>
public class DrawScope
{
    const int SrcOverBlendMode = 3;

    readonly IDrawScope _jvm;

    internal DrawScope(IDrawScope jvm) => _jvm = jvm;

    /// <summary>Current drawing bounds in pixels.</summary>
    public Size Size => Size.FromPacked(_jvm.Size);

    /// <summary>Center of the current drawing bounds.</summary>
    public Offset Center => Offset.FromPacked(_jvm.Center);

    /// <summary>Layout direction inherited from the composition.</summary>
    public LayoutDirection LayoutDirection => _jvm.LayoutDirection;

    /// <summary>
    /// Android canvas backing the current Compose draw pass. Prefer the
    /// Compose-native methods on this type unless a platform API is required.
    /// </summary>
    public Android.Graphics.Canvas NativeCanvas
    {
        get
        {
            var canvas = _jvm.DrawContext?.Canvas
                ?? throw new InvalidOperationException("DrawScope.DrawContext.Canvas was not available.");
            return NativeCanvasApi.GetNativeCanvas(canvas)
                ?? throw new InvalidOperationException("Compose native Canvas conversion returned null.");
        }
    }

    /// <summary>
    /// Runs drawing commands with a rectangular clip, then restores the
    /// previous clip even when <paramref name="draw"/> throws.
    /// </summary>
    public void ClipRect(
        Rect bounds,
        Action<DrawScope> draw,
        ClipOperation operation = ClipOperation.Intersect)
    {
        ArgumentNullException.ThrowIfNull(draw);
        WithSavedState(context =>
        {
            context.Transform.ClipRect(
                bounds.Left, bounds.Top, bounds.Right, bounds.Bottom, (int)operation);
            draw(this);
        });
    }

    /// <summary>
    /// Runs drawing commands with the current logical bounds as a rectangular
    /// clip, then restores the previous clip.
    /// </summary>
    public void ClipRect(
        Action<DrawScope> draw,
        ClipOperation operation = ClipOperation.Intersect) =>
        ClipRect(new Rect(0f, 0f, Size.Width, Size.Height), draw, operation);

    /// <summary>
    /// Runs drawing commands with a path clip, then restores the previous clip
    /// even when <paramref name="draw"/> throws.
    /// </summary>
    public void ClipPath(
        Path path,
        Action<DrawScope> draw,
        ClipOperation operation = ClipOperation.Intersect)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(draw);
        WithSavedState(context =>
        {
            context.Transform.ClipPath(path.Jvm, (int)operation);
            draw(this);
        });
    }

    /// <summary>
    /// Runs drawing commands in a translated coordinate space, then restores
    /// the previous transform.
    /// </summary>
    public void Translate(Offset offset, Action<DrawScope> draw)
    {
        ArgumentNullException.ThrowIfNull(draw);
        WithSavedState(context =>
        {
            context.Transform.Translate(offset.X, offset.Y);
            draw(this);
        });
    }

    /// <summary>
    /// Runs drawing commands in a clockwise-rotated coordinate space, then
    /// restores the previous transform.
    /// </summary>
    public void Rotate(float degrees, Action<DrawScope> draw, Offset? pivot = null)
    {
        ArgumentNullException.ThrowIfNull(draw);
        WithSavedState(context =>
        {
            context.Transform.Rotate(degrees, (pivot ?? Center).Packed);
            draw(this);
        });
    }

    /// <summary>
    /// Runs drawing commands in a uniformly scaled coordinate space, then
    /// restores the previous transform.
    /// </summary>
    public void Scale(float scale, Action<DrawScope> draw, Offset? pivot = null) =>
        Scale(scale, scale, draw, pivot);

    /// <summary>
    /// Runs drawing commands in an independently scaled coordinate space,
    /// then restores the previous transform.
    /// </summary>
    public void Scale(
        float scaleX,
        float scaleY,
        Action<DrawScope> draw,
        Offset? pivot = null)
    {
        ArgumentNullException.ThrowIfNull(draw);
        WithSavedState(context =>
        {
            context.Transform.Scale(scaleX, scaleY, (pivot ?? Center).Packed);
            draw(this);
        });
    }

    /// <summary>
    /// Insets every edge of the logical drawing bounds while running
    /// <paramref name="draw"/>, then restores the previous bounds.
    /// </summary>
    public void Inset(float inset, Action<DrawScope> draw) =>
        Inset(inset, inset, inset, inset, draw);

    /// <summary>
    /// Insets horizontal and vertical edges of the logical drawing bounds
    /// while running <paramref name="draw"/>.
    /// </summary>
    public void Inset(float horizontal, float vertical, Action<DrawScope> draw) =>
        Inset(horizontal, vertical, horizontal, vertical, draw);

    /// <summary>
    /// Insets the logical drawing bounds and translates the coordinate origin
    /// while running <paramref name="draw"/>, then restores both.
    /// </summary>
    public void Inset(
        float left,
        float top,
        float right,
        float bottom,
        Action<DrawScope> draw)
    {
        ArgumentNullException.ThrowIfNull(draw);
        WithSavedState(context =>
        {
            context.Transform.Inset(left, top, right, bottom);
            draw(this);
        });
    }

    /// <summary>
    /// Batches clipping and transform operations under one state save, runs
    /// drawing commands, and restores the previous canvas state and bounds.
    /// </summary>
    public void WithTransform(
        Action<DrawTransform> transform,
        Action<DrawScope> draw)
    {
        ArgumentNullException.ThrowIfNull(transform);
        ArgumentNullException.ThrowIfNull(draw);
        WithSavedState(context =>
        {
            var scope = new DrawTransform(context.Transform);
            try
            {
                transform(scope);
            }
            finally
            {
                scope.Invalidate();
            }
            draw(this);
        });
    }

    /// <summary>
    /// Provides the Compose canvas backing this draw pass for low-level
    /// operations. The borrowed canvas must not be retained or disposed.
    /// </summary>
    public void DrawIntoCanvas(Action<BoundCanvas> draw)
    {
        ArgumentNullException.ThrowIfNull(draw);
        draw(DrawContext().Canvas);
    }

    /// <summary>Draws a filled or stroked rectangle with a solid color.</summary>
    public void DrawRect(
        Color color,
        Offset? topLeft = null,
        Size? size = null,
        float alpha = 1f,
        BoundDrawStyle? style = null)
    {
        var origin = topLeft ?? Offset.Zero;
        var extent = size ?? RemainingSize(origin);
        _jvm.DrawRect(color.ToPacked(), origin.Packed, extent.Packed, alpha,
            style ?? DrawingStyle.Fill, null, SrcOverBlendMode);
    }

    /// <summary>Draws a filled or stroked rectangle with a brush.</summary>
    public void DrawRect(
        BoundBrush brush,
        Offset? topLeft = null,
        Size? size = null,
        float alpha = 1f,
        BoundDrawStyle? style = null)
    {
        ArgumentNullException.ThrowIfNull(brush);
        var origin = topLeft ?? Offset.Zero;
        var extent = size ?? RemainingSize(origin);
        _jvm.DrawRect(brush, origin.Packed, extent.Packed, alpha,
            style ?? DrawingStyle.Fill, null, SrcOverBlendMode);
    }

    /// <summary>Draws a filled or stroked circle with a solid color.</summary>
    public void DrawCircle(
        Color color,
        float? radius = null,
        Offset? center = null,
        float alpha = 1f,
        BoundDrawStyle? style = null)
    {
        var extent = Size;
        _jvm.DrawCircle(color.ToPacked(),
            radius ?? MathF.Min(extent.Width, extent.Height) / 2f,
            (center ?? Center).Packed,
            alpha, style ?? DrawingStyle.Fill, null, SrcOverBlendMode);
    }

    /// <summary>Draws a filled or stroked circle with a brush.</summary>
    public void DrawCircle(
        BoundBrush brush,
        float? radius = null,
        Offset? center = null,
        float alpha = 1f,
        BoundDrawStyle? style = null)
    {
        ArgumentNullException.ThrowIfNull(brush);
        var extent = Size;
        _jvm.DrawCircle(brush,
            radius ?? MathF.Min(extent.Width, extent.Height) / 2f,
            (center ?? Center).Packed,
            alpha, style ?? DrawingStyle.Fill, null, SrcOverBlendMode);
    }

    /// <summary>Draws a line with a solid color.</summary>
    public void DrawLine(
        Color color,
        Offset start,
        Offset end,
        float strokeWidth = 0f,
        StrokeCap cap = StrokeCap.Butt,
        float alpha = 1f) =>
        _jvm.DrawLine(color.ToPacked(), start.Packed, end.Packed, strokeWidth, (int)cap,
            null, alpha, null, SrcOverBlendMode);

    /// <summary>Draws a path-effect-stroked line with a solid color.</summary>
    public void DrawLine(
        Color color,
        Offset start,
        Offset end,
        PathEffect pathEffect,
        float strokeWidth = 0f,
        StrokeCap cap = StrokeCap.Butt,
        float alpha = 1f)
    {
        ArgumentNullException.ThrowIfNull(pathEffect);
        _jvm.DrawLine(color.ToPacked(), start.Packed, end.Packed, strokeWidth, (int)cap,
            pathEffect.Jvm, alpha, null, SrcOverBlendMode);
    }

    /// <summary>Draws a line with a brush.</summary>
    public void DrawLine(
        BoundBrush brush,
        Offset start,
        Offset end,
        float strokeWidth = 0f,
        StrokeCap cap = StrokeCap.Butt,
        float alpha = 1f)
    {
        ArgumentNullException.ThrowIfNull(brush);
        _jvm.DrawLine(brush, start.Packed, end.Packed, strokeWidth, (int)cap,
            null, alpha, null, SrcOverBlendMode);
    }

    /// <summary>Draws a path-effect-stroked line with a brush.</summary>
    public void DrawLine(
        BoundBrush brush,
        Offset start,
        Offset end,
        PathEffect pathEffect,
        float strokeWidth = 0f,
        StrokeCap cap = StrokeCap.Butt,
        float alpha = 1f)
    {
        ArgumentNullException.ThrowIfNull(brush);
        ArgumentNullException.ThrowIfNull(pathEffect);
        _jvm.DrawLine(brush, start.Packed, end.Packed, strokeWidth, (int)cap,
            pathEffect.Jvm, alpha, null, SrcOverBlendMode);
    }

    /// <summary>Draws points or connected line segments with a solid color.</summary>
    public void DrawPoints(
        IReadOnlyList<Offset> points,
        PointMode mode,
        Color color,
        float strokeWidth = 0f,
        StrokeCap cap = StrokeCap.Butt,
        PathEffect? pathEffect = null,
        float alpha = 1f,
        BoundColorFilter? colorFilter = null)
    {
        ArgumentNullException.ThrowIfNull(points);
        using var paint = DrawingPaint.CreatePointPaint(
            strokeWidth, cap, pathEffect, colorFilter, alpha);
        paint.Color = color.ToPacked();
        DrawContext().Canvas.DrawRawPoints((int)mode, DrawingPaint.Flatten(points), paint);
    }

    /// <summary>Draws points or connected line segments with a brush.</summary>
    public void DrawPoints(
        IReadOnlyList<Offset> points,
        PointMode mode,
        BoundBrush brush,
        float strokeWidth = 0f,
        StrokeCap cap = StrokeCap.Butt,
        PathEffect? pathEffect = null,
        float alpha = 1f,
        BoundColorFilter? colorFilter = null)
    {
        ArgumentNullException.ThrowIfNull(points);
        ArgumentNullException.ThrowIfNull(brush);
        using var paint = DrawingPaint.CreatePointPaint(
            strokeWidth, cap, pathEffect, colorFilter, alpha);
        brush.ApplyTo(Size.Packed, paint, alpha);
        paint.ColorFilter = colorFilter;
        DrawContext().Canvas.DrawRawPoints((int)mode, DrawingPaint.Flatten(points), paint);
    }

    /// <summary>Draws a filled or stroked oval with a solid color.</summary>
    public void DrawOval(
        Color color,
        Offset? topLeft = null,
        Size? size = null,
        float alpha = 1f,
        BoundDrawStyle? style = null)
    {
        var origin = topLeft ?? Offset.Zero;
        _jvm.DrawOval(color.ToPacked(), origin.Packed, (size ?? RemainingSize(origin)).Packed,
            alpha, style ?? DrawingStyle.Fill, null, SrcOverBlendMode);
    }

    /// <summary>Draws a filled or stroked oval with a brush.</summary>
    public void DrawOval(
        BoundBrush brush,
        Offset? topLeft = null,
        Size? size = null,
        float alpha = 1f,
        BoundDrawStyle? style = null)
    {
        ArgumentNullException.ThrowIfNull(brush);
        var origin = topLeft ?? Offset.Zero;
        _jvm.DrawOval(brush, origin.Packed, (size ?? RemainingSize(origin)).Packed,
            alpha, style ?? DrawingStyle.Fill, null, SrcOverBlendMode);
    }

    /// <summary>Draws a filled or stroked arc with a solid color.</summary>
    public void DrawArc(
        Color color,
        float startAngle,
        float sweepAngle,
        bool useCenter,
        Offset? topLeft = null,
        Size? size = null,
        float alpha = 1f,
        BoundDrawStyle? style = null)
    {
        var origin = topLeft ?? Offset.Zero;
        _jvm.DrawArc(color.ToPacked(), startAngle, sweepAngle, useCenter, origin.Packed,
            (size ?? RemainingSize(origin)).Packed, alpha,
            style ?? DrawingStyle.Fill, null, SrcOverBlendMode);
    }

    /// <summary>Draws a filled or stroked arc with a brush.</summary>
    public void DrawArc(
        BoundBrush brush,
        float startAngle,
        float sweepAngle,
        bool useCenter,
        Offset? topLeft = null,
        Size? size = null,
        float alpha = 1f,
        BoundDrawStyle? style = null)
    {
        ArgumentNullException.ThrowIfNull(brush);
        var origin = topLeft ?? Offset.Zero;
        _jvm.DrawArc(brush, startAngle, sweepAngle, useCenter, origin.Packed,
            (size ?? RemainingSize(origin)).Packed, alpha,
            style ?? DrawingStyle.Fill, null, SrcOverBlendMode);
    }

    /// <summary>Draws a filled or stroked rounded rectangle with a solid color.</summary>
    public void DrawRoundRect(
        Color color,
        CornerRadius cornerRadius,
        Offset? topLeft = null,
        Size? size = null,
        float alpha = 1f,
        BoundDrawStyle? style = null)
    {
        var origin = topLeft ?? Offset.Zero;
        _jvm.DrawRoundRect(color.ToPacked(), origin.Packed,
            (size ?? RemainingSize(origin)).Packed, cornerRadius.Packed,
            style ?? DrawingStyle.Fill, alpha, null, SrcOverBlendMode);
    }

    /// <summary>Draws a filled or stroked rounded rectangle with a brush.</summary>
    public void DrawRoundRect(
        BoundBrush brush,
        CornerRadius cornerRadius,
        Offset? topLeft = null,
        Size? size = null,
        float alpha = 1f,
        BoundDrawStyle? style = null)
    {
        ArgumentNullException.ThrowIfNull(brush);
        var origin = topLeft ?? Offset.Zero;
        _jvm.DrawRoundRect(brush, origin.Packed,
            (size ?? RemainingSize(origin)).Packed, cornerRadius.Packed,
            alpha, style ?? DrawingStyle.Fill, null, SrcOverBlendMode);
    }

    /// <summary>Draws a path with a solid color.</summary>
    public void DrawPath(
        Path path,
        Color color,
        float alpha = 1f,
        BoundDrawStyle? style = null)
    {
        ArgumentNullException.ThrowIfNull(path);
        _jvm.DrawPath(path.Jvm, color.ToPacked(), alpha,
            style ?? DrawingStyle.Fill, null, SrcOverBlendMode);
    }

    /// <summary>Draws a path with a brush.</summary>
    public void DrawPath(
        Path path,
        BoundBrush brush,
        float alpha = 1f,
        BoundDrawStyle? style = null)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(brush);
        _jvm.DrawPath(path.Jvm, brush, alpha,
            style ?? DrawingStyle.Fill, null, SrcOverBlendMode);
    }

    /// <summary>Draws an image bitmap at a pixel offset.</summary>
    public void DrawImage(
        ImageBitmap image,
        Offset? topLeft = null,
        float alpha = 1f,
        BoundDrawStyle? style = null,
        BoundColorFilter? colorFilter = null)
    {
        ArgumentNullException.ThrowIfNull(image);
        DrawImage(image.Jvm, topLeft, alpha, style, colorFilter);
    }

    /// <summary>Draws a bound Compose image bitmap at a pixel offset.</summary>
    public void DrawImage(
        BoundImageBitmap image,
        Offset? topLeft = null,
        float alpha = 1f,
        BoundDrawStyle? style = null,
        BoundColorFilter? colorFilter = null)
    {
        ArgumentNullException.ThrowIfNull(image);
        _jvm.DrawImage(
            image,
            (topLeft ?? Offset.Zero).Packed,
            alpha,
            style ?? DrawingStyle.Fill,
            colorFilter,
            SrcOverBlendMode);
    }

    /// <summary>
    /// Draws a source rectangle from an image into an integer destination
    /// rectangle.
    /// </summary>
    public void DrawImage(
        ImageBitmap image,
        IntOffset sourceOffset,
        IntSize sourceSize,
        IntOffset destinationOffset,
        IntSize destinationSize,
        float alpha = 1f,
        BoundDrawStyle? style = null,
        BoundColorFilter? colorFilter = null,
        FilterQuality filterQuality = FilterQuality.Low)
    {
        ArgumentNullException.ThrowIfNull(image);
        DrawImage(
            image.Jvm, sourceOffset, sourceSize, destinationOffset, destinationSize,
            alpha, style, colorFilter, filterQuality);
    }

    /// <summary>
    /// Draws a source rectangle from a bound Compose image into an integer
    /// destination rectangle.
    /// </summary>
    public void DrawImage(
        BoundImageBitmap image,
        IntOffset sourceOffset,
        IntSize sourceSize,
        IntOffset destinationOffset,
        IntSize destinationSize,
        float alpha = 1f,
        BoundDrawStyle? style = null,
        BoundColorFilter? colorFilter = null,
        FilterQuality filterQuality = FilterQuality.Low)
    {
        ArgumentNullException.ThrowIfNull(image);
        DrawingPaint.ValidateImageRegion(sourceSize, destinationSize);
        using var paint = PaintFactory.Paint()
            ?? throw new InvalidOperationException("Compose Paint factory returned null.");
        paint.Alpha = alpha;
        paint.ColorFilter = colorFilter;
        paint.BlendMode = SrcOverBlendMode;
        paint.FilterQuality = (int)filterQuality;
        DrawingPaint.ApplyStyle(paint, style ?? DrawingStyle.Fill);
        DrawContext().Canvas.DrawImageRect(
            image,
            sourceOffset.Packed,
            sourceSize.Packed,
            destinationOffset.Packed,
            destinationSize.Packed,
            paint);
    }

    /// <summary>Draws a Compose outline with a solid color.</summary>
    public void DrawOutline(
        BoundOutline outline,
        Color color,
        float alpha = 1f,
        BoundDrawStyle? style = null,
        BoundColorFilter? colorFilter = null)
    {
        ArgumentNullException.ThrowIfNull(outline);
        AndroidX.Compose.UI.Graphics.OutlineKt.DrawOutline(
            _jvm, outline, color.ToPacked(), alpha,
            style ?? DrawingStyle.Fill, colorFilter, SrcOverBlendMode);
    }

    /// <summary>Draws a Compose outline with a brush.</summary>
    public void DrawOutline(
        BoundOutline outline,
        BoundBrush brush,
        float alpha = 1f,
        BoundDrawStyle? style = null,
        BoundColorFilter? colorFilter = null)
    {
        ArgumentNullException.ThrowIfNull(outline);
        ArgumentNullException.ThrowIfNull(brush);
        AndroidX.Compose.UI.Graphics.OutlineKt.DrawOutline(
            _jvm, outline, brush, alpha,
            style ?? DrawingStyle.Fill, colorFilter, SrcOverBlendMode);
    }

    /// <summary>Draws a generic path outline with a solid color.</summary>
    public void DrawOutline(
        Path path,
        Color color,
        float alpha = 1f,
        BoundDrawStyle? style = null,
        BoundColorFilter? colorFilter = null)
    {
        ArgumentNullException.ThrowIfNull(path);
        using var outline = new BoundOutline.Generic(path.Jvm);
        DrawOutline(outline, color, alpha, style, colorFilter);
    }

    /// <summary>Draws a generic path outline with a brush.</summary>
    public void DrawOutline(
        Path path,
        BoundBrush brush,
        float alpha = 1f,
        BoundDrawStyle? style = null,
        BoundColorFilter? colorFilter = null)
    {
        ArgumentNullException.ThrowIfNull(path);
        using var outline = new BoundOutline.Generic(path.Jvm);
        DrawOutline(outline, brush, alpha, style, colorFilter);
    }

    /// <summary>Draws an existing measured text layout with a solid color.</summary>
    public void DrawText(
        BoundTextLayoutResult textLayout,
        Color color,
        Offset? topLeft = null,
        float alpha = float.NaN,
        BoundShadow? shadow = null,
        TextDecoration? decoration = null,
        BoundDrawStyle? style = null)
    {
        ArgumentNullException.ThrowIfNull(textLayout);
        ComposeBridges.DrawTextColor(
            _jvm,
            textLayout,
            color.ToPacked(),
            (topLeft ?? Offset.Zero).Packed,
            alpha,
            shadow,
            decoration,
            style,
            SrcOverBlendMode);
    }

    /// <summary>Draws an existing measured text layout with a brush.</summary>
    public void DrawText(
        BoundTextLayoutResult textLayout,
        BoundBrush brush,
        Offset? topLeft = null,
        float alpha = float.NaN,
        BoundShadow? shadow = null,
        TextDecoration? decoration = null,
        BoundDrawStyle? style = null)
    {
        ArgumentNullException.ThrowIfNull(textLayout);
        ArgumentNullException.ThrowIfNull(brush);
        ComposeBridges.DrawTextBrush(
            _jvm,
            textLayout,
            brush,
            (topLeft ?? Offset.Zero).Packed,
            alpha,
            shadow,
            decoration,
            style,
            SrcOverBlendMode);
    }

    Size RemainingSize(Offset topLeft)
    {
        var extent = Size;
        return new Size(
            MathF.Max(0f, extent.Width - topLeft.X),
            MathF.Max(0f, extent.Height - topLeft.Y));
    }

    BoundDrawContext DrawContext() => _jvm.DrawContext
        ?? throw new InvalidOperationException("DrawScope.DrawContext was not available.");

    void WithSavedState(Action<BoundDrawContext> body)
    {
        var context = DrawContext();
        var canvas = context.Canvas;
        var previousSize = context.Size;
        canvas.Save();
        try
        {
            body(context);
        }
        finally
        {
            try
            {
                context.Size = previousSize;
            }
            finally
            {
                canvas.Restore();
            }
        }
    }

}
