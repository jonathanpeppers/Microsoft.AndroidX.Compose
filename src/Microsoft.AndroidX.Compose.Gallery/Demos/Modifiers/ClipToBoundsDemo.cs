using AndroidX.Compose.Gallery.Registry;

namespace AndroidX.Compose.Gallery.Demos.Modifiers;

/// <summary>Shows content moving beyond the same bounds with and without clipping.</summary>
public static class ClipToBoundsDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id: "modifiers-clip-to-bounds",
        CategoryId: "modifiers",
        Title: "Clip to bounds",
        Description: "The red tile is offset 40 dp. Only the second viewport clips overflow; measurement stays unchanged.",
        Build: _ => new Column
        {
            Modifier.Padding(16),
            new Text("Unclipped: red extends past the gray viewport"),
            Viewport(false),
            new Text("Clipped: red stops at the gray viewport edge") { Modifier = Modifier.Padding(top: 24) },
            Viewport(true),
        });

    static Box Viewport(bool clip)
    {
        var modifier = Modifier.Size(120, 64).Background(Color.LightGray);
        return new Box
        {
            clip ? modifier.ClipToBounds() : modifier,
            new Box { Modifier.Size(120, 64).Offset(x: 40).Background(Color.Red) },
        };
    }
}
