namespace AndroidX.Compose.Samples.Reply;

/// <summary>
/// Builds the Reply root composition: the pinned Reply theme wrapping
/// adaptive navigation and a <see cref="NavHost"/>.
/// </summary>
/// <remarks>
/// Width classes select a bottom bar (compact), rail (medium), or permanent
/// drawer (expanded). Fold-aware list/detail remains separate.
/// </remarks>
public static class ReplyApp
{
    internal static Action<NavigationSuiteType>? NavigationTypeObserver { get; set; }

    /// <summary>Compose the Reply app at the same top-level boundary as upstream Kotlin.</summary>
    [Composable]
    public static void Content(
        NavController nav,
        ReplyState state)
    {
        var actions = new ReplyNavigationActions(nav);
        ReplyTheme.Build(BuildNavHost(nav, actions, state)).Render();
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
                BuildNavigation(Route.Inbox, actions, compact =>
                    ReplyInboxScreen.Build(
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
                        },
                        showComposeFab: compact)),
            },
            new NavDestination(Route.Articles)
                { BuildNavigation(Route.Articles, actions, _ => EmptyComingSoon.Build()) },
            new NavDestination(Route.DirectMessages)
                { BuildNavigation(Route.DirectMessages, actions, _ => EmptyComingSoon.Build()) },
            new NavDestination(Route.Groups)
                { BuildNavigation(Route.Groups, actions, _ => EmptyComingSoon.Build()) },
            new NavDestination(Route.EmailDetailPattern, entry =>
            {
                var idStr = entry.Arguments?.GetString("emailId");
                if (!long.TryParse(idStr, out var id))
                    throw new InvalidOperationException($"Invalid Reply email route argument '{idStr}'.");
                var email = LocalEmailsDataProvider.Get(id)
                    ?? throw new InvalidOperationException($"Reply email {id} was not found.");
                Action close = () => actions.CloseEmail(state);
                return BuildNavigation(Route.Inbox, actions, compact => new Box
                    {
                        new BackHandler(close),
                        ReplyEmailDetail.Build(
                            email: email,
                            onBackPressed: close,
                            showComposeFab: compact),
                    });
            }),
        };
    }

    static ComposableNode BuildNavigation(
        string route,
        ReplyNavigationActions actions,
        Func<bool, ComposableNode> bodyFactory) =>
        new Composed(c =>
        {
            var size = c.CurrentWindowAdaptiveInfo().WindowSizeClass;
            var navigationType =
                size.IsWidthAtLeastBreakpoint(
                    AndroidX.Window.Core.Layout.WindowSizeClass.WidthDpExpandedLowerBound)
                    ? NavigationSuiteType.NavigationDrawer
                    : size.IsWidthAtLeastBreakpoint(
                        AndroidX.Window.Core.Layout.WindowSizeClass.WidthDpMediumLowerBound)
                        ? NavigationSuiteType.NavigationRail
                        : NavigationSuiteType.NavigationBar;
            NavigationTypeObserver?.Invoke(navigationType);

            var body = bodyFactory(navigationType == NavigationSuiteType.NavigationBar);
            var navigation = new NavigationSuiteScaffold
            {
                NavigationSuiteType = navigationType,
                Content = route == Route.Inbox ? body : new Box
                {
                    new BackHandler(() => actions.NavigateTo(TopLevelDestinations.All[0])),
                    body,
                },
            };
            foreach (var destination in TopLevelDestinations.All)
            {
                bool selected = route == destination.Route;
                string label = c.StringResource(destination.LabelResourceId);
                navigation.Add(new NavigationSuiteItem(
                    selected: selected,
                    onClick: () => actions.NavigateTo(destination))
                {
                    NavigationSuiteType = navigationType,
                    Icon = new Icon(
                        selected ? destination.SelectedIcon : destination.UnselectedIcon,
                        label),
                    Label = new Text(label),
                });
            }
            return navigation;
        });
}
