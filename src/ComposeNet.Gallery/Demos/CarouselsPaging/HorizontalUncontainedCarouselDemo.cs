using ComposeNet.Gallery.Demos.Shared;
using ComposeNet.Gallery.Registry;

namespace ComposeNet.Gallery.Demos.CarouselsPaging;

/// <summary>Material 3 uncontained carousel with fixed-width items.</summary>
public static class HorizontalUncontainedCarouselDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "carousels-uncontained",
        CategoryId:  "carousels-paging",
        Title:       "HorizontalUncontainedCarousel",
        Description: "Fixed 200dp item width; edge items aren't shrunk to fit.",
        Build:       () => new HorizontalUncontainedCarousel<int>(
            items: Enumerable.Range(0, 12).ToList(),
            itemWidth:   200f,
            itemContent: i => new Box
            {
                Modifier.Companion
                    .FillMaxSize()
                    .Clip(20)
                    .Background(Palette.Pastels[i % Palette.Pastels.Length]),
                new Text($"Item {i}") { Modifier = Modifier.Companion.Padding(16) },
            })
        {
            Modifier    = Modifier.Companion.FillMaxWidth().Height(160),
            ItemSpacing = 8f,
        });
}
