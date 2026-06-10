using AndroidX.Compose.Gallery.Registry;

namespace AndroidX.Compose.Gallery.Demos.Navigation;

/// <summary>WideNavigationRail — persistent rail with three icon+label items.</summary>
public static class WideNavigationRailDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "navigation-rail-wide",
        CategoryId:  "navigation",
        Title:       "WideNavigationRail",
        Description: "Persistent vertical rail with three selectable items. The enabled toggle disables all rail items.",
        Build:       c =>
        {
            var idx     = c.MutableStateOf(0);
            var enabled = c.MutableStateOf(true);
            return new Column
            {
                new Row
                {
                    new Switch(@checked: enabled.Value, onCheckedChange: v => enabled.Value = v),
                    new Text(enabled.Value ? "Enabled" : "Disabled"),
                },
                new Text($"WideNavigationRail (selected: {idx})"),
                // The rail wants to fill its parent vertically. The DemoScreen
                // wrapper provides infinite height (vertical scroll), so we
                // bound it with a fixed-height Box.
                new Box
                {
                    Modifier.Height(320),
                    new WideNavigationRail
                    {
                        new WideNavigationRailItem(selected: idx.Value == 0, onClick: () => idx.Value = 0, enabled: enabled.Value)
                        {
                            Icon  = new Text("🏠"),
                            Label = new Text("Home"),
                        },
                        new WideNavigationRailItem(selected: idx.Value == 1, onClick: () => idx.Value = 1, enabled: enabled.Value)
                        {
                            Icon  = new Text("🔍"),
                            Label = new Text("Search"),
                        },
                        new WideNavigationRailItem(selected: idx.Value == 2, onClick: () => idx.Value = 2, enabled: enabled.Value)
                        {
                            Icon  = new Text("⚙"),
                            Label = new Text("Settings"),
                        },
                    },
                },
            };
        });
}
