using AndroidX.Compose.UI.Graphics;
using ComposeNet.Gallery.Registry;

namespace ComposeNet.Gallery.Demos.Modifiers;

/// <summary>Background shapes, shadow, and border modifiers on shared shapes.</summary>
public static class ShapesAndShadow
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "modifiers-shapes-shadow",
        CategoryId:  "modifiers",
        Title:       "Shapes & shadow",
        Description: "Background(color, Shape) for Circle / RoundedPercent / CutCorners, plus shadow + border on a shared shape.",
        Build:       () => new Column
        {
            new Text("Background + Shape:"),
            new Row
            {
                new Box
                {
                    Modifier.Companion.Size(56).Background(Color.FromRgb(0xD0, 0xBC, 0xFF), Shape.Circle()),
                    new Text("●") { Modifier = Modifier.Companion.Padding(16) },
                },
                new Spacer { Modifier = Modifier.Companion.WidthIn(8, null) },
                new Box
                {
                    Modifier.Companion.Size(56).Background(Color.FromRgb(0xB3, 0xE5, 0xFC), Shape.RoundedPercent(25)),
                    new Text("◼") { Modifier = Modifier.Companion.Padding(16) },
                },
                new Spacer { Modifier = Modifier.Companion.WidthIn(8, null) },
                new Box
                {
                    Modifier.Companion.Size(56).Background(Color.FromRgb(0xC8, 0xE6, 0xC9), Shape.CutCorners(10)),
                    new Text("◆") { Modifier = Modifier.Companion.Padding(16) },
                },
            },
            new Text("Shadow + Border + Background (shared shape):"),
            new Box
            {
                Modifier.Companion
                    .Padding(8)
                    .Shadow(8, Shape.RoundedCorners(16))
                    .Background(Color.FromRgb(0xFF, 0xE0, 0xB2), Shape.RoundedCorners(16))
                    .Border(2, Color.FromRgb(0xEF, 0x6C, 0x00), Shape.RoundedCorners(16))
                    .Padding(16),
                new Text("Shadow + Border + Background on Shape.RoundedCorners(16)")
                {
                    Color = Color.Black,
                },
            },
            new Text("AspectRatio (16:9, height-first):"),
            new Box
            {
                Modifier.Companion
                    .FillMaxWidth()
                    .Height(80)
                    .AspectRatio(16f / 9f, matchHeightConstraintsFirst: true)
                    .Background(Color.FromRgb(0xCC, 0xC2, 0xDC)),
                new Text("16:9 (height-first)")
                {
                    Color    = Color.Black,
                    Modifier = Modifier.Companion.Padding(8),
                },
            },
        });
}
