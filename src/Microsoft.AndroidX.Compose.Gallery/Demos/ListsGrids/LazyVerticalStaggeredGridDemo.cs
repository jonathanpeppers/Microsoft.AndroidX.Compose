using AndroidX.Compose.Gallery.Demos.Shared;
using AndroidX.Compose.Gallery.Registry;

namespace AndroidX.Compose.Gallery.Demos.ListsGrids;

/// <summary>LazyVerticalStaggeredGrid — cells of varying heights stagger across columns.</summary>
public static class LazyVerticalStaggeredGridDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "lists-staggered-grid",
        CategoryId:  "lists-grids",
        Title:       "LazyVerticalStaggeredGrid",
        Description: "Adaptive 120dp columns; each cell picks a varying height for a true staggered layout.",
        Build:       _ => new LazyVerticalStaggeredGrid<int>(
            columns:     StaggeredGridCells.Adaptive(120f),
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
        });
}
