using AndroidX.Compose.Gallery.Registry;
using BoundBrush = AndroidX.Compose.UI.Graphics.Brush;

namespace AndroidX.Compose.Gallery.Demos.Containers;

/// <summary>Brush gradients applied via <c>Modifier.Background</c> / <c>Modifier.Border</c>.</summary>
public static class BrushDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id: "containers-brush",
        CategoryId: "containers",
        Title: "Brush gradients",
        Description: "Linear, horizontal, vertical, radial and sweep gradients via Brush.* factories.",
        Build: _ => Build());

    static Column Build()
    {
        var magenta = new Color(0xFF, 0xFF, 0x00, 0xFF);
        var cyan = new Color(0xFF, 0x00, 0xFF, 0xFF);
        var yellow = new Color(0xFF, 0xFF, 0xFF, 0x00);
        var orange = new Color(0xFF, 0xFF, 0x88, 0x00);

        Box Tile(BoundBrush brush, Shape? shape = null) =>
            new()
            {
                Modifier.FillMaxWidth()
                    .Height(new Dp(80))
                    .Background(brush, shape),
            };

        return new Column(verticalArrangement: Arrangement.SpacedBy(12))
        {
            Modifier.FillMaxWidth(),

            new Text("LinearGradient — top→bottom"),
            Tile(Brush.LinearGradient(magenta, cyan)),

            new Text("HorizontalGradient — left→right"),
            Tile(Brush.HorizontalGradient(yellow, orange)),

            new Text("VerticalGradient — RoundedCornerShape(16)"),
            Tile(Brush.VerticalGradient(magenta, yellow, cyan), new RoundedCornerShape(new Dp(16))),

            new Text("RadialGradient — Shape.Circle"),
            Tile(Brush.RadialGradient(yellow, magenta), Shape.Circle()),

            new Text("SweepGradient — rainbow"),
            Tile(Brush.SweepGradient(magenta, yellow, cyan, magenta)),

            new Text("SolidColor brush (sanity check)"),
            Tile(Brush.SolidColor(orange)),

            new Text("Border with horizontal gradient"),
            new Box
            {
                Modifier.FillMaxWidth()
                    .Height(new Dp(80))
                    .Border(new Dp(4), Brush.HorizontalGradient(magenta, cyan, yellow), new RoundedCornerShape(new Dp(12))),
            },
        };
    }
}
