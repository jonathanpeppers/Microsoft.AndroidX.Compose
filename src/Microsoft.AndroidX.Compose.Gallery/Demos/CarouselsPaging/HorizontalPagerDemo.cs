using AndroidX.Compose.Gallery.Demos.Shared;
using AndroidX.Compose.Gallery.Registry;

namespace AndroidX.Compose.Gallery.Demos.CarouselsPaging;

/// <summary>HorizontalPager — swipe between independent page composables.</summary>
public static class HorizontalPagerDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "carousels-horizontal-pager",
        CategoryId:  "carousels-paging",
        Title:       "HorizontalPager",
        Description: "Swipe, snap, animate, or request a page through shared PagerState.",
        Build:       c =>
        {
            var items = new[] { 0, 1, 2 };
            var state = c.Remember(() => new PagerState(pageCount: () => items.Length));
            return new Column
            {
                new HorizontalPager<int>(
                    items:       items,
                    itemContent: i => new Box
                    {
                        Modifier
                            .FillMaxSize()
                            .Clip(20)
                            .Background(Palette.Pastels[i % Palette.Pastels.Length]),
                        new Text($"Screen {i + 1}")
                        {
                            Modifier = Modifier.Padding(16),
                        },
                    })
                {
                    State    = state,
                    Modifier = Modifier.FillMaxWidth().Height(200),
                },
                new Text($"Page {state.CurrentPage + 1} of {state.PageCount}"),
                new Row(horizontalArrangement: Arrangement.SpacedBy(8.Dp()))
                {
                    new Button(() => _ = state.ScrollToPageAsync(0))
                    {
                        new Text("Snap first"),
                    },
                    new Button(() => _ = state.AnimateScrollToPageAsync(items.Length - 1))
                    {
                        new Text("Animate last"),
                    },
                    new Button(() => state.RequestScrollToPage(1, 0.25f))
                    {
                        new Text("Request middle + offset"),
                    },
                },
            };
        });
}
