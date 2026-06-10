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
            var state = vm.UiState.CollectAsStateWithLifecycle().Value;

            // Search-bar toggle: tap the magnifier in the action row to swap
            // the wordmark title for an inline OutlinedTextField. The search
            // is purely visual today — wiring it to filter the feed needs
            // Modifier.interceptKey(Key.Enter), tracked in #159.
            var searchOpen  = c.MutableStateOf(false);
            var searchQuery = c.MutableStateOf(string.Empty);
            var snackbarMessage = snackbars.Message.Value;

            return new Scaffold
            {
                TopBar = BuildTopBar(searchOpen, searchQuery, drawerState, vm),
                SnackbarHost = snackbarMessage is null
                    ? null
                    : new Snackbar { Body = new Text(snackbarMessage) },
                Body = state switch
                {
                    HomeUiState.Loading       => BuildLoading(),
                    HomeUiState.Error e       => BuildError(e.Message, () => _ = vm.RefreshAsync()),
                    HomeUiState.HasPosts h    => BuildBody(h, bookmarks, onSelectPost, vm, snackbars),
                    _                         => new Spacer(),
                },
            };
        });

    static ComposableNode BuildTopBar(
        MutableState<bool> searchOpen,
        MutableState<string> searchQuery,
        DrawerStateHolder drawerState,
        HomeViewModel vm) =>
        new CenterAlignedTopAppBar
        {
            NavigationIcon = new IconButton(onClick: () => _ = drawerState.OpenAsync())
            {
                new Icon(Resource.Drawable.ic_menu, "Open navigation drawer"),
            },
            Title = searchOpen.Value
                ? new OutlinedTextField(searchQuery)
                {
                    Modifier    = Modifier.FillMaxWidth().Padding(horizontal: 8, vertical: 0),
                    SingleLine  = true,
                    Placeholder = new Text("Search JetNews"),
                }
                : new Icon(Resource.Drawable.ic_jetnews_wordmark, "JetNews")
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
                        new Icon(Resource.Drawable.ic_close, "Close search"),
                    },
                }
                : new Row
                {
                    new IconButton(onClick: () => searchOpen.Value = true)
                    {
                        new Icon(Resource.Drawable.ic_search, "Search"),
                    },
                    new IconButton(onClick: () => _ = vm.RefreshAsync())
                    {
                        new Icon(Resource.Drawable.ic_refresh, "Refresh"),
                    },
                },
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

    static Column BuildError(string message, Action onRetry) =>
        new()
        {
            Modifier.FillMaxSize().Padding(24),
            new Text("Couldn't load the feed")
            {
                FontSize   = 18,
                FontWeight = FontWeight.SemiBold,
            },
            new Spacer { Modifier = Modifier.Height(8) },
            new Text(message),
            new Spacer { Modifier = Modifier.Height(16) },
            new Button(onClick: onRetry)
            {
                new Text("Try again"),
            },
        };

    static PullToRefreshBox BuildBody(HomeUiState.HasPosts state,
                                      BookmarksViewModel bookmarks,
                                      Action<string> onSelectPost,
                                      HomeViewModel vm,
                                      SnackbarController snackbars)
    {
        var feed = state.Feed;
        var rows = new List<HomeRow>
        {
            new HomeRow.SectionHeader("Top stories for you"),
            new HomeRow.Highlight(feed.Highlighted),
            new HomeRow.Divider(),
        };

        foreach (var p in feed.Recommended)
            rows.Add(new HomeRow.Recommended(p));
        rows.Add(new HomeRow.Divider());

        rows.Add(new HomeRow.SectionHeader("Popular on JetNews"));
        rows.Add(new HomeRow.PopularCarousel(feed.Popular));
        rows.Add(new HomeRow.Divider());

        rows.Add(new HomeRow.SectionHeader("Based on your history"));
        foreach (var p in feed.Recent)
            rows.Add(new HomeRow.Recommended(p));

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
                itemContent: row => BuildRow(row, bookmarks, onSelectPost, snackbars))
            {
                Modifier = Modifier.FillMaxSize(),
            },
        };
    }

    static ComposableNode BuildRow(HomeRow row,
                                   BookmarksViewModel bookmarks,
                                   Action<string> onSelectPost,
                                   SnackbarController snackbars) =>
        row switch
        {
            HomeRow.Highlight h        => HomeCards.BuildHighlight(h.Post, onSelectPost),
            HomeRow.SectionHeader s    => BuildSectionHeader(s.Label),
            HomeRow.Recommended r      => HomeCards.BuildSimple(r.Post, bookmarks, onSelectPost, snackbars),
            HomeRow.PopularCarousel pc => BuildPopularCarousel(pc.Posts, onSelectPost),
            HomeRow.Divider            => new HorizontalDivider
            {
                Modifier = Modifier.Padding(horizontal: 14, vertical: 0),
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

    static Box BuildSectionHeader(string label) =>
        new()
        {
            Modifier.FillMaxWidth().Padding(start: 16, end: 16, top: 16, bottom: 8),
            new Text(label)
            {
                FontSize   = 16,
                FontWeight = FontWeight.SemiBold,
            },
        };
}
