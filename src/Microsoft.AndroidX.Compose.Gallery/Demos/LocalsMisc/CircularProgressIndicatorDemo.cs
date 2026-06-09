using AndroidX.Compose.Gallery.Registry;

namespace AndroidX.Compose.Gallery.Demos.LocalsMisc;

/// <summary>CircularProgressIndicator — Material 3 indeterminate spinner.</summary>
public static class CircularProgressIndicatorDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "misc-circular-progress",
        CategoryId:  "locals-misc",
        Title:       "CircularProgressIndicator",
        Description: "Material 3 indeterminate circular spinner.",
        Build:       _ => new Column
        {
            new Text("Indeterminate circular spinner:"),
            new CircularProgressIndicator(),
        });
}
