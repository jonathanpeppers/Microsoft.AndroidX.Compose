using AndroidX.Compose.Material3;
using AndroidX.Compose.Runtime;

namespace AndroidX.Compose;

/// <summary>
/// Material 3 <c>CircularProgressIndicator</c>. Defaults to the
/// indeterminate animation; set <see cref="Progress"/> to render a
/// determinate indicator:
/// <code>
/// new CircularProgressIndicator()
/// new CircularProgressIndicator { Progress = 0.45f }
/// </code>
/// </summary>
public sealed class CircularProgressIndicator : ComposableNode
{
    FloatFunction0? _progressFunction;

    /// <summary>
    /// Optional progress fraction. When non-null, renders the determinate
    /// overload; when null, renders the indeterminate animation. Compose
    /// coerces finite values to the 0..1 range.
    /// </summary>
    public float? Progress { get; set; }

    /// <summary>Optional explicit color. Leave null to use the Material default.</summary>
    public Color? Color { get; set; }

    /// <summary>Optional stroke width. Leave null for the Material default.</summary>
    public Dp? StrokeWidthDp { get; set; }

    /// <summary>Optional explicit track color. Leave null to use the Material default.</summary>
    public Color? TrackColor { get; set; }

    public override void Render(IComposer composer)
    {
        var modifier = BuildModifier();

        if (Progress is { } progress)
        {
            int determinateDefaults = (int)CircularProgressIndicatorDeterminateDefault.All;
            if (modifier is not null)        determinateDefaults &= ~(int)CircularProgressIndicatorDeterminateDefault.Modifier;
            if (Color.HasValue)              determinateDefaults &= ~(int)CircularProgressIndicatorDeterminateDefault.Color;
            if (StrokeWidthDp.HasValue)      determinateDefaults &= ~(int)CircularProgressIndicatorDeterminateDefault.StrokeWidth;
            if (TrackColor.HasValue)         determinateDefaults &= ~(int)CircularProgressIndicatorDeterminateDefault.TrackColor;

            var progressFunction = _progressFunction ??=
                new FloatFunction0(() => Progress ?? 0f);
            int changed = composer.DiffSlot(
                progress,
                ComposeExtensions.DiffSlotShift(0));

            ProgressIndicatorKt.CircularProgressIndicator(
                progress:    progressFunction,
                modifier:    modifier,
                color:       Color      is { } pc ? pc.ToPacked() : 0L,
                strokeWidth: Dp.Pack(StrokeWidthDp),
                trackColor:  TrackColor is { } pt ? pt.ToPacked() : 0L,
                p5:          0,
                gapSize:     0f,
                _composer:   composer,
                strokeCap:   changed,
                _changed:    determinateDefaults);
            return;
        }

        int defaults = (int)CircularProgressIndicatorDefault.All;
        if (modifier is not null)        defaults &= ~(int)CircularProgressIndicatorDefault.Modifier;
        if (Color.HasValue)              defaults &= ~(int)CircularProgressIndicatorDefault.Color;
        if (StrokeWidthDp.HasValue)      defaults &= ~(int)CircularProgressIndicatorDefault.StrokeWidth;
        if (TrackColor.HasValue)         defaults &= ~(int)CircularProgressIndicatorDefault.TrackColor;

        ProgressIndicatorKt.CircularProgressIndicator(
            modifier:    modifier,
            color:       Color      is { } c ? c.ToPacked() : 0L,
            strokeWidth: Dp.Pack(StrokeWidthDp),
            trackColor:  TrackColor is { } t ? t.ToPacked() : 0L,
            p4:          0,
            gapSize:     0f,
            _composer:   composer,
            strokeCap:   0,
            _changed:    defaults);
    }
}
