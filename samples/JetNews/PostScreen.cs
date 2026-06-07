using System;
using System.Collections.Generic;
using ComposeNet;

namespace ComposeNet.Samples.JetNews;

/// <summary>
/// Article reader. A <see cref="Scaffold"/> with a back-button top app
/// bar, a bookmark / share bottom app bar, and a
/// <see cref="LazyColumn{T}"/> body that renders the post's paragraphs.
/// </summary>
public static class PostScreen
{
    /// <summary>Materialize the article screen for a single post.</summary>
    public static Scaffold Build(Post post, MutableStateList<string> bookmarks, Action onBack) =>
        new()
        {
            TopBar = new TopAppBar
            {
                Title = new Text(post.Metadata.Author)
                {
                    FontSize   = 16,
                    FontWeight = FontWeight.Medium,
                },
                NavigationIcon = new IconButton(onClick: onBack)
                {
                    new Icon(Resource.Drawable.ic_arrow_back, "Back"),
                },
            },
            BottomBar = new BottomAppBar
            {
                BookmarkButton.Build(post.Id, bookmarks),
                new IconButton(onClick: NoOp)
                {
                    new Icon(Resource.Drawable.ic_share, "Share"),
                },
            },
            Body = BuildBody(post),
        };

    static LazyColumn<PostRow> BuildBody(Post post)
    {
        var rows = new List<PostRow> { new PostRow.Hero(post) };
        for (int i = 0; i < post.Paragraphs.Count; i++)
            rows.Add(new PostRow.Body(post.Paragraphs[i], i));

        return new LazyColumn<PostRow>(items: rows, itemContent: BuildRow)
        {
            Modifier = Modifier.Companion.FillMaxSize(),
        };
    }

    static ComposableNode BuildRow(PostRow row) => row switch
    {
        PostRow.Hero h => BuildHero(h.Post),
        PostRow.Body b => PostBody.BuildParagraph(b.Paragraph),
        _              => new Spacer(),
    };

    static Column BuildHero(Post post) =>
        new()
        {
            Modifier.Companion.FillMaxWidth(),
            new Image(post.HeroId, "")
            {
                Modifier = Modifier.Companion
                    .FillMaxWidth()
                    .AspectRatio(992f / 296f),
            },
            new Column
            {
                Modifier.Companion.FillMaxWidth().Padding(16),
                new Text(post.Title)
                {
                    FontSize   = 22,
                    FontWeight = FontWeight.SemiBold,
                },
                new Spacer(Modifier.Companion.Height(4)),
                new Text(post.Subtitle)
                {
                    FontSize = 14,
                    Color    = Color.FromHex("#666666"),
                },
                new Spacer(Modifier.Companion.Height(8)),
                new Text($"{post.Metadata.Author} · {post.Metadata.Date} · {post.Metadata.ReadTimeMinutes} min read")
                {
                    FontSize = 12,
                    Color    = Color.FromHex("#666666"),
                },
            },
            new HorizontalDivider
            {
                Modifier = Modifier.Companion.Padding(horizontal: 16, vertical: 8),
            },
        };

    static void NoOp() { }
}
