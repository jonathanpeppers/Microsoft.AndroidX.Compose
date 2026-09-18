namespace AndroidX.Compose.Samples.Reply;

/// <summary>
/// Navigation actions wrapper — encapsulates how the rest of the UI
/// asks the <see cref="NavController"/> to switch top-level
/// destinations. Port of upstream's <c>ReplyNavigationActions</c>.
/// </summary>
/// <remarks>
/// Upstream uses <c>NavOptions</c> with <c>popUpTo(graph.startDestinationId)</c>
/// + <c>launchSingleTop</c> + <c>restoreState</c> to make the bottom
/// nav behave like a tab strip. This graph has a flat, fixed Inbox start
/// destination, so its route is the equivalent pop target.
/// </remarks>
public sealed class ReplyNavigationActions
{
    readonly NavController _nav;

    /// <summary>Creates actions for the app's remembered navigation controller.</summary>
    public ReplyNavigationActions(NavController nav)
    {
        ArgumentNullException.ThrowIfNull(nav);
        _nav = nav;
    }

    /// <summary>Navigate to a top-level destination.</summary>
    public void NavigateTo(ReplyTopLevelDestination destination)
    {
        ArgumentNullException.ThrowIfNull(destination);
        _nav.Navigate(destination.Route, new NavOptions
        {
            PopUpToRoute = Route.Inbox,
            PopUpToSaveState = true,
            LaunchSingleTop = true,
            RestoreState = true,
        });
    }

    /// <summary>Opens an email from the inbox or a search result without duplicating it.</summary>
    public void OpenEmail(long emailId) =>
        _nav.Navigate(Route.EmailDetail(emailId), new NavOptions
        {
            PopUpToRoute = Route.Inbox,
            LaunchSingleTop = true,
        });

    /// <summary>Closes detail through the same action for system Back and the app-bar Up button.</summary>
    public void CloseEmail(ReplyState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (!_nav.PopBackStack())
            throw new InvalidOperationException("Reply email detail has no inbox to return to.");
        state.OpenedEmailId.Value = 0L;
    }
}
