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
    /// <param name="bookmarks">Post ids the user has bookmarked.</param>
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
    public static ComposableNode Build(
        NavController nav,
        MutableState<string> currentRoute,
        MutableStateList<string> bookmarks,
        MutableStateList<string> selectedTopics,
        MutableStateList<string> selectedPeople,
        MutableStateList<string> selectedPublications,
        MutableState<int> interestsTab) =>
        new MaterialTheme
        {
            new ModalNavigationDrawer
            {
                Drawer  = JetnewsDrawer.Build(nav, currentRoute),
                Content = BuildNavHost(nav, currentRoute, bookmarks, selectedTopics, selectedPeople, selectedPublications, interestsTab),
            },
        };

    static NavHost BuildNavHost(
        NavController nav,
        MutableState<string> currentRoute,
        MutableStateList<string> bookmarks,
        MutableStateList<string> selectedTopics,
        MutableStateList<string> selectedPeople,
        MutableStateList<string> selectedPublications,
        MutableState<int> interestsTab)
    {
        return new NavHost(startDestination: Routes.Home, navController: nav)
        {
            new Composable(Routes.Home)
            {
                HomeScreen.Build(PostsRepo.Feed, bookmarks, postId =>
                {
                    nav.Navigate(Routes.Post(postId));
                }),
            },
            new Composable(Routes.Interests)
            {
                InterestsScreen.Build(selectedTopics, selectedPeople, selectedPublications, interestsTab),
            },
            new Composable(Routes.PostPattern, entry =>
            {
                var id = entry.Arguments?.GetString("postId") ?? string.Empty;
                return PostScreen.Build(
                    post:      PostsRepo.Find(id),
                    bookmarks: bookmarks,
                    onBack:    () => nav.NavigateUp());
            }),
        };
    }
}
