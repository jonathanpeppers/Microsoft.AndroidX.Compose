using Microsoft.AndroidX.Compose.Gallery.Registry;

namespace Microsoft.AndroidX.Compose.Gallery.Demos.Theming;

/// <summary>
/// Overrides the M3 shape scale via <c>MaterialTheme.BuildShapes(...)</c>
/// and shows <c>Card</c> and other M3 components picking up the new
/// per-slot shapes.
/// </summary>
public static class CustomShapesDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "theming-shapes",
        CategoryId:  "theming",
        Title:       "Custom Shapes",
        Description: "MaterialTheme.BuildShapes(extraSmall, small, medium, large, extraLarge) — the 5 M3 shape slots. Cards pull from medium, FAB pulls from large, AssistChip pulls from small.",
        Build:       () =>
        {
            var themed = new MaterialTheme
            {
                Shapes = MaterialTheme.BuildShapes(
                    small:  Shape.RoundedPercent(50),
                    medium: Shape.CutCorners(20),
                    large:  Shape.RoundedCorners(24)),
                UseDynamicColor = false,
            };
            themed.Add(new Column(verticalArrangement: Arrangement.SpacedBy(12))
            {
                new Card { new Text("Card now uses cut corners (medium)") { Modifier = Modifier.Companion.Padding(16) } },
                new Row(horizontalArrangement: Arrangement.SpacedBy(8))
                {
                    new AssistChip(onClick: () => { }) { Label = new Text("Pill chip (small=50%)") },
                },
                new FloatingActionButton(onClick: () => { }) { new Text("FAB — large=24 dp") },
            });

            return new Column(verticalArrangement: Arrangement.SpacedBy(16))
            {
                new Text("Default M3 shapes:"),
                new Card { new Text("Card — Shapes.Medium (12 dp)") { Modifier = Modifier.Companion.Padding(16) } },
                new Text("Custom shape scale (small=50%, medium=cut-corner, large=24 dp):"),
                themed,
            };
        });
}
