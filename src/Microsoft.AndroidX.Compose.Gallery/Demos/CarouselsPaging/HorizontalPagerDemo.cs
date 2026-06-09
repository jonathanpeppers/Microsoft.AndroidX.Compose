using Microsoft.AndroidX.Compose.Gallery.Demos.Shared;
using Microsoft.AndroidX.Compose.Gallery.Registry;

namespace Microsoft.AndroidX.Compose.Gallery.Demos.CarouselsPaging;

/// <summary>HorizontalPager — swipe between independent page composables.</summary>
public static class HorizontalPagerDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "carousels-horizontal-pager",
        CategoryId:  "carousels-paging",
        Title:       "HorizontalPager",
        Description: "Three swipeable screens with a shared PagerState driving the indicator below.",
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
                        Modifier.Companion
                            .FillMaxSize()
                            .Clip(20)
                            .Background(Palette.Pastels[i % Palette.Pastels.Length]),
                        new Text($"Screen {i + 1}")
                        {
                            Modifier = Modifier.Companion.Padding(16),
                        },
                    })
                {
                    State    = state,
                    Modifier = Modifier.Companion.FillMaxWidth().Height(200),
                },
                new Text($"Page {state.CurrentPage + 1} of {state.PageCount}"),
            };
        });
}
