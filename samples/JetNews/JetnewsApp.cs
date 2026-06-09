using System;
using System.Collections.Generic;
using AndroidX.Compose.Runtime;
using ComposeNet;

namespace ComposeNet.Samples.JetNews;

/// <summary>
/// Builds the JetNews root composition: a <see cref="MaterialTheme"/>
/// containing a <see cref="ModalNavigationDrawer"/> whose content slot
/// hosts the <see cref="NavHost"/> with the three top-level screens.
/// </summary>
public static class JetnewsApp
{
    /// <summary>
    /// Materialize the JetNews tree for one composition pass.
    /// </summary>
    /// <param name="nav">Navigation controller, remembered by the caller.</param>
    /// <param name="currentRoute">
    /// Mirror of the currently-active top-level route — drives drawer
    /// item highlighting. Updated in tandem with
    /// <see cref="NavController.Navigate(string)"/> calls.
    /// </param>
    /// <param name="drawerState">
    /// State holder for the <see cref="ModalNavigationDrawer"/>.
    /// Threaded through so the Home / Interests top-bar hamburger
    /// icons can fire <see cref="DrawerStateHolder.OpenAsync"/> and
    /// the drawer items can fire
    /// <see cref="DrawerStateHolder.CloseAsync"/>.
    /// </param>
    /// <param name="bookmarks">The shared bookmarks view model.</param>
    /// <param name="selectedTopics">
    /// "Section/Topic" keys for topics the user has subscribed to (e.g.
    /// <c>"Android/Jetpack Compose"</c>).
    /// </param>
    /// <param name="selectedPeople">Names of followed people.</param>
    /// <param name="selectedPublications">Names of subscribed publications.</param>
    /// <param name="interestsTab">
    /// Currently selected tab index on the Interests screen (0 = Topics,
    /// 1 = People, 2 = Publications).
    /// </param>
    /// <param name="snackbars">
    /// App-wide snackbar controller — bookmark toggles and the share
    /// dialog all route their transient feedback through this single
    /// instance so navigating between Home and Post doesn't lose a
    /// queued message mid-fade.
    /// </param>
    /// <param name="onShare">
    /// Invoked when the user picks "Share anyway" in the article share
    /// dialog. Typically wired to <see cref="Android.Content.Intent.ActionSend"/>
    /// from <see cref="MainActivity"/> so the chooser launches with the
    /// activity as its <see cref="Android.Content.Context"/>.
    /// </param>
    public static ComposableNode Build(
        NavController nav,
        MutableState<string> currentRoute,
        DrawerStateHolder drawerState,
        BookmarksViewModel bookmarks,
        MutableStateList<string> selectedTopics,
        MutableStateList<string> selectedPeople,
        MutableStateList<string> selectedPublications,
        MutableState<int> interestsTab,
        SnackbarController snackbars,
        Action<Post>? onShare = null) =>
        new MaterialTheme
        {
            new ModalNavigationDrawer(drawerState)
            {
                Drawer  = JetnewsDrawer.Build(nav, currentRoute, drawerState),
                Content = BuildNavHost(nav, currentRoute, drawerState, bookmarks, selectedTopics, selectedPeople, selectedPublications, interestsTab, snackbars, onShare),
            },
        };

    static NavHost BuildNavHost(
        NavController nav,
        MutableState<string> currentRoute,
        DrawerStateHolder drawerState,
        BookmarksViewModel bookmarks,
        MutableStateList<string> selectedTopics,
        MutableStateList<string> selectedPeople,
        MutableStateList<string> selectedPublications,
        MutableState<int> interestsTab,
        SnackbarController snackbars,
        Action<Post>? onShare)
    {
        return new NavHost(startDestination: Routes.Home, navController: nav)
        {
            new Composable(Routes.Home, _ =>
                HomeScreen.Build(bookmarks, drawerState, postId =>
                {
                    nav.Navigate(Routes.Post(postId));
                }, snackbars)),
            new Composable(Routes.Interests)
            {
                InterestsScreen.Build(selectedTopics, selectedPeople, selectedPublications, interestsTab, drawerState),
            },
            new Composable(Routes.PostPattern, entry =>
            {
                var id = entry.Arguments?.GetString("postId") ?? string.Empty;
                return PostScreen.Build(
                    post:      PostsRepo.Find(id),
                    bookmarks: bookmarks,
                    onBack:    () => nav.NavigateUp(),
                    snackbars: snackbars,
                    onShare:   onShare);
            }),
        };
    }
}
