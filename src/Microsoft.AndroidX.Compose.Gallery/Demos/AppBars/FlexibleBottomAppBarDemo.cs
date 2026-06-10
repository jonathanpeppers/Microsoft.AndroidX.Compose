using AndroidX.Compose.Gallery.Registry;

namespace AndroidX.Compose.Gallery.Demos.AppBars;

/// <summary>FlexibleBottomAppBar — variable height bottom bar.</summary>
public static class FlexibleBottomAppBarDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "appbars-bottom-flexible",
        CategoryId:  "app-bars-tabs",
        Title:       "FlexibleBottomAppBar",
        Description: "Flexible variant of BottomAppBar; content drives the bar's height.",
        Build:       c =>
        {
            var count = c.MutableStateOf(0);
            return new Column
            {
                new Text($"count = {count}"),
                new FlexibleBottomAppBar
                {
                    new IconButton(onClick: () => count++) { new Text("+") },
                    new IconButton(onClick: () => count--) { new Text("−") },
                },
            };
        });
}
