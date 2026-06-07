namespace ComposeNet.Samples.Reply;

/// <summary>
/// A top-level navigation destination — one of the four tabs surfaced
/// in the bottom navigation bar. Port of upstream's
/// <c>ReplyTopLevelDestination</c>.
/// </summary>
public sealed class ReplyTopLevelDestination
{
    public ReplyTopLevelDestination(
        string route,
        int    selectedIcon,
        int    unselectedIcon,
        string iconTextId)
    {
        Route          = route;
        SelectedIcon   = selectedIcon;
        UnselectedIcon = unselectedIcon;
        IconTextId     = iconTextId;
    }

    /// <summary>The nav route this destination corresponds to.</summary>
    public string Route { get; }

    /// <summary>Drawable resource for the selected (filled) icon variant.</summary>
    public int SelectedIcon { get; }

    /// <summary>Drawable resource for the unselected (outlined) icon variant.</summary>
    public int UnselectedIcon { get; }

    /// <summary>The accessibility label for this destination's icon.</summary>
    public string IconTextId { get; }
}
