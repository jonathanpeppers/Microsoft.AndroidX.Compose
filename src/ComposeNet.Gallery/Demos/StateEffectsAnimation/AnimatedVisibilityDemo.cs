using ComposeNet.Gallery.Registry;

namespace ComposeNet.Gallery.Demos.StateEffectsAnimation;

/// <summary>AnimatedVisibility — fade and expand a child in/out as a bool toggles.</summary>
public static class AnimatedVisibilityDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "anim-visibility",
        CategoryId:  "state",
        Title:       "AnimatedVisibility",
        Description: "Toggle a card in and out. Compose applies a default fade + expand transition.",
        Build:       () =>
        {
            var visible = Compose.Remember(() => new MutableState<bool>(true));
            return new Column
            {
                new Button(onClick: () => visible.Value = !visible.Value)
                {
                    new Text(visible.Value ? "Hide" : "Show"),
                },
                new AnimatedVisibility(visible.Value)
                {
                    new Card
                    {
                        Modifier.Companion.Padding(8),
                        new Text("👋 I fade in and out"),
                    },
                },
            };
        });
}
