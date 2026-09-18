namespace AndroidX.Compose.Samples.JetNews;

/// <summary>
/// JetNews home feed — top app bar with hamburger / search action row,
/// then a <see cref="LazyColumn{T}"/> with the highlighted post and the
/// recommended / popular / recent lists. Each card taps through to the
/// article screen via the supplied callback.
/// </summary>
/// <remarks>
/// Refactored for the UDF pattern: the feed and bookmarks come from a
/// <see cref="HomeViewModel"/> acquired via
/// <see cref="ComposeExtensions.ViewModel{T}(Func{T}, int, string)"/>, and the
/// body switches on a <see cref="HomeUiState"/> discriminated union
/// (<see cref="HomeUiState.Loading"/> /
/// <see cref="HomeUiState.HasPosts"/> /
/// <see cref="HomeUiState.Error"/>).
/// </remarks>
public static class HomeScreen
{
    /// <summary>Materialize the home screen.</summary>
    public static ComposableNode Build(
        BookmarksViewModel bookmarks,
        DrawerStateHolder drawerState,
        Action<string> onSelectPost,
        SnackbarController snackbars,
        IPostsRepository? repository = null) =>
        new Composed(c =>
        {
            var vm = c.ViewModel(() => new HomeViewModel(repository));
            var state = vm.UiState.Value;
            var searchOpen  = c.MutableStateOf(false);
            var searchQuery = c.MutableStateOf(string.Empty);
            var snackbarMessage = snackbars.Message.Value;
            var topBarState = c.RememberTopAppBarState();
            var scrollBehavior = c.PinnedScrollBehavior(topBarState);
            var typography = c.Typography();
            string openNavigation = c.StringResource(Resource.String.cd_open_navigation_drawer);
            string search = c.StringResource(Resource.String.cd_search);
            string closeSearch = c.StringResource(Resource.String.cd_close_search);
            string refresh = c.StringResource(Resource.String.cd_refresh);
            string appName = c.StringResource(Resource.String.app_name);
            string searchPlaceholder = c.StringResource(Resource.String.home_search);
            string loadError = c.StringResource(Resource.String.home_load_error);
            string retry = c.StringResource(Resource.String.retry);
            string topStories = c.StringResource(Resource.String.home_top_section_title);
            string popular = c.StringResource(Resource.String.home_popular_section_title);
            string history = c.StringResource(Resource.String.home_post_based_on_history);
            string searchResults = c.StringResource(Resource.String.home_search_results);
            string noSearchResults = c.StringResource(
                Resource.String.home_search_no_results,
                searchQuery.Value.Trim());

            return new Scaffold
            {
                TopBar = BuildTopBar(
                    searchOpen,
                    searchQuery,
                    drawerState,
                    vm,
                    scrollBehavior,
                    openNavigation,
                    search,
                    closeSearch,
                    refresh,
                    appName,
                    searchPlaceholder),
                SnackbarHost = snackbarMessage is null
                    ? null
                    : new Snackbar { Body = new Text(snackbarMessage) },
                BodyContent = padding => state switch
                {
                    HomeUiState.Loading       => BuildLoading(),
                    HomeUiState.Error e       => BuildError(
                        e.Message,
                        () => _ = vm.RefreshAsync(),
                        loadError,
                        retry,
                        typography),
                    HomeUiState.HasPosts h    => BuildBody(
                        h,
                        bookmarks,
                        onSelectPost,
                        vm,
                        snackbars,
                        padding,
                        searchQuery.Value,
                        scrollBehavior,
                        topStories,
                        popular,
                        history,
                        searchResults,
                        noSearchResults,
                        typography),
                    _                         => new Spacer(),
                },
            };
        });

    static ComposableNode BuildTopBar(
        MutableState<bool> searchOpen,
        MutableState<string> searchQuery,
        DrawerStateHolder drawerState,
        HomeViewModel vm,
        AndroidX.Compose.Material3.ITopAppBarScrollBehavior scrollBehavior,
        string openNavigation,
        string search,
        string closeSearch,
        string refresh,
        string appName,
        string searchPlaceholder) =>
        new CenterAlignedTopAppBar
        {
            NavigationIcon = new IconButton(onClick: () => _ = drawerState.OpenAsync())
            {
                new Icon(Resource.Drawable.ic_menu, openNavigation),
            },
            Title = searchOpen.Value
                ? new OutlinedTextField(searchQuery, singleLine: true)
                {
                    Modifier    = Modifier.FillMaxWidth().Padding(horizontal: 8),
                    Placeholder = new Text(searchPlaceholder),
                }
                : new Icon(Resource.Drawable.ic_jetnews_wordmark, appName)
                {
                    Modifier = Modifier.Height(24),
                },
            Actions = searchOpen.Value
                ? new Row
                {
                    new IconButton(onClick: () =>
                    {
                        searchQuery.Value = string.Empty;
                        searchOpen.Value  = false;
                    })
                    {
                        new Icon(Resource.Drawable.ic_close, closeSearch),
                    },
                }
                : new Row
                {
                    new IconButton(onClick: () => searchOpen.Value = true)
                    {
                        new Icon(Resource.Drawable.ic_search, search),
                    },
                    new IconButton(onClick: () => _ = vm.RefreshAsync())
                    {
                        new Icon(Resource.Drawable.ic_refresh, refresh),
                    },
                },
            ScrollBehavior = scrollBehavior,
        };

    static Box BuildLoading() =>
        new()
        {
            Modifier.FillMaxSize(),
            new CircularProgressIndicator
            {
                Modifier = Modifier.Align(Alignment.Center),
            },
        };

    static Column BuildError(
        string message,
        Action onRetry,
        string title,
        string retry,
        AndroidX.Compose.Material3.Typography typography) =>
        new()
        {
            Modifier.FillMaxSize().Padding(24),
            new Text(title).WithTypography(typography.TitleLarge),
            new Spacer { Modifier = Modifier.Height(8) },
            new Text(message).WithTypography(typography.BodyMedium),
            new Spacer { Modifier = Modifier.Height(16) },
            new Button(onClick: onRetry)
            {
                new Text(retry).WithTypography(typography.LabelLarge),
            },
        };

    static PullToRefreshBox BuildBody(HomeUiState.HasPosts state,
                                      BookmarksViewModel bookmarks,
                                      Action<string> onSelectPost,
                                      HomeViewModel vm,
                                      SnackbarController snackbars,
                                      PaddingValues padding,
                                      string query,
                                      AndroidX.Compose.Material3.ITopAppBarScrollBehavior scrollBehavior,
                                      string topStories,
                                      string popular,
                                      string history,
                                      string searchResults,
                                      string noSearchResults,
                                      AndroidX.Compose.Material3.Typography typography)
    {
        var feed = state.Feed;
        List<HomeRow> rows = [];
        if (query.Trim().Length > 0)
        {
            rows.Add(new HomeRow.SectionHeader(searchResults));
            var matches = PostSearch.Filter(feed, query);
            if (matches.Count == 0)
            {
                rows.Add(new HomeRow.SearchEmpty(noSearchResults));
            }
            else
            {
                foreach (var post in matches)
                    rows.Add(new HomeRow.Recommended(post));
            }
        }
        else
        {
            rows.Add(new HomeRow.SectionHeader(topStories));
            rows.Add(new HomeRow.Highlight(feed.Highlighted));
            rows.Add(new HomeRow.Divider());

            foreach (var post in feed.Recommended)
                rows.Add(new HomeRow.Recommended(post));
            rows.Add(new HomeRow.Divider());

            rows.Add(new HomeRow.SectionHeader(popular));
            rows.Add(new HomeRow.PopularCarousel(feed.Popular));
            rows.Add(new HomeRow.Divider());

            rows.Add(new HomeRow.SectionHeader(history));
            foreach (var post in feed.Recent)
                rows.Add(new HomeRow.Recommended(post));
        }

        return new PullToRefreshBox(
            isRefreshing: state.IsRefreshing,
            onRefresh:    () =>
            {
                // Drop the gesture when a refresh is already in flight.
                // Without this guard a quick second pull could stack
                // two LoadFeedAsync invocations whose completion order
                // isn't guaranteed.
                if (!state.IsRefreshing)
                    _ = vm.RefreshAsync();
            })
        {
            Modifier.FillMaxSize(),

            new LazyColumn<HomeRow>(
                items: rows,
                itemContent: row => BuildRow(
                    row,
                    bookmarks,
                    onSelectPost,
                    snackbars,
                    typography))
            {
                ContentPadding = padding,
                Modifier       = Modifier
                    .FillMaxSize()
                    .NestedScroll(scrollBehavior.NestedScrollConnection),
            },
        };
    }

    static ComposableNode BuildRow(HomeRow row,
                                   BookmarksViewModel bookmarks,
                                   Action<string> onSelectPost,
                                   SnackbarController snackbars,
                                   AndroidX.Compose.Material3.Typography typography) =>
        row switch
        {
            HomeRow.Highlight h        => HomeCards.BuildHighlight(h.Post, onSelectPost),
            HomeRow.SectionHeader s    => BuildSectionHeader(s.Label, typography),
            HomeRow.Recommended r      => HomeCards.BuildSimple(r.Post, bookmarks, onSelectPost, snackbars),
            HomeRow.SearchEmpty e      => new Text(e.Message)
            {
                Modifier = Modifier.Padding(16),
            }.WithTypography(typography.BodyLarge),
            HomeRow.PopularCarousel pc => BuildPopularCarousel(pc.Posts, onSelectPost),
            HomeRow.Divider            => new HorizontalDivider
            {
                Modifier = Modifier.Padding(horizontal: 14),
            },
            _ => new Spacer(),
        };

    static LazyRow<Post> BuildPopularCarousel(IReadOnlyList<Post> posts,
                                              Action<string> onSelectPost) =>
        new(items: posts,
            itemContent: p => HomeCards.BuildPopular(p, onSelectPost))
        {
            Modifier              = Modifier.FillMaxWidth().Height(244).Padding(start: 16, top: 4, end: 16, bottom: 16),
            HorizontalArrangement = Arrangement.SpacedBy(8.Dp()),
        };

    static Box BuildSectionHeader(
        string label,
        AndroidX.Compose.Material3.Typography typography) =>
        new()
        {
            Modifier.FillMaxWidth().Padding(start: 16, end: 16, top: 16, bottom: 8),
            new Text(label).WithTypography(typography.TitleMedium),
        };
}
