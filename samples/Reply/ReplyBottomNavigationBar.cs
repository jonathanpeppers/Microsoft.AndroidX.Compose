namespace AndroidX.Compose.Samples.Reply;

/// <summary>
/// Bottom navigation bar with the four top-level destinations
/// (Inbox, Articles, DMs, Groups). Port of upstream's
/// <c>ReplyBottomNavigationBar</c>.
/// </summary>
public static class ReplyBottomNavigationBar
{
    /// <summary>Build the bottom nav bar.</summary>
    public static NavigationBar Build(
        string                       currentRoute,
        Action<ReplyTopLevelDestination> onNavigate)
    {
        var bar = new NavigationBar();
        foreach (var destination in TopLevelDestinations.All)
        {
            bool isSelected = currentRoute == destination.Route;
            bar.Add(new NavigationBarItem(
                selected: isSelected,
                onClick:  () => onNavigate(destination))
            {
                Icon = new Icon(
                    drawableResourceId: isSelected
                        ? destination.SelectedIcon
                        : destination.UnselectedIcon,
                    contentDescription: destination.IconTextId),
            });
        }
        return bar;
    }
}
