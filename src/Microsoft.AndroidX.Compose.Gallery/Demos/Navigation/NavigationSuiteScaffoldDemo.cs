using AndroidX.Compose.Gallery.Registry;

namespace AndroidX.Compose.Gallery.Demos.Navigation;

/// <summary>Adaptive navigation scaffold with short-bar, wide-rail, and visibility-state controls.</summary>
public static class NavigationSuiteScaffoldDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "navigation-suite-scaffold",
        CategoryId:  "navigation",
        Title:       "NavigationSuiteScaffold",
        Description: "Adaptive short navigation bar or wide rail with generated item and show/hide state APIs.",
        Build:       c =>
        {
            var selected = c.MutableStateOf(0);
            var useWideRail = c.MutableStateOf(false);
            var state = c.Remember(() => new NavigationSuiteScaffoldState());
            var suiteType = useWideRail.Value
                ? NavigationSuiteType.WideNavigationRailCollapsed
                : NavigationSuiteType.ShortNavigationBarCompact;

            var scaffold = new NavigationSuiteScaffold(state)
            {
                NavigationSuiteType = suiteType,
                Content = new Box
                {
                    Modifier.FillMaxSize(),
                    new Text(
                        $"Selected: {selected}\n" +
                        $"Type: {suiteType}\n" +
                        $"Visibility: {state.CurrentValue}"),
                },
            };
            scaffold.Add(Modifier.FillMaxWidth().Height(360));
            scaffold.Add(new NavigationSuiteItem(
                selected: selected.Value == 0,
                onClick:  () => selected.Value = 0)
            {
                NavigationSuiteType = suiteType,
                Icon  = new Text("H"),
                Label = new Text("Home"),
            });
            scaffold.Add(new NavigationSuiteItem(
                selected: selected.Value == 1,
                onClick:  () => selected.Value = 1)
            {
                NavigationSuiteType = suiteType,
                Icon  = new Text("S"),
                Label = new Text("Search"),
            });
            scaffold.Add(new NavigationSuiteItem(
                selected: selected.Value == 2,
                onClick:  () => selected.Value = 2)
            {
                NavigationSuiteType = suiteType,
                Icon  = new Text("P"),
                Label = new Text("Settings"),
            });

            return new Column(verticalArrangement: Arrangement.SpacedBy(8.Dp()))
            {
                new Row(horizontalArrangement: Arrangement.SpacedBy(8.Dp()))
                {
                    new Button(() => useWideRail.Value = !useWideRail.Value)
                    {
                        new Text(useWideRail.Value ? "Use short bar" : "Use wide rail"),
                    },
                    new Button(() => _ = state.ToggleAsync())
                    {
                        new Text("Toggle visibility"),
                    },
                },
                scaffold,
            };
        });
}
