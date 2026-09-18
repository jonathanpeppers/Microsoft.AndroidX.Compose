using AndroidX.Compose.Material3.Adaptive;
using AndroidX.Compose.Material3.Adaptive.Layout;
using AndroidX.Compose.Runtime;

namespace AndroidX.Compose.Samples.Reply;

/// <summary>
/// Builds the Reply root composition: the pinned Reply theme wrapping
/// adaptive navigation and a <see cref="NavHost"/>.
/// </summary>
/// <remarks>
/// Width classes select a bottom bar (compact), rail (medium), or permanent
/// drawer (expanded). Material 3 adaptive pane directives place list and
/// detail content around separating or occluding vertical hinges.
/// </remarks>
public static class ReplyApp
{
    /// <summary>Compose the Reply app at the same top-level boundary as upstream Kotlin.</summary>
    [Composable]
    public static void Content(
        NavController nav,
        ReplyState state,
        Action<NavigationSuiteType>? navigationTypeObserver = null,
        WindowAdaptiveInfo? adaptiveInfoOverride = null,
        Action<PaneScaffoldDirective>? paneDirectiveObserver = null)
    {
        var actions = new ReplyNavigationActions(nav);
        var paneNavigator =
            Composables.RememberListDetailPaneScaffoldNavigator<long>(
                windowAdaptiveInfo: adaptiveInfoOverride);
        ReplyTheme.Build(BuildNavHost(
            nav,
            actions,
            state,
            paneNavigator,
            paneDirectiveObserver,
            navigationTypeObserver)).Render();
    }

    static NavHost BuildNavHost(
        NavController nav,
        ReplyNavigationActions actions,
        ReplyState state,
        ListDetailPaneScaffoldNavigator<long> paneNavigator,
        Action<PaneScaffoldDirective>? paneDirectiveObserver,
        Action<NavigationSuiteType>? navigationTypeObserver)
    {
        return new NavHost(startDestination: Route.Inbox, navController: nav)
        {
            new NavDestination(Route.Inbox)
            {
                BuildNavigation(Route.Inbox, actions, navigationTypeObserver, compact =>
                    BuildListDetail(
                        actions,
                        state,
                        paneNavigator,
                        paneDirectiveObserver,
                        selectedEmail: null,
                        compact)),
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
                return BuildNavigation(
                    Route.Inbox,
                    actions,
                    navigationTypeObserver,
                    compact => BuildListDetail(
                        actions,
                        state,
                        paneNavigator,
                        paneDirectiveObserver,
                        email,
                        compact));
            }),
        };
    }

    static ComposableNode BuildListDetail(
        ReplyNavigationActions actions,
        ReplyState state,
        ListDetailPaneScaffoldNavigator<long> paneNavigator,
        Action<PaneScaffoldDirective>? paneDirectiveObserver,
        Email? selectedEmail,
        bool compact) =>
        new Composed(c =>
        {
            if (paneDirectiveObserver is not null)
            {
                var directive = paneNavigator.ScaffoldDirective;
                c.SideEffect(() => paneDirectiveObserver(directive));
            }
            var scope = c.RememberCoroutineScope();
            void Open(long id) => Run(scope, async ct =>
            {
                await paneNavigator.NavigateToAsync(
                    AdaptivePaneRole.Detail,
                    id,
                    ct);
                actions.OpenEmail(id);
                state.OpenedEmailId.Value = id;
            });
            void Close() => Run(scope, async ct =>
            {
                const PaneBackNavigationBehavior behavior =
                    PaneBackNavigationBehavior
                        .PopUntilCurrentDestinationChange;
                if (paneNavigator.CanNavigateBack(behavior))
                {
                    await paneNavigator.NavigateBackAsync(
                        behavior,
                        cancellationToken: ct);
                }
                actions.CloseEmail(state);
            });
            long selectedEmailId = selectedEmail?.Id ?? 0L;
            c.LaunchedEffect(selectedEmailId, async ct =>
            {
                if (selectedEmail is null)
                {
                    if (paneNavigator.HasCurrentDestination &&
                        paneNavigator.CurrentPane != AdaptivePaneRole.List)
                    {
                        const PaneBackNavigationBehavior behavior =
                            PaneBackNavigationBehavior
                                .PopUntilCurrentDestinationChange;
                        if (paneNavigator.CanNavigateBack(behavior))
                        {
                            await paneNavigator.NavigateBackAsync(
                                behavior,
                                cancellationToken: ct);
                        }
                        else
                        {
                            await paneNavigator.NavigateToAsync(
                                AdaptivePaneRole.List,
                                cancellationToken: ct);
                        }
                    }
                }
                else if (
                    paneNavigator.CurrentPane != AdaptivePaneRole.Detail ||
                    paneNavigator.CurrentContentKey != selectedEmail.Id)
                {
                    await paneNavigator.NavigateToAsync(
                        AdaptivePaneRole.Detail,
                        selectedEmail.Id,
                        ct);
                }
            });

            var list = ReplyInboxScreen.Build(
                emails:           LocalEmailsDataProvider.AllEmails,
                openedEmailId:    state.OpenedEmailId.Value,
                selectedEmailIds: state.SelectedEmailIds,
                navigateToDetail: Open,
                toggleSelection: id =>
                {
                    if (state.SelectedEmailIds.Contains(id))
                        state.SelectedEmailIds.Remove(id);
                    else
                        state.SelectedEmailIds.Add(id);
                },
                showComposeFab: compact,
                composeFabExpanded: state.ComposeFabExpanded);
            ComposableNode detail = selectedEmail is null
                ? BuildEmptyDetail(c)
                : new Box
                {
                    new BackHandler(Close),
                    ReplyEmailDetail.Build(
                        email: selectedEmail,
                        onBackPressed: Close,
                        showComposeFab: compact,
                        composeFabExpanded: state.ComposeFabExpanded.Value),
                };

            return new ListDetailPaneScaffold<long>(paneNavigator)
            {
                Modifier = Modifier.FillMaxSize()
                    .Semantics(s => s.TestTagsAsResourceId(true)),
                ListPane = new Box
                {
                    Modifier.FillMaxSize().TestTag("reply-list-pane"),
                    list,
                },
                DetailPane = new Box
                {
                    Modifier.FillMaxSize().TestTag("reply-detail-pane"),
                    detail,
                },
            };
        });

    static ComposableNode BuildEmptyDetail(IComposer composer) =>
        new Box
        {
            Modifier.FillMaxSize(),
            new Text(composer.StringResource(Resource.String.reply_select_email))
            {
                Modifier = Modifier.Align(Alignment.Center),
            },
        };

    static async void Run(
        CoroutineScope scope,
        Func<CancellationToken, Task> action)
    {
        try
        {
            await scope.Launch(action);
        }
        catch (OperationCanceledException)
        {
            // Leaving composition cancels this await; Kotlin may finish the transition.
        }
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
