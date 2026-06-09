using global::AndroidX.Compose.Material3;
using global::AndroidX.Compose.Runtime;

namespace Microsoft.AndroidX.Compose;

/// <summary>
/// Material 3 indeterminate <c>LinearProgressIndicator</c>. Use to show
/// loading state when the duration is unknown:
/// <code>
/// new LinearProgressIndicator { Modifier = Modifier.Companion.FillMaxWidth() }
/// </code>
/// The determinate (progress-driven) overload is not yet wrapped — the
/// progress callback parameter requires a state-reading <c>Function0&lt;Float&gt;</c>
/// adapter that hasn't been added yet.
/// </summary>
public sealed class LinearProgressIndicator : ComposableNode
{
    /// <summary>Optional ARGB color (packed Compose <c>Color</c> long). Leave null for the Material default.</summary>
    public long? ColorArgb { get; set; }

    /// <summary>Optional track ARGB color (packed Compose <c>Color</c> long). Leave null for the Material default.</summary>
    public long? TrackColorArgb { get; set; }

    public override void Render(IComposer composer)
    {
        var modifier = BuildModifier();

        int defaults = (int)LinearProgressIndicatorDefault.All;
        if (modifier is not null)        defaults &= ~(int)LinearProgressIndicatorDefault.Modifier;
        if (ColorArgb.HasValue)          defaults &= ~(int)LinearProgressIndicatorDefault.Color;
        if (TrackColorArgb.HasValue)     defaults &= ~(int)LinearProgressIndicatorDefault.TrackColor;

        ProgressIndicatorKt.LinearProgressIndicator(
            modifier:   modifier,
            color:      ColorArgb       ?? 0L,
            trackColor: TrackColorArgb  ?? 0L,
            p3:         0,
            gapSize:    0f,
            _composer:  composer,
            strokeCap:  0,
            _changed:   defaults);
    }
}
