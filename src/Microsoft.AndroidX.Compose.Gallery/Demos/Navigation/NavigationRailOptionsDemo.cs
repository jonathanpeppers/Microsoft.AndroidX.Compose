using AndroidX.Compose.Gallery.Registry;

namespace AndroidX.Compose.Gallery.Demos.Navigation;

/// <summary>NavigationRailItem enabled and label-visibility defaults.</summary>
public static class NavigationRailOptionsDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "navigation-rail-options",
        CategoryId:  "navigation",
        Title:       "Navigation rail options",
        Description: "Toggle item enabled state and alwaysShowLabel.",
        Build:       c =>
        {
            var selected = c.MutableStateOf(0);
            var enabled = c.MutableStateOf(true);
            var labels = c.MutableStateOf(true);
            return new Column
            {
                new Row(horizontalArrangement: Arrangement.SpacedBy(8.Dp()),
                        verticalAlignment: Alignment.Vertical.CenterVertically)
                {
                    new Switch(@checked: enabled.Value, onCheckedChange: value => enabled.Value = value),
                    new Text("Enabled"),
                    new Switch(@checked: labels.Value, onCheckedChange: value => labels.Value = value),
                    new Text("Always show labels"),
                },
                new Box
                {
                    Modifier.Height(280),
                    new NavigationRail
                    {
                        new NavigationRailItem(
                            selected: selected.Value == 0,
                            onClick: () => selected.Value = 0,
                            enabled: enabled.Value,
                            alwaysShowLabel: labels.Value)
                        {
                            Icon = new Text("H"),
                            Label = new Text("Home"),
                        },
                        new NavigationRailItem(
                            selected: selected.Value == 1,
                            onClick: () => selected.Value = 1,
                            enabled: enabled.Value,
                            alwaysShowLabel: labels.Value)
                        {
                            Icon = new Text("S"),
                            Label = new Text("Search"),
                        },
                    },
                },
            };
        });
}
