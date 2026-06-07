using ComposeNet.Gallery.Registry;

namespace ComposeNet.Gallery.Demos.AppBars;

/// <summary>MediumFlexibleTopAppBar with Title + Subtitle.</summary>
public static class MediumFlexibleTopAppBarDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "appbars-medium-flexible",
        CategoryId:  "app-bars-tabs",
        Title:       "MediumFlexibleTopAppBar",
        Description: "Two-line app bar — Title + Subtitle.",
        Build:       () =>
        {
            var count = Compose.Remember(() => new MutableNumberState<int>(0));
            return new Column
            {
                new MediumFlexibleTopAppBar
                {
                    Title    = new Text("Project Atlas"),
                    Subtitle = new Text($"count={count}"),
                },
                new Button(onClick: () => count++) { new Text("Bump count") },
            };
        });
}
