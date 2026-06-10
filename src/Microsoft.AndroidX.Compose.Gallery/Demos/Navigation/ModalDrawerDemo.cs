using AndroidX.Compose.Gallery.Registry;

namespace AndroidX.Compose.Gallery.Demos.Navigation;

/// <summary>ModalNavigationDrawer — swipes in over the content with a scrim.</summary>
public static class ModalDrawerDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "navigation-drawer-modal",
        CategoryId:  "navigation",
        Title:       "ModalNavigationDrawer",
        Description: "Edge-swipe to reveal; scrim taps dismiss.",
        Build:       c =>
        {
            var count = c.MutableStateOf(0);
            // Drawers fill their parent. Bound them so they don't fight the
            // gallery's outer scroll for height.
            return new Box
            {
                Modifier.Companion.Height(320),
                new ModalNavigationDrawer
                {
                    Drawer = new ModalDrawerSheet
                    {
                        new Text("Modal drawer"),
                        new Text("• Inbox"),
                        new Text("• Sent"),
                        new Text("• Drafts"),
                    },
                    Content = new Column
                    {
                        new Text("Main content"),
                        new Text("Swipe right from edge →"),
                        new Text($"Count: {count}"),
                        new Button(onClick: () => count++) { new Text("+1") },
                    },
                },
            };
        });
}
