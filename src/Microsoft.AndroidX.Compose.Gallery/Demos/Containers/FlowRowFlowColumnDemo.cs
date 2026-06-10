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
        Description: "Wrapping row of chip-styled Cards plus a wrapping column.",
        Build:       _ => new Column
        {
            new Text("FlowRow (wraps when out of width)"),
            new FlowRow
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
        });
}
