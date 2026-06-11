using AndroidX.Compose.Runtime;

namespace AndroidX.Compose;

/// <summary>
/// Material 3 <c>PullToRefreshBox</c>. A container that wraps scrollable
/// content (typically a <see cref="LazyColumn{T}"/>) with the Material
/// pull-to-refresh gesture: the user drags past the threshold and
/// Compose invokes <c>onRefresh</c>. The caller owns the busy flag
/// (<c>isRefreshing</c>) and clears it when the async reload finishes.
/// </summary>
/// <remarks>
/// Pass an explicit <see cref="PullToRefreshState"/> to
/// observe pull progress (<see cref="PullToRefreshState.DistanceFraction"/> /
/// <see cref="PullToRefreshState.IsAnimating"/>) for a custom indicator
/// or analytics; omit it and Compose creates one internally via
/// <c>rememberPullToRefreshState()</c>. The stock Material 3 spinner is
/// always used — indicator customization is not yet exposed (the
/// underlying container + optional <c>Function3</c> slot is the same
/// hybrid shape the facade generator can't model for <c>BottomAppBar</c>;
/// see <c>.github/copilot-instructions.md</c>).
///
/// <code>
/// var refreshing = Remember(() =&gt; new MutableState&lt;bool&gt;(false));
///
/// new PullToRefreshBox(
///     isRefreshing: refreshing.Value,
///     onRefresh:    () =&gt; StartReload(refreshing))
/// {
///     new LazyColumn&lt;int&gt;(items: rows, itemContent: i =&gt; new Text($"Row {i}"))
///     {
///         Modifier = Modifier.FillMaxSize(),
///     },
/// }
/// </code>
///
/// The generated <c>Render()</c> calls
/// <c>ComposeBridges.RememberPullToRefreshState</c> to obtain the JVM
/// state handle, populates the wrapper's <c>Jvm</c> field on first
/// render (so subsequent property reads on the wrapper hit the live
/// state), and forwards the handle into the
/// <c>androidx.compose.material3.pulltorefresh.PullToRefreshKt.PullToRefreshBox</c>
/// composable.
///
/// When used as the <see cref="Scaffold.Body"/>, the scaffold-supplied
/// <c>PaddingValues</c> handle is forwarded to the wrapped scrollable
/// child instead of being applied as a <c>Modifier.padding</c> on the
/// PullToRefreshBox frame. That matches upstream Material's "list items
/// scroll under top/bottom bars" idiom — the pull spinner still appears
/// inside the visible area, and the inner <see cref="LazyColumn{T}"/>'s
/// first/last items stay reachable above and below the chrome.
/// </remarks>
public sealed partial class PullToRefreshBox
{
    /// <summary>
    /// Forward the <see cref="Scaffold"/>-supplied <c>PaddingValues</c>
    /// handle into the wrapped scrollable child instead of consuming it
    /// as a <c>Modifier.padding</c> on the PullToRefreshBox frame. See
    /// the type-level remarks for the rationale.
    /// </summary>
    internal override void Render(IComposer composer, IntPtr contentPadding)
    {
        if (contentPadding == IntPtr.Zero)
        {
            Render(composer);
            return;
        }

        SetFirstChildContentPadding(contentPadding);
        try { Render(composer); }
        finally { SetFirstChildContentPadding(IntPtr.Zero); }
    }
}
