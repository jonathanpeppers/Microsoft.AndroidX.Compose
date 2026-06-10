using AndroidX.Compose.Gallery.Registry;

namespace AndroidX.Compose.Gallery.Demos.Containers;

/// <summary>HorizontalDivider and VerticalDivider.</summary>
public static class DividerDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "containers-divider",
        CategoryId:  "containers",
        Title:       "Divider",
        Description: "HorizontalDivider between Column rows; VerticalDivider inside a Row.",
        Build:       _ => new Column
        {
            new Text("Above"),
            new HorizontalDivider { Modifier = Modifier.Padding(0, 8) },
            new Text("Below"),
            new Spacer { Modifier = Modifier.Height(16) },
            new Row
            {
                new Text("Left"),
                new VerticalDivider { Modifier = Modifier.Padding(8, 0) },
                new Text("Right"),
            },
        });
}
