using ComposeNet.Gallery.Registry;

namespace ComposeNet.Gallery.Demos.Navigation;

/// <summary>PermanentNavigationDrawer — always visible; ideal for tablets / foldables.</summary>
public static class PermanentDrawerDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "navigation-drawer-permanent",
        CategoryId:  "navigation",
        Title:       "PermanentNavigationDrawer",
        Description: "Drawer is always shown beside the content.",
        Build:       () =>
        {
            var count = Compose.Remember(() => new MutableNumberState<int>(0));
            return new PermanentNavigationDrawer
            {
                Drawer = new PermanentDrawerSheet
                {
                    new Text("Permanent drawer"),
                    new Text("• Inbox"),
                    new Text("• Sent"),
                    new Text("• Drafts"),
                },
                Content = new Column
                {
                    new Text("Main content"),
                    new Text($"Count: {count}"),
                    new Button(onClick: () => count++) { new Text("+1") },
                },
            };
        });
}
