namespace AndroidX.Compose.Samples.JetNews;

/// <summary>
/// UI state for the JetNews home screen — a discriminated union over
/// the three observable states the feed can be in. Modelled the same
/// way as <see cref="HomeRow"/>: <c>abstract record</c> with sealed
/// nested cases the screen <c>switch</c>es on.
/// </summary>
/// <remarks>
/// Single-value UI state is the canonical UDF idiom: one
/// <see cref="MutableManagedState{T}"/> exposed by the view
/// model holds whichever case is current; the composable matches on
/// the type and renders the matching surface (loading spinner /
/// error banner / feed lazy column). Recomposition is automatic when
/// the view model swaps in a new instance.
/// </remarks>
public abstract record HomeUiState
{
    /// <summary>The feed is being fetched — the screen shows a centred progress indicator.</summary>
    public sealed record Loading : HomeUiState;

    /// <summary>
    /// The feed loaded successfully. <paramref name="Feed"/> is the
    /// most recent payload; <paramref name="IsRefreshing"/> tracks a
    /// background re-fetch (e.g. pull-to-refresh) so the UI can show
    /// a small inline indicator without dropping the visible posts.
    /// </summary>
    public sealed record HasPosts(PostsFeed Feed, bool IsRefreshing = false) : HomeUiState;

    /// <summary>
    /// The feed fetch failed. <paramref name="Message"/> is a
    /// user-displayable description (the sample uses the exception
    /// message; a real app would map to localised strings).
    /// </summary>
    public sealed record Error(string Message) : HomeUiState;
}
