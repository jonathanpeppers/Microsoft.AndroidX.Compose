namespace AndroidX.Compose.Samples.Reply;

/// <summary>
/// Email detail screen — a toolbar item plus a <see cref="LazyColumn{T}"/>
/// of <see cref="ReplyEmailThreadItem"/>s. Port of upstream's
/// <c>ReplyEmailDetail</c> for the single-pane layout.
/// </summary>
public static class ReplyEmailDetail
{
    /// <summary>Build the email detail screen.</summary>
    public static ComposableNode Build(
        Email email,
        Action onBackPressed,
        bool showComposeFab) =>
        new Composed(c =>
        {
            IReadOnlyList<Email?> items = [null, .. email.Threads];
            var scheme = c.ColorScheme();
            var content = new Box
            {
                Modifier.FillMaxSize(),
                new LazyColumn<Email?>(
                    items: items,
                    itemContent: item => item is null
                        ? EmailDetailAppBar.Build(email, onBackPressed)
                        : ReplyEmailThreadItem.Build(item))
                {
                    Modifier = Modifier
                        .FillMaxSize()
                        .Background(Color.FromPacked(scheme.InverseOnSurface)),
                    ContentPadding = c.SystemBarsInsets()
                        .Only(WindowInsetsSides.Bottom)
                        .AsPaddingValues(c),
                    Key = static item => item?.Id ?? long.MinValue,
                },
            };
            if (showComposeFab)
            {
                content.Add(new Box
                {
                    Modifier.Align(Alignment.BottomEnd).Padding(16),
                    ReplyComposeFab.Build(c, expanded: true),
                });
            }
            return content;
        });
}
