using AndroidX.Compose;
using AndroidX.Compose.Material3;
using AndroidX.Compose.Runtime;
using Kotlin.Ranges;
using Microsoft.AndroidX.Compose.Maui.Platform;
using Microsoft.Maui.Handlers;
using ComposeSlider = AndroidX.Compose.Slider;

namespace Microsoft.AndroidX.Compose.Maui.Handlers;

/// <summary>
/// MAUI <see cref="Microsoft.Maui.Controls.Slider"/> handler that renders
/// through Jetpack Compose's Material 3 <c>Slider</c> composable. Replaces
/// MAUI's stock <c>SeekBar</c>-based handler when the consumer calls
/// <see cref="Hosting.AppHostBuilderExtensions.UseAndroidXCompose"/>.
/// </summary>
/// <remarks>
/// <para>Folds into the page's single composition via
/// <see cref="ComposeElementHandler{TVirtualView}"/> /
/// <see cref="IComposeHandler"/>. Compose's <c>onValueChange</c>
/// lambda forwards to <see cref="IRange.Value"/> using the same
/// feedback-loop guard as <see cref="EntryHandler.OnValueChanged"/>:
/// the Compose <see cref="MutableState{T}"/> equality check
/// short-circuits the secondary write so the slider doesn't re-enter
/// recomposition when MAUI's two-way binding round-trips the same
/// value.</para>
///
/// <para>The MAUI <c>Minimum</c> / <c>Maximum</c> are surfaced through a
/// Kotlin <see cref="IClosedFloatingPointRange"/> built with
/// <see cref="RangesKt.RangeTo(float, float)"/>; only constructed when
/// the bounds differ from Compose's default <c>[0f, 1f]</c> so a
/// MAUI Slider with default bounds doesn't allocate a wrapper per
/// recomposition.</para>
///
/// <para>The thumb / track colours map onto a
/// <see cref="SliderColors"/> built via the
/// <c>composer.SliderColors(...)</c> extension; only the three slots
/// MAUI exposes (<c>thumbColor</c>, <c>activeTrackColor</c>,
/// <c>inactiveTrackColor</c>) are wired — tick colours and the four
/// disabled siblings stay at the Material default.</para>
/// </remarks>
public partial class SliderHandler : ComposeElementHandler<ISlider>
{
    /// <summary>
    /// Property mapper that forwards MAUI <see cref="ISlider"/> property
    /// changes to the Compose-backed <see cref="ComposeView"/>.
    /// </summary>
    public static IPropertyMapper<ISlider, SliderHandler> Mapper =
        new PropertyMapper<ISlider, SliderHandler>(ViewHandler.ViewMapper)
        {
            [nameof(IRange.Value)]              = MapValue,
            [nameof(IRange.Minimum)]            = MapMinimum,
            [nameof(IRange.Maximum)]            = MapMaximum,
            [nameof(ISlider.MinimumTrackColor)] = MapMinimumTrackColor,
            [nameof(ISlider.MaximumTrackColor)] = MapMaximumTrackColor,
            [nameof(ISlider.ThumbColor)]        = MapThumbColor,
            // TODO: ISlider.ThumbImageSource — Material 3's Slider draws
            // its thumb as a fixed circle; supplying a custom drawable
            // requires the lower-level Slider(state, ..., thumb = { ... })
            // overload plus piping the resolved Painter through our
            // ImageSourceLoader. Larger surgery than fits this PR.
        };

    /// <summary>Command mapper (inherits view-level commands; no extras).</summary>
    public static CommandMapper<ISlider, SliderHandler> CommandMapper =
        new(ViewCommandMapper);

    readonly MutableState<float> _value             = new(0f);
    readonly MutableState<float> _min               = new(0f);
    readonly MutableState<float> _max               = new(1f);
    readonly MutableState<long?> _thumbColor        = new((long?)null);
    readonly MutableState<long?> _minTrackColor     = new((long?)null);
    readonly MutableState<long?> _maxTrackColor     = new((long?)null);

    /// <summary>Construct a handler with the default mappers.</summary>
    public SliderHandler() : base(Mapper, CommandMapper) { }

    /// <summary>Construct a handler with custom mappers.</summary>
    public SliderHandler(IPropertyMapper? mapper, CommandMapper? commandMapper = null)
        : base(mapper ?? Mapper, commandMapper ?? CommandMapper) { }

    /// <inheritdoc/>
    public override ComposableNode BuildNode(IComposer composer)
    {
        var virtualView = VirtualView
            ?? throw new InvalidOperationException("VirtualView not set on SliderHandler.");

        var min        = _min.Value;
        var max        = _max.Value;
        var thumb      = _thumbColor.Value;
        var minTrack   = _minTrackColor.Value;
        var maxTrack   = _maxTrackColor.Value;

        var slider = new ComposeSlider(_value.Value, OnValueChanged);

        // Only allocate a Kotlin ClosedFloatingPointRange when the
        // bounds aren't Compose's stock [0, 1] — RangeTo always
        // allocates so this is the cheapest skip.
        if (min != 0f || max != 1f)
            slider.ValueRange = RangesKt.RangeTo(min, max);

        // Build SliderColors only if any of the three MAUI slots are
        // populated — otherwise let M3's theme defaults apply.
        if (thumb is not null || minTrack is not null || maxTrack is not null)
            slider.Colors = composer.SliderColors(
                thumbColor:         thumb,
                activeTrackColor:   minTrack,
                inactiveTrackColor: maxTrack);

        slider.PrependModifier(Modifier.FillMaxWidth().ApplyGestures(virtualView, MauiContext).ApplySemantics(virtualView));
        return slider;
    }

    void OnValueChanged(float newValue)
    {
        // Update Compose state synchronously so the rendered position
        // stays pinned where the user just dragged the thumb (Compose
        // snaps `value` back on the next recompose; lagging here causes
        // the thumb to ping-pong). Updating VirtualView.Value triggers
        // MAUI's standard property pipeline (data binding, behaviors)
        // which re-enters MapValue with the same float — that's a no-op
        // on MutableState<float>, so no feedback loop.
        _value.Value = newValue;
        if (VirtualView is { } slider)
            slider.Value = newValue;
    }

    /// <summary>Map <see cref="IRange.Value"/> to the Compose value slot.</summary>
    public static void MapValue(SliderHandler handler, ISlider slider) =>
        handler._value.Value = (float)slider.Value;

    /// <summary>Map <see cref="IRange.Minimum"/> to the Compose <c>valueRange</c> start.</summary>
    public static void MapMinimum(SliderHandler handler, ISlider slider) =>
        handler._min.Value = (float)slider.Minimum;

    /// <summary>Map <see cref="IRange.Maximum"/> to the Compose <c>valueRange</c> end.</summary>
    public static void MapMaximum(SliderHandler handler, ISlider slider) =>
        handler._max.Value = (float)slider.Maximum;

    /// <summary>Map <see cref="ISlider.MinimumTrackColor"/> to <c>SliderColors.activeTrackColor</c>.</summary>
    public static void MapMinimumTrackColor(SliderHandler handler, ISlider slider) =>
        handler._minTrackColor.Value = ColorMapping.ToPackedLong(slider.MinimumTrackColor);

    /// <summary>Map <see cref="ISlider.MaximumTrackColor"/> to <c>SliderColors.inactiveTrackColor</c>.</summary>
    public static void MapMaximumTrackColor(SliderHandler handler, ISlider slider) =>
        handler._maxTrackColor.Value = ColorMapping.ToPackedLong(slider.MaximumTrackColor);

    /// <summary>Map <see cref="ISlider.ThumbColor"/> to <c>SliderColors.thumbColor</c>.</summary>
    public static void MapThumbColor(SliderHandler handler, ISlider slider) =>
        handler._thumbColor.Value = ColorMapping.ToPackedLong(slider.ThumbColor);
}
