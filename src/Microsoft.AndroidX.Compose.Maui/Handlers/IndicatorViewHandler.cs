using AndroidX.Compose;
using AndroidX.Compose.Runtime;
using Microsoft.AndroidX.Compose.Maui.Platform;
using Microsoft.Maui.Handlers;
using ComposeColor      = AndroidX.Compose.Color;
using IndicatorShape    = Microsoft.Maui.Controls.IndicatorShape;
using MauiIndicatorView = Microsoft.Maui.Controls.IndicatorView;

namespace Microsoft.AndroidX.Compose.Maui.Handlers;

/// <summary>
/// MAUI <see cref="MauiIndicatorView"/> handler that synthesises the
/// classic dot strip out of a Compose <see cref="Row"/> of
/// <see cref="Box"/> tiles. Replaces MAUI's
/// <c>IndicatorStackLayout</c>-backed handler when the consumer calls
/// <see cref="Hosting.AppHostBuilderExtensions.UseAndroidXCompose"/>.
/// </summary>
/// <remarks>
/// <para>Compose has no first-class indicator primitive — Material 3
/// leaves carousel position rendering to the consumer. The handler
/// generates one <see cref="Box"/> per <see cref="MauiIndicatorView.Count"/>,
/// each <see cref="MauiIndicatorView.IndicatorSize"/> dp wide,
/// clipped to a <c>RoundedCornerShape(50)</c> circle (or
/// <c>RoundedCornerShape(0)</c> square per
/// <see cref="MauiIndicatorView.IndicatorsShape"/>), tinted with
/// <see cref="MauiIndicatorView.SelectedIndicatorColor"/> for the
/// active <see cref="MauiIndicatorView.Position"/> and
/// <see cref="MauiIndicatorView.IndicatorColor"/> for everything
/// else.</para>
///
/// <para><b>Position is one-way (MAUI → Compose) for now.</b> The
/// two-way binding to <c>CarouselView.Position</c> is owned by the
/// <see cref="CarouselViewHandler"/> work in Phase 3 — until then the
/// indicator visualises whatever MAUI / consumer code writes to
/// <see cref="MauiIndicatorView.Position"/>; tapping a dot doesn't
/// scroll the carousel.</para>
///
/// <para><b><see cref="MauiIndicatorView.IndicatorTemplate"/> is not
/// honoured.</b> Custom-templated indicators need MAUI's stock
/// <c>IndicatorStackLayout</c> rendering which doesn't fit the single
/// <c>ComposeView</c>-per-page contract. To use templated indicators,
/// don't register this Compose handler against
/// <see cref="MauiIndicatorView"/>: skip the <c>AddHandler</c>
/// override and stock MAUI takes over for that one control. When a
/// template is set on a Compose-routed indicator the handler logs a
/// debug warning and renders dots regardless.</para>
///
/// <para><see cref="MauiIndicatorView.Count"/> /
/// <see cref="MauiIndicatorView.IndicatorSize"/> are <c>int</c> /
/// <c>double</c> values that change rarely; both go through a
/// version-counter <see cref="MutableState{T}"/> instead of being
/// observed directly. <see cref="BuildNode"/> reads the live values
/// off <see cref="ViewHandler{TVirtualView,TPlatformView}.VirtualView"/>
/// when the slot bumps, mirroring <see cref="LayoutHandler"/>'s
/// strategy for non-primitive properties.</para>
/// </remarks>
public partial class IndicatorViewHandler : ComposeElementHandler<MauiIndicatorView>
{
    /// <summary>
    /// Property mapper that forwards MAUI <see cref="MauiIndicatorView"/>
    /// changes to the Compose-backed <see cref="ComposeView"/>.
    /// </summary>
    public static IPropertyMapper<MauiIndicatorView, IndicatorViewHandler> Mapper =
        new PropertyMapper<MauiIndicatorView, IndicatorViewHandler>(ViewHandler.ViewMapper)
        {
            [nameof(MauiIndicatorView.Count)]                  = MapCount,
            [nameof(MauiIndicatorView.Position)]               = MapPosition,
            [nameof(MauiIndicatorView.IndicatorColor)]         = MapIndicatorColor,
            [nameof(MauiIndicatorView.SelectedIndicatorColor)] = MapSelectedIndicatorColor,
            [nameof(MauiIndicatorView.IndicatorSize)]          = MapIndicatorSize,
            [nameof(MauiIndicatorView.IndicatorsShape)]        = MapIndicatorsShape,
            [nameof(MauiIndicatorView.HideSingle)]             = MapHideSingle,
            [nameof(MauiIndicatorView.MaximumVisible)]         = MapMaximumVisible,
        };

    /// <summary>Command mapper (inherits view-level commands; no extras).</summary>
    public static CommandMapper<MauiIndicatorView, IndicatorViewHandler> CommandMapper =
        new(ViewCommandMapper);

    // Count + IndicatorSize don't fit MutableState<T> idiomatic types
    // for re-keying purposes — a version counter recomposes BuildNode
    // which then reads the live values off VirtualView.
    readonly MutableState<int>   _countVersion         = new(0);
    readonly MutableState<int>   _indicatorSizeVersion = new(0);
    readonly MutableState<int>   _shapeVersion         = new(0);
    readonly MutableState<int>   _position             = new(0);
    readonly MutableState<long?> _indicatorColor       = new((long?)null);
    readonly MutableState<long?> _selectedColor        = new((long?)null);
    readonly MutableState<bool>  _hideSingle           = new(true);
    readonly MutableState<int>   _maximumVisible       = new(int.MaxValue);

    /// <summary>Construct a handler with the default mappers.</summary>
    public IndicatorViewHandler() : base(Mapper, CommandMapper) { }

    /// <summary>Construct a handler with custom mappers.</summary>
    public IndicatorViewHandler(IPropertyMapper? mapper, CommandMapper? commandMapper = null)
        : base(mapper ?? Mapper, commandMapper ?? CommandMapper) { }

    /// <inheritdoc/>
    public override ComposableNode BuildNode(IComposer composer)
    {
        // Read every version slot so recompositions trigger when any
        // observed-by-version property bumps.
        _ = _countVersion.Value;
        _ = _indicatorSizeVersion.Value;
        _ = _shapeVersion.Value;

        var view = VirtualView
            ?? throw new InvalidOperationException("VirtualView not set on IndicatorViewHandler.");

        int    count     = view.Count;
        int    position  = _position.Value;
        bool   hideSingle = _hideSingle.Value;
        int    maxVisible = _maximumVisible.Value;
        double sizeDp    = view.IndicatorSize > 0 ? view.IndicatorSize : 6.0;
        var    shape     = view.IndicatorsShape == IndicatorShape.Square
                              ? new RoundedCornerShape(0)
                              : new RoundedCornerShape(50);

        // HideSingle (MAUI default = true): collapse the strip when
        // there's only one carousel page — there's nothing to indicate.
        // Returning an empty Row keeps the slot alive so the next
        // recompose with count >= 2 patches in dots without
        // re-establishing the parent layout.
        int visibleCount = hideSingle && count <= 1 ? 0 : count;
        // MaximumVisible caps how many dots are rendered (stock MAUI
        // truncates from the tail; we mirror that — a 10-page carousel
        // with MaximumVisible = 5 shows dots [0..4], the page indicator
        // for pages 5-9 disappears).
        if (maxVisible > 0)
            visibleCount = Math.Min(visibleCount, maxVisible);

        long inactiveColor = _indicatorColor.Value
            ?? ColorMapping.ToPackedLong(Microsoft.Maui.Graphics.Colors.LightGrey)
            ?? throw new InvalidOperationException("Failed to pack LightGrey color.");
        long activeColor   = _selectedColor.Value
            ?? ColorMapping.ToPackedLong(Microsoft.Maui.Graphics.Colors.Black)
            ?? throw new InvalidOperationException("Failed to pack Black color.");

        if (view.IndicatorTemplate is not null)
        {
            // Templated indicators aren't lowered to Compose — see
            // <remarks> on this handler. Render the dot fallback so
            // the user sees something instead of empty space.
            System.Diagnostics.Debug.WriteLine(
                "[Microsoft.AndroidX.Compose.Maui] IndicatorView.IndicatorTemplate is set, but " +
                "the Compose-backed handler renders dots regardless. " +
                "Drop the AddHandler<IndicatorView, IndicatorViewHandler>() registration " +
                "to fall back to stock MAUI templating.");
        }

        // Outer Row carries ApplyViewProperties (Opacity / Translation /
        // Scale / Rotation / Clip / IsVisible / Shadow) so per-page
        // animations / fades work the same as MAUI's stock indicator.
        var row = new Row(
            horizontalArrangement: Arrangement.SpacedBy(new Dp((float)Math.Max(4.0, sizeDp / 2.0))),
            verticalAlignment:     global::AndroidX.Compose.Alignment.Vertical.CenterVertically)
        {
            Modifier = Modifier.Companion.ApplyViewProperties(view).ApplySemantics(view),
        };

        for (int i = 0; i < visibleCount; i++)
        {
            long color   = i == position ? activeColor : inactiveColor;
            row.Add(new Box
            {
                Modifier = Modifier
                    .Size(new Dp((float)sizeDp))
                    .Clip(shape)
                    .Background(ComposeColor.FromPacked(color)),
            });
        }

        return row;
    }

    /// <summary>Bump the count version slot.</summary>
    public static void MapCount(IndicatorViewHandler handler, MauiIndicatorView _) =>
        handler._countVersion.Value++;

    /// <summary>Map <see cref="MauiIndicatorView.Position"/> to the active-dot slot.</summary>
    public static void MapPosition(IndicatorViewHandler handler, MauiIndicatorView view) =>
        handler._position.Value = view.Position;

    /// <summary>Map <see cref="MauiIndicatorView.IndicatorColor"/> to the inactive-dot tint.</summary>
    public static void MapIndicatorColor(IndicatorViewHandler handler, MauiIndicatorView view) =>
        handler._indicatorColor.Value = ColorMapping.ToPackedLong(view.IndicatorColor);

    /// <summary>Map <see cref="MauiIndicatorView.SelectedIndicatorColor"/> to the active-dot tint.</summary>
    public static void MapSelectedIndicatorColor(IndicatorViewHandler handler, MauiIndicatorView view) =>
        handler._selectedColor.Value = ColorMapping.ToPackedLong(view.SelectedIndicatorColor);

    /// <summary>Bump the indicator-size version slot.</summary>
    public static void MapIndicatorSize(IndicatorViewHandler handler, MauiIndicatorView _) =>
        handler._indicatorSizeVersion.Value++;

    /// <summary>Bump the indicator-shape version slot.</summary>
    public static void MapIndicatorsShape(IndicatorViewHandler handler, MauiIndicatorView _) =>
        handler._shapeVersion.Value++;

    /// <summary>
    /// Map <see cref="MauiIndicatorView.HideSingle"/>: when
    /// <see langword="true"/>, the dot strip is suppressed while
    /// <see cref="MauiIndicatorView.Count"/> is <c>0</c> or <c>1</c>.
    /// </summary>
    public static void MapHideSingle(IndicatorViewHandler handler, MauiIndicatorView view) =>
        handler._hideSingle.Value = view.HideSingle;

    /// <summary>
    /// Map <see cref="MauiIndicatorView.MaximumVisible"/> to a hard cap on
    /// the number of dots rendered; tail dots beyond the cap are hidden.
    /// </summary>
    public static void MapMaximumVisible(IndicatorViewHandler handler, MauiIndicatorView view) =>
        handler._maximumVisible.Value = view.MaximumVisible;
}
