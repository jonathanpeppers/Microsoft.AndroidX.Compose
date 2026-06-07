using AndroidX.Compose.UI.Graphics;
using ComposeNet.Gallery.Registry;

namespace ComposeNet.Gallery.Demos.Modifiers;

/// <summary>Rotate, Scale, and Alpha transform modifiers.</summary>
public static class RotateScaleAlpha
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "modifiers-rotate-scale-alpha",
        CategoryId:  "modifiers",
        Title:       "Rotate, scale, alpha",
        Description: "Static transforms applied via Modifier.Rotate / .Scale / .Alpha; each tile also carries a TestTag for UI tests.",
        Build:       () => new Row
        {
            new Box
            {
                Modifier.Companion
                    .TestTag("rotate-tile")
                    .Size(56)
                    .Rotate(15f)
                    .Background(Color.FromRgb(0xEF, 0xB8, 0xC8)),
                new Text("⟲") { Modifier = Modifier.Companion.Padding(16) },
            },
            new Spacer { Modifier = Modifier.Companion.WidthIn(8, null) },
            new Box
            {
                Modifier.Companion
                    .TestTag("scale-tile")
                    .Size(56)
                    .Scale(0.85f, 1.15f)
                    .Background(Color.FromRgb(0xFF, 0xCD, 0xD2)),
                new Text("↕") { Modifier = Modifier.Companion.Padding(16) },
            },
            new Spacer { Modifier = Modifier.Companion.WidthIn(8, null) },
            new Box
            {
                Modifier.Companion
                    .TestTag("alpha-tile")
                    .Size(56)
                    .Alpha(0.4f)
                    .Background(Color.FromRgb(0xD7, 0xCC, 0xC8)),
                new Text("◐") { Modifier = Modifier.Companion.Padding(16) },
            },
        });
}
