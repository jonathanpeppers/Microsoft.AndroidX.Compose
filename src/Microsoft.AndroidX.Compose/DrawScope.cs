using AndroidX.Compose.UI.Graphics.Drawscope;
using AndroidX.Compose.UI.Unit;
using BoundBrush = AndroidX.Compose.UI.Graphics.Brush;
using BoundDrawStyle = AndroidX.Compose.UI.Graphics.Drawscope.DrawStyle;
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

    Size RemainingSize(Offset topLeft)
    {
        var extent = Size;
        return new Size(
            MathF.Max(0f, extent.Width - topLeft.X),
            MathF.Max(0f, extent.Height - topLeft.Y));
    }
}
