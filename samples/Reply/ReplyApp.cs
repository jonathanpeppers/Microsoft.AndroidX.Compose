using System.Collections.Generic;
using AndroidX.Compose.Runtime;
using ComposeNet;

namespace ComposeNet.Samples.Reply;

/// <summary>
/// Builds the Reply root composition: a <see cref="MaterialTheme"/>
/// wrapping a <see cref="Scaffold"/> with a bottom <c>NavigationBar</c>
/// and a <see cref="NavHost"/> body.
/// </summary>
/// <remarks>
/// Upstream Reply uses <c>NavigationSuiteScaffoldLayout</c> +
/// <c>WindowSizeClass</c> to pick between bottom nav (compact), nav
/// rail (medium), and permanent drawer (expanded). That adaptive
/// scaffold isn't bound yet (#143), so this port pins to bottom nav.
/// </remarks>
public static class ReplyApp
{
    /// <summary>Materialize the Reply tree for one composition pass.</summary>
    public static ComposableNode Build(
        NavController        nav,
        MutableState<string> currentRoute,
        MutableState<long?>  openedEmailId,
        MutableStateList<long> selectedEmailIds) =>
        new MaterialTheme
        {
            new Scaffold
            {
                BottomBar = ReplyBottomNavigationBar.Build(
                    currentRoute: currentRoute.Value,
                    onNavigate:   destination =>
                    {
                        currentRoute.Value = destination.Route;
                        nav.Navigate(destination.Route);
                    }),
                Body = BuildNavHost(nav, currentRoute, openedEmailId, selectedEmailIds),
            },
        };

    static NavHost BuildNavHost(
        NavController          nav,
        MutableState<string>   currentRoute,
        MutableState<long?>    openedEmailId,
        MutableStateList<long> selectedEmailIds)
    {
        return new NavHost(startDestination: Route.Inbox, navController: nav)
        {
            new Composable(Route.Inbox)
            {
                ReplyInboxScreen.Build(
                    emails:           LocalEmailsDataProvider.AllEmails,
                    openedEmailId:    openedEmailId.Value,
                    selectedEmailIds: selectedEmailIds,
                    navigateToDetail: id =>
                    {
                        openedEmailId.Value = id;
                        nav.Navigate(Route.EmailDetail(id));
                    },
                    toggleSelection: id =>
                    {
                        if (selectedEmailIds.Contains(id))
                            selectedEmailIds.Remove(id);
                        else
                            selectedEmailIds.Add(id);
                    }),
            },
            new Composable(Route.Articles)   { EmptyComingSoon.Build() },
            new Composable(Route.DirectMessages) { EmptyComingSoon.Build() },
            new Composable(Route.Groups)     { EmptyComingSoon.Build() },
            new Composable(Route.EmailDetailPattern, entry =>
            {
                var idStr = entry.Arguments?.GetString("emailId") ?? "0";
                if (!long.TryParse(idStr, out var id))
                    id = 0;
                var email = LocalEmailsDataProvider.Get(id)
                            ?? LocalEmailsDataProvider.AllEmails[0];
                return ReplyEmailDetail.Build(
                    email:         email,
                    onBackPressed: () =>
                    {
                        openedEmailId.Value = null;
                        nav.NavigateUp();
                    });
            }),
        };
    }
}
