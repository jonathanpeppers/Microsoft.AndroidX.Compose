using AndroidX.Compose.Material3;
using AndroidX.Compose.Runtime;

namespace ComposeNet;

/// <summary>
/// Material 3 <c>DismissibleNavigationDrawer</c> — a drawer that
/// pushes the main content aside when open (no scrim). The
/// <see cref="Drawer"/> slot typically holds a
/// <see cref="DismissibleDrawerSheet"/>. Toggle by horizontal swipe.
/// </summary>
/// <remarks>
/// Calls the bound C# entry point directly — the wrapper is bound
/// natively. See <see cref="ModalNavigationDrawer"/> for the
/// suspend-function caveat that forces gesture-only interaction.
/// </remarks>
public sealed class DismissibleNavigationDrawer : ComposableNode
{
    /// <summary>Required: the slide-in drawer panel.</summary>
    public ComposableNode? Drawer { get; set; }

    /// <summary>Required: the main content shown next to the drawer.</summary>
    public ComposableNode? Content { get; set; }

    /// <summary>
    /// Initial drawer state on first composition. Defaults to closed.
    /// </summary>
    public bool InitiallyOpen { get; set; }

    /// <summary>
    /// Optional veto invoked before the drawer transitions between
    /// <c>Open</c> and <c>Closed</c> (Compose Kotlin
    /// <c>confirmStateChange</c>). Return <c>true</c> to allow the
    /// change, <c>false</c> to block it. When <c>null</c> all
    /// transitions are allowed.
    /// </summary>
    public System.Func<DrawerValue, bool>? ConfirmStateChange { get; set; }

    // Allocate once per node and reuse across recompositions — the
    // Java peer is part of `rememberDrawerState`'s cache key, so a
    // fresh adapter each pass would drop the cached DrawerState.
    readonly DrawerConfirmStateChange _confirm = new();

    public override void Render(IComposer composer)
    {
        if (Drawer is null)
            throw new System.InvalidOperationException(
                "DismissibleNavigationDrawer.Drawer is required.");
        if (Content is null)
            throw new System.InvalidOperationException(
                "DismissibleNavigationDrawer.Content is required.");

        var initial = InitiallyOpen ? DrawerValue.Open! : DrawerValue.Closed!;

        _confirm.Callback = ConfirmStateChange;

        var state = NavigationDrawerKt.RememberDrawerState(
            initialValue:        initial,
            confirmStateChange:  _confirm,
            _composer:           composer,
            p3:                  0,
            _changed:            0);

        var drawer  = ComposableLambdas.Wrap2(composer, c => Drawer.Render(c));
        var content = ComposableLambdas.Wrap2(composer, c => Content.Render(c));
        // Param order: drawerContent (0, provided), modifier (1, def),
        // drawerState (2, provided), gesturesEnabled (3, provided),
        // content (4, provided). $default = 0b00010 = 2.
        NavigationDrawerKt.DismissibleNavigationDrawer(
            drawerContent:    drawer,
            modifier:         null,
            drawerState:      state,
            gesturesEnabled:  true,
            content:          content,
            _composer:        composer,
            p6:               0,
            _changed:         0b00010);
    }
}
