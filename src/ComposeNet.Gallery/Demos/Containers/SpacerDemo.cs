using AndroidX.Compose.UI.Graphics;
using ComposeNet.Gallery.Registry;

namespace ComposeNet.Gallery.Demos.Containers;

/// <summary>Spacer — a sized empty slot in a layout.</summary>
public static class SpacerDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "containers-spacer",
        CategoryId:  "containers",
        Title:       "Spacer",
        Description: "Spacer with explicit Height / Width for fixed gaps.",
        Build:       () => new Column
        {
            new Text("Above 32-dp Spacer"),
            new Spacer { Modifier = Modifier.Companion.Height(32) },
            new Text("Below 32-dp Spacer"),
            new Row
            {
                new Text("Left"),
                new Spacer { Modifier = Modifier.Companion.Width(48) },
                new Text("Right (after 48-dp Spacer)"),
            },
        });
}
