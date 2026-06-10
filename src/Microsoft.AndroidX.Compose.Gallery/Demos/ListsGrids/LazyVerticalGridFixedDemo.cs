using AndroidX.Compose.Gallery.Registry;

namespace AndroidX.Compose.Gallery.Demos.ListsGrids;

/// <summary>LazyVerticalGrid with a fixed column count.</summary>
public static class LazyVerticalGridFixedDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "lists-grid-fixed",
        CategoryId:  "lists-grids",
        Title:       "LazyVerticalGrid (Fixed 3)",
        Description: "Three equally sized columns; cells flow top-to-bottom, left-to-right.",
        Build:       _ => new LazyVerticalGrid<int>(
            columns:     GridCells.Fixed(3),
            items:       Enumerable.Range(0, 60).ToList(),
            itemContent: i => new Card
            {
                Modifier.Companion.Padding(4),
                new Text($"Cell {i}"),
            })
        {
            Modifier = Modifier.Companion.FillMaxWidth().Height(320),
        });
}
