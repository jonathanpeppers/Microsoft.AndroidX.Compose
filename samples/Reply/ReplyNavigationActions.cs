namespace ComposeNet.Samples.Reply;

/// <summary>
/// Navigation actions wrapper — encapsulates how the rest of the UI
/// asks the <see cref="NavController"/> to switch top-level
/// destinations. Port of upstream's <c>ReplyNavigationActions</c>.
/// </summary>
/// <remarks>
/// Upstream uses <c>NavOptions</c> with <c>popUpTo(graph.startDestinationId)</c>
/// + <c>launchSingleTop</c> + <c>restoreState</c> to make the bottom
/// nav behave like a tab strip (re-tapping a destination doesn't push
/// it onto the back stack). Those <c>NavOptions</c> APIs aren't yet
/// surfaced on the C# <see cref="NavController"/>, so this port uses
/// the simpler <see cref="NavController.Navigate(string)"/> overload —
/// re-tapping a tab still navigates, which Compose Navigation will
/// dedupe at the destination level. Documented as a gap in the
/// sample's README.
/// </remarks>
public sealed class ReplyNavigationActions
{
    readonly NavController _nav;

    public ReplyNavigationActions(NavController nav)
    {
        _nav = nav ?? throw new ArgumentNullException(nameof(nav));
    }

    /// <summary>Navigate to a top-level destination.</summary>
    public void NavigateTo(ReplyTopLevelDestination destination) =>
        _nav.Navigate(destination.Route);
}
