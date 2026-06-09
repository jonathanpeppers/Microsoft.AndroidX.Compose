namespace Microsoft.AndroidX.Compose.Samples.JetNews;

/// <summary>
/// View model for the JetNews home screen. Owns the feed
/// <see cref="MutableStateFlow{T}"/>, kicks off the initial fetch
/// from its ctor, and exposes <see cref="RefreshAsync"/> as the
/// only user-facing command — the canonical UDF surface.
/// </summary>
/// <remarks>
/// Bookmarking lives in a sibling <see cref="BookmarksViewModel"/>
/// so feed reload and bookmark toggling have independent state
/// scopes; this VM is concerned only with the feed.
/// </remarks>
public sealed class HomeViewModel : ViewModel
{
    readonly IPostsRepository _repo;
    readonly MutableStateFlow<HomeUiState> _uiState = new(new HomeUiState.Loading());

    /// <summary>The single observable feed state.</summary>
    public IStateFlow<HomeUiState> UiState => _uiState;

    /// <summary>
    /// Construct and kick off the initial feed fetch. The
    /// <see cref="ViewModel.LaunchAsync"/> body cancels cleanly if
    /// the user navigates away before the fetch returns.
    /// </summary>
    /// <param name="repo">
    /// Posts data source. Defaults to <see cref="PostsRepository.Instance"/>.
    /// </param>
    public HomeViewModel(IPostsRepository? repo = null)
    {
        _repo = repo ?? PostsRepository.Instance;
        _ = LaunchAsync(LoadFeedAsync);
    }

    /// <summary>
    /// Refresh the feed. If the current state is
    /// <see cref="HomeUiState.HasPosts"/>, the visible posts stay on
    /// screen and only the <c>IsRefreshing</c> flag flips while the
    /// re-fetch runs; on first load (state is
    /// <see cref="HomeUiState.Loading"/> or
    /// <see cref="HomeUiState.Error"/>) the screen returns to the
    /// loading spinner.
    /// </summary>
    public Task RefreshAsync() => LaunchAsync(LoadFeedAsync);

    async Task LoadFeedAsync(CancellationToken cancellationToken)
    {
        _uiState.Update(static current => current switch
        {
            HomeUiState.HasPosts h => h with { IsRefreshing = true },
            _ => new HomeUiState.Loading(),
        });
        try
        {
            var feed = await _repo.GetFeedAsync(cancellationToken).ConfigureAwait(true);
            _uiState.Value = new HomeUiState.HasPosts(feed);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Screen left — let ViewModel.LaunchAsync swallow.
            throw;
        }
        catch (Exception ex)
        {
            _uiState.Value = new HomeUiState.Error(ex.Message);
        }
    }
}

