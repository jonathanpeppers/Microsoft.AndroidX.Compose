using AndroidX.Compose.Material3;
using AndroidX.Compose.Runtime;

namespace ComposeNet;

/// <summary>
/// Material 3 <c>PermanentNavigationDrawer</c> — always-visible side
/// drawer, intended for tablets / large screens. The
/// <see cref="Drawer"/> slot typically holds a
/// <see cref="PermanentDrawerSheet"/>.
/// </summary>
/// <remarks>
/// Calls the bound C# entry point directly — the wrapper isn't
/// hash-mangled so no JNI bridge is needed. Only the inner
/// <c>PermanentDrawerSheet-afqeVBk</c> overload is stripped (see
/// <see cref="PermanentDrawerSheet"/>).
/// </remarks>
public sealed class PermanentNavigationDrawer : ComposableNode
{
    /// <summary>Required: the always-visible drawer panel.</summary>
    public ComposableNode? Drawer { get; set; }

    /// <summary>Required: the main content shown next to the drawer.</summary>
    public ComposableNode? Content { get; set; }

    public override void Render(IComposer composer)
    {
        if (Drawer is null)
            throw new System.InvalidOperationException(
                "PermanentNavigationDrawer.Drawer is required.");
        if (Content is null)
            throw new System.InvalidOperationException(
                "PermanentNavigationDrawer.Content is required.");

        var drawer  = ComposableLambdas.Wrap2(composer, c => Drawer.Render(c));
        var content = ComposableLambdas.Wrap2(composer, c => Content.Render(c));
        // Param order: drawerContent (bit 0, provided), modifier (bit 1,
        // defaulted), content (bit 2, provided). $default = 0b010 = 2.
        NavigationDrawerKt.PermanentNavigationDrawer(
            drawerContent: drawer,
            modifier:      null,
            content:       content,
            _composer:     composer,
            p4:            0,
            _changed:      0b010);
    }
}
