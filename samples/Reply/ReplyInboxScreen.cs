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
        Action<long> toggleSelection,
        bool showComposeFab,
        MutableState<bool> composeFabExpanded) =>
        new Composed(c =>
        {
            var listState = c.RememberLazyListState();
            bool expanded = listState.LastScrolledBackward || !listState.CanScrollBackward;
            if (composeFabExpanded.Value != expanded)
                c.SideEffect(() => composeFabExpanded.Value = expanded);
            var list = new LazyColumn<Email>(
                items: emails,
                itemContent: email =>
                    ReplyEmailListItem.Build(
                        email:            email,
                        navigateToDetail: navigateToDetail,
                        toggleSelection:  toggleSelection,
                        isOpened:         openedEmailId == email.Id,
                        isSelected:       selectedEmailIds.Contains(email.Id)))
            {
                Modifier = Modifier.FillMaxWidth()
                    .Padding(top: 80),
                State = listState,
                ContentPadding = c.SystemBarsInsets()
                    .Only(WindowInsetsSides.Bottom)
                    .AsPaddingValues(c),
                Key = static email => email.Id,
            };
            var content = new Box
            {
                Modifier.FillMaxSize()
                    .StatusBarsPadding()
                    .Semantics("Inbox messages")
                    .Focusable(),
                list,
                new ReplySearchBar(emails, navigateToDetail),
            };
            if (showComposeFab)
            {
                content.Add(new Box
                {
                    Modifier.Align(Alignment.BottomEnd).Padding(16),
                    ReplyComposeFab.Build(
                        c,
                        expanded: expanded),
                });
            }
            return content;
        });

}
