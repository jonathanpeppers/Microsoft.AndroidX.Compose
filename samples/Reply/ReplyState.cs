namespace AndroidX.Compose.Samples.Reply;

/// <summary>Activity-owned email context; navigation and list scroll state remain owned by Compose.</summary>
public sealed class ReplyState
{
    /// <summary>Restores the opened email and selection when Android recreates the activity.</summary>
    public ReplyState(Bundle? savedInstanceState = null)
    {
        OpenedEmailId = new(savedInstanceState?.GetLong("reply-opened-email", 0L) ?? 0L);
        SelectedEmailIds = new(savedInstanceState?.GetLongArray("reply-selected-emails") ?? []);
        ComposeFabExpanded = new(savedInstanceState?.GetBoolean("reply-compose-fab-expanded", true) ?? true);
    }

    /// <summary>The highlighted email, reset to the first email when detail closes.</summary>
    public MutableState<long> OpenedEmailId { get; }

    /// <summary>The multi-selection, which does not consume Back in the pinned Kotlin sample.</summary>
    public MutableStateList<long> SelectedEmailIds { get; }

    /// <summary>The latest inbox scroll-derived Compose FAB expansion state.</summary>
    public MutableState<bool> ComposeFabExpanded { get; }

    /// <summary>Saves email context alongside Compose Navigation's own saved state.</summary>
    public void Save(Bundle outState)
    {
        ArgumentNullException.ThrowIfNull(outState);
        outState.PutLong("reply-opened-email", OpenedEmailId.Value);
        outState.PutLongArray("reply-selected-emails", [.. SelectedEmailIds]);
        outState.PutBoolean("reply-compose-fab-expanded", ComposeFabExpanded.Value);
    }
}
