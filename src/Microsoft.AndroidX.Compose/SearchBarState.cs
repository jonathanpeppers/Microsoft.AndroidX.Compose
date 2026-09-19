using AndroidX.Compose.Material3;

namespace AndroidX.Compose;

/// <summary>
/// Caller-supplied state holder for <see cref="SearchBar"/> /
/// <see cref="TopSearchBar"/> and their paired expanded popups
/// (<see cref="ExpandedDockedSearchBar"/> /
/// <see cref="ExpandedFullScreenSearchBar"/>). The underlying JVM
/// <c>androidx.compose.material3.SearchBarState</c> is owned by the active
/// composition and shared by every rendered consumer of this wrapper.
/// </summary>
/// <remarks>
/// Compose's new state-based SearchBar API splits the always-visible
/// collapsed input bar (<see cref="SearchBar"/>) from the popup with
/// the search results (<see cref="ExpandedFullScreenSearchBar"/> or
/// <see cref="ExpandedDockedSearchBar"/>). Both halves are *always*
/// rendered, referencing the same <see cref="SearchBarState"/>; Compose
/// internally toggles the popup's visibility from the state.
///
/// <code>
/// var search = Remember(() =&gt; new SearchBarState());
/// var input  = Remember(() =&gt; new SearchBarTextFieldState());
///
/// new Box
/// {
///     new SearchBar(state: search)
///     {
///         InputField = new SearchBarInputField(input, search)
///         {
///             Placeholder = new Text("Search"),
///             LeadingIcon = new Text("🔍"),
///         },
///     },
///     new ExpandedFullScreenSearchBar(state: search)
///     {
///         InputField = new SearchBarInputField(input, search),
///         new Text("Result A"),
///         new Text("Result B"),
///     },
/// }
/// </code>
///
/// The active native owner publishes one managed JVM peer for sibling
/// consumers. Removing every consumer retires that peer and retains the
/// settled collapsed or expanded value; re-entry creates a new
/// composition-owned peer initialized from the retained value.
/// </remarks>
public sealed class SearchBarState
{
    SearchBarValue _rememberValue;

    internal AndroidX.Compose.Material3.SearchBarState? Jvm;
    internal SearchBarValue RememberValue => _rememberValue;

    /// <summary>Initial collapsed or expanded value used when the state first binds.</summary>
    public SearchBarValue InitialValue { get; }

    /// <summary>Creates search-bar state initially collapsed.</summary>
    public SearchBarState()
        : this(
            SearchBarValue.Collapsed
            ?? throw new InvalidOperationException(
                "SearchBarValue.Collapsed was unavailable."))
    {
    }

    /// <summary>Creates search-bar state with the requested initial value.</summary>
    /// <param name="initialValue">Initial collapsed or expanded state.</param>
    public SearchBarState(SearchBarValue initialValue)
    {
        ArgumentNullException.ThrowIfNull(initialValue);
        InitialValue = initialValue;
        _rememberValue = initialValue;
    }

    internal void BindJvm(AndroidX.Compose.Material3.SearchBarState jvm) =>
        Jvm = jvm;

    internal void UnbindJvm()
    {
        if (Jvm is not { } jvm)
            return;
        try
        {
            _rememberValue = jvm.CurrentValue;
        }
        finally
        {
            Jvm = null;
        }
    }

    /// <summary>
    /// Current collapsed or expanded state. While unbound, returns the
    /// initial or most recently retired value.
    /// </summary>
    public SearchBarValue CurrentValue => Jvm?.CurrentValue ?? _rememberValue;

    /// <summary>
    /// Target state during animation. While unbound, returns the initial or
    /// most recently retired value.
    /// </summary>
    public SearchBarValue TargetValue => Jvm?.TargetValue ?? _rememberValue;

    /// <summary>
    /// Expansion progress from <c>0</c> (collapsed) through <c>1</c>
    /// (expanded).
    /// </summary>
    public float Progress
    {
        get
        {
            if (Jvm is { } jvm)
                return jvm.Progress;
            var expanded = SearchBarValue.Expanded
                ?? throw new InvalidOperationException("SearchBarValue.Expanded was unavailable.");
            return _rememberValue.Equals(expanded) ? 1f : 0f;
        }
    }

    /// <summary>Whether an expand, collapse, or snap animation is active.</summary>
    public bool IsAnimating => Jvm?.IsAnimating ?? false;

    /// <summary>Animates the search bar to its expanded state.</summary>
    /// <param name="cancellationToken">Cancels the returned task and stops the Kotlin animation at its next cancellable suspend point.</param>
    public Task ExpandAsync(CancellationToken cancellationToken = default) =>
        SuspendBridge.Invoke(
            cont => ComposeBridges.SearchBarStateAnimateToExpanded(
                RequireJvm().Handle, cont),
            cancellationToken);

    /// <summary>Animates the search bar to its collapsed state.</summary>
    /// <param name="cancellationToken">Cancels the returned task and stops the Kotlin animation at its next cancellable suspend point.</param>
    public Task CollapseAsync(CancellationToken cancellationToken = default) =>
        SuspendBridge.Invoke(
            cont => ComposeBridges.SearchBarStateAnimateToCollapsed(
                RequireJvm().Handle, cont),
            cancellationToken);

    /// <summary>Snaps expansion progress to a value from <c>0</c> through <c>1</c>.</summary>
    /// <param name="fraction">Expansion fraction in <c>[0, 1]</c>.</param>
    /// <param name="cancellationToken">Cancels the returned task and the underlying Kotlin operation.</param>
    public Task SnapToAsync(
        float fraction,
        CancellationToken cancellationToken = default)
    {
        if (!(fraction >= 0f && fraction <= 1f))
            throw new ArgumentOutOfRangeException(
                nameof(fraction), fraction, "Fraction must be in the range [0, 1].");
        return SuspendBridge.Invoke(
            cont => ComposeBridges.SearchBarStateSnapTo(
                RequireJvm().Handle, fraction, cont),
            cancellationToken);
    }

    AndroidX.Compose.Material3.SearchBarState RequireJvm() =>
        Jvm ?? throw new InvalidOperationException(
            "SearchBarState is not bound. Render it with SearchBar before controlling it.");
}
