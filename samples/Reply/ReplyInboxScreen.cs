namespace AndroidX.Compose.Samples.Reply;

/// <summary>
/// Inbox screen — a <see cref="LazyColumn{T}"/> of
/// <see cref="ReplyEmailListItem"/>s. Port of upstream's
/// <c>ReplyEmailListPane</c> for the single-pane layout, with the
/// docked search bar simplified to a plain top-row entry.
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
                Modifier = Modifier.FillMaxWidth().Padding(top: 80, bottom: 0, start: 0, end: 0),
            },
            new ReplySearchBar(),
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
