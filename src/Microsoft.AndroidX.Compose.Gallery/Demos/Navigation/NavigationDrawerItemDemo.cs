using AndroidX.Compose.Gallery.Registry;

namespace AndroidX.Compose.Gallery.Demos.Navigation;

/// <summary>
/// NavigationDrawerItem — selected-state pill, icon and badge slots.
/// </summary>
public static class NavigationDrawerItemDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "navigation-drawer-item",
        CategoryId:  "navigation",
        Title:       "NavigationDrawerItem",
        Description: "Selected-state pill, Icon and Badge slots, click handling.",
        Build:       c =>
        {
            var selected = c.Remember(() => new MutableNumberState<int>(0));
            string[] labels = { "Inbox", "Outbox", "Favorites", "Trash" };
            return new PermanentDrawerSheet
            {
                new Text("Mail") { Modifier = Modifier.Padding(horizontal: 28, vertical: 16) },
                new NavigationDrawerItem(selected: selected.Value == 0, onClick: () => selected.Value = 0)
                {
                    Label = new Text("Inbox"),
                    Icon  = new Text("📥"),
                    Badge = new Text("24"),
                },
                new NavigationDrawerItem(selected: selected.Value == 1, onClick: () => selected.Value = 1)
                {
                    Label = new Text("Outbox"),
                    Icon  = new Text("📤"),
                },
                new NavigationDrawerItem(selected: selected.Value == 2, onClick: () => selected.Value = 2)
                {
                    Label = new Text("Favorites"),
                    Icon  = new Text("⭐"),
                    Badge = new Text("3"),
                },
                new NavigationDrawerItem(selected: selected.Value == 3, onClick: () => selected.Value = 3)
                {
                    Label = new Text("Trash"),
                    Icon  = new Text("🗑️"),
                },
                new Text($"Selected: {labels[selected.Value]}")
                {
                    Modifier = Modifier.Padding(horizontal: 28, vertical: 16),
                },
            };
        });
}
