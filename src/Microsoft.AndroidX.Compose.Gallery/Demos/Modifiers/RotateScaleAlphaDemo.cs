using AndroidX.Compose.Gallery.Registry;

namespace AndroidX.Compose.Gallery.Demos.Modifiers;

/// <summary>Rotate, Scale, and Alpha transform modifiers.</summary>
public static class RotateScaleAlphaDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "modifiers-rotate-scale-alpha",
        CategoryId:  "modifiers",
        Title:       "Rotate, scale, alpha",
        Description: "Static transforms applied via Modifier.Rotate / .Scale / .Alpha; each tile also carries a TestTag for UI tests.",
        Build:       _ => new Column
        {
            new Row(horizontalArrangement: Arrangement.SpacedBy(16.Dp()))
            {
                new Column
                {
                    new Box
                    {
                        Modifier.Companion
                            .TestTag("rotate-tile")
                            .Size(56)
                            .Rotate(15f)
                            .Background(Color.FromRgb(0xEF, 0xB8, 0xC8)),
                        new Text("⟲") { Color = Color.Black, Modifier = Modifier.Companion.Padding(16) },
                    },
                    new Text("Rotate 15°"),
                },
                new Column
                {
                    new Box
                    {
                        Modifier.Companion
                            .TestTag("scale-tile")
                            .Size(56)
                            .Scale(0.85f, 1.15f)
                            .Background(Color.FromRgb(0xFF, 0xCD, 0xD2)),
                        new Text("↕") { Color = Color.Black, Modifier = Modifier.Companion.Padding(16) },
                    },
                    new Text("Scale 0.85×1.15"),
                },
                new Column
                {
                    new Box
                    {
                        Modifier.Companion
                            .TestTag("alpha-tile")
                            .Size(56)
                            .Alpha(0.4f)
                            .Background(Color.FromRgb(0xD7, 0xCC, 0xC8)),
                        new Text("◐") { Color = Color.Black, Modifier = Modifier.Companion.Padding(16) },
                    },
                    new Text("Alpha 0.4"),
                },
            },
        });
}
