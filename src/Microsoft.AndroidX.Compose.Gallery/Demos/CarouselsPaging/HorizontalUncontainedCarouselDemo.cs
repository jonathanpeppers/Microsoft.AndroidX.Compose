using AndroidX.Compose.Gallery.Demos.Shared;
using AndroidX.Compose.Gallery.Registry;

namespace AndroidX.Compose.Gallery.Demos.CarouselsPaging;

/// <summary>Material 3 uncontained carousel with fixed-width items.</summary>
public static class HorizontalUncontainedCarouselDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "carousels-uncontained",
        CategoryId:  "carousels-paging",
        Title:       "HorizontalUncontainedCarousel",
        Description: "Fixed 200dp item width; edge items aren't shrunk to fit.",
        Build:       _ => new HorizontalUncontainedCarousel<int>(
            items:       Enumerable.Range(0, 12).ToList(),
            itemWidth:   200f,
            itemContent: i => new Box
            {
                Modifier
                    .FillMaxSize()
                    .Clip(20)
                    .Background(Palette.Pastels[i % Palette.Pastels.Length]),
                new Text($"Item {i}") { Modifier = Modifier.Padding(16) },
            })
        {
            Modifier    = Modifier.FillMaxWidth().Height(160),
            ItemSpacing = 8.Dp(),
        });
}
