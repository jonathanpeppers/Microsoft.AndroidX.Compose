using AndroidX.Compose.Gallery.Demos.Shared;
using AndroidX.Compose.Gallery.Registry;

namespace AndroidX.Compose.Gallery.Demos.CarouselsPaging;

/// <summary>Centered hero carousel — focused item enlarged in the middle.</summary>
public static class HorizontalCenteredHeroCarouselDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "carousels-hero",
        CategoryId:  "carousels-paging",
        Title:       "HorizontalCenteredHeroCarousel",
        Description: "Focused 'hero' item centered; neighbors peek from the edges.",
        Build:       _ => new HorizontalCenteredHeroCarousel<int>(
            items:       Enumerable.Range(0, 8).ToList(),
            itemContent: i => new Box
            {
                Modifier
                    .FillMaxSize()
                    .Clip(24)
                    .Background(Palette.Pastels[i % Palette.Pastels.Length]),
                new Text($"Hero {i}") { Modifier = Modifier.Padding(16) },
            })
        {
            Modifier     = Modifier.FillMaxWidth().Height(220),
            MaxItemWidth = 280,
            ItemSpacing  = 8,
        });
}
