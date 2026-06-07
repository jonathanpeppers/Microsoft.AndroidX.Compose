using AndroidX.Compose.UI.Graphics;
using ComposeNet.Gallery.Registry;

namespace ComposeNet.Gallery.Demos.StateEffectsAnimation;

/// <summary>AnimatedContent&lt;T&gt; — Material 3's richer counterpart to Crossfade.</summary>
public static class AnimatedContentDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "anim-animated-content",
        CategoryId:  "state-effects",
        Title:       "AnimatedContent<int>",
        Description: "Like Crossfade, but with a slide and size change in addition to the fade. Each step uses a different colour so the transition is visible.",
        Build:       () =>
        {
            var step = Compose.Remember(() => new MutableNumberState<int>(0));
            ComposeNet.Color[] palette =
            [
                ComposeNet.Color.FromRgb(0xB3, 0xE5, 0xFC),
                ComposeNet.Color.FromRgb(0xC8, 0xE6, 0xC9),
                ComposeNet.Color.FromRgb(0xFF, 0xE0, 0xB2),
                ComposeNet.Color.FromRgb(0xF8, 0xBB, 0xD0),
                ComposeNet.Color.FromRgb(0xD1, 0xC4, 0xE9),
            ];
            return new Column
            {
                new Row(horizontalArrangement: Arrangement.SpacedBy(8))
                {
                    new Button(onClick: () => step--) { new Text("−") },
                    new Button(onClick: () => step++) { new Text("+") },
                },
                new AnimatedContent<int>(
                    targetState: step.Value,
                    content:     i => new Box
                    {
                        Modifier.Companion
                            .FillMaxWidth()
                            .Height(96)
                            .Background(palette[((i % palette.Length) + palette.Length) % palette.Length]),
                        new Text($"Frame {i}")
                        {
                            Color    = ComposeNet.Color.Black,
                            Modifier = Modifier.Companion.Padding(16),
                        },
                    }),
            };
        });
}
