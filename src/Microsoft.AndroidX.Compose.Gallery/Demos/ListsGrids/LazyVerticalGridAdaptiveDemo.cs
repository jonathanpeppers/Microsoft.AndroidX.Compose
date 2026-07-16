using AndroidX.Compose.Gallery.Registry;

namespace AndroidX.Compose.Gallery.Demos.ListsGrids;

/// <summary>Adaptive LazyVerticalGrid with observable managed state and async scrolling.</summary>
public static class LazyVerticalGridAdaptiveDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "lists-grid-adaptive",
        CategoryId:  "lists-grids",
        Title:       "LazyVerticalGrid state",
        Description: "Adaptive Dp cells with live position and animated managed scrolling.",
        Build:       c =>
        {
            var state = c.RememberLazyGridState();
            var scope = c.RememberCoroutineScope();
            return new Column(verticalArrangement: Arrangement.SpacedBy(8.Dp()))
            {
                new Text($"First visible: {state.FirstVisibleItemIndex} ({state.FirstVisibleItemScrollOffset}px)"),
                new Row(horizontalArrangement: Arrangement.SpacedBy(8.Dp()))
                {
                    new Button(() => _ = scope.Launch(
                        ct => state.AnimateScrollToItemAsync(39, cancellationToken: ct)))
                    {
                        new Text("To end"),
                    },
                    new Button(() => _ = scope.Launch(
                        ct => state.ScrollToItemAsync(0, cancellationToken: ct)))
                    {
                        new Text("To start"),
                    },
                },
                new LazyVerticalGrid<int>(
                    columns:     GridCells.Adaptive(96.Dp()),
                    items:       Enumerable.Range(0, 40).ToList(),
                    itemContent: i => new Card
                    {
                        Modifier.Padding(4),
                        new Text($"A {i}"),
                    })
                {
                    Modifier = Modifier.FillMaxWidth().Height(320),
                    State = state,
                },
            };
        });
}
