using Microsoft.AndroidX.Compose.Gallery.Demos.Shared;
using Microsoft.AndroidX.Compose.Gallery.Registry;

namespace Microsoft.AndroidX.Compose.Gallery.Demos.CarouselsPaging;

/// <summary>Multi-browse carousel — preferred width with edge items shrunk to fit.</summary>
public static class HorizontalMultiBrowseCarouselDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "carousels-multibrowse",
        CategoryId:  "carousels-paging",
        Title:       "HorizontalMultiBrowseCarousel",
        Description: "240dp preferred item width; edge items shrink via the keyline strategy.",
        Build:       _ => new HorizontalMultiBrowseCarousel<int>(
            items:              Enumerable.Range(0, 12).ToList(),
            preferredItemWidth: 240f,
            itemContent:        i => new Box
            {
                Modifier.Companion
                    .FillMaxSize()
                    .Clip(20)
                    .Background(Palette.Pastels[i % Palette.Pastels.Length]),
                new Text($"#{i:D2}") { Modifier = Modifier.Companion.Padding(16) },
            })
        {
            Modifier    = Modifier.Companion.FillMaxWidth().Height(180),
            ItemSpacing = 8f,
        });
}
