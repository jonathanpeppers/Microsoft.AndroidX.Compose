using ComposeNet.Gallery.Registry;

namespace ComposeNet.Gallery.Demos.LocalsMisc;

/// <summary>
/// Image — ContentScale, Alignment, and Alpha slots. Renders the same
/// drawable in a rectangular tile under each ContentScale value so the
/// scaling behaviour is visible side-by-side, plus a row of alignment
/// presets and an alpha-fade ladder.
/// </summary>
public static class ImageContentScaleDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "misc-image-content-scale",
        CategoryId:  "locals-misc",
        Title:       "Image — ContentScale / Alignment / Alpha",
        Description: "Exercises the optional ContentScale, Alignment, and Alpha slots on the Image facade — one tile per ContentScale value, plus alignment and alpha rows.",
        Build:       () => new Column
        {
            new Text("ContentScale (24×24 star, 160×80 tile):"),
            ContentScaleTile("Fit", ContentScale.Fit),
            ContentScaleTile("Crop", ContentScale.Crop),
            ContentScaleTile("FillHeight", ContentScale.FillHeight),
            ContentScaleTile("FillWidth", ContentScale.FillWidth),
            ContentScaleTile("FillBounds", ContentScale.FillBounds),
            ContentScaleTile("Inside", ContentScale.Inside),
            ContentScaleTile("None", ContentScale.None),

            new Text("Alignment inside a 160×80 tile (ContentScale.None):"),
            AlignmentTile("TopStart", Alignment.TopStart),
            AlignmentTile("Center", Alignment.Center),
            AlignmentTile("BottomEnd", Alignment.BottomEnd),

            new Text("Alpha (100% → 25%):"),
            AlphaTile("Alpha 1.0", 1.0f),
            AlphaTile("Alpha 0.5", 0.5f),
            AlphaTile("Alpha 0.25", 0.25f),
        });

    static ComposableNode ContentScaleTile(string label, ContentScale scale) => new Column
    {
        new Text(label),
        new Box
        {
            Modifier.Companion
                .Border(1, Color.Gray)
                .Background(Color.LightGray)
                .Width(160)
                .Height(80),
            new Image(Resource.Drawable.ic_star, $"Star — {label}")
            {
                Modifier      = Modifier.Companion.FillMaxSize(),
                ContentScale  = scale,
            },
        },
    };

    static ComposableNode AlignmentTile(string label, Alignment alignment) => new Column
    {
        new Text(label),
        new Box
        {
            Modifier.Companion
                .Border(1, Color.Gray)
                .Background(Color.LightGray)
                .Width(160)
                .Height(80),
            new Image(Resource.Drawable.ic_star, $"Star — {label}")
            {
                Modifier      = Modifier.Companion.FillMaxSize(),
                ContentScale  = ContentScale.None,
                Alignment     = alignment,
            },
        },
    };

    static ComposableNode AlphaTile(string label, float alpha) => new Column
    {
        new Text(label),
        new Box
        {
            Modifier.Companion
                .Border(1, Color.Gray)
                .Background(Color.LightGray)
                .Width(160)
                .Height(80),
            new Image(Resource.Drawable.ic_star, $"Star — {label}")
            {
                Modifier      = Modifier.Companion.FillMaxSize(),
                ContentScale  = ContentScale.Fit,
                Alpha         = alpha,
            },
        },
    };
}
