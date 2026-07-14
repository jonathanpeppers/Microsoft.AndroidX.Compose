using AndroidX.Compose.Gallery.Registry;

namespace AndroidX.Compose.Gallery.Demos.Containers;

/// <summary>FlowRow wraps children to a new line when they overflow width.</summary>
public static class FlowRowFlowColumnDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "containers-flow",
        CategoryId:  "containers",
        Title:       "FlowRow / FlowColumn",
        Description: "Bounded FlowRow and FlowColumn item/line counts.",
        Build:       _ => new Column
        {
            new Text("FlowRow: max 2 items per row, 2 lines"),
            new FlowRow(maxItemsInEachRow: 2, maxLines: 2)
            {
                Modifier.FillMaxWidth().Padding(4),
                new Card { Modifier.Padding(4), new Text("Music") },
                new Card { Modifier.Padding(4), new Text("Movies") },
                new Card { Modifier.Padding(4), new Text("Podcasts") },
                new Card { Modifier.Padding(4), new Text("News") },
                new Card { Modifier.Padding(4), new Text("Sports") },
                new Card { Modifier.Padding(4), new Text("Books") },
                new Card { Modifier.Padding(4), new Text("Games") },
                new Card { Modifier.Padding(4), new Text("Photography") },
            },
            new Text("FlowColumn: max 2 items per column, 2 lines"),
            new FlowColumn(maxItemsInEachColumn: 2, maxLines: 2)
            {
                Modifier.Height(120).Padding(4),
                new Card { Modifier.Padding(4), new Text("One") },
                new Card { Modifier.Padding(4), new Text("Two") },
                new Card { Modifier.Padding(4), new Text("Three") },
                new Card { Modifier.Padding(4), new Text("Four") },
                new Card { Modifier.Padding(4), new Text("Five") },
            },
        });
}
