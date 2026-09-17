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
    /// <summary>Compose the Reply app at the same top-level boundary as upstream Kotlin.</summary>
    [Composable]
    public static void Content(
        NavController nav,
        ReplyState state,
        Action<NavigationSuiteType>? navigationTypeObserver = null)
    {
        var actions = new ReplyNavigationActions(nav);
        ReplyTheme.Build(BuildNavHost(nav, actions, state, navigationTypeObserver)).Render();
    }

    static NavHost BuildNavHost(
        NavController nav,
        ReplyNavigationActions actions,
        ReplyState state,
        Action<NavigationSuiteType>? navigationTypeObserver)
    {
        return new NavHost(startDestination: Route.Inbox, navController: nav)
        {
            new NavDestination(Route.Inbox)
            {
                BuildNavigation(Route.Inbox, actions, navigationTypeObserver, compact =>
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
                        showComposeFab: compact,
                        composeFabExpanded: state.ComposeFabExpanded)),
            },
            new NavDestination(Route.Articles)
                { BuildNavigation(Route.Articles, actions, navigationTypeObserver, _ => EmptyComingSoon.Build()) },
            new NavDestination(Route.DirectMessages)
                { BuildNavigation(Route.DirectMessages, actions, navigationTypeObserver, _ => EmptyComingSoon.Build()) },
            new NavDestination(Route.Groups)
                { BuildNavigation(Route.Groups, actions, navigationTypeObserver, _ => EmptyComingSoon.Build()) },
            new NavDestination(Route.EmailDetailPattern, entry =>
            {
                var idStr = entry.Arguments?.GetString("emailId");
                if (!long.TryParse(idStr, out var id))
                    throw new InvalidOperationException($"Invalid Reply email route argument '{idStr}'.");
                var email = LocalEmailsDataProvider.Get(id)
                    ?? throw new InvalidOperationException($"Reply email {id} was not found.");
                Action close = () => actions.CloseEmail(state);
                return BuildNavigation(Route.Inbox, actions, navigationTypeObserver, compact => new Box
                    {
                        new BackHandler(close),
                        ReplyEmailDetail.Build(
                            email: email,
                            onBackPressed: close,
                            showComposeFab: compact,
                            composeFabExpanded: state.ComposeFabExpanded.Value),
                    });
            }),
        };
    }

    static ComposableNode BuildNavigation(
        string route,
        ReplyNavigationActions actions,
        Action<NavigationSuiteType>? navigationTypeObserver,
        Func<bool, ComposableNode> bodyFactory) =>
        new Composed(c =>
        {
            var size = c.CurrentWindowAdaptiveInfo(
                supportLargeAndXLargeWidth: true).WindowSizeClass;
            var navigationType = ResolveNavigationType(
                widthAtLeastMedium: size.IsWidthAtLeastBreakpoint(
                    AndroidX.Window.Core.Layout.WindowSizeClass.WidthDpMediumLowerBound),
                heightAtLeastMedium: size.IsHeightAtLeastBreakpoint(
                    AndroidX.Window.Core.Layout.WindowSizeClass.HeightDpMediumLowerBound),
                widthAtLeastLarge: size.IsWidthAtLeastBreakpoint(
                    AndroidX.Window.Core.Layout.WindowSizeClass.WidthDpLargeLowerBound));
            if (navigationTypeObserver is not null)
                c.SideEffect(() => navigationTypeObserver(navigationType));

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

    internal static NavigationSuiteType ResolveNavigationType(
        bool widthAtLeastMedium,
        bool heightAtLeastMedium,
        bool widthAtLeastLarge) =>
        !widthAtLeastMedium || !heightAtLeastMedium
            ? NavigationSuiteType.NavigationBar
            : widthAtLeastLarge
                ? NavigationSuiteType.NavigationDrawer
                : NavigationSuiteType.NavigationRail;
}
