using AndroidX.Compose.Animation.Core;
using AndroidX.Compose.Material3;
using AndroidX.Compose.Runtime;

namespace AndroidX.Compose.Samples.Jetchat;

// Visual derivations from RecordButton.kt at compose-samples 4c1fe7586e2fbf1c934925ef8ab64d3803361423.
internal sealed class RecordButtonVisuals(
    IState<float> scale, IState<float> alpha, IState<Color> iconColor, Color containerColor)
{
    internal Color IconColor => iconColor.Value;

    internal ComposableNode Background => new Box
    {
        Modifier.MatchParentSize().AspectRatio(1f).Alpha(alpha.Value).Scale(scale.Value)
            .Clip(Shape.Circle()).Background(containerColor),
    };

    internal static RecordButtonVisuals Read(IComposer composer, bool recording)
    {
        var transition = composer.UpdateTransition(recording, "record");
        var spring = composer.Remember(() => AnimationSpecs.Spring(
            Spring.DampingRatioMediumBouncy, Spring.StiffnessLow));
        var fade = composer.Remember(() => AnimationSpecs.Tween(2000));
        var tint = composer.Remember(() => AnimationSpecs.Tween(200));
        var content = composer.Consume(ContentColorKt.LocalContentColor) as UI.Graphics.Color
            ?? throw new InvalidOperationException("Jetchat record button has no native LocalContentColor.");
        var container = Color.FromPacked(unchecked((long)content.Value));
        var foreground = Color.FromPacked(ColorSchemeKt.ContentColorFor(container.ToPacked(), composer, 0));
        var scale = transition.AnimateFloat(composer, value => value ? 2f : 1f, spring, "record-scale");
        var alpha = transition.AnimateFloat(composer, value => value ? 1f : 0f, fade, "record-alpha");
        var icon = transition.AnimateColor(composer, value => value ? foreground : container, tint, "record-icon");
        return new RecordButtonVisuals(scale, alpha, icon, container);
    }
}
