using AndroidX.Compose.Gallery.Registry;

namespace AndroidX.Compose.Gallery.Demos.ListsGrids;

/// <summary>Mutates stable records in all six lazy containers and both pagers.</summary>
public static class StableCollectionKeysDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id: "stable-collection-keys",
        CategoryId: "lists-grids",
        Title: "Stable collection keys",
        Description: "Tap a record, then insert, delete, or reverse. Its counter and viewport follow its ID.",
        Build: c =>
        {
            var records = c.Remember(() => new MutableManagedState<IReadOnlyList<int>>([1, 2, 3, 4, 5, 6]));
            var nextId = c.Remember(() => new MutableNumberState<int>(7));
            var items = records.Value;
            var viewport = Modifier.FillMaxWidth().Height(180);
            return new Column
            {
                new Text("Each counter belongs to its record, not its position. Scroll each viewport independently."),
                new Row
                {
                    new Button(() => records.Value = [nextId.Value++, .. records.Value]) { new Text("Insert") },
                    new Button(() => records.Value = records.Value.Skip(1).ToArray()) { new Text("Delete first") },
                    new Button(() => records.Value = records.Value.Reverse().ToArray()) { new Text("Reverse") },
                },
                new Text("LazyColumn"),
                new LazyColumn<int>(items, Row) { Key = Id, Modifier = viewport },
                new Text("LazyRow"),
                new LazyRow<int>(items, Row) { Key = Id, Modifier = viewport },
                new Text("Vertical grid"),
                new LazyVerticalGrid<int>(GridCells.Fixed(2), items, Row) { Key = Id, Modifier = viewport },
                new Text("Horizontal grid"),
                new LazyHorizontalGrid<int>(GridCells.Fixed(2), items, Row) { Key = Id, Modifier = viewport },
                new Text("Vertical staggered grid"),
                new LazyVerticalStaggeredGrid<int>(StaggeredGridCells.Fixed(2), items, Row) { Key = Id, Modifier = viewport },
                new Text("Horizontal staggered grid"),
                new LazyHorizontalStaggeredGrid<int>(StaggeredGridCells.Fixed(2), items, Row) { Key = Id, Modifier = viewport },
                new Text("Horizontal pager"),
                new HorizontalPager<int>(items, Row) { Key = Id, Modifier = viewport },
                new Text("Vertical pager"),
                new VerticalPager<int>(items, Row) { Key = Id, Modifier = viewport },
            };
        });

    static object Id(int item) => item;
    static ComposableNode Row(int item) => new StableCollectionKeyRow(item);
}
