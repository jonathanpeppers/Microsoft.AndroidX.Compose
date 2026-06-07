using ComposeNet.Gallery.Registry;

namespace ComposeNet.Gallery.Demos.StateEffectsAnimation;

/// <summary>AnimatedContent&lt;T&gt; — Material 3's richer counterpart to Crossfade.</summary>
public static class AnimatedContentDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "anim-animated-content",
        CategoryId:  "state",
        Title:       "AnimatedContent<int>",
        Description: "Like Crossfade, but with size + slide animation in addition to the fade.",
        Build:       () =>
        {
            var step = Compose.Remember(() => new MutableNumberState<int>(0));
            return new Column
            {
                new Row(horizontalArrangement: Arrangement.SpacedBy(8))
                {
                    new Button(onClick: () => step--) { new Text("−") },
                    new Button(onClick: () => step++) { new Text("+") },
                },
                new AnimatedContent<int>(
                    targetState: step.Value,
                    content:     i => new Card
                    {
                        Modifier.Companion.Padding(8),
                        new Text($"Frame {i}"),
                    }),
            };
        });
}
