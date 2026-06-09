using Android.OS;

namespace ComposeNet.Samples.JetNews;

/// <summary>
/// Sample-side bridge that emulates Material's
/// <c>SnackbarHostState.showSnackbar</c> for screens that just need to
/// flash a transient message ("Bookmark added", "Share unavailable",
/// etc.). The bound <c>showSnackbar</c> is a Kotlin <c>suspend</c>
/// function and isn't exposed through ComposeNet yet — see the
/// <see cref="SnackbarHost"/> docs that recommend toggling a
/// <see cref="MutableState{T}"/> + rendering a
/// <see cref="Snackbar"/> directly into the
/// <see cref="Scaffold.SnackbarHost"/> slot. This class
/// wraps that workaround behind a <see cref="Show"/> method so callers
/// can fire-and-forget.
/// </summary>
/// <remarks>
/// One controller is typically remembered at the activity / nav-graph
/// level so feedback persists across screen swaps until the auto-clear
/// timer expires (Material's own host state has the same behaviour).
/// </remarks>
public sealed class SnackbarController
{
    int _token;

    /// <summary>
    /// Currently-visible message, or <c>null</c> when no snackbar
    /// should be drawn. Reading this property inside a composable
    /// triggers recomposition when <see cref="Show"/> changes it.
    /// </summary>
    public MutableState<string?> Message { get; } = new(null);

    /// <summary>
    /// Display <paramref name="message"/> in any
    /// <see cref="SnackbarHost"/>-shaped slot bound to this
    /// controller, then auto-clear it after
    /// <paramref name="durationMs"/>. Calling <see cref="Show"/> again
    /// before the previous timer fires replaces the visible message
    /// and cancels the earlier auto-clear (via a monotonically
    /// increasing token) so back-to-back taps don't blink the bar
    /// off mid-display.
    /// </summary>
    /// <param name="message">User-visible text.</param>
    /// <param name="durationMs">
    /// Auto-clear delay, in milliseconds. Defaults to 2.5s — roughly
    /// matches Material's <c>SnackbarDuration.Short</c>.
    /// </param>
    public void Show(string message, int durationMs = 2500)
    {
        var token = Interlocked.Increment(ref _token);
        Message.Value = message;
        new Handler(Looper.MainLooper!).PostDelayed(() =>
        {
            // Only clear if no later Show happened — without the token
            // a fast tap-tap would dismiss the second message early.
            if (Volatile.Read(ref _token) == token)
                Message.Value = null;
        }, durationMs);
    }
}
