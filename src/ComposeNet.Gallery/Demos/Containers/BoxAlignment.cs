using AndroidX.Compose.UI.Graphics;
using ComposeNet.Gallery.Registry;

namespace ComposeNet.Gallery.Demos.Containers;

/// <summary>Box stacks children at a single alignment slot.</summary>
public static class BoxAlignment
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "containers-box-alignment",
        CategoryId:  "containers",
        Title:       "Box alignment",
        Description: "Multiple children stacked inside one Box.",
        Build:       () => new Box
        {
            Modifier.Companion
                .Size(120)
                .Background(Color.FromRgb(0xB3, 0xE5, 0xFC)),
            new Text("Front") { Modifier = Modifier.Companion.Padding(8) },
        });
}
