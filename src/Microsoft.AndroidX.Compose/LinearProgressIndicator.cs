using AndroidX.Compose.Material3;
using AndroidX.Compose.Runtime;

namespace AndroidX.Compose;

/// <summary>
/// Material 3 <c>LinearProgressIndicator</c>. Defaults to the
/// indeterminate animation; set <see cref="Progress"/> to a non-null
/// value to render the determinate (progress-driven) overload:
/// <code>
/// new LinearProgressIndicator { Modifier = Modifier.FillMaxWidth() }
/// new LinearProgressIndicator { Progress = 0.45f }
/// </code>
/// </summary>
public sealed class LinearProgressIndicator : ComposableNode
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

    /// <summary>Optional explicit track color. Leave null to use the Material default.</summary>
    public Color? TrackColor { get; set; }

    public override void Render(IComposer composer)
    {
        var modifier = BuildModifier();

        if (Progress is { } progress)
        {
            var defaults = LinearProgressIndicatorDeterminateDefault.All;
            if (modifier is not null)        defaults &= ~LinearProgressIndicatorDeterminateDefault.Modifier;
            if (Color.HasValue)              defaults &= ~LinearProgressIndicatorDeterminateDefault.Color;
            if (TrackColor.HasValue)         defaults &= ~LinearProgressIndicatorDeterminateDefault.TrackColor;

            var progressFunction = _progressFunction ??=
                new FloatFunction0(() => Progress ?? 0f);
            int changed = composer.DiffSlot(
                progress,
                ComposeExtensions.DiffSlotShift(0));

            ProgressIndicatorKt.LinearProgressIndicator(
                progress:          progressFunction,
                modifier:          modifier,
                color:             Color      is { } pc ? pc.ToPacked() : 0L,
                trackColor:        TrackColor is { } pt ? pt.ToPacked() : 0L,
                p4:                0,
                gapSize:           0f,
                drawStopIndicator: null,
                _composer:         composer,
                strokeCap:         changed,
                _changed:          (int)defaults);
            return;
        }

        var indeterminateDefaults = LinearProgressIndicatorDefault.All;
        if (modifier is not null)        indeterminateDefaults &= ~LinearProgressIndicatorDefault.Modifier;
        if (Color.HasValue)              indeterminateDefaults &= ~LinearProgressIndicatorDefault.Color;
        if (TrackColor.HasValue)         indeterminateDefaults &= ~LinearProgressIndicatorDefault.TrackColor;

        ProgressIndicatorKt.LinearProgressIndicator(
            modifier:   modifier,
            color:      Color      is { } c ? c.ToPacked() : 0L,
            trackColor: TrackColor is { } t ? t.ToPacked() : 0L,
            p3:         0,
            gapSize:    0f,
            _composer:  composer,
            strokeCap:  0,
            _changed:   (int)indeterminateDefaults);
    }
}
