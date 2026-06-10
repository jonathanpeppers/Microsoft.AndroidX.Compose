using AndroidX.Compose.Gallery.Registry;

namespace AndroidX.Compose.Gallery.Demos.AppBars;

/// <summary>LargeFlexibleTopAppBar with Title, Subtitle, and trailing Actions.</summary>
public static class LargeFlexibleTopAppBarDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "appbars-large-flexible",
        CategoryId:  "app-bars-tabs",
        Title:       "LargeFlexibleTopAppBar",
        Description: "Big title for hero screens; supports Title / Subtitle / Actions.",
        Build:       c =>
        {
            var count = c.Remember(() => new MutableNumberState<int>(0));
            return new LargeFlexibleTopAppBar
            {
                Title    = new Text("Tickets"),
                Subtitle = new Text($"{count} open"),
                Actions  = new Row
                {
                    new IconButton(onClick: () => count++) { new Text("+") },
                },
            };
        });
}
