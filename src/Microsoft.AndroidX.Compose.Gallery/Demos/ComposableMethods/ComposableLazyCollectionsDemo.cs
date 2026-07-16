using AndroidX.Compose.Gallery.Registry;
using static AndroidX.Compose.Composables;

namespace AndroidX.Compose.Gallery.Demos.ComposableMethods;

/// <summary>Exercises generic composable-method lowering across every typed lazy container.</summary>
public static class ComposableLazyCollectionsDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "composable-lazy-collections",
        CategoryId:  "composable-methods",
        Title:       "Generic lazy collections",
        Description: "All six typed lazy containers rendered through generic composerless APIs.",
        Build:       static _ => new ComposableDemoAdapter(() => LazyCollections()));

    /// <summary>Renders bounded examples of every generic lazy composable entry point.</summary>
    [Composable]
    public static void LazyCollections()
    {
        IReadOnlyList<int> items = [1, 2, 3, 4, 5, 6];
        var viewport = Modifier.FillMaxWidth().Height(120);

        Column(() =>
        {
            Text("LazyColumn");
            LazyColumn(items, item => Text($"Row {item}"), modifier: viewport);

            Text("LazyRow");
            LazyRow(items, item => Text($"Item {item}"), modifier: viewport);

            Text("LazyVerticalGrid");
            LazyVerticalGrid(
                GridCells.Fixed(2),
                items,
                item => Text($"Cell {item}"),
                modifier: viewport);

            Text("LazyHorizontalGrid");
            LazyHorizontalGrid(
                GridCells.Fixed(2),
                items,
                item => Text($"Cell {item}"),
                modifier: viewport);

            Text("LazyVerticalStaggeredGrid");
            LazyVerticalStaggeredGrid(
                StaggeredGridCells.Fixed(2),
                items,
                item => Text($"Tile {item}"),
                modifier: viewport);

            Text("LazyHorizontalStaggeredGrid");
            LazyHorizontalStaggeredGrid(
                StaggeredGridCells.Fixed(2),
                items,
                item => Text($"Tile {item}"),
                modifier: viewport);
        });
    }
}
