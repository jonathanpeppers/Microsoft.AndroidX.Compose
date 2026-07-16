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
        Description: "Fixed 200dp items with remembered state and programmatic scrolling.",
        Build:       c =>
        {
            IReadOnlyList<int> items = Enumerable.Range(0, 12).ToList();
            var state = c.Remember(() => new CarouselState(() => items.Count));
            return new Column
            {
                new HorizontalUncontainedCarousel<int>(
                    items:       items,
                    itemWidth:   new Dp(200),
                    itemContent: i => new Box
                    {
                        Modifier
                            .FillMaxSize()
                            .Clip(20)
                            .Background(Palette.Pastels[i % Palette.Pastels.Length]),
                        new Text($"Item {i}") { Modifier = Modifier.Padding(16) },
                    })
                {
                    Modifier = Modifier.FillMaxWidth().Height(160),
                    State = state,
                    ItemSpacing = 8,
                },
                new Row
                {
                    new Button(() => _ = state.ScrollToItemAsync(0)) { new Text("First") },
                    new Button(() => _ = state.AnimateScrollToItemAsync(items.Count - 1)) { new Text("Last") },
                },
            };
        });
}
