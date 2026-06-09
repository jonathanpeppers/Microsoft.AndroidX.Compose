using System;
using System.Collections.Generic;
using AndroidX.Compose.Runtime;
using ComposeNet;

namespace ComposeNet.Samples.JetNews;

/// <summary>
/// JetNews home feed — top app bar with hamburger / search action row,
/// then a <see cref="LazyColumn{T}"/> with the highlighted post and the
/// recommended / popular / recent lists. Each card taps through to the
/// article screen via the supplied callback.
/// </summary>
/// <remarks>
/// Refactored for the UDF pattern: the feed and bookmarks come from a
/// <see cref="HomeViewModel"/> acquired via
/// <see cref="Compose.ViewModel{T}(Func{T}, int, string)"/>, and the
/// body switches on a <see cref="HomeUiState"/> discriminated union
/// (<see cref="HomeUiState.Loading"/> /
/// <see cref="HomeUiState.HasPosts"/> /
/// <see cref="HomeUiState.Error"/>).
/// </remarks>
public static class HomeScreen
{
    /// <summary>Materialize the home screen.</summary>
    public static Scaffold Build(
        BookmarksViewModel bookmarks,
        DrawerStateHolder drawerState,
        Action<string> onSelectPost,
        IPostsRepository? repository = null)
    {
        var vm = Compose.ViewModel(() => new HomeViewModel(repository));
        var state = vm.UiState.CollectAsStateWithLifecycle().Value;

        return new Scaffold
        {
            TopBar = new CenterAlignedTopAppBar
            {
                NavigationIcon = new IconButton(onClick: () => _ = drawerState.OpenAsync())
                {
                    new Icon(Resource.Drawable.ic_menu, "Open navigation drawer"),
                },
                Title = new Icon(Resource.Drawable.ic_jetnews_wordmark, "JetNews")
                {
                    Modifier = Modifier.Companion.Height(24),
                },
                Actions = new Row
                {
                    new IconButton(onClick: () => _ = vm.RefreshAsync())
                    {
                        new Icon(Resource.Drawable.ic_refresh, "Refresh"),
                    },
                },
            },
            Body = state switch
            {
                HomeUiState.Loading       => BuildLoading(),
                HomeUiState.Error e       => BuildError(e.Message, () => _ = vm.RefreshAsync()),
                HomeUiState.HasPosts h    => BuildBody(h.Feed, bookmarks, onSelectPost),
                _                         => new Spacer(),
            },
        };
    }

    static Box BuildLoading() =>
        new()
        {
            Modifier.Companion.FillMaxSize(),
            new CircularProgressIndicator
            {
                Modifier = Modifier.Companion.Align(Alignment.Center),
            },
        };

    static Column BuildError(string message, Action onRetry) =>
        new()
        {
            Modifier.Companion.FillMaxSize().Padding(24),
            new Text("Couldn't load the feed")
            {
                FontSize   = 18,
                FontWeight = FontWeight.SemiBold,
            },
            new Spacer { Modifier = Modifier.Companion.Height(8) },
            new Text(message),
            new Spacer { Modifier = Modifier.Companion.Height(16) },
            new Button(onClick: onRetry)
            {
                new Text("Try again"),
            },
        };

    static LazyColumn<HomeRow> BuildBody(PostsFeed feed,
                                         BookmarksViewModel bookmarks,
                                         Action<string> onSelectPost)
    {
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

        return new LazyColumn<HomeRow>(
            items: rows,
            itemContent: row => BuildRow(row, bookmarks, onSelectPost))
        {
            Modifier = Modifier.Companion.FillMaxSize(),
        };
    }

    static ComposableNode BuildRow(HomeRow row,
                                   BookmarksViewModel bookmarks,
                                   Action<string> onSelectPost) =>
        row switch
        {
            HomeRow.Highlight h        => HomeCards.BuildHighlight(h.Post, onSelectPost),
            HomeRow.SectionHeader s    => BuildSectionHeader(s.Label),
            HomeRow.Recommended r      => HomeCards.BuildSimple(r.Post, bookmarks, onSelectPost),
            HomeRow.PopularCarousel pc => BuildPopularCarousel(pc.Posts, onSelectPost),
            HomeRow.Divider            => new HorizontalDivider
            {
                Modifier = Modifier.Companion.Padding(horizontal: 14, vertical: 0),
            },
            _ => new Spacer(),
        };

    static LazyRow<Post> BuildPopularCarousel(IReadOnlyList<Post> posts,
                                              Action<string> onSelectPost) =>
        new(items: posts,
            itemContent: p => HomeCards.BuildPopular(p, onSelectPost))
        {
            Modifier              = Modifier.Companion.FillMaxWidth().Height(244).Padding(start: 16, top: 4, end: 16, bottom: 16),
            HorizontalArrangement = Arrangement.SpacedBy(8),
        };

    static Box BuildSectionHeader(string label) =>
        new()
        {
            Modifier.Companion.FillMaxWidth().Padding(start: 16, end: 16, top: 16, bottom: 8),
            new Text(label)
            {
                FontSize   = 16,
                FontWeight = FontWeight.SemiBold,
            },
        };
}
