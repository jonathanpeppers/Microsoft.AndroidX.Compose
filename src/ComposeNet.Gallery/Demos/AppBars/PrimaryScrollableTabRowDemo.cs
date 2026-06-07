using ComposeNet.Gallery.Registry;

namespace ComposeNet.Gallery.Demos.AppBars;

/// <summary>PrimaryScrollableTabRow with mixed Tab / LeadingIconTab / CustomTab variants.</summary>
public static class PrimaryScrollableTabRowDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "appbars-primary-scrollable-tabrow",
        CategoryId:  "appbars",
        Title:       "PrimaryScrollableTabRow",
        Description: "Scrolls horizontally when tabs overflow; mixes Tab, LeadingIconTab, CustomTab.",
        Build:       () =>
        {
            var sub = Compose.Remember(() => new MutableNumberState<int>(0));
            return new Column
            {
                new PrimaryScrollableTabRow(selectedTabIndex: sub.Value)
                {
                    new Tab(selected: sub.Value == 0, onClick: () => sub.Value = 0) { Text = new Text("Greeting") },
                    new Tab(selected: sub.Value == 1, onClick: () => sub.Value = 1) { Text = new Text("Counter") },
                    new LeadingIconTab(selected: sub.Value == 2, onClick: () => sub.Value = 2)
                    {
                        Text = new Text("List"),
                        Icon = new Text("📋"),
                    },
                    new CustomTab(selected: sub.Value == 3, onClick: () => sub.Value = 3)
                    {
                        new Column { new Text("Custom"), new Text("tab") },
                    },
                },
                new Text($"Selected: tab {sub.Value}"),
            };
        });
}
