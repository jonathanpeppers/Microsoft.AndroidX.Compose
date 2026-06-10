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
    public static ComposableNode Build(
        Post post,
        BookmarksViewModel bookmarks,
        Action onBack,
        SnackbarController snackbars,
        Action<Post>? onShare = null) =>
        new Composed(c =>
        {
            var showShareDialog = c.Remember(() => new MutableState<bool>(false));
            var snackbarMessage = snackbars.Message.Value;

            // Wrap the Scaffold + the conditional dialog in a Box so the
            // dialog renders as an overlay above the chrome regardless of
            // where it sits in the tree (mirrors AlertDialogDemo's shape).
            return new Box
            {
                Modifier.Companion.FillMaxSize(),

                new Scaffold
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
                        BookmarkButton.Build(
                            post.Id,
                            bookmarks,
                            onToggled: isChecked => snackbars.Show(isChecked
                                ? "Added to bookmarks"
                                : "Removed from bookmarks")),
                        new IconButton(onClick: () => showShareDialog.Value = true)
                        {
                            new Icon(Resource.Drawable.ic_share, "Share"),
                        },
                    },
                    SnackbarHost = snackbarMessage is null
                        ? null
                        : new Snackbar { Body = new Text(snackbarMessage) },
                    Body = BuildBody(post),
                },

                showShareDialog.Value
                    ? BuildShareDialog(post, showShareDialog, snackbars, onShare)
                    : (ComposableNode?)null,
            };
        });

    static AlertDialog BuildShareDialog(Post post,
                                        MutableState<bool> showShareDialog,
                                        SnackbarController snackbars,
                                        Action<Post>? onShare) =>
        new(onDismissRequest: () => showShareDialog.Value = false)
        {
            Title = new Text("Share article"),
            Text  = new Text("Functionality not available 😞"),
            ConfirmButton = new Button(onClick: () =>
            {
                showShareDialog.Value = false;
                if (onShare is not null)
                {
                    onShare(post);
                }
                else
                {
                    snackbars.Show("Sharing isn't wired up in this build");
                }
            })
            {
                new Text(onShare is null ? "OK" : "Share anyway"),
            },
            DismissButton = new Button(onClick: () => showShareDialog.Value = false)
            {
                new Text("Cancel"),
            },
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
}
