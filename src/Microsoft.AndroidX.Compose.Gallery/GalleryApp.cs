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
    /// <summary>
    /// Build the gallery's root composable. Allocates the shared
    /// <see cref="NavController"/> + <see cref="DrawerStateHolder"/>
    /// inside <c>ComposeRuntime.Remember</c> so they survive recomposition.
    /// </summary>
    public static ComposableNode Build()
    {
        var nav    = ComposeRuntime.Remember(() => new NavController());
        var drawer = ComposeRuntime.Remember(() => new DrawerStateHolder(global::AndroidX.Compose.Material3.DrawerValue.Closed));

        MainActivity.Nav = nav;

        return new MaterialTheme
        {
            new Surface
            {
                Modifier.Companion.FillMaxSize(),
                new ModalNavigationDrawer(drawerState: drawer)
                {
                    Drawer       = new GalleryDrawer(nav, drawer),
                    Content      = new GalleryScaffold(nav, drawer),
                },
            },
        };
    }
}
