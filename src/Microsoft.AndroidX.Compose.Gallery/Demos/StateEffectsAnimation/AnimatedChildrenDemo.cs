using AndroidX.Compose.Animation.Core;
using AndroidX.Compose.Gallery.Registry;
using static AndroidX.Compose.Composables;

namespace AndroidX.Compose.Gallery.Demos.StateEffectsAnimation;

/// <summary>Independent child transitions inside both animated content scopes.</summary>
public static class AnimatedChildrenDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id: "anim-children",
        CategoryId: "state-effects",
        Title: "Parent and child transitions",
        Description: "The parent fades quickly; the child scales slowly. Includes tree, composerless, and AnimatedContent children.",
        Build: c =>
        {
            var visible = c.MutableStateOf(true);
            var step = c.MutableStateOf(0);
            var fast = AnimationSpecKt.Tween(180, 0, EasingKt.LinearEasing);
            var slow = AnimationSpecKt.Tween(900, 0, EasingKt.LinearEasing);
            return new Column
            {
                Modifier.Padding(8),
                new Button(() => visible.Value = !visible.Value) { new Text("Toggle visibility") },
                new Text("Tree: parent fade + independent child scale"),
                new AnimatedVisibility(visible.Value,
                    Transitions.FadeIn(initialAlpha: 0.65f, animationSpec: fast),
                    Transitions.FadeOut(targetAlpha: 0.65f, animationSpec: fast))
                {
                    new Column
                    {
                        new Text("Parent transition only"),
                        new Text("Child scales over 900 ms")
                        {
                            Modifier = Modifier.AnimateEnterExit(
                                Transitions.ScaleIn(0.2f, slow), Transitions.ScaleOut(0.2f, slow))
                                .Padding(16),
                        },
                    },
                },
                new Text("Composerless: default child fade"),
                new Composed(_ =>
                {
                    DirectChild(visible.Value);
                    return null;
                }),
                new Button(() => step.Value++) { new Text("Next AnimatedContent state") },
                new AnimatedContent<int>(step.Value, value => new Text($"State {value}: independent child scale")
                {
                    Modifier = Modifier.AnimateEnterExit(
                        Transitions.ScaleIn(0.2f, slow), Transitions.ScaleOut(0.2f, slow))
                        .Padding(16),
                }),
            };
        });

    [Composable]
    internal static void DirectChild(bool visible) =>
        AnimatedVisibility(visible, () =>
            Box(() => Text("Default child fade inside a Box", modifier: Modifier.AnimateEnterExit().Padding(16))));
}
