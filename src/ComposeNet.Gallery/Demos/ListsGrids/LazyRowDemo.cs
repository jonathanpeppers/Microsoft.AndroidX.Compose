using ComposeNet.Gallery.Registry;

namespace ComposeNet.Gallery.Demos.ListsGrids;

/// <summary>LazyRow as a horizontally scrolling carousel of 50 cards.</summary>
public static class LazyRowDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "lists-lazy-row",
        CategoryId:  "lists-grids",
        Title:       "LazyRow",
        Description: "Horizontally scrolling row that only composes visible items.",
        Build:       () => new LazyRow<int>(
            items:       System.Linq.Enumerable.Range(0, 50).ToList(),
            itemContent: i => new Card
            {
                Modifier.Companion.Padding(4).Size(80),
                new Text($"#{i}"),
            })
        {
            Modifier = Modifier.Companion.FillMaxWidth(),
        });
}
