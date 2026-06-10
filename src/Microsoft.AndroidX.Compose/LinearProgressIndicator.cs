using AndroidX.Compose.Material3;
using AndroidX.Compose.Runtime;

namespace AndroidX.Compose;

/// <summary>
/// Material 3 indeterminate <c>LinearProgressIndicator</c>. Use to show
/// loading state when the duration is unknown:
/// <code>
/// new LinearProgressIndicator { Modifier = Modifier.FillMaxWidth() }
/// </code>
/// The determinate (progress-driven) overload is not yet wrapped — the
/// progress callback parameter requires a state-reading <c>Function0&lt;Float&gt;</c>
/// adapter that hasn't been added yet.
/// </summary>
public sealed class LinearProgressIndicator : ComposableNode
{
    /// <summary>Optional explicit color. Leave null to use the Material default.</summary>
    public Color? Color { get; set; }

    /// <summary>Optional explicit track color. Leave null to use the Material default.</summary>
    public Color? TrackColor { get; set; }

    public override void Render(IComposer composer)
    {
        var modifier = BuildModifier();

        int defaults = (int)LinearProgressIndicatorDefault.All;
        if (modifier is not null)        defaults &= ~(int)LinearProgressIndicatorDefault.Modifier;
        if (Color.HasValue)              defaults &= ~(int)LinearProgressIndicatorDefault.Color;
        if (TrackColor.HasValue)         defaults &= ~(int)LinearProgressIndicatorDefault.TrackColor;

        ProgressIndicatorKt.LinearProgressIndicator(
            modifier:   modifier,
            color:      Color      is { } c ? c : 0L,
            trackColor: TrackColor is { } t ? t : 0L,
            p3:         0,
            gapSize:    0f,
            _composer:  composer,
            strokeCap:  0,
            _changed:   defaults);
    }
}
