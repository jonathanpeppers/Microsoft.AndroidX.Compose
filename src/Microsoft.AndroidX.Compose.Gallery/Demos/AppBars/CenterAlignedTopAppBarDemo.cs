using Microsoft.AndroidX.Compose.Gallery.Registry;

namespace Microsoft.AndroidX.Compose.Gallery.Demos.AppBars;

/// <summary>CenterAlignedTopAppBar — title centered, optional nav/action icons.</summary>
public static class CenterAlignedTopAppBarDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "appbars-center-top",
        CategoryId:  "app-bars-tabs",
        Title:       "CenterAlignedTopAppBar",
        Description: "Centered title with leading nav icon and trailing action.",
        Build:       () => new CenterAlignedTopAppBar
        {
            Title          = new Text("Inbox"),
            NavigationIcon = new IconButton(onClick: () => { }) { new Text("☰") },
            Actions        = new Row
            {
                new IconButton(onClick: () => { }) { new Text("⋮") },
            },
        });
}
