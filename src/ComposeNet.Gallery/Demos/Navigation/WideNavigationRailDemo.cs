using ComposeNet.Gallery.Registry;

namespace ComposeNet.Gallery.Demos.Navigation;

/// <summary>WideNavigationRail — persistent rail with three icon+label items.</summary>
public static class WideNavigationRailDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "navigation-rail-wide",
        CategoryId:  "navigation",
        Title:       "WideNavigationRail",
        Description: "Persistent vertical rail with three selectable items.",
        Build:       () =>
        {
            var idx = Compose.Remember(() => new MutableNumberState<int>(0));
            return new Column
            {
                new Text($"WideNavigationRail (selected: {idx})"),
                new Row
                {
                    new WideNavigationRail
                    {
                        new WideNavigationRailItem(selected: idx.Value == 0, onClick: () => idx.Value = 0)
                        {
                            Icon  = new Text("🏠"),
                            Label = new Text("Home"),
                        },
                        new WideNavigationRailItem(selected: idx.Value == 1, onClick: () => idx.Value = 1)
                        {
                            Icon  = new Text("🔍"),
                            Label = new Text("Search"),
                        },
                        new WideNavigationRailItem(selected: idx.Value == 2, onClick: () => idx.Value = 2)
                        {
                            Icon  = new Text("⚙"),
                            Label = new Text("Settings"),
                        },
                    },
                },
            };
        });
}
