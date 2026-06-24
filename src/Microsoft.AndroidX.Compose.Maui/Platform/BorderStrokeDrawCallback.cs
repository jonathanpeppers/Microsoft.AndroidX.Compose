using Android.Graphics;
using Android.Runtime;
using Kotlin.Jvm.Functions;
using AndroidColor    = Android.Graphics.Color;
using AndroidPaint    = Android.Graphics.Paint;
using AndroidPath     = Android.Graphics.Path;
using AndroidRectF    = Android.Graphics.RectF;
using ComposeBridges  = AndroidX.Compose.ComposeBridges;
using DrawScopeApi    = AndroidX.Compose.UI.Graphics.Drawscope.IDrawScope;
using NativeCanvasApi = AndroidX.Compose.UI.Graphics.AndroidCanvas_androidKt;
using LineCap         = Microsoft.Maui.Graphics.LineCap;
using LineJoin        = Microsoft.Maui.Graphics.LineJoin;
using MauiBorderShape = Microsoft.Maui.Controls.Shapes;

namespace Microsoft.AndroidX.Compose.Maui.Platform;

/// <summary>
/// <c>Function1&lt;DrawScope, Unit&gt;</c> adapter that paints a MAUI
/// <see cref="Microsoft.Maui.Controls.Border"/> stroke directly via
/// <see cref="AndroidPaint"/> + <see cref="Android.Graphics.Canvas"/>.
/// Used by <see cref="Handlers.BorderHandler"/> when any of the
/// dashed-stroke / line-cap / line-join / miter-limit knobs diverge
/// from the trivial solid-border defaults — Compose's
/// <c>Modifier.border</c> only paints a solid stroke and so can't
/// model those properties.
/// </summary>
/// <remarks>
/// <para>Allocated <strong>once per <see cref="Handlers.BorderHandler"/>
/// instance</strong> (held as a <c>readonly</c> field) so the JNI peer
/// stays stable across recompositions. The handler mutates the public
/// fields below before calling <c>composer.recompose()</c>; the
/// <see cref="Invoke"/> body reads the latest values on each draw pass.
/// Re-allocating the JCW per recomposition would churn the JNI ref
/// table without any payoff.</para>
///
/// <para>The drawing path bypasses Compose's <c>DrawScope</c> drawing
/// primitives — every one of those takes an inline-class param
/// (<c>Color</c>, <c>Offset</c>, <c>Size</c>, <c>CornerRadius</c>,
/// <c>BlendMode</c>) and is consequently stripped from the binding.
/// Instead we walk <c>DrawScope.DrawContext.Canvas</c> down to the
/// native <see cref="Canvas"/> via the (now-bound) <c>IDrawScope</c>
/// interface + <c>AndroidCanvas_androidKt.GetNativeCanvas</c>
/// extension, and draw with directly-bound <see cref="AndroidPaint"/>
/// + <see cref="DashPathEffect"/> APIs.</para>
///
/// <para>Public so it can be referenced from a <c>readonly</c> field
/// on the handler. Not part of the developer-facing API.</para>
/// </remarks>
[Register("net/compose/maui/BorderStrokeDrawCallback")]
public sealed class BorderStrokeDrawCallback : Java.Lang.Object, IFunction1
{
    /// <summary>
    /// Reused across draw passes — properties are reconfigured
    /// in-place on each <see cref="Invoke"/> call.
    /// </summary>
    readonly AndroidPaint _paint = new(PaintFlags.AntiAlias)
    {
        Dither = true,
    };

    /// <summary>Stroke colour as packed <c>0xAARRGGBB</c>; <c>0</c> = no stroke.</summary>
    public int StrokeArgb { get; set; }

    /// <summary>Stroke thickness in DIPs (matches MAUI's <c>StrokeThickness</c>).</summary>
    public float StrokeThicknessDip { get; set; }

    /// <summary>
    /// Dash on/off lengths in <em>stroke-thickness units</em> (matches
    /// MAUI semantics). Multiplied by stroke thickness in pixels at
    /// draw time. <see langword="null"/> or fewer than two entries =
    /// solid stroke.
    /// </summary>
    public float[]? StrokeDashPattern { get; set; }

    /// <summary>Dash phase offset in stroke-thickness units.</summary>
    public float StrokeDashOffset { get; set; }

    /// <summary>Cap style (Butt / Round / Square).</summary>
    public LineCap StrokeLineCap { get; set; } = LineCap.Butt;

    /// <summary>Join style (Miter / Round / Bevel).</summary>
    public LineJoin StrokeLineJoin { get; set; } = LineJoin.Miter;

    /// <summary>Miter limit (Skia/MAUI default is <c>10</c>).</summary>
    public float StrokeMiterLimit { get; set; } = 10f;

    /// <summary>
    /// Shape to stroke. <see langword="null"/>, <see cref="Rectangle"/>,
    /// <see cref="RoundRectangle"/>, and <see cref="Ellipse"/> are
    /// honoured; everything else falls back to a plain rectangle.
    /// </summary>
    public Microsoft.Maui.Graphics.IShape? Shape { get; set; }

    /// <summary>Display density in pixels-per-DIP, captured by the handler.</summary>
    public float Density { get; set; } = 1f;

    /// <summary>
    /// Kotlin <c>Function1.invoke</c> entry point. <paramref name="p0"/>
    /// is the Compose <c>DrawScope</c> the runtime hands back inside
    /// <c>Modifier.drawBehind</c>.
    /// </summary>
    public Java.Lang.Object? Invoke(Java.Lang.Object? p0)
    {
        var unit = Kotlin.Unit.Instance
            ?? throw new InvalidOperationException("Kotlin.Unit.Instance not available.");

        if (p0 is null || StrokeArgb == 0 || StrokeThicknessDip <= 0f || Density <= 0f)
            return unit;

        // `Xamarin.AndroidX.Compose.UI.Graphics 1.11.2.2` finally binds
        // `IDrawScope` / `IDrawContext` and the `AndroidCanvas_androidKt`
        // extension, so the DrawScope → native Canvas walk runs through
        // managed APIs instead of hand-written JNI.
        var drawScope = p0.JavaCast<DrawScopeApi>();
        if (drawScope is null)
            return unit;

        var composeCanvas = drawScope.DrawContext?.Canvas;
        if (composeCanvas is null)
            return unit;

        var nativeCanvas = NativeCanvasApi.GetNativeCanvas(composeCanvas);
        if (nativeCanvas is null)
            return unit;

        long packedSize = drawScope.Size;
        float widthPx  = ComposeBridges.UnpackSizeWidth(packedSize);
        float heightPx = ComposeBridges.UnpackSizeHeight(packedSize);
        if (widthPx <= 0f || heightPx <= 0f)
            return unit;

        float strokePx = StrokeThicknessDip * Density;
        ConfigurePaint(strokePx);

        // Match Modifier.border semantics — the stroke straddles the
        // bounds, so inset by half the stroke width so the visible
        // stroke draws inside the geometry.
        float inset = strokePx / 2f;
        float left   = inset;
        float top    = inset;
        float right  = widthPx  - inset;
        float bottom = heightPx - inset;
        if (right <= left || bottom <= top)
            return unit;

        switch (Shape)
        {
            case MauiBorderShape.RoundRectangle rr:
                DrawRoundRectangle(nativeCanvas, rr, left, top, right, bottom);
                break;
            case MauiBorderShape.Ellipse:
                DrawOvalInternal(nativeCanvas, left, top, right, bottom);
                break;
            case MauiBorderShape.Rectangle:
            case null:
            default:
                DrawRectInternal(nativeCanvas, left, top, right, bottom);
                break;
        }

        return unit;
    }

    void ConfigurePaint(float strokePx)
    {
        _paint.SetStyle(AndroidPaint.Style.Stroke);
        _paint.Color           = new AndroidColor(StrokeArgb);
        _paint.StrokeWidth     = strokePx;
        _paint.StrokeCap       = MapLineCap(StrokeLineCap);
        _paint.StrokeJoin      = MapLineJoin(StrokeLineJoin);
        _paint.StrokeMiter     = StrokeMiterLimit;
        _paint.SetPathEffect(BuildDashEffect(strokePx));
    }

    DashPathEffect? BuildDashEffect(float strokePx)
    {
        var pattern = StrokeDashPattern;
        if (pattern is null || pattern.Length < 2)
            return null;

        // MAUI's pattern values are multiples of stroke thickness.
        // Convert to pixels for Android. DashPathEffect requires an
        // even number of intervals (alternating on/off pairs); double
        // the array when odd so the user-visible pattern still cycles
        // the way MAUI documents it.
        bool needsDouble = (pattern.Length & 1) == 1;
        int outLength    = needsDouble ? pattern.Length * 2 : pattern.Length;
        var intervalsPx  = new float[outLength];
        for (int i = 0; i < pattern.Length; i++)
            intervalsPx[i] = Math.Max(0f, pattern[i] * strokePx);
        if (needsDouble)
            Array.Copy(intervalsPx, 0, intervalsPx, pattern.Length, pattern.Length);

        return new DashPathEffect(intervalsPx, StrokeDashOffset * strokePx);
    }

    void DrawRoundRectangle(Canvas canvas, MauiBorderShape.RoundRectangle rr, float left, float top, float right, float bottom)
    {
        var corners = rr.CornerRadius;
        float tlX = (float)corners.TopLeft     * Density;
        float trX = (float)corners.TopRight    * Density;
        float brX = (float)corners.BottomRight * Density;
        float blX = (float)corners.BottomLeft  * Density;

        // Fast path — uniform corners use Canvas.drawRoundRect, which
        // is cheaper than a full Path round-trip.
        if (tlX == trX && trX == brX && brX == blX)
        {
            using var rect = new AndroidRectF(left, top, right, bottom);
            canvas.DrawRoundRect(rect, tlX, tlX, _paint);
            return;
        }

        // Per-corner radii — fall back to a Path. Each corner takes
        // (rx, ry); we use rx==ry since MAUI's CornerRadius is a single
        // scalar per corner.
        var radii = new float[]
        {
            tlX, tlX, // top-left
            trX, trX, // top-right
            brX, brX, // bottom-right
            blX, blX, // bottom-left
        };
        using var path        = new AndroidPath();
        using var pathRect    = new AndroidRectF(left, top, right, bottom);
        var direction = AndroidPath.Direction.Cw
            ?? throw new InvalidOperationException("Path.Direction.Cw not available.");
        path.AddRoundRect(pathRect, radii, direction);
        canvas.DrawPath(path, _paint);
    }

    void DrawRectInternal(Canvas canvas, float left, float top, float right, float bottom)
    {
        using var rect = new AndroidRectF(left, top, right, bottom);
        canvas.DrawRect(rect, _paint);
    }

    void DrawOvalInternal(Canvas canvas, float left, float top, float right, float bottom)
    {
        using var rect = new AndroidRectF(left, top, right, bottom);
        canvas.DrawOval(rect, _paint);
    }

    static AndroidPaint.Cap MapLineCap(LineCap cap) => cap switch
    {
        LineCap.Round  => AndroidPaint.Cap.Round  ?? throw new InvalidOperationException("Paint.Cap.Round not available."),
        LineCap.Square => AndroidPaint.Cap.Square ?? throw new InvalidOperationException("Paint.Cap.Square not available."),
        _              => AndroidPaint.Cap.Butt   ?? throw new InvalidOperationException("Paint.Cap.Butt not available."),
    };

    static AndroidPaint.Join MapLineJoin(LineJoin join) => join switch
    {
        LineJoin.Round => AndroidPaint.Join.Round ?? throw new InvalidOperationException("Paint.Join.Round not available."),
        LineJoin.Bevel => AndroidPaint.Join.Bevel ?? throw new InvalidOperationException("Paint.Join.Bevel not available."),
        _              => AndroidPaint.Join.Miter ?? throw new InvalidOperationException("Paint.Join.Miter not available."),
    };
}
