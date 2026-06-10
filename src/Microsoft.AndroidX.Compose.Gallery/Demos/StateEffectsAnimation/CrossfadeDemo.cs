using AndroidX.Compose.Gallery.Registry;

namespace AndroidX.Compose.Gallery.Demos.StateEffectsAnimation;

/// <summary>Crossfade&lt;T&gt; — cross-dissolve between content keyed on a value.</summary>
public static class CrossfadeDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "anim-crossfade",
        CategoryId:  "state-effects",
        Title:       "Crossfade<int>",
        Description: "Whenever the int changes, the previous panel fades out as the new one fades in. Each step uses a different colour so the cross-dissolve is visible.",
        Build:       c =>
        {
            var step = c.Remember(() => new MutableNumberState<int>(0));
            Color[] palette =
            [
                Color.FromRgb(0xEF, 0x9A, 0x9A),
                Color.FromRgb(0x90, 0xCA, 0xF9),
                Color.FromRgb(0xA5, 0xD6, 0xA7),
                Color.FromRgb(0xFF, 0xCC, 0x80),
                Color.FromRgb(0xCE, 0x93, 0xD8),
            ];
            return new Column
            {
                new Row(horizontalArrangement: Arrangement.SpacedBy(8.Dp()))
                {
                    new Button(onClick: () => step--) { new Text("−") },
                    new Button(onClick: () => step++) { new Text("+") },
                },
                new Crossfade<int>(
                    targetState: step.Value,
                    content:     i => new Box
                    {
                        Modifier.Companion
                            .FillMaxWidth()
                            .Height(96)
                            .Background(palette[((i % palette.Length) + palette.Length) % palette.Length]),
                        new Text($"Step {i}")
                        {
                            Color    = Color.Black,
                            Modifier = Modifier.Companion.Padding(16),
                        },
                    }),
            };
        });
}
