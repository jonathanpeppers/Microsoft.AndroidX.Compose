using System;
using ComposeNet;

namespace ComposeNet.Samples.JetNews;

/// <summary>
/// Card factories for the home feed. The "highlighted" card is the
/// large header-style card at the top of the feed; "simple" cards are
/// the smaller rows in the recommended/popular/recent lists.
/// </summary>
internal static class HomeCards
{
    static readonly Color SubtleText = Color.FromHex("#666666");

    public static Card BuildHighlight(Post post,
                                      MutableStateList<string> bookmarks,
                                      Action<string> onSelectPost) =>
        new()
        {
            Modifier.Companion
                .FillMaxWidth()
                .Padding(horizontal: 16, vertical: 8)
                .Clickable(() => onSelectPost(post.Id)),
            new Column
            {
                Modifier.Companion.FillMaxWidth(),
                new Box
                {
                    Modifier.Companion
                        .FillMaxWidth()
                        .Height(180)
                        .Background(post.HeroColor),
                },
                new Column
                {
                    Modifier.Companion.FillMaxWidth().Padding(16),
                    new Text(post.Title)
                    {
                        FontSize   = 20,
                        FontWeight = FontWeight.SemiBold,
                    },
                    new Spacer(Modifier.Companion.Height(4)),
                    new Text(post.Subtitle)
                    {
                        FontSize = 14,
                        Color    = SubtleText,
                    },
                    new Spacer(Modifier.Companion.Height(8)),
                    BuildMeta(post),
                },
            },
        };

    public static Row BuildSimple(Post post,
                                  MutableStateList<string> bookmarks,
                                  Action<string> onSelectPost) =>
        new()
        {
            Modifier.Companion
                .FillMaxWidth()
                .Padding(horizontal: 16, vertical: 8)
                .Clickable(() => onSelectPost(post.Id)),
            new Box
            {
                Modifier.Companion
                    .Size(56)
                    .Clip(8)
                    .Background(post.HeroColor),
            },
            new Spacer(Modifier.Companion.Width(12)),
            new Column
            {
                Modifier.Companion.Weight(1f, fill: true),
                new Text(post.Title)
                {
                    FontSize   = 16,
                    FontWeight = FontWeight.Medium,
                    MaxLines   = 2,
                },
                new Spacer(Modifier.Companion.Height(4)),
                new Text($"{post.Metadata.Author} · {post.Metadata.ReadTimeMinutes} min read")
                {
                    FontSize = 12,
                    Color    = SubtleText,
                },
            },
            BookmarkButton.Build(post.Id, bookmarks),
        };

    static Row BuildMeta(Post post) =>
        new()
        {
            Modifier.Companion.FillMaxWidth(),
            new Text($"{post.Metadata.Author} · {post.Metadata.Date} · {post.Metadata.ReadTimeMinutes} min read")
            {
                FontSize = 12,
                Color    = SubtleText,
                Modifier = Modifier.Companion.Weight(1f, fill: true),
            },
        };
}
