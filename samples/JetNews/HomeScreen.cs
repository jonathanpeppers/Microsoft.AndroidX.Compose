using System;
using System.Collections.Generic;
using AndroidX.Compose.Runtime;
using ComposeNet;

namespace ComposeNet.Samples.JetNews;

/// <summary>
/// JetNews home feed — top app bar with a search-only action row, then
/// a <see cref="LazyColumn{T}"/> with the highlighted post and the
/// recommended / popular / recent lists. Each card taps through to the
/// article screen via the supplied callback.
/// </summary>
public static class HomeScreen
{
    /// <summary>Materialize the home screen.</summary>
    public static Scaffold Build(
        PostsFeed feed,
        MutableStateList<string> bookmarks,
        Action<string> onSelectPost) =>
        new()
        {
            TopBar = new CenterAlignedTopAppBar
            {
                Title = new Image(Resource.Drawable.ic_jetnews_wordmark, "JetNews")
                {
                    Modifier = Modifier.Companion.Height(24),
                },
                Actions = new Row
                {
                    new IconButton(onClick: NoOp)
                    {
                        new Icon(Resource.Drawable.ic_search, "Search"),
                    },
                },
            },
            Body = BuildBody(feed, bookmarks, onSelectPost),
        };

    static Column BuildBody(PostsFeed feed,
                            MutableStateList<string> bookmarks,
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

        return new Column
        {
            Modifier.Companion.FillMaxSize(),
            new LazyColumn<HomeRow>(
                items: rows,
                itemContent: row => BuildRow(row, bookmarks, onSelectPost))
            {
                Modifier = Modifier.Companion.FillMaxSize(),
            },
        };
    }

    static ComposableNode BuildRow(HomeRow row,
                                   MutableStateList<string> bookmarks,
                                   Action<string> onSelectPost) =>
        row switch
        {
            HomeRow.Highlight h        => HomeCards.BuildHighlight(h.Post, bookmarks, onSelectPost),
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

    static void NoOp() { }
}
