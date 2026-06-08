using ComposeNet.Gallery.Registry;

namespace ComposeNet.Gallery.Demos.StateEffectsAnimation;

/// <summary>Enter/Exit transition factories — fade, scale, slide.</summary>
public static class EnterExitTransitionsDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "transitions",
        CategoryId:  "state-effects",
        Title:       "Enter/Exit transitions",
        Description: "Combine Transitions.FadeIn/Out, ScaleIn/Out, and SlideIn/OutVertically with AnimatedVisibility.",
        Build:       () =>
        {
            var visible = Compose.Remember(() => new MutableState<bool>(true));
            return new Column
            {
                new Button(onClick: () => visible.Value = !visible.Value)
                {
                    new Text(visible.Value ? "Hide all" : "Show all"),
                },

                new Text("Fade in / Fade out"),
                new AnimatedVisibility(visible.Value, Transitions.FadeIn(), Transitions.FadeOut())
                {
                    new Card
                    {
                        Modifier.Companion.Padding(8),
                        new Text("I fade"),
                    },
                },

                new Text("Scale in / Scale out"),
                new AnimatedVisibility(visible.Value, Transitions.ScaleIn(), Transitions.ScaleOut())
                {
                    new Card
                    {
                        Modifier.Companion.Padding(8),
                        new Text("I scale"),
                    },
                },

                new Text("Slide in vertically / Slide out vertically"),
                new AnimatedVisibility(visible.Value, Transitions.SlideInVertically(), Transitions.SlideOutVertically())
                {
                    new Card
                    {
                        Modifier.Companion.Padding(8),
                        new Text("I slide vertically"),
                    },
                },

                new Text("Slide in horizontally / Slide out horizontally"),
                new AnimatedVisibility(visible.Value, Transitions.SlideInHorizontally(), Transitions.SlideOutHorizontally())
                {
                    new Card
                    {
                        Modifier.Companion.Padding(8),
                        new Text("I slide horizontally"),
                    },
                },
            };
        });
}
