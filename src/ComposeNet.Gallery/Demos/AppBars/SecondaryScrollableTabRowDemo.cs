using ComposeNet.Gallery.Registry;

namespace ComposeNet.Gallery.Demos.AppBars;

/// <summary>SecondaryScrollableTabRow — secondary-emphasis scrolling tab strip.</summary>
public static class SecondaryScrollableTabRowDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "appbars-secondary-scrollable-tabrow",
        CategoryId:  "app-bars-tabs",
        Title:       "SecondaryScrollableTabRow",
        Description: "M3 secondary tab row variant; same selection model as PrimaryScrollableTabRow.",
        Build:       () =>
        {
            var sub = Compose.Remember(() => new MutableNumberState<int>(0));
            return new Column
            {
                new SecondaryScrollableTabRow(selectedTabIndex: sub.Value)
                {
                    new Tab(selected: sub.Value == 0, onClick: () => sub.Value = 0) { Text = new Text("One") },
                    new Tab(selected: sub.Value == 1, onClick: () => sub.Value = 1) { Text = new Text("Two") },
                    new Tab(selected: sub.Value == 2, onClick: () => sub.Value = 2) { Text = new Text("Three") },
                    new Tab(selected: sub.Value == 3, onClick: () => sub.Value = 3) { Text = new Text("Four") },
                    new Tab(selected: sub.Value == 4, onClick: () => sub.Value = 4) { Text = new Text("Five") },
                },
                new Text($"Selected: tab {sub.Value}"),
            };
        });
}
