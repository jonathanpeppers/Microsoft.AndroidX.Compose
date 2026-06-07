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
                Title = new Text("JetNews")
                {
                    FontSize   = 18,
                    FontWeight = FontWeight.SemiBold,
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
            new HomeRow.Highlight(feed.Highlighted),
            new HomeRow.SectionHeader("Recommended for you"),
        };
        foreach (var p in feed.Recommended)
            rows.Add(new HomeRow.Recommended(p));

        rows.Add(new HomeRow.SectionHeader("Popular on JetNews"));
        foreach (var p in feed.Popular)
            rows.Add(new HomeRow.Recommended(p));

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
            _ => new Spacer(),
        };

    static Box BuildSectionHeader(string label) =>
        new()
        {
            Modifier.Companion.FillMaxWidth().Padding(horizontal: 16, vertical: 12),
            new Text(label)
            {
                FontSize   = 14,
                FontWeight = FontWeight.SemiBold,
            },
        };

    static void NoOp() { }
}
