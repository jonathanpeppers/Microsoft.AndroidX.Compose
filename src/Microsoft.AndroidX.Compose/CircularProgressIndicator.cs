using AndroidX.Compose.Material3;
using AndroidX.Compose.Runtime;

namespace AndroidX.Compose;

/// <summary>
/// Material 3 indeterminate <c>CircularProgressIndicator</c>. Use to show
/// loading state when the duration is unknown:
/// <code>
/// new CircularProgressIndicator()
/// </code>
/// The determinate (progress-driven) overload is not yet wrapped — the
/// progress callback parameter requires a state-reading <c>Function0&lt;Float&gt;</c>
/// adapter that hasn't been added yet.
/// </summary>
public sealed class CircularProgressIndicator : ComposableNode
{
    /// <summary>Optional ARGB color (packed Compose <c>Color</c> long). Leave null for the Material default.</summary>
    public long? ColorArgb { get; set; }

    /// <summary>Optional stroke width in Dp. Leave null for the Material default.</summary>
    public float? StrokeWidthDp { get; set; }

    /// <summary>Optional track ARGB color (packed Compose <c>Color</c> long). Leave null for the Material default.</summary>
    public long? TrackColorArgb { get; set; }

    public override void Render(IComposer composer)
    {
        var modifier = BuildModifier();

        int defaults = (int)CircularProgressIndicatorDefault.All;
        if (modifier is not null)        defaults &= ~(int)CircularProgressIndicatorDefault.Modifier;
        if (ColorArgb.HasValue)          defaults &= ~(int)CircularProgressIndicatorDefault.Color;
        if (StrokeWidthDp.HasValue)      defaults &= ~(int)CircularProgressIndicatorDefault.StrokeWidth;
        if (TrackColorArgb.HasValue)     defaults &= ~(int)CircularProgressIndicatorDefault.TrackColor;

        ProgressIndicatorKt.CircularProgressIndicator(
            modifier:    modifier,
            color:       ColorArgb       ?? 0L,
            strokeWidth: StrokeWidthDp   ?? 0f,
            trackColor:  TrackColorArgb  ?? 0L,
            p4:          0,
            gapSize:     0f,
            _composer:   composer,
            strokeCap:   0,
            _changed:    defaults);
    }
}
