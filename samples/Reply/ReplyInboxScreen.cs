using System.Collections.Generic;
using AndroidX.Compose.Runtime;
using ComposeNet;

namespace ComposeNet.Samples.Reply;

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
        long?                    openedEmailId,
        IReadOnlyList<long>      selectedEmailIds,
        System.Action<long>      navigateToDetail,
        System.Action<long>      toggleSelection) =>
        new Box
        {
            Modifier.Companion.FillMaxSize(),
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
                Modifier = Modifier.Companion.FillMaxSize().Padding(top: 80, bottom: 80, start: 0, end: 0),
            },
            new ReplySearchBar(),
            new Box
            {
                Modifier.Companion
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
