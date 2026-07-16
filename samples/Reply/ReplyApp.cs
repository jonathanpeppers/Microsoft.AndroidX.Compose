namespace AndroidX.Compose.Samples.Reply;

/// <summary>
/// Builds the Reply root composition: a <see cref="MaterialTheme"/>
/// wrapping a <see cref="Scaffold"/> with a bottom <c>NavigationBar</c>
/// and a <see cref="NavHost"/> body.
/// </summary>
/// <remarks>
/// Upstream Reply uses <c>NavigationSuiteScaffoldLayout</c> +
/// <c>WindowSizeClass</c> to pick between bottom nav (compact), nav
/// rail (medium), and permanent drawer (expanded). The
/// <c>WindowSizeClass</c> read itself is now available (issue #143 —
/// see <c>composer.CurrentWindowAdaptiveInfo()</c>), but
/// <c>NavigationSuiteScaffold</c> lives in the
/// <c>Xamarin.AndroidX.Compose.Material3.Adaptive.NavigationSuite</c>
/// package which isn't referenced yet, so this port still pins to
/// bottom nav.
/// </remarks>
public static class ReplyApp
{
    /// <summary>Compose the Reply app at the same top-level boundary as upstream Kotlin.</summary>
    [Composable]
    public static void Content(
        NavController        nav,
        MutableState<string> currentRoute,
        MutableState<long>   openedEmailId,
        MutableStateList<long> selectedEmailIds)
    {
        new MaterialTheme
        {
            new Scaffold
            {
                BottomBar = ReplyBottomNavigationBar.Build(
                    currentRoute: currentRoute.Value,
                    onNavigate:   destination =>
                    {
                        currentRoute.Value = destination.Route;
                        new ReplyNavigationActions(nav).NavigateTo(destination);
                    }),
                Body = BuildNavHost(nav, currentRoute, openedEmailId, selectedEmailIds),
            },
        }.Render();
    }

    static NavHost BuildNavHost(
        NavController          nav,
        MutableState<string>   currentRoute,
        MutableState<long>     openedEmailId,
        MutableStateList<long> selectedEmailIds)
    {
        return new NavHost(startDestination: Route.Inbox, navController: nav)
        {
            new NavDestination(Route.Inbox)
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
            new NavDestination(Route.Articles)   { EmptyComingSoon.Build() },
            new NavDestination(Route.DirectMessages) { EmptyComingSoon.Build() },
            new NavDestination(Route.Groups)     { EmptyComingSoon.Build() },
            new NavDestination(Route.EmailDetailPattern, entry =>
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
                        openedEmailId.Value = 0L;
                        nav.NavigateUp();
                    });
            }),
        };
    }
}
