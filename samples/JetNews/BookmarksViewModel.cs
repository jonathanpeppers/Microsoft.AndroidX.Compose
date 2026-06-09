namespace ComposeNet.Samples.JetNews;

/// <summary>
/// View model holding the user's bookmarked post ids. Lives at the
/// activity / nav-graph level so both <see cref="HomeScreen"/> and
/// <see cref="PostScreen"/> see the same toggled set, and so the
/// list survives configuration change (the activity's
/// <see cref="AndroidX.Lifecycle.ViewModelStore"/> retains the
/// instance across <c>Activity.OnCreate</c> calls).
/// </summary>
/// <remarks>
/// Bookmarking is intentionally split out of <see cref="HomeViewModel"/>
/// because it's a different scope of state: the feed is per-screen
/// and re-fetched on demand, while bookmarks are app-wide and
/// permanent for the user. Modeling it as a separate VM also avoids
/// the footgun where the host <see cref="MainActivity"/> hands the
/// raw <see cref="MutableStateList{T}"/> to <see cref="HomeViewModel"/>
/// — after a configuration change the host would build a fresh list
/// while the retained VM still references the stale one.
/// </remarks>
public sealed class BookmarksViewModel : ViewModel
{
    /// <summary>
    /// Observable set of bookmarked post ids. Reads inside a
    /// composable trigger recomposition when the membership
    /// changes (per <see cref="MutableStateList{T}"/> snapshot
    /// semantics).
    /// </summary>
    public MutableStateList<string> Bookmarks { get; } = new();

    /// <summary>Returns <c>true</c> if <paramref name="postId"/> is bookmarked.</summary>
    public bool Contains(string postId)
    {
        ArgumentNullException.ThrowIfNull(postId);
        return Bookmarks.Contains(postId);
    }

    /// <summary>
    /// Flip bookmark membership for <paramref name="postId"/> —
    /// removes the entry if present, adds it otherwise.
    /// </summary>
    public void Toggle(string postId)
    {
        ArgumentNullException.ThrowIfNull(postId);
        if (!Bookmarks.Remove(postId))
            Bookmarks.Add(postId);
    }

    /// <summary>
    /// Set <paramref name="postId"/> bookmark membership to
    /// <paramref name="bookmarked"/>. Idempotent — no change is
    /// made when the desired state already matches.
    /// </summary>
    public void Set(string postId, bool bookmarked)
    {
        ArgumentNullException.ThrowIfNull(postId);
        if (bookmarked)
        {
            if (!Bookmarks.Contains(postId))
                Bookmarks.Add(postId);
        }
        else
        {
            Bookmarks.Remove(postId);
        }
    }
}
