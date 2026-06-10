using AndroidX.Compose.Gallery.Registry;

namespace AndroidX.Compose.Gallery.Demos.ListsGrids;

/// <summary>LazyVerticalGrid sized to fit as many 96-dp columns as available width allows.</summary>
public static class LazyVerticalGridAdaptiveDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "lists-grid-adaptive",
        CategoryId:  "lists-grids",
        Title:       "LazyVerticalGrid (Adaptive 96dp)",
        Description: "Column count chosen at layout time from a minimum cell width.",
        Build:       _ => new LazyVerticalGrid<int>(
            columns:     GridCells.Adaptive(96f),
            items:       Enumerable.Range(0, 40).ToList(),
            itemContent: i => new Card
            {
                Modifier.Padding(4),
                new Text($"A {i}"),
            })
        {
            Modifier = Modifier.FillMaxWidth().Height(320),
        });
}
