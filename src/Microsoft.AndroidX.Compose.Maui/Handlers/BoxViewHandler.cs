using AndroidX.Compose;
using AndroidX.Compose.Runtime;
using Microsoft.AndroidX.Compose.Maui.Platform;
using Microsoft.Maui.Handlers;
using ComposeColor = AndroidX.Compose.Color;
using MauiBoxView  = Microsoft.Maui.Controls.BoxView;

namespace Microsoft.AndroidX.Compose.Maui.Handlers;

/// <summary>
/// MAUI <see cref="MauiBoxView"/> handler that renders as a Compose
/// <see cref="Box"/> with
/// <c>Modifier.Background(color).Clip(shape).FillMaxSize()</c>.
/// Folds into the page's single composition via
/// <see cref="ComposeElementHandler{TVirtualView}"/> /
/// <see cref="IComposeHandler"/>.
/// </summary>
/// <remarks>
/// <para>BoxView's <see cref="MauiBoxView.Color"/> (legacy color
/// element) takes precedence over <see cref="IView.BackgroundColor"/>.
/// If both are unset the box renders transparent — matches MAUI's
/// <c>BoxRenderer</c> behaviour.</para>
///
/// <para><see cref="MauiBoxView.CornerRadius"/> is uniform (single
/// value applied to all four corners), so the per-corner Compose
/// <see cref="RoundedCornerShape"/> ctor reduces to the single-radius
/// overload.</para>
///
/// <para>BoxView extends <see cref="IShapeView"/> + <see cref="IStroke"/>
/// in the MAUI contract but the typical usage is a solid rectangle
/// — stroke / shape extension support is intentionally omitted.</para>
/// </remarks>
public partial class BoxViewHandler : ComposeElementHandler<MauiBoxView>
{
    /// <summary>
    /// Property mapper that forwards MAUI <see cref="MauiBoxView"/>
    /// changes to the Compose-backed <see cref="ComposeView"/>.
    /// </summary>
    public static IPropertyMapper<MauiBoxView, BoxViewHandler> Mapper =
        new PropertyMapper<MauiBoxView, BoxViewHandler>(ViewHandler.ViewMapper)
        {
            [nameof(MauiBoxView.Color)]            = MapColor,
            [nameof(MauiBoxView.BackgroundColor)]  = MapBackgroundColor,
            [nameof(MauiBoxView.CornerRadius)]     = MapCornerRadius,
            // VisualElement.WidthRequest / HeightRequest live on the
            // Controls layer, not IView. Use string literals so the
            // mapper still picks up the change notifications.
            ["WidthRequest"]                       = MapSizeRequest,
            ["HeightRequest"]                      = MapSizeRequest,
        };

    /// <summary>Command mapper (inherits view-level commands; no extras).</summary>
    public static CommandMapper<MauiBoxView, BoxViewHandler> CommandMapper =
        new(ViewCommandMapper);

    readonly MutableState<long?> _color           = new((long?)null);
    readonly MutableState<long?> _backgroundColor = new((long?)null);
    // CornerRadius is a struct — use the version-counter pattern and
    // re-read VirtualView.CornerRadius live in BuildNode (same trick
    // as LayoutHandler._paddingVersion).
    readonly MutableState<int>   _cornerVersion   = new(0);
    // WidthRequest / HeightRequest are doubles but the "is set" sentinel
    // (-1) is convention; bump a version slot and re-read live.
    readonly MutableState<int>   _sizeVersion     = new(0);

    /// <summary>Construct a handler with the default mappers.</summary>
    public BoxViewHandler() : base(Mapper, CommandMapper) { }

    /// <summary>Construct a handler with custom mappers.</summary>
    public BoxViewHandler(IPropertyMapper? mapper, CommandMapper? commandMapper = null)
        : base(mapper ?? Mapper, commandMapper ?? CommandMapper) { }

    /// <inheritdoc/>
    public override ComposableNode BuildNode(IComposer composer)
    {
        var virtualView = VirtualView
            ?? throw new InvalidOperationException("VirtualView not set on BoxViewHandler.");

        _ = _cornerVersion.Value;  // subscribe — CornerRadius change bumps this
        _ = _sizeVersion.Value;    // subscribe — Width/HeightRequest change bumps this
        var color = _color.Value ?? _backgroundColor.Value;
        var corner = virtualView.CornerRadius;

        // Single-radius reduction — BoxView.CornerRadius applies the
        // same value to all four corners. MAUI's CornerRadius struct
        // exposes per-corner doubles for compatibility with
        // RoundRectangle, but BoxView always populates them all the
        // same way.
        var radius = (float)Math.Max(0, corner.TopLeft);
        Shape? shape = radius > 0 ? new RoundedCornerShape(new Dp(radius)) : null;

        // BoxView is a content-less leaf, so it can't draw anything
        // unless the modifier chain gives it explicit dimensions:
        //
        //   * WidthRequest set + HeightRequest set → Modifier.Size(w, h).
        //   * WidthRequest set only                → Modifier.Width(w);
        //     height collapses to 0 in an unbounded parent (Column / Row),
        //     same behavior as stock MAUI.
        //   * HeightRequest set only               → FillMaxWidth + Height(h),
        //     i.e. a horizontal divider that spans the parent.
        //   * Neither set                          → FillMaxSize, expecting
        //     a bounded parent. (Default 40 dp behavior is left to MAUI's
        //     measure pass — won't apply here because we fold into a
        //     single ComposeView.)
        var widthReq  = virtualView.WidthRequest;
        var heightReq = virtualView.HeightRequest;
        Modifier modifier = (widthReq, heightReq) switch
        {
            ( >= 0d, >= 0d ) => Modifier.Size(new Dp((float)widthReq), new Dp((float)heightReq)),
            ( >= 0d, _    ) => Modifier.Width(new Dp((float)widthReq)),
            ( _,    >= 0d ) => Modifier.FillMaxWidth().Height(new Dp((float)heightReq)),
            _                => Modifier.FillMaxSize(),
        };
        if (shape is not null)
            modifier = modifier.Clip(shape);
        if (color.HasValue)
            modifier = modifier.Background(new ComposeColor(color.Value), shape);
        modifier = modifier.ApplyGestures(virtualView, MauiContext).ApplySemantics(virtualView);

        return new Box { Modifier = modifier };
    }

    /// <summary>Map <see cref="MauiBoxView.Color"/>.</summary>
    public static void MapColor(BoxViewHandler handler, MauiBoxView box) =>
        handler._color.Value = ColorMapping.ToPackedLong(box.Color);

    /// <summary>Map <see cref="Microsoft.Maui.Controls.VisualElement.BackgroundColor"/>.</summary>
    public static void MapBackgroundColor(BoxViewHandler handler, MauiBoxView box) =>
        handler._backgroundColor.Value = ColorMapping.ToPackedLong(box.BackgroundColor);

    /// <summary>Bump the corner-radius version slot.</summary>
    public static void MapCornerRadius(BoxViewHandler handler, MauiBoxView _) =>
        handler._cornerVersion.Value++;

    /// <summary>Bump the size-version slot when the virtual view's <c>WidthRequest</c> or <c>HeightRequest</c> changes.</summary>
    public static void MapSizeRequest(BoxViewHandler handler, MauiBoxView _) =>
        handler._sizeVersion.Value++;
}
