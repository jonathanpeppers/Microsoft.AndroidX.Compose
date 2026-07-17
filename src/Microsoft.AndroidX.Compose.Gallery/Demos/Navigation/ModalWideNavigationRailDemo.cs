using AndroidX.Compose.Gallery.Registry;

namespace AndroidX.Compose.Gallery.Demos.Navigation;

/// <summary>ModalWideNavigationRail — overlay rail dismissed on item tap.</summary>
public static class ModalWideNavigationRailDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "navigation-rail-modal",
        CategoryId:  "navigation",
        Title:       "ModalWideNavigationRail",
        Description: "Overlay rail driven by expandable, collapsible, toggle, and snap state APIs.",
        Build:       c =>
        {
            var idx     = c.MutableStateOf(0);
            var collapsed = AndroidX.Compose.Material3.WideNavigationRailValue.Collapsed
                ?? throw new InvalidOperationException(
                    "WideNavigationRailValue.Collapsed was unavailable.");
            var state = c.Remember(() => new WideNavigationRailState(
                collapsed));
            return new Column
            {
                new Text($"ModalWideNavigationRail (selected: {idx}; target: {state.TargetValue}; animating: {state.IsAnimating})"),
                new Row(horizontalArrangement: Arrangement.SpacedBy(8.Dp()))
                {
                    new Button(() => _ = state.ExpandAsync()) { new Text("Expand") },
                    new Button(() => _ = state.CollapseAsync()) { new Text("Collapse") },
                    new Button(() => _ = state.ToggleAsync()) { new Text("Toggle") },
                },
                new Box
                {
                    Modifier.FillMaxWidth().Height(320),
                    new ModalWideNavigationRail(state)
                    {
                        new WideNavigationRailItem(
                            selected: idx.Value == 0,
                            onClick:  () => idx.Value = 0)
                        {
                            Icon  = new Text("🏠"),
                            Label = new Text("Home"),
                        },
                        new WideNavigationRailItem(
                            selected: idx.Value == 1,
                            onClick:  () => idx.Value = 1)
                        {
                            Icon  = new Text("🔍"),
                            Label = new Text("Search"),
                        },
                        new WideNavigationRailItem(
                            selected: idx.Value == 2,
                            onClick:  () => idx.Value = 2)
                        {
                            Icon  = new Text("⚙"),
                            Label = new Text("Settings"),
                        },
                        new WideNavigationRailItem(
                            selected: false,
                            onClick: () => _ = state.SnapToAsync(collapsed))
                        {
                            Icon  = new Text("✕"),
                            Label = new Text("Close"),
                        },
                    },
                },
            };
        });
}
