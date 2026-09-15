namespace AndroidX.Compose.Samples.Reply;

/// <summary>
/// Builds the Reply root composition: a <see cref="MaterialTheme"/>
/// wrapping a <see cref="NavHost"/> whose destinations own their
/// <see cref="Scaffold"/> and selected bottom-navigation item.
/// </summary>
/// <remarks>
/// Upstream Reply uses <c>NavigationSuiteScaffoldLayout</c> +
/// <c>WindowSizeClass</c> to pick between bottom nav (compact), nav
/// rail (medium), and permanent drawer (expanded).
/// The adaptive APIs are available, but this port deliberately keeps
/// the single-pane, bottom-navigation layout; fold-aware integration is separate.
/// </remarks>
public static class ReplyApp
{
    /// <summary>Compose the Reply app at the same top-level boundary as upstream Kotlin.</summary>
    [Composable]
    public static void Content(
        NavController nav,
        ReplyState state)
    {
        var actions = new ReplyNavigationActions(nav);
        new MaterialTheme
        {
            BuildNavHost(nav, actions, state),
        }.Render();
    }

    static NavHost BuildNavHost(
        NavController nav,
        ReplyNavigationActions actions,
        ReplyState state)
    {
        return new NavHost(startDestination: Route.Inbox, navController: nav)
        {
            new NavDestination(Route.Inbox)
            {
                BuildScaffold(Route.Inbox, actions, ReplyInboxScreen.Build(
                    emails:           LocalEmailsDataProvider.AllEmails,
                    openedEmailId:    state.OpenedEmailId.Value,
                    selectedEmailIds: state.SelectedEmailIds,
                    navigateToDetail: id =>
                    {
                        actions.OpenEmail(id);
                        state.OpenedEmailId.Value = id;
                    },
                    toggleSelection: id =>
                    {
                        if (state.SelectedEmailIds.Contains(id))
                            state.SelectedEmailIds.Remove(id);
                        else
                            state.SelectedEmailIds.Add(id);
                    })),
            },
            new NavDestination(Route.Articles)
                { BuildScaffold(Route.Articles, actions, EmptyComingSoon.Build()) },
            new NavDestination(Route.DirectMessages)
                { BuildScaffold(Route.DirectMessages, actions, EmptyComingSoon.Build()) },
            new NavDestination(Route.Groups)
                { BuildScaffold(Route.Groups, actions, EmptyComingSoon.Build()) },
            new NavDestination(Route.EmailDetailPattern, entry =>
            {
                var idStr = entry.Arguments?.GetString("emailId");
                if (!long.TryParse(idStr, out var id))
                    throw new InvalidOperationException($"Invalid Reply email route argument '{idStr}'.");
                var email = LocalEmailsDataProvider.Get(id)
                    ?? throw new InvalidOperationException($"Reply email {id} was not found.");
                Action close = () => actions.CloseEmail(state);
                return BuildScaffold(Route.Inbox, actions, new Box
                {
                    new BackHandler(close),
                    ReplyEmailDetail.Build(email: email, onBackPressed: close),
                });
            }),
        };
    }

    static Scaffold BuildScaffold(
        string route, ReplyNavigationActions actions, ComposableNode body) => new()
    {
        BottomBar = ReplyBottomNavigationBar.Build(route, actions.NavigateTo),
        // Kotlin keeps detail inside Inbox. Restore that tab's saved route on
        // top-level Back too, rather than popping directly to the bare list.
        Body = route == Route.Inbox ? body : new Box
        {
            new BackHandler(() => actions.NavigateTo(TopLevelDestinations.All[0])),
            body,
        },
    };
}
