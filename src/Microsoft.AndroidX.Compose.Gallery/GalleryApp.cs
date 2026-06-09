namespace Microsoft.AndroidX.Compose.Gallery;

/// <summary>
/// Top-level composition for the gallery: theme, drawer, scaffold,
/// and the <see cref="NavHost"/> that drives the four routes
/// (<c>home</c>, <c>category/{id}</c>, <c>demo/{id}</c>, <c>search</c>).
/// </summary>
/// <remarks>
/// Designed to be the sole composition rendered by
/// <see cref="ComposeActivity.SetContent(Func{ComposableNode})"/>:
/// <code>
/// SetContent(() => GalleryApp.Build());
/// </code>
/// </remarks>
public static class GalleryApp
{
    /// <summary>Logcat tag used by gallery demos that write to the system log.</summary>
    public const string LogTag = "GALLERY";

    /// <summary>
    /// Build the gallery's root composable. Allocates the shared
    /// <see cref="NavController"/> + <see cref="DrawerStateHolder"/>
    /// inside <c>ComposeRuntime.Remember</c> so they survive recomposition.
    /// </summary>
    public static ComposableNode Build()
    {
        var nav          = ComposeRuntime.Remember(() => new NavController());
        var drawer       = ComposeRuntime.Remember(() => new DrawerStateHolder(global::AndroidX.Compose.Material3.DrawerValue.Closed));
        // Shared by GalleryScaffold (drives back-arrow / title) and the
        // drawer below (drives edge-swipe). Lives here so both see the
        // same MutableState; each route's body updates it in a
        // DisposableEffect.
        var currentRoute = ComposeRuntime.Remember(() => new MutableState<string>("home"));

        MainActivity.Nav = nav;

        return new MaterialTheme
        {
            new Surface
            {
                Modifier.Companion.FillMaxSize(),
                new ModalNavigationDrawer(drawerState: drawer)
                {
                    Drawer          = new GalleryDrawer(nav, drawer),
                    Content         = new GalleryScaffold(nav, drawer, currentRoute),
                    // Match the top-app-bar affordance: edge-swipe to open
                    // the drawer only when the hamburger is showing
                    // (i.e. at the home destination). On sub-pages the
                    // bar shows a back arrow, so swipe-to-open would
                    // contradict the visible nav contract and step on
                    // the system back-gesture.
                    GesturesEnabled = currentRoute.Value == "home",
                },
            },
        };
    }
}
