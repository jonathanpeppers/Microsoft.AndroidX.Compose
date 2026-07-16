using AndroidX.Compose.Gallery.Demos.Shared;
using AndroidX.Compose.Gallery.Registry;

namespace AndroidX.Compose.Gallery.Demos.ListsGrids;

/// <summary>Staggered grid with managed state, Dp cell sizing, and async scrolling.</summary>
public static class LazyVerticalStaggeredGridDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "lists-staggered-grid",
        CategoryId:  "lists-grids",
        Title:       "LazyVerticalStaggeredGrid",
        Description: "Adaptive Dp columns with live position and animated managed scrolling.",
        Build:       c =>
        {
            var state = c.RememberLazyStaggeredGridState();
            var scope = c.RememberCoroutineScope();
            return new Column(verticalArrangement: Arrangement.SpacedBy(8.Dp()))
            {
                new Text($"First visible: {state.FirstVisibleItemIndex} ({state.FirstVisibleItemScrollOffset}px)"),
                new Row(horizontalArrangement: Arrangement.SpacedBy(8.Dp()))
                {
                    new Button(() => _ = scope.Launch(
                        ct => state.AnimateScrollToItemAsync(29, cancellationToken: ct)))
                    {
                        new Text("To end"),
                    },
                    new Button(() => _ = scope.Launch(
                        ct => state.ScrollToItemAsync(0, cancellationToken: ct)))
                    {
                        new Text("To start"),
                    },
                },
                new LazyVerticalStaggeredGrid<int>(
                    columns:     StaggeredGridCells.Adaptive(120.Dp()),
                    items:       Enumerable.Range(0, 30).ToList(),
                    itemContent: i => new Card
                    {
                        Modifier
                            .Padding(4)
                            .Height(60 + (i % 5) * 30)
                            .Background(Palette.Pastels[i % Palette.Pastels.Length]),
                        new Text($"#{i:D2}")
                        {
                            Modifier = Modifier.Padding(8),
                        },
                    })
                {
                    Modifier = Modifier.FillMaxWidth().Height(360),
                    State = state,
                },
            };
        });
}
