
namespace ComposeNet.Samples.Reply;

/// <summary>
/// Static list of <see cref="ReplyTopLevelDestination"/>s surfaced in
/// the navigation bar / rail / drawer. Port of upstream's
/// <c>TOP_LEVEL_DESTINATIONS</c>.
/// </summary>
public static class TopLevelDestinations
{
    /// <summary>The four top-level destinations, in display order.</summary>
    public static readonly IReadOnlyList<ReplyTopLevelDestination> All = new[]
    {
        new ReplyTopLevelDestination(
            route:          Route.Inbox,
            selectedIcon:   Resource.Drawable.ic_inbox,
            unselectedIcon: Resource.Drawable.ic_inbox,
            iconTextId:     "Inbox"),
        new ReplyTopLevelDestination(
            route:          Route.Articles,
            selectedIcon:   Resource.Drawable.ic_article,
            unselectedIcon: Resource.Drawable.ic_article,
            iconTextId:     "Articles"),
        new ReplyTopLevelDestination(
            route:          Route.DirectMessages,
            selectedIcon:   Resource.Drawable.ic_chat_bubble_outline,
            unselectedIcon: Resource.Drawable.ic_chat_bubble_outline,
            iconTextId:     "DM"),
        new ReplyTopLevelDestination(
            route:          Route.Groups,
            selectedIcon:   Resource.Drawable.ic_group,
            unselectedIcon: Resource.Drawable.ic_group,
            iconTextId:     "Groups"),
    };
}
