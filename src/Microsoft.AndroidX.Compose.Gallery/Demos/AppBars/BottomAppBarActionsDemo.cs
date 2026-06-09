using Microsoft.AndroidX.Compose.Gallery.Registry;

namespace Microsoft.AndroidX.Compose.Gallery.Demos.AppBars;

/// <summary>BottomAppBar with three icon actions.</summary>
public static class BottomAppBarActionsDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "appbars-bottom-actions",
        CategoryId:  "app-bars-tabs",
        Title:       "BottomAppBar — actions",
        Description: "Action icons laid out left-to-right; no FAB slot.",
        Build:       () =>
        {
            var count = ComposeRuntime.Remember(() => new MutableNumberState<int>(0));
            return new Column
            {
                new Text($"count = {count}"),
                new BottomAppBar
                {
                    new IconButton(onClick: () => count--) { new Text("−") },
                    new IconButton(onClick: () => count.Value = 0) { new Text("↺") },
                    new IconButton(onClick: () => count++) { new Text("+") },
                },
            };
        });
}
