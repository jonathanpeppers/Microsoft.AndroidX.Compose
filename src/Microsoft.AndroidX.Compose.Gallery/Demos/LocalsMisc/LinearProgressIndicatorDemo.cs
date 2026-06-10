using AndroidX.Compose.Gallery.Registry;

namespace AndroidX.Compose.Gallery.Demos.LocalsMisc;

/// <summary>LinearProgressIndicator — Material 3 indeterminate horizontal bar.</summary>
public static class LinearProgressIndicatorDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "misc-linear-progress",
        CategoryId:  "locals-misc",
        Title:       "LinearProgressIndicator",
        Description: "Material 3 indeterminate horizontal progress bar.",
        Build:       _ => new Column
        {
            new Text("Indeterminate horizontal bar:"),
            new LinearProgressIndicator { Modifier = Modifier.FillMaxWidth() },
        });
}
