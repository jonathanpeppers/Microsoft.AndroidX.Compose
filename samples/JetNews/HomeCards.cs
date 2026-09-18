namespace AndroidX.Compose.Samples.JetNews;

/// <summary>
/// Card factories for the home feed. Three shapes mirror upstream's
/// <c>PostCardTop</c> (highlighted hero), <c>PostCardSimple</c>
/// (recommended row with thumbnail), and <c>PostCardPopular</c>
/// (280-wide carousel card).
/// </summary>
internal static class HomeCards
{
    public static ComposableNode BuildHighlight(
        Post post,
        Action<string> onSelectPost) =>
        new Composed(c =>
        {
            var typography = c.Typography();
            string metadata = c.StringResource(
                Resource.String.home_post_min_read,
                post.Metadata.Date,
                post.Metadata.ReadTimeMinutes);
            return new Column
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
                Spacer.Height(16),
                new Text(post.Title)
                {
                    Modifier = Modifier.Padding(bottom: 8),
                }.WithTypography(typography.HeadlineLarge),
                new Text(post.Metadata.Author)
                {
                    Modifier = Modifier.Padding(bottom: 4),
                }.WithTypography(typography.LabelLarge),
                new Text(metadata).WithTypography(typography.BodySmall),
            };
        });

    public static ComposableNode BuildSimple(
        Post post,
        BookmarksViewModel bookmarks,
        Action<string> onSelectPost,
        SnackbarController? snackbars = null) =>
        new Composed(c =>
        {
            var typography = c.Typography();
            string metadata = c.StringResource(
                Resource.String.home_post_min_read,
                post.Metadata.Author,
                post.Metadata.ReadTimeMinutes);
            string added = c.StringResource(Resource.String.bookmark_added);
            string removed = c.StringResource(Resource.String.bookmark_removed);
            return new Row
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
                    Modifier.Weight(1f, fill: true).Padding(vertical: 10),
                    new Text(post.Title)
                    {
                        MaxLines = 3,
                    }.WithTypography(typography.TitleMedium),
                    new Text(metadata).WithTypography(typography.BodyMedium),
                },
                BookmarkButton.Build(
                    post.Id,
                    bookmarks,
                    onToggled: snackbars is null
                        ? null
                        : isChecked => snackbars.Show(isChecked ? added : removed)),
            };
        });

    public static ComposableNode BuildPopular(
        Post post,
        Action<string> onSelectPost) =>
        new Composed(c =>
        {
            var typography = c.Typography();
            string metadata = c.StringResource(
                Resource.String.home_post_min_read,
                post.Metadata.Date,
                post.Metadata.ReadTimeMinutes);
            return new Card
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
                            MaxLines = 2,
                        }.WithTypography(typography.TitleMedium),
                        Spacer.Weight(1f),
                        new Text(post.Metadata.Author)
                        {
                            MaxLines = 1,
                        }.WithTypography(typography.LabelMedium),
                        new Text(metadata).WithTypography(typography.BodySmall),
                    },
                },
            };
        });
}
