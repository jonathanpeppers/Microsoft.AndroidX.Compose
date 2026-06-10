using AndroidX.Compose.Gallery.Registry;

namespace AndroidX.Compose.Gallery.Demos.Containers;

/// <summary>Surface — themed background you can nest under any content.</summary>
public static class SurfaceDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "containers-surface",
        CategoryId:  "containers",
        Title:       "Surface",
        Description: "M3 Surface pulls its container color from MaterialTheme.",
        Build:       _ => new Column
        {
            new Surface
            {
                new Text("Inside a Surface — picks up the theme's surface color."),
            },
            new Spacer { Modifier = Modifier.Height(8) },
            new Surface
            {
                Modifier.Padding(16),
                new Text("Surface + outer padding"),
            },
        });
}
