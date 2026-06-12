using AndroidX.Compose;
using AndroidX.Compose.Material3;
using AndroidX.Compose.Runtime;
using Microsoft.AndroidX.Compose.Maui.Platform;
using Microsoft.Maui.Handlers;
using ComposeSwitch = AndroidX.Compose.Switch;

namespace Microsoft.AndroidX.Compose.Maui.Handlers;

/// <summary>
/// MAUI <see cref="Microsoft.Maui.Controls.Switch"/> handler that
/// renders through Jetpack Compose's Material 3 <c>Switch</c>
/// composable. Replaces MAUI's stock <c>SwitchCompat</c>-based
/// handler when the consumer calls
/// <see cref="Hosting.AppHostBuilderExtensions.UseAndroidXCompose"/>.
/// </summary>
/// <remarks>
/// <para>Folds into the page's single composition via
/// <see cref="ComposeElementHandler{TVirtualView}"/> /
/// <see cref="IComposeHandler"/>. Two-way binding mirrors
/// <see cref="EntryHandler"/>: Compose's <c>onCheckedChange</c> writes
/// the new value back to <see cref="ISwitch.IsOn"/>, which re-enters
/// <see cref="MapIsOn"/> — that's a no-op against the already-updated
/// <see cref="MutableState{T}"/> so the loop short-circuits.</para>
///
/// <para>MAUI's <see cref="Microsoft.Maui.Controls.Switch.OnColor"/>
/// (a.k.a. <see cref="ISwitch.TrackColor"/>) tints the track when the
/// switch is on; MAUI's <see cref="ISwitch.ThumbColor"/> tints the
/// thumb. Both are forwarded to Compose's
/// <see cref="SwitchColors"/> via
/// <see cref="ComposeExtensions.SwitchColors"/>. Because MAUI doesn't
/// distinguish a "checked" vs "unchecked" thumb tint we apply the
/// same tint to both states, matching the stock MAUI rendering.</para>
/// </remarks>
public partial class SwitchHandler : ComposeElementHandler<ISwitch>
{
    /// <summary>
    /// Property mapper that forwards MAUI <see cref="ISwitch"/>
    /// property changes to the Compose-backed <see cref="ComposeView"/>.
    /// </summary>
    public static IPropertyMapper<ISwitch, SwitchHandler> Mapper =
        new PropertyMapper<ISwitch, SwitchHandler>(ViewHandler.ViewMapper)
        {
            [nameof(ISwitch.IsOn)]       = MapIsOn,
            [nameof(ISwitch.TrackColor)] = MapTrackColor,
            [nameof(ISwitch.ThumbColor)] = MapThumbColor,
        };

    /// <summary>Command mapper (inherits view-level commands; no extras).</summary>
    public static CommandMapper<ISwitch, SwitchHandler> CommandMapper =
        new(ViewCommandMapper);

    readonly MutableState<bool>  _on         = new(false);
    readonly MutableState<long?> _trackColor = new((long?)null);
    readonly MutableState<long?> _thumbColor = new((long?)null);

    /// <summary>Construct a handler with the default mappers.</summary>
    public SwitchHandler() : base(Mapper, CommandMapper) { }

    /// <summary>Construct a handler with custom mappers.</summary>
    public SwitchHandler(IPropertyMapper? mapper, CommandMapper? commandMapper = null)
        : base(mapper ?? Mapper, commandMapper ?? CommandMapper) { }

    /// <inheritdoc/>
    public override ComposableNode BuildNode(IComposer composer)
    {
        var virtualView = VirtualView;
        ArgumentNullException.ThrowIfNull(virtualView);

        var track = _trackColor.Value;
        var thumb = _thumbColor.Value;
        var sw = new ComposeSwitch(@checked: _on.Value,
                                   onCheckedChange: OnCheckedChanged);
        if (track is not null || thumb is not null)
            sw.Colors = composer.SwitchColors(
                checkedThumbColor:   thumb,
                checkedTrackColor:   track,
                uncheckedThumbColor: thumb,
                uncheckedTrackColor: track);
        sw.PrependModifier(Modifier.Companion.ApplyGestures(virtualView, MauiContext));
        return sw;
    }

    void OnCheckedChanged(bool newValue)
    {
        // Sync Compose state synchronously so the rendered thumb stays
        // pinned where the user dragged it. Re-entrant write through
        // VirtualView.IsOn lands in MapIsOn — equality on MutableState
        // short-circuits the second pass.
        _on.Value = newValue;
        if (VirtualView is { } v)
            v.IsOn = newValue;
    }

    /// <summary>Map <see cref="ISwitch.IsOn"/> to the Compose <c>checked</c> slot.</summary>
    public static void MapIsOn(SwitchHandler handler, ISwitch sw) =>
        handler._on.Value = sw.IsOn;

    /// <summary>
    /// Map <see cref="ISwitch.TrackColor"/> (MAUI <c>OnColor</c>) to
    /// Compose's <see cref="SwitchColors"/> <c>checkedTrackColor</c>
    /// slot.
    /// </summary>
    public static void MapTrackColor(SwitchHandler handler, ISwitch sw) =>
        handler._trackColor.Value = ColorMapping.ToPackedLong(sw.TrackColor);

    /// <summary>
    /// Map <see cref="ISwitch.ThumbColor"/> to Compose's
    /// <see cref="SwitchColors"/> <c>checkedThumbColor</c> slot.
    /// </summary>
    public static void MapThumbColor(SwitchHandler handler, ISwitch sw) =>
        handler._thumbColor.Value = ColorMapping.ToPackedLong(sw.ThumbColor);
}
