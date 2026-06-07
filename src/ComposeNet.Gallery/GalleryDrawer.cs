using AndroidX.Compose.Runtime;
using ComposeNet.Gallery.Registry;

namespace ComposeNet.Gallery;

/// <summary>
/// The drawer body that slides in from the left. Lists Home plus every
/// <see cref="Catalog.Categories"/> entry; tapping a row navigates and
/// closes the drawer (fire-and-forget — the close animation runs in
/// parallel with the destination's first composition).
/// </summary>
public sealed class GalleryDrawer : ComposableNode
{
    readonly NavController _nav;
    readonly DrawerStateHolder _drawer;

    /// <summary>Construct a drawer bound to <paramref name="nav"/> + <paramref name="drawer"/>.</summary>
    public GalleryDrawer(NavController nav, DrawerStateHolder drawer)
    {
        _nav    = nav;
        _drawer = drawer;
    }

    public override void Render(IComposer composer)
    {
        var sheet = new ModalDrawerSheet
        {
            Modifier.Companion.Padding(8),
            new Text("ComposeNet Gallery"),
            new Spacer { Modifier = Modifier.Companion.Height(8) },
            new HorizontalDivider(),
            new Spacer { Modifier = Modifier.Companion.Height(8) },
            new GalleryDrawerItem("🏠", "Home", () => Navigate("home")),
            new HorizontalDivider { Modifier = Modifier.Companion.Padding(0, 8) },
        };

        foreach (var category in Catalog.Categories)
        {
            var c = category;
            sheet.Add(new GalleryDrawerItem(
                c.Glyph,
                c.Title,
                () => Navigate($"category/{c.Id}")));
        }

        sheet.Render(composer);
    }

    void Navigate(string route)
    {
        // Fire-and-forget — the close animation runs in parallel with
        // the destination's first composition. Wrapping in async/await
        // would require a host coroutine we don't have here; the
        // SuspendBridge already invokes Close on the UI dispatcher.
        _ = _drawer.CloseAsync();
        _nav.Navigate(route);
    }
}
