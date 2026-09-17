
namespace AndroidX.Compose.Samples.Reply;

/// <summary>
/// Static list of <see cref="ReplyTopLevelDestination"/>s surfaced in
/// the navigation bar / rail / drawer. Port of upstream's
/// <c>TOP_LEVEL_DESTINATIONS</c>.
/// </summary>
public static class TopLevelDestinations
{
    /// <summary>The four top-level destinations, in display order.</summary>
    public static readonly IReadOnlyList<ReplyTopLevelDestination> All =
    [
        new ReplyTopLevelDestination(
            route:          Route.Inbox,
            selectedIcon:   Resource.Drawable.ic_inbox,
            unselectedIcon: Resource.Drawable.ic_inbox,
            labelResourceId: Resource.String.reply_tab_inbox),
        new ReplyTopLevelDestination(
            route:          Route.Articles,
            selectedIcon:   Resource.Drawable.ic_article,
            unselectedIcon: Resource.Drawable.ic_article,
            labelResourceId: Resource.String.reply_tab_articles),
        new ReplyTopLevelDestination(
            route:          Route.DirectMessages,
            selectedIcon:   Resource.Drawable.ic_chat_bubble_outline,
            unselectedIcon: Resource.Drawable.ic_chat_bubble_outline,
            labelResourceId: Resource.String.reply_tab_direct_messages),
        new ReplyTopLevelDestination(
            route:          Route.Groups,
            selectedIcon:   Resource.Drawable.ic_group,
            unselectedIcon: Resource.Drawable.ic_group,
            labelResourceId: Resource.String.reply_tab_groups),
    ];
}
