namespace AndroidX.Compose.Samples.JetNews;

/// <summary>
/// Card factories for the home feed. Three shapes mirror upstream's
/// <c>PostCardTop</c> (highlighted hero), <c>PostCardSimple</c>
/// (recommended row with thumbnail), and <c>PostCardPopular</c>
/// (280-wide carousel card).
/// </summary>
internal static class HomeCards
{
    public static Column BuildHighlight(Post post,
                                        Action<string> onSelectPost) =>
        new()
        {
            Modifier
                .FillMaxWidth()
                .Padding(16)
                .Clickable(() => onSelectPost(post.Id)),
            new Image(post.HeroId, "")
            {
                Modifier = Modifier
                    .FillMaxWidth()
                    .AspectRatio(992f / 296f)
                    .Clip(16),
            },
            new Spacer(Modifier.Height(16)),
            new Text(post.Title)
            {
                FontSize   = 22,
                FontWeight = FontWeight.SemiBold,
                Modifier   = Modifier.Padding(bottom: 8, start: 0, end: 0, top: 0),
            },
            new Text(post.Metadata.Author)
            {
                FontSize   = 14,
                FontWeight = FontWeight.Medium,
                Modifier   = Modifier.Padding(bottom: 4, start: 0, end: 0, top: 0),
            },
            new Text($"{post.Metadata.Date} · {post.Metadata.ReadTimeMinutes} min read")
            {
                FontSize = 12,
            },
        };

    public static Row BuildSimple(Post post,
                                  BookmarksViewModel bookmarks,
                                  Action<string> onSelectPost,
                                  SnackbarController? snackbars = null) =>
        new()
        {
            Modifier
                .FillMaxWidth()
                .Clickable(() => onSelectPost(post.Id)),
            new Image(post.ThumbId, "")
            {
                Modifier = Modifier.Padding(16).Size(40).Clip(8),
            },
            new Column
            {
                Modifier.Weight(1f, fill: true).Padding(vertical: 10, horizontal: 0),
                new Text(post.Title)
                {
                    FontSize   = 16,
                    FontWeight = FontWeight.Medium,
                    MaxLines   = 3,
                },
                new Text($"{post.Metadata.Author} · {post.Metadata.ReadTimeMinutes} min read")
                {
                    FontSize = 14,
                },
            },
            BookmarkButton.Build(
                post.Id,
                bookmarks,
                onToggled: snackbars is null
                    ? null
                    : isChecked => snackbars.Show(isChecked
                        ? "Added to bookmarks"
                        : "Removed from bookmarks")),
        };

    public static Card BuildPopular(Post post, Action<string> onSelectPost) =>
        new()
        {
            Modifier.Width(280).Height(220).Clickable(() => onSelectPost(post.Id)),
            new Column
            {
                Modifier.FillMaxSize(),
                new Image(post.HeroId, "")
                {
                    Modifier = Modifier
                        .FillMaxWidth()
                        .AspectRatio(992f / 296f),
                },
                new Column
                {
                    Modifier.FillMaxSize().Padding(16),
                    new Text(post.Title)
                    {
                        FontSize   = 16,
                        FontWeight = FontWeight.SemiBold,
                        MaxLines   = 2,
                    },
                    new Spacer(Modifier.Weight(1f, fill: true)),
                    new Text(post.Metadata.Author)
                    {
                        FontSize = 13,
                        MaxLines = 1,
                    },
                    new Text($"{post.Metadata.Date} · {post.Metadata.ReadTimeMinutes} min read")
                    {
                        FontSize = 12,
                    },
                },
            },
        };
}
