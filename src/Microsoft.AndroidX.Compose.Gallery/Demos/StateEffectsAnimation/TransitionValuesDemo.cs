using AndroidX.Compose.Animation.Core;
using AndroidX.Compose.Gallery.Registry;

namespace AndroidX.Compose.Gallery.Demos.StateEffectsAnimation;

/// <summary>One typed target drives a spring scale, tween alpha, and tween color together.</summary>
public static class TransitionValuesDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id: "transition-values",
        CategoryId: "state-effects",
        Title: "Synchronized transition values",
        Description: "Toggle while running to interrupt a native spring and two tweens. Idle comes from the transition.",
        Build: c =>
        {
            var active = c.MutableStateOf(false);
            var spring = c.Remember(() => AnimationSpecs.Spring(Spring.DampingRatioMediumBouncy, Spring.StiffnessLow));
            var fade = c.Remember(() => AnimationSpecs.Tween(2000));
            var tint = c.Remember(() => AnimationSpecs.Tween(200));
            var transition = c.UpdateTransition(active.Value, "gallery-record");
            var scale = transition.AnimateFloat(c, recording => recording ? 2f : 1f, spring, "scale");
            var alpha = transition.AnimateFloat(c, recording => recording ? 1f : 0f, fade, "alpha");
            var color = transition.AnimateColor(c, recording => recording ? Color.Blue : Color.Red, tint, "color");
            return new Column
            {
                new Button(() => active.Value = !active.Value) { new Text(active.Value ? "Stop" : "Record") },
                new Text($"Target: {transition.TargetState}; native {(transition.IsIdle ? "idle" : "animating")}"),
                new Text($"Spring scale: {scale.Value:F2}; tween alpha: {alpha.Value:F2}"),
                new Box
                {
                    Modifier.Height(160).FillMaxWidth(),
                    new Box
                    {
                        Modifier.Align(Alignment.Center).Size(56).Scale(scale.Value)
                            .Alpha(alpha.Value).Background(Color.FromHex("#BBDEFB"), Shape.Circle()),
                    },
                    new Text("REC")
                    {
                        Modifier = Modifier.Align(Alignment.Center),
                        Color = color.Value,
                    },
                },
                new Text("All values share one transition. Stop before alpha completes to see native interruption."),
            };
        });
}
