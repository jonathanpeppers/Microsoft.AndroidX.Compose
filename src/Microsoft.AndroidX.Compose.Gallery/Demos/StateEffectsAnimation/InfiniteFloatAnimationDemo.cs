using AndroidX.Compose.Animation.Core;
using AndroidX.Compose.Gallery.Registry;

namespace AndroidX.Compose.Gallery.Demos.StateEffectsAnimation;

/// <summary>Native composition-owned infinite float animation with reverse repeat timing.</summary>
public static class InfiniteFloatAnimationDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id: "infinite-float-animation",
        CategoryId: "state-effects",
        Title: "Infinite float animation",
        Description: "A native 2000 ms tween reverses at each endpoint and stops when removed from composition.",
        Build: c =>
        {
            var visible = c.MutableStateOf(true);
            var transition = c.RememberInfiniteTransition("gallery-infinite-float");
            var spec = c.Remember(() => AnimationSpecs.InfiniteRepeatable(
                AnimationSpecs.Tween(2000, easing: EasingKt.LinearEasing), RepeatMode.Reverse));
            float scale = visible.Value
                ? transition.AnimateFloat(c, 1f, 0.2f, spec, "scale").Value
                : 1f;
            return new Column
            {
                new Button(() => visible.Value = !visible.Value)
                {
                    new Text(visible.Value ? "Remove animation" : "Re-add animation"),
                },
                new Text(visible.Value
                    ? $"Native scale: {scale:F2}"
                    : "Animation call removed; native frame work is cancelled."),
                visible.Value
                    ? new Box
                    {
                        Modifier.Height(160).FillMaxWidth(),
                        new Box
                        {
                            Modifier.Align(Alignment.Center).Size(72).Scale(scale)
                                .Background(Color.Red, Shape.Circle()),
                        },
                    }
                    : null,
                new Text("Each direction takes 2000 ms; reduced or disabled system animations use Compose policy."),
            };
        });
}
