using AndroidX.Compose.Material3;
using AndroidX.Compose.Runtime;

namespace ComposeNet;

/// <summary>
/// Material 3 <c>ModalNavigationDrawer</c> — a phone-sized drawer that
/// slides over the content with a scrim. The <see cref="Drawer"/>
/// slot typically holds a <see cref="ModalDrawerSheet"/>. The drawer
/// is opened by edge-swipe or programmatically via the underlying
/// <c>DrawerState</c>; tap the scrim to close.
/// </summary>
/// <remarks>
/// Calls the bound C# entry point directly — the wrapper is bound
/// natively. Driving open/close from C# requires Kotlin
/// <c>suspend</c> calls (<c>DrawerState.open()</c> /
/// <c>close()</c>), so this facade exposes only swipe-to-toggle
/// behavior using a <c>rememberDrawerState</c>-backed state.
/// </remarks>
public sealed class ModalNavigationDrawer : ComposableNode
{
    /// <summary>Required: the slide-in drawer panel.</summary>
    public ComposableNode? Drawer { get; set; }

    /// <summary>Required: the main content shown beneath the drawer.</summary>
    public ComposableNode? Content { get; set; }

    /// <summary>
    /// Initial drawer state on first composition. Defaults to closed.
    /// </summary>
    public bool InitiallyOpen { get; set; }

    /// <summary>
    /// Optional veto invoked before the drawer transitions between
    /// <c>Open</c> and <c>Closed</c> (Compose Kotlin
    /// <c>confirmStateChange</c>). Return <c>true</c> to allow the
    /// change, <c>false</c> to block it (e.g. don't let the user close
    /// the drawer while a form is dirty). When <c>null</c> all
    /// transitions are allowed.
    /// </summary>
    public System.Func<DrawerValue, bool>? ConfirmStateChange { get; set; }

    // Allocate once per node and reuse across recompositions — the
    // Java peer is part of `rememberDrawerState`'s cache key, so a
    // fresh adapter each pass would drop the cached DrawerState.
    readonly DrawerConfirmStateChange _confirm = new();

    internal override void Render(IComposer composer)
    {
        if (Drawer is null)
            throw new System.InvalidOperationException(
                "ModalNavigationDrawer.Drawer is required.");
        if (Content is null)
            throw new System.InvalidOperationException(
                "ModalNavigationDrawer.Content is required.");

        var initial = InitiallyOpen ? DrawerValue.Open! : DrawerValue.Closed!;

        _confirm.Callback = ConfirmStateChange;

        // Param order: initialValue (bit 0, provided), confirmStateChange
        // (bit 1, provided — passing a real non-null adapter avoids
        // Kotlin's $default substitution misfiring for the lambda
        // parameter). $default = 0.
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
        // scrimColor (4, def — 0L is not a valid color sentinel),
        // content (5, provided). $default = 0b010010 = 18.
        NavigationDrawerKt.ModalNavigationDrawer(
            drawerContent:    drawer,
            modifier:         null,
            drawerState:      state,
            gesturesEnabled:  true,
            scrimColor:       0L,
            content:          content,
            _composer:        composer,
            p7:               0,
            _changed:         0b010010);
    }
}
