using System;
using System.Linq;
using AndroidX.Compose.Runtime;
using ComposeNet.Gallery.Registry;
using ComposeNet.Gallery.Screens;

namespace ComposeNet.Gallery;

/// <summary>
/// Top-level composition for the gallery: theme, drawer, scaffold,
/// and the <see cref="NavHost"/> that drives the four routes
/// (<c>home</c>, <c>category/{id}</c>, <c>demo/{id}</c>, <c>search</c>).
/// </summary>
/// <remarks>
/// Designed to be the sole composition rendered by
/// <see cref="ComposeActivity.SetContent(System.Func{ComposableNode})"/>:
/// <code>
/// SetContent(() => GalleryApp.Build());
/// </code>
/// </remarks>
public static class GalleryApp
{
    /// <summary>
    /// Build the gallery's root composable. Allocates the shared
    /// <see cref="NavController"/> + <see cref="DrawerStateHolder"/>
    /// inside <c>Compose.Remember</c> so they survive recomposition.
    /// </summary>
    public static ComposableNode Build()
    {
        var nav    = Compose.Remember(() => new NavController());
        var drawer = Compose.Remember(() => new DrawerStateHolder(AndroidX.Compose.Material3.DrawerValue.Closed));

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
