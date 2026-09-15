namespace AndroidX.Compose.Samples.Reply;

/// <summary>
/// Inbox screen — a <see cref="LazyColumn{T}"/> of
/// <see cref="ReplyEmailListItem"/>s. Port of upstream's
/// <c>ReplyEmailListPane</c> for the single-pane layout, with the
/// interactive docked search bar above the inbox.
/// </summary>
public static class ReplyInboxScreen
{
    /// <summary>Build the inbox.</summary>
    public static ComposableNode Build(
        IReadOnlyList<Email>     emails,
        long                     openedEmailId,
        IReadOnlyList<long>      selectedEmailIds,
        Action<long> navigateToDetail,
        Action<long> toggleSelection) =>
        new Box
        {
            Modifier.FillMaxSize(),
            new LazyColumn<Email>(
                items: emails,
                itemContent: email =>
                    ReplyEmailListItem.Build(
                        email:            email,
                        navigateToDetail: navigateToDetail,
                        toggleSelection:  toggleSelection,
                        isOpened:         openedEmailId == email.Id,
                        isSelected:       selectedEmailIds.Contains(email.Id)))
            {
                Modifier = Modifier.FillMaxWidth().Padding(top: 80),
                Key = static email => email.Id,
            },
            new ReplySearchBar(emails, navigateToDetail),
            new Box
            {
                Modifier
                    .Align(Alignment.BottomEnd)
                    .Padding(16),
                new ExtendedFloatingActionButton(onClick: NoOp, expanded: true)
                {
                    Icon = new Icon(Resource.Drawable.ic_edit, "Edit"),
                    Text = new Text("Compose"),
                },
            },
        };

    static void NoOp() { }
}
