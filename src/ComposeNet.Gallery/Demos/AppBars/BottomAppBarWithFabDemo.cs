using ComposeNet.Gallery.Registry;

namespace ComposeNet.Gallery.Demos.AppBars;

/// <summary>BottomAppBar with the trailing FloatingActionButton slot.</summary>
public static class BottomAppBarWithFabDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "appbars-bottom-fab",
        CategoryId:  "app-bars-tabs",
        Title:       "BottomAppBar — with FAB",
        Description: "BottomAppBar exposes a FloatingActionButton slot for the primary action.",
        Build:       () =>
        {
            var count = Compose.Remember(() => new MutableNumberState<int>(0));
            return new Column
            {
                new Text($"count = {count}"),
                new BottomAppBar
                {
                    FloatingActionButton = new FloatingActionButton(onClick: () => count++)
                    {
                        new Text("+"),
                    },
                },
            };
        });
}
