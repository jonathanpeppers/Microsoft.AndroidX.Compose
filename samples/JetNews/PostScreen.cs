namespace AndroidX.Compose.Samples.JetNews;

/// <summary>
/// Article reader. A <see cref="Scaffold"/> with a back-button top app
/// bar, a bookmark / share bottom app bar, and a
/// <see cref="LazyColumn{T}"/> body that renders the post's paragraphs.
/// </summary>
public static class PostScreen
{
    /// <summary>Materialize the article screen for a single post.</summary>
    /// <param name="post">The post being rendered.</param>
    /// <param name="bookmarks">Shared bookmark set; toggling fires <paramref name="snackbars"/>.</param>
    /// <param name="onBack">Up-navigation callback, fired by the back arrow.</param>
    /// <param name="snackbars">Sample-side snackbar controller for transient feedback.</param>
    /// <param name="onShare">
    /// Optional callback fired when the user picks "Share" in the
    /// <see cref="AlertDialog"/> the share button now opens — typically
    /// fires <see cref="Android.Content.Intent.ActionSend"/>. When
    /// <c>null</c>, the dialog only shows the "not available" message.
    /// </param>
    /// <param name="onOpenLink">Opens a URL from an annotated article link.</param>
    public static ComposableNode Build(
        Post post,
        BookmarksViewModel bookmarks,
        Action onBack,
        SnackbarController snackbars,
        Action<string> onOpenLink,
        Action<Post>? onShare = null) =>
        new Composed(c =>
        {
            var showShareDialog = c.MutableStateOf(false);
            var snackbarMessage = snackbars.Message.Value;
            var topBarState = c.RememberTopAppBarState();
            var scrollBehavior = c.EnterAlwaysScrollBehavior(topBarState);
            var typography = c.Typography();
            string navigateUp = c.StringResource(Resource.String.cd_navigate_up);
            string share = c.StringResource(Resource.String.cd_share);
            string added = c.StringResource(Resource.String.bookmark_added);
            string removed = c.StringResource(Resource.String.bookmark_removed);
            string shareArticle = c.StringResource(Resource.String.post_share_article);
            string unavailable = c.StringResource(Resource.String.post_functionality_not_available);
            string shareAnyway = c.StringResource(Resource.String.post_share_anyway);
            string sharingUnavailable = c.StringResource(Resource.String.post_share_unavailable);
            string ok = c.StringResource(Resource.String.post_ok);
            string cancel = c.StringResource(Resource.String.post_cancel);

            return new Box
            {
                Modifier.FillMaxSize(),

                new Scaffold
                {
                    TopBar = new TopAppBar
                    {
                        Title = new Text(post.Metadata.Author)
                            .WithTypography(typography.LabelLarge),
                        NavigationIcon = new IconButton(onClick: onBack)
                        {
                            new Icon(Resource.Drawable.ic_arrow_back, navigateUp),
                        },
                        ScrollBehavior = scrollBehavior,
                    },
                    BottomBar = new BottomAppBar
                    {
                        BookmarkButton.Build(
                            post.Id,
                            bookmarks,
                            onToggled: isChecked => snackbars.Show(isChecked
                                ? added
                                : removed)),
                        new IconButton(onClick: () => showShareDialog.Value = true)
                        {
                            new Icon(Resource.Drawable.ic_share, share),
                        },
                    },
                    SnackbarHost = snackbarMessage is null
                        ? null
                        : new Snackbar { Body = new Text(snackbarMessage) },
                    Body = BuildBody(post, onOpenLink, scrollBehavior),
                },

                showShareDialog.Value
                    ? BuildShareDialog(
                        post,
                        showShareDialog,
                        snackbars,
                        onShare,
                        shareArticle,
                        unavailable,
                        shareAnyway,
                        sharingUnavailable,
                        ok,
                        cancel)
                    : (ComposableNode?)null,
            };
        });

    static AlertDialog BuildShareDialog(Post post,
                                        MutableState<bool> showShareDialog,
                                        SnackbarController snackbars,
                                        Action<Post>? onShare,
                                        string shareArticle,
                                        string unavailable,
                                        string shareAnyway,
                                        string sharingUnavailable,
                                        string ok,
                                        string cancel) =>
        new(onDismissRequest: () => showShareDialog.Value = false)
        {
            Shape = new RoundedCornerShape(20.Dp()),
            Title = new Text(shareArticle),
            Text  = new Text(unavailable),
            ConfirmButton = new Button(onClick: () =>
            {
                showShareDialog.Value = false;
                if (onShare is not null)
                {
                    onShare(post);
                }
                else
                {
                    snackbars.Show(sharingUnavailable);
                }
            })
            {
                new Text(onShare is null ? ok : shareAnyway),
            },
            DismissButton = new Button(onClick: () => showShareDialog.Value = false)
            {
                new Text(cancel),
            },
        };

    static LazyColumn<PostRow> BuildBody(
        Post post,
        Action<string> onOpenLink,
        AndroidX.Compose.Material3.ITopAppBarScrollBehavior scrollBehavior)
    {
        List<PostRow> rows = [new PostRow.Hero(post)];
        for (int i = 0; i < post.Paragraphs.Count; i++)
            rows.Add(new PostRow.Body(post.Paragraphs[i], i));

        return new LazyColumn<PostRow>(
            items: rows,
            itemContent: row => BuildRow(row, onOpenLink))
        {
            Modifier = Modifier
                .FillMaxSize()
                .NestedScroll(scrollBehavior.NestedScrollConnection),
        };
    }

    static ComposableNode BuildRow(PostRow row, Action<string> onOpenLink) => row switch
    {
        PostRow.Hero h => BuildHero(h.Post),
        PostRow.Body b => PostBody.BuildParagraph(b.Paragraph, onOpenLink),
        _              => new Spacer(),
    };

    static ComposableNode BuildHero(Post post) =>
        new Composed(c =>
        {
            var typography = c.Typography();
            var scheme = c.ColorScheme();
            string metadata = c.StringResource(
                Resource.String.post_min_read,
                post.Metadata.Author,
                post.Metadata.Date,
                post.Metadata.ReadTimeMinutes);
            return new Column
            {
                Modifier.FillMaxWidth(),
                new Image(post.HeroId, "")
                {
                    Modifier = Modifier
                        .FillMaxWidth()
                        .AspectRatio(992f / 296f),
                },
                new Column
                {
                    Modifier.FillMaxWidth().Padding(16),
                    new Text(post.Title).WithTypography(typography.HeadlineLarge),
                    Spacer.Height(4),
                    new Text(post.Subtitle)
                    {
                        Color = Color.FromPacked(scheme.OnSurfaceVariant),
                    }.WithTypography(typography.BodyMedium),
                    Spacer.Height(8),
                    new Text(metadata)
                    {
                        Color = Color.FromPacked(scheme.OnSurfaceVariant),
                    }.WithTypography(typography.BodySmall),
                },
                new HorizontalDivider
                {
                    Modifier = Modifier.Padding(horizontal: 16, vertical: 8),
                },
            };
        });
}
