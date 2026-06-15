using AndroidX.Compose;
using AndroidX.Compose.Runtime;
using Microsoft.AndroidX.Compose.Maui.Platform;
using Microsoft.Maui.Handlers;
using ComposeColor       = AndroidX.Compose.Color;
using MauiBorder         = Microsoft.Maui.Controls.Border;
using MauiBorderShape    = Microsoft.Maui.Controls.Shapes;
using MauiLineCap        = Microsoft.Maui.Graphics.LineCap;
using MauiLineJoin       = Microsoft.Maui.Graphics.LineJoin;

namespace Microsoft.AndroidX.Compose.Maui.Handlers;

/// <summary>
/// MAUI <see cref="Microsoft.Maui.Controls.Border"/> handler that
/// renders as a Compose <see cref="Box"/> with a single
/// <c>Modifier.Border(...).Background(...).Clip(...)</c> chain.
/// Folds into the page's single composition via
/// <see cref="ComposeElementHandler{TVirtualView}"/> /
/// <see cref="IComposeHandler"/>.
/// </summary>
/// <remarks>
/// <para><c>StrokeShape</c> mapping:</para>
/// <list type="bullet">
///   <item><description><c>RoundRectangle</c> →
///     <see cref="RoundedCornerShape"/> using the X-radius of the
///     four corners (Compose's <c>RoundedCornerShape</c> takes per-corner
///     <see cref="Dp"/>s; we pull <c>CornerRadius.TopLeft</c> etc.).</description></item>
///   <item><description><c>Ellipse</c> →
///     <c>RoundedCornerShape(50)</c> (50 % rounded — fully circular for
///     square layouts; ellipse for rectangular ones).</description></item>
///   <item><description><c>Rectangle</c> / <c>null</c> → no clip.</description></item>
/// </list>
///
/// <para><c>Stroke</c> only honours <c>SolidPaint</c>; gradient/image
/// brushes silently drop the stroke (mirrors stock MAUI's Android
/// border drawable, which has the same constraint).</para>
///
/// <para><strong>Dashed-stroke geometry</strong>
/// (<see cref="MauiBorder.StrokeDashPattern"/>,
/// <see cref="MauiBorder.StrokeDashOffset"/>,
/// <see cref="MauiBorder.StrokeLineCap"/>,
/// <see cref="MauiBorder.StrokeLineJoin"/>,
/// <see cref="MauiBorder.StrokeMiterLimit"/>) can't be expressed via
/// Compose's <c>Modifier.border</c> (solid-only). When any of those
/// properties diverges from the defaults the handler switches to a
/// <c>Modifier.drawBehind</c> path driven by
/// <see cref="BorderStrokeDrawCallback"/>, which paints the stroke
/// directly via <see cref="Android.Graphics.Paint"/> +
/// <see cref="Android.Graphics.DashPathEffect"/>. The fast path
/// continues to use <c>Modifier.Border</c> for trivial solid borders so
/// the simple case still benefits from Compose's stroke renderer.</para>
/// </remarks>
public partial class BorderHandler : ComposeElementHandler<MauiBorder>
{
    /// <summary>
    /// Property mapper that forwards MAUI <see cref="MauiBorder"/>
    /// changes to the Compose-backed <see cref="ComposeView"/>.
    /// </summary>
    public static IPropertyMapper<MauiBorder, BorderHandler> Mapper =
        new PropertyMapper<MauiBorder, BorderHandler>(ViewHandler.ViewMapper)
        {
            [nameof(MauiBorder.Stroke)]                     = MapStroke,
            [nameof(MauiBorder.StrokeThickness)]            = MapStrokeThickness,
            [nameof(MauiBorder.StrokeShape)]                = MapShape,
            // Stock MAUI exposes the abstract IBorderStroke.Shape key as
            // a separate string. Aliased here so MAUI's PropertyMapper
            // looks up "Shape" and routes to the same handler.
            ["Shape"]                                       = MapShape,
            ["Content"]                                     = MapContent,
            [nameof(IPadding.Padding)]                      = MapPadding,
            [nameof(IView.Background)]                      = MapBackground,
            // Dashed-stroke / cap / join / miter geometry — Compose's
            // Modifier.border can't express any of these, so the handler
            // switches to a Modifier.drawBehind path driven by
            // BorderStrokeDrawCallback when any of these diverge from
            // the trivial solid-border defaults.
            [nameof(MauiBorder.StrokeDashPattern)]          = MapStrokeDashPattern,
            [nameof(MauiBorder.StrokeDashOffset)]           = MapStrokeDashOffset,
            [nameof(MauiBorder.StrokeLineCap)]              = MapStrokeLineCap,
            [nameof(MauiBorder.StrokeLineJoin)]             = MapStrokeLineJoin,
            [nameof(MauiBorder.StrokeMiterLimit)]           = MapStrokeMiterLimit,
        };

    /// <summary>Command mapper (inherits view-level commands; no extras).</summary>
    public static CommandMapper<MauiBorder, BorderHandler> CommandMapper =
        new(ViewCommandMapper);

    readonly MutableState<long?> _strokeColor    = new((long?)null);
    readonly MutableState<float> _strokeWidth    = new(0f);
    readonly MutableState<long?> _backgroundColor = new((long?)null);
    // Shape (IShape) and Padding (Thickness) are reference / struct types
    // not allowed in MutableState<T>. Bump version slots and re-read live.
    readonly MutableState<int>   _shapeVersion   = new(0);
    readonly MutableState<int>   _paddingVersion = new(0);
    readonly MutableState<int>   _contentVersion = new(0);
    // Dashed-stroke geometry version slot — bumped from any of the five
    // MapStroke* mappers below. The actual values live as fields on
    // `_strokeDrawCallback` and are re-read inside `BuildNode` (which
    // pushes them into the JCW just before the draw lambda is set).
    readonly MutableState<int>   _strokeGeometryVersion = new(0);

    // Allocated once per handler instance so the JNI peer (and the
    // backing Paint) survives every recomposition. Mappers mutate the
    // fields in place; BuildNode reconfigures the rest just before
    // taking it on the modifier chain.
    readonly BorderStrokeDrawCallback _strokeDrawCallback = new();

    /// <summary>Construct a handler with the default mappers.</summary>
    public BorderHandler() : base(Mapper, CommandMapper) { }

    /// <summary>Construct a handler with custom mappers.</summary>
    public BorderHandler(IPropertyMapper? mapper, CommandMapper? commandMapper = null)
        : base(mapper ?? Mapper, commandMapper ?? CommandMapper) { }

    /// <inheritdoc/>
    public override ComposableNode BuildNode(IComposer composer)
    {
        // Subscribe to live-read slots so a Padding/StrokeShape/Content
        // change recomposes the Box.
        _ = _shapeVersion.Value;
        _ = _paddingVersion.Value;
        _ = _contentVersion.Value;
        _ = _strokeGeometryVersion.Value;

        var context = MauiContext
            ?? throw new InvalidOperationException("MauiContext not set on BorderHandler.");

        var border = (Microsoft.Maui.Controls.Border?)VirtualView
            ?? throw new InvalidOperationException("VirtualView not set on BorderHandler.");
        var padding = (border as IPadding)?.Padding ?? Thickness.Zero;
        var width   = _strokeWidth.Value;
        var stroke  = _strokeColor.Value;
        var bg      = _backgroundColor.Value;
        var shape   = ResolveShape(border.StrokeShape);

        Modifier? modifier = null;
        if (shape is not null)
            modifier = (modifier ?? Modifier.Companion).Clip(shape);
        if (bg.HasValue)
            modifier = (modifier ?? Modifier.Companion)
                .Background(new ComposeColor(bg.Value), shape);
        if (stroke.HasValue && width > 0f)
        {
            // Custom geometry (dashes / non-default cap / join / miter)
            // can't be expressed via Modifier.border, which paints a
            // solid stroke only. Switch to a Paint+Canvas drawBehind
            // path when any of the five geometry knobs diverge from
            // the trivial defaults.
            if (HasCustomStrokeGeometry(border))
            {
                ConfigureStrokeDrawCallback(border, stroke.Value, width);
                modifier = (modifier ?? Modifier.Companion).DrawBehind(_strokeDrawCallback);
            }
            else
            {
                modifier = (modifier ?? Modifier.Companion)
                    .Border(new Dp(width), new ComposeColor(stroke.Value), shape);
            }
        }
        if (padding != Thickness.Zero)
        {
            modifier = (modifier ?? Modifier.Companion).Padding(
                start:  new Dp((float)padding.Left),
                top:    new Dp((float)padding.Top),
                end:    new Dp((float)padding.Right),
                bottom: new Dp((float)padding.Bottom));
        }

        var box = new Box();
        modifier = (modifier ?? Modifier.Companion).ApplyGestures(border, context).ApplySemantics(border);
        box.Modifier = modifier;

        // Walk the Border's content (IContentView.PresentedContent)
        // through the same ComposeWalker pipeline used by Page /
        // ContentView.
        if ((border as IContentView)?.PresentedContent is { } content)
            box.Add(c => ComposeWalker.Render(content, c, context));

        return box;
    }

    /// <summary>
    /// Resolve <c>Border.StrokeShape</c> (IShape) to a Compose
    /// <see cref="Shape"/> handle. Pattern-matches the three concrete
    /// shape types MAUI ships in <c>Microsoft.Maui.Controls.Shapes</c>.
    /// </summary>
    static Shape? ResolveShape(Microsoft.Maui.Graphics.IShape? shape) => shape switch
    {
        MauiBorderShape.RoundRectangle rr =>
            // CornerRadius is per-corner in DIPs.  Compose's
            // RoundedCornerShape takes (topStart, topEnd, bottomEnd,
            // bottomStart) in Dp.
            new RoundedCornerShape(
                topStart:    new Dp((float)rr.CornerRadius.TopLeft),
                topEnd:      new Dp((float)rr.CornerRadius.TopRight),
                bottomEnd:   new Dp((float)rr.CornerRadius.BottomRight),
                bottomStart: new Dp((float)rr.CornerRadius.BottomLeft)),
        // No bound circle facade — RoundedCornerShape(50%) collapses
        // to a circle for square layouts and stays an ellipse for
        // non-square ones, which mirrors MAUI's Ellipse semantics.
        MauiBorderShape.Ellipse        => new RoundedCornerShape(50),
        MauiBorderShape.Rectangle      => null,
        _                              => null,
    };

    // Returns true when at least one of the five stroke-geometry knobs
    // diverges from the trivial defaults (solid stroke, butt caps,
    // miter joins, default miter limit). When false we can take the
    // fast `Modifier.border` path which uses Compose's stroke renderer.
    // Reads via IBorderStroke because the concrete MauiBorder surface
    // uses XAML-flavoured types (PenLineCap, double, ...).
    static bool HasCustomStrokeGeometry(MauiBorder border)
    {
        var stroke = (IBorderStroke)border;
        return stroke.StrokeDashPattern is { Length: >= 2 } ||
               stroke.StrokeDashOffset != 0f ||
               stroke.StrokeLineCap    != MauiLineCap.Butt ||
               stroke.StrokeLineJoin   != MauiLineJoin.Miter ||
               stroke.StrokeMiterLimit != 10f;
    }

    void ConfigureStrokeDrawCallback(MauiBorder border, long strokeColor, float strokeThickness)
    {
        // ARGB int for native Paint — derived directly from the live
        // SolidPaint Color so the dashed path stays in sync with the
        // brush even when MapStroke doesn't refire (e.g. opacity tweak
        // bubbling through SolidPaint).
        var solidColor = (border as IStroke)?.Stroke is SolidPaint solid
            ? solid.Color
            : null;
        var stroke = (IBorderStroke)border;
        _strokeDrawCallback.StrokeArgb         = ColorMapping.ToArgb(solidColor);
        _strokeDrawCallback.StrokeThicknessDip = strokeThickness;
        _strokeDrawCallback.StrokeDashPattern  = stroke.StrokeDashPattern;
        _strokeDrawCallback.StrokeDashOffset   = stroke.StrokeDashOffset;
        _strokeDrawCallback.StrokeLineCap      = stroke.StrokeLineCap;
        _strokeDrawCallback.StrokeLineJoin     = stroke.StrokeLineJoin;
        _strokeDrawCallback.StrokeMiterLimit   = stroke.StrokeMiterLimit;
        _strokeDrawCallback.Shape              = stroke.Shape;
        var metrics = global::Android.Content.Res.Resources.System?.DisplayMetrics
            ?? throw new InvalidOperationException("Resources.System.DisplayMetrics not available.");
        _strokeDrawCallback.Density            = metrics.Density;

        // Silence "strokeColor is unused" — it's already derived from
        // SolidPaint above, but kept on the signature so the caller
        // can pass through the cached MutableState slot when MAUI's
        // stroke property cycles through null between updates.
        _ = strokeColor;
    }

    /// <summary>Map <see cref="MauiBorder.Stroke"/> by extracting
    /// <see cref="SolidPaint"/>.<see cref="SolidPaint.Color"/>.</summary>
    public static void MapStroke(BorderHandler handler, MauiBorder border) =>
        handler._strokeColor.Value = (border as IStroke)?.Stroke is SolidPaint solid
            ? ColorMapping.ToPackedLong(solid.Color)
            : null;

    /// <summary>Map <see cref="MauiBorder.StrokeThickness"/>.</summary>
    public static void MapStrokeThickness(BorderHandler handler, MauiBorder border) =>
        handler._strokeWidth.Value = (float)border.StrokeThickness;

    /// <summary>Bump the shape version slot.</summary>
    public static void MapShape(BorderHandler handler, MauiBorder _) =>
        handler._shapeVersion.Value++;

    /// <summary>Bump the content version slot.</summary>
    public static void MapContent(BorderHandler handler, MauiBorder _) =>
        handler._contentVersion.Value++;

    /// <summary>Bump the padding version slot.</summary>
    public static void MapPadding(BorderHandler handler, MauiBorder _) =>
        handler._paddingVersion.Value++;

    /// <summary>Map <see cref="IView.Background"/> by extracting
    /// <see cref="SolidPaint"/>.</summary>
    public static void MapBackground(BorderHandler handler, MauiBorder border) =>
        handler._backgroundColor.Value = (border as IView)?.Background is SolidPaint solid
            ? ColorMapping.ToPackedLong(solid.Color)
            : null;

    /// <summary>
    /// Bump the stroke-geometry version slot in response to
    /// <see cref="MauiBorder.StrokeDashPattern"/> changes. The actual
    /// pattern array is read live inside <see cref="BuildNode"/>.
    /// </summary>
    public static void MapStrokeDashPattern(BorderHandler handler, MauiBorder _) =>
        handler._strokeGeometryVersion.Value++;

    /// <summary>Bump the stroke-geometry version slot for
    /// <see cref="MauiBorder.StrokeDashOffset"/>.</summary>
    public static void MapStrokeDashOffset(BorderHandler handler, MauiBorder _) =>
        handler._strokeGeometryVersion.Value++;

    /// <summary>Bump the stroke-geometry version slot for
    /// <see cref="MauiBorder.StrokeLineCap"/>.</summary>
    public static void MapStrokeLineCap(BorderHandler handler, MauiBorder _) =>
        handler._strokeGeometryVersion.Value++;

    /// <summary>Bump the stroke-geometry version slot for
    /// <see cref="MauiBorder.StrokeLineJoin"/>.</summary>
    public static void MapStrokeLineJoin(BorderHandler handler, MauiBorder _) =>
        handler._strokeGeometryVersion.Value++;

    /// <summary>Bump the stroke-geometry version slot for
    /// <see cref="MauiBorder.StrokeMiterLimit"/>.</summary>
    public static void MapStrokeMiterLimit(BorderHandler handler, MauiBorder _) =>
        handler._strokeGeometryVersion.Value++;
}
