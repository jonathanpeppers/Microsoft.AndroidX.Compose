using AndroidX.Compose;
using AndroidX.Compose.Runtime;
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

    /// <summary>Construct a handler with the default mappers.</summary>
    public BoxViewHandler() : base(Mapper, CommandMapper) { }

    /// <summary>Construct a handler with custom mappers.</summary>
    public BoxViewHandler(IPropertyMapper? mapper, CommandMapper? commandMapper = null)
        : base(mapper ?? Mapper, commandMapper ?? CommandMapper) { }

    /// <inheritdoc/>
    public override ComposableNode BuildNode(IComposer composer)
    {
        _ = _cornerVersion.Value;  // subscribe — CornerRadius change bumps this
        var color = _color.Value ?? _backgroundColor.Value;
        var corner = VirtualView?.CornerRadius ?? default;

        // Single-radius reduction — BoxView.CornerRadius applies the
        // same value to all four corners. MAUI's CornerRadius struct
        // exposes per-corner doubles for compatibility with
        // RoundRectangle, but BoxView always populates them all the
        // same way.
        var radius = (float)Math.Max(0, corner.TopLeft);
        Shape? shape = radius > 0 ? new RoundedCornerShape(new Dp(radius)) : null;

        Modifier? modifier = Modifier.Companion.FillMaxSize();
        if (shape is not null)
            modifier = modifier.Clip(shape);
        if (color.HasValue)
            modifier = modifier.Background(new ComposeColor(color.Value), shape);

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
}
