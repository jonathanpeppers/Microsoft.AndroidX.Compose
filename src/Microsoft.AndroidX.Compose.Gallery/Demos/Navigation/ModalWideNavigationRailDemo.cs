using Microsoft.AndroidX.Compose.Gallery.Registry;

namespace Microsoft.AndroidX.Compose.Gallery.Demos.Navigation;

/// <summary>ModalWideNavigationRail — overlay rail dismissed on item tap.</summary>
public static class ModalWideNavigationRailDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "navigation-rail-modal",
        CategoryId:  "navigation",
        Title:       "ModalWideNavigationRail",
        Description: "Overlay rail; tap an item or the Close row to dismiss.",
        Build:       c =>
        {
            var idx     = c.Remember(() => new MutableNumberState<int>(0));
            var visible = c.Remember(() => new MutableState<bool>(false));
            return new Column
            {
                new Text($"ModalWideNavigationRail (selected: {idx})"),
                new Row
                {
                    new Button(onClick: () => visible.Value = !visible.Value)
                    {
                        new Text(visible.Value ? "Hide modal rail" : "Open modal rail"),
                    },
                },
                visible.Value
                    ? new ModalWideNavigationRail
                    {
                        new WideNavigationRailItem(
                            selected: idx.Value == 0,
                            onClick:  () => { idx.Value = 0; visible.Value = false; })
                        {
                            Icon  = new Text("🏠"),
                            Label = new Text("Home"),
                        },
                        new WideNavigationRailItem(
                            selected: idx.Value == 1,
                            onClick:  () => { idx.Value = 1; visible.Value = false; })
                        {
                            Icon  = new Text("🔍"),
                            Label = new Text("Search"),
                        },
                        new WideNavigationRailItem(
                            selected: idx.Value == 2,
                            onClick:  () => { idx.Value = 2; visible.Value = false; })
                        {
                            Icon  = new Text("⚙"),
                            Label = new Text("Settings"),
                        },
                        new WideNavigationRailItem(selected: false, onClick: () => visible.Value = false)
                        {
                            Icon  = new Text("✕"),
                            Label = new Text("Close"),
                        },
                    }
                    : (ComposableNode?)null,
            };
        });
}
