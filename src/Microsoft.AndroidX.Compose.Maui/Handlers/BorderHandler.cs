using AndroidX.Compose;
using AndroidX.Compose.Runtime;
using Microsoft.AndroidX.Compose.Maui.Platform;
using Microsoft.Maui.Handlers;
using ComposeColor      = AndroidX.Compose.Color;
using MauiBorder        = Microsoft.Maui.Controls.Border;
using MauiBorderShape   = Microsoft.Maui.Controls.Shapes;

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
            ["Content"]                                     = MapContent,
            [nameof(IPadding.Padding)]                      = MapPadding,
            [nameof(IView.Background)]                      = MapBackground,
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
            modifier = (modifier ?? Modifier.Companion)
                .Border(new Dp(width), new ComposeColor(stroke.Value), shape);
        if (padding != Thickness.Zero)
        {
            modifier = (modifier ?? Modifier.Companion).Padding(
                start:  new Dp((float)padding.Left),
                top:    new Dp((float)padding.Top),
                end:    new Dp((float)padding.Right),
                bottom: new Dp((float)padding.Bottom));
        }

        var box = new Box();
        modifier = (modifier ?? Modifier.Companion).ApplyGestures(border, context);
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
}
