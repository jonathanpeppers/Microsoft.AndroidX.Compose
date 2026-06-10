using AndroidX.Compose.Gallery.Registry;

namespace AndroidX.Compose.Gallery.Demos.Containers;

/// <summary>RoundedCornerShape applied to Card via the new <c>Shape</c> ctor slot.</summary>
public static class RoundedCornerShapeDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "containers-rounded-corner-shape",
        CategoryId:  "containers",
        Title:       "RoundedCornerShape",
        Description: "Card with explicit corner radii — int = percent, Dp = density-independent pixels.",
        Build:       _ => Build());

    static Column Build()
    {
        var square = new Card
        {
            Modifier.FillMaxWidth(),
            new Column
            {
                Modifier.Padding(16),
                new Text("RoundedCornerShape(0)"),
                new Text("Square — int 0 means 0% radius."),
            },
        };
        square.Shape = new RoundedCornerShape(0);

        var dp8 = new Card
        {
            Modifier.FillMaxWidth(),
            new Column
            {
                Modifier.Padding(16),
                new Text("RoundedCornerShape(Dp 8)"),
                new Text("8 dp on every corner — density-independent pixels."),
            },
        };
        dp8.Shape = new RoundedCornerShape(new Dp(8));

        var pill = new Card
        {
            Modifier.FillMaxWidth(),
            new Column
            {
                Modifier.Padding(16),
                new Text("RoundedCornerShape(50)"),
                new Text("Pill — int 50 means 50% radius (half the smaller dimension)."),
            },
        };
        pill.Shape = new RoundedCornerShape(50);

        var topOnly = new Card
        {
            Modifier.FillMaxWidth(),
            new Column
            {
                Modifier.Padding(16),
                new Text("Top-only 16 dp"),
                new Text("Independent corner radii via the 4-Dp ctor."),
            },
        };
        topOnly.Shape = new RoundedCornerShape(
            topStart:    new Dp(16),
            topEnd:      new Dp(16),
            bottomEnd:   new Dp(0),
            bottomStart: new Dp(0));

        return new Column(verticalArrangement: Arrangement.SpacedBy(12))
        {
            Modifier.FillMaxWidth(),
            square,
            dp8,
            pill,
            topOnly,
        };
    }
}
