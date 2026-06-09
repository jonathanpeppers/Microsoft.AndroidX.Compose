using global::AndroidX.Compose.Material3;
using Microsoft.AndroidX.Compose.Gallery.Registry;

namespace Microsoft.AndroidX.Compose.Gallery.Demos.Navigation;

/// <summary>DismissibleNavigationDrawer — pushes content sideways; initially open.</summary>
public static class DismissibleDrawerDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "navigation-drawer-dismissible",
        CategoryId:  "navigation",
        Title:       "DismissibleNavigationDrawer",
        Description: "Drawer pushes the content rather than overlaying. Starts open via InitialValue.",
        Build:       () =>
        {
            var count = ComposeRuntime.Remember(() => new MutableNumberState<int>(0));
            return new Box
            {
                Modifier.Companion.Height(320),
                new DismissibleNavigationDrawer
                {
                    InitialValue = DrawerValue.Open!,
                    Drawer = new DismissibleDrawerSheet
                    {
                        new Text("Dismissible drawer"),
                        new Text("• Inbox"),
                        new Text("• Sent"),
                        new Text("• Drafts"),
                    },
                    Content = new Column
                    {
                        new Text("Main content"),
                        new Text("Swipe horizontally to toggle"),
                        new Text($"Count: {count}"),
                        new Button(onClick: () => count++) { new Text("+1") },
                    },
                },
            };
        });
}
