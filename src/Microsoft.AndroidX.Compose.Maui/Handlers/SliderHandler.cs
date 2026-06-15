using AndroidX.Compose;
using AndroidX.Compose.Material3;
using AndroidX.Compose.Runtime;
using AndroidX.Compose.UI.Platform;
using Kotlin.Jvm.Functions;
using Kotlin.Ranges;
using Microsoft.AndroidX.Compose.Maui.Loaders;
using Microsoft.AndroidX.Compose.Maui.Platform;
using Microsoft.Maui.Handlers;
using ComposeImage   = AndroidX.Compose.Image;
using ComposePainter = AndroidX.Compose.UI.Graphics.Painter.Painter;
using ComposeSlider  = AndroidX.Compose.Slider;

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
///
/// <para><see cref="ISlider.ThumbImageSource"/> is resolved through
/// the shared <see cref="ImageSourceLoader"/> (the same helper that
/// backs <see cref="ImageHandler"/>). When a thumb image is supplied
/// the handler swaps to a hand-written
/// <see cref="ComposableNode"/> that calls the lower-level
/// <c>SliderKt.Slider(..., thumb = { ... })</c> overload directly with
/// an <see cref="ComposeImage"/> thumb slot — Material 3's stock
/// <c>Slider</c> draws its thumb as a fixed circle and has no public
/// way to inject a custom drawable.</para>
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
            [nameof(ISlider.ThumbImageSource)]  = MapThumbImageSource,
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
    ImageSourceLoader?           _thumbLoader;

    // Lazy — sliders without ThumbImageSource set never allocate the
    // loader or its IImageSourcePart adapter.
    ImageSourceLoader ThumbLoader =>
        _thumbLoader ??= new ImageSourceLoader(
            this,
            () => VirtualView is ISlider s ? new SliderThumbImageSourcePart(s) : null);

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

        // When the consumer set a ThumbImageSource and it resolved to
        // either a packaged drawable id or a runtime-decoded
        // BitmapPainter, drop into the hand-written thumb-slot path
        // so the image actually paints in the thumb slot.
        if (_thumbLoader is { } loader &&
            (loader.Painter.Value is not null || loader.DrawableResourceId.Value is not null))
        {
            return new ImageThumbSlider(
                this, virtualView, _value.Value, min, max, thumb, minTrack, maxTrack,
                loader.DrawableResourceId.Value, loader.Painter.Value);
        }

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

    /// <inheritdoc/>
    protected override void DisconnectHandler(ComposeView platformView)
    {
        _thumbLoader?.Reset();
        base.DisconnectHandler(platformView);
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

    /// <summary>
    /// Map <see cref="ISlider.ThumbImageSource"/> through the shared
    /// <see cref="ImageSourceLoader"/>. When the loader resolves a
    /// painter or drawable id, <see cref="BuildNode"/> swaps to the
    /// hand-written <see cref="ImageThumbSlider"/> render path; while
    /// the load is in flight (or the source is null) the default
    /// <see cref="ComposeSlider"/> facade keeps drawing the Material
    /// stock thumb circle.
    /// </summary>
    /// <remarks>
    /// Declared <c>async void</c> deliberately — mirrors
    /// <see cref="ImageHandler.MapSource"/>; see that mapper's remarks
    /// for the fire-and-forget rationale.
    /// </remarks>
    public static async void MapThumbImageSource(SliderHandler handler, ISlider slider) =>
        await handler.ThumbLoader.LoadAsync(slider.ThumbImageSource).ConfigureAwait(false);

    /// <summary>
    /// Adapter exposing the slider's <see cref="ISlider.ThumbImageSource"/>
    /// to <see cref="ImageSourceLoader"/> as an
    /// <see cref="IImageSourcePart"/>. <see cref="ISlider"/> itself
    /// doesn't implement <see cref="IImageSourcePart"/> (only the
    /// stand-alone <see cref="Microsoft.Maui.IImage"/> /
    /// <see cref="IImageButton"/> virtual views do), so we wrap it.
    /// </summary>
    sealed class SliderThumbImageSourcePart : IImageSourcePart
    {
        readonly ISlider _slider;
        public SliderThumbImageSourcePart(ISlider slider) => _slider = slider;

        public IImageSource? Source           => _slider.ThumbImageSource;
        public bool          IsAnimationPlaying => false;
        public bool          IsLoading          => false;

        public void UpdateIsLoading(bool isLoading) { /* no-op */ }
    }

    /// <summary>
    /// Hand-written <see cref="ComposableNode"/> that drives the
    /// <c>SliderKt.Slider(value, onValueChange, ..., thumb)</c>
    /// overload directly so the thumb slot can render a caller-supplied
    /// <see cref="ISlider.ThumbImageSource"/>. Lives here (not as a
    /// generated facade) because no current <c>[ComposeFacade]</c>
    /// shape models "swap the thumb slot with an
    /// <see cref="ComposeImage"/> built from a runtime-resolved
    /// painter or drawable resource id".
    /// </summary>
    sealed class ImageThumbSlider : ComposableNode
    {
        // SliderKt.Slider(value, onValueChange, modifier, enabled,
        //                 onValueChangeFinished, colors, interactionSource,
        //                 steps, thumb, track, valueRange,
        //                 _composer, $changed, $changed1, $default)
        // — $default mask bits 0..10, one per user param above (skipping
        // _composer and the two $changed slots).
        const int BitEnabled               = 1 << 3;
        const int BitOnValueChangeFinished = 1 << 4;
        const int BitColors                = 1 << 5;
        const int BitInteractionSource     = 1 << 6;
        const int BitSteps                 = 1 << 7;
        const int BitTrack                 = 1 << 9;
        const int BitValueRange            = 1 << 10;

        // Compose's stock thumb is a 20-dp filled circle; sizing the
        // image slightly bigger keeps the source bitmap visible without
        // overflowing the slider's intrinsic height.
        static readonly Modifier s_thumbSize = Modifier.Size(40);

        readonly SliderHandler   _owner;
        readonly ISlider         _virtualView;
        readonly float           _value;
        readonly float           _min;
        readonly float           _max;
        readonly long?           _thumbColor;
        readonly long?           _minTrack;
        readonly long?           _maxTrack;
        readonly int?            _drawableId;
        readonly ComposePainter? _painter;

        public ImageThumbSlider(
            SliderHandler   owner,
            ISlider         virtualView,
            float           value,
            float           min,
            float           max,
            long?           thumbColor,
            long?           minTrack,
            long?           maxTrack,
            int?            drawableId,
            ComposePainter? painter)
        {
            _owner       = owner;
            _virtualView = virtualView;
            _value       = value;
            _min         = min;
            _max         = max;
            _thumbColor  = thumbColor;
            _minTrack    = minTrack;
            _maxTrack    = maxTrack;
            _drawableId  = drawableId;
            _painter     = painter;
        }

        public override void Render(IComposer composer)
        {
            var onValueChange = new ComposableLambda1(boxed =>
            {
                var f = boxed as Java.Lang.Float
                    ?? throw new InvalidCastException(
                        $"Expected java.lang.Float in Slider.onValueChange; got '{boxed?.Class?.Name ?? "null"}'.");
                _owner.OnValueChanged(f.FloatValue());
            });

            // Thumb slot — Painter wins over drawable id (matches
            // ImageHandler / ImageButtonHandler), so a freshly-loaded
            // BitmapPainter immediately replaces any stale fast-path
            // resource id.
            var thumbNode = _painter is { } p
                ? new ComposeImage(p) { Modifier = s_thumbSize }
                : _drawableId is int id
                    ? new ComposeImage(id) { Modifier = s_thumbSize }
                    : (ComposableNode)new Box { Modifier = s_thumbSize };
            IFunction3 thumb = ComposableLambdas.Wrap3(
                composer,
                c => thumbNode.Render(c));

            SliderColors? colors = null;
            if (_thumbColor is not null || _minTrack is not null || _maxTrack is not null)
                colors = composer.SliderColors(
                    thumbColor:         _thumbColor,
                    activeTrackColor:   _minTrack,
                    inactiveTrackColor: _maxTrack);

            IClosedFloatingPointRange? valueRange = null;
            if (_min != 0f || _max != 1f)
                valueRange = RangesKt.RangeTo(_min, _max);

            var modifier = Modifier.FillMaxWidth()
                .ApplyGestures(_virtualView, _owner.MauiContext)
                .ApplySemantics(_virtualView);

            // $default mask — set a bit for every slot we leave at its
            // Kotlin default (null / 0 / stock theme). value, onValueChange,
            // modifier and thumb are always supplied so their bits stay
            // cleared. enabled is supplied explicitly so bit 3 clears.
            int defaultMask =
                BitOnValueChangeFinished |
                BitInteractionSource |
                BitSteps |
                BitTrack;
            if (colors is null)     defaultMask |= BitColors;
            if (valueRange is null) defaultMask |= BitValueRange;

            SliderKt.Slider(
                value:                  _value,
                onValueChange:          onValueChange,
                modifier:               modifier.Build(),
                enabled:                true,
                onValueChangeFinished:  null,
                colors:                 colors,
                interactionSource:      null,
                p7:                     0,
                thumb:                  thumb,
                track:                  null,
                valueRange:             valueRange,
                _composer:              composer,
                steps:                  0,
                _changed:               0,
                _changed1:              defaultMask);
        }
    }
}
