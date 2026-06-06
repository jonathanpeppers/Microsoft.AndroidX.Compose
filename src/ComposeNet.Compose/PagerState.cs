using AndroidX.Compose.Foundation.Pager;

namespace ComposeNet;

/// <summary>
/// Caller-supplied state holder for <see cref="HorizontalPager{T}"/> /
/// <see cref="VerticalPager{T}"/>. Wraps the bound
/// <c>androidx.compose.foundation.pager.PagerState</c>; the underlying
/// JVM instance is rebound every recomposition by the pager facade
/// (mirroring Kotlin's <c>rememberPagerState { pageCount }</c>), so
/// reads like <see cref="CurrentPage"/> always reflect the latest
/// remembered state.
/// </summary>
/// <remarks>
/// <para>
/// Typical usage — <c>Remember</c> a state instance and pass it to a
/// pager so other UI can react to scroll position:
/// </para>
/// <code>
/// var pagerState = Remember(() =&gt; new PagerState());
///
/// new Column
/// {
///     new HorizontalPager&lt;int&gt;(
///         items:        new[] { 0, 1, 2 },
///         itemContent:  i =&gt; new Text($"Page {i}"))
///     {
///         State = pagerState,
///     },
///     new Text($"Page {pagerState.CurrentPage} of 3"),
/// }
/// </code>
/// <para>
/// <strong>Render order matters in v1.</strong> Place readers
/// (indicators, text overlays, etc.) <em>after</em> the pager in the
/// node tree so the pager's first render binds <c>Jvm</c> before the
/// reader's render reads it. A reader rendered <em>before</em> the
/// pager on first composition will read the pre-binding fallback
/// (<c>0</c> / <c>0f</c>) and won't subscribe to the underlying
/// snapshot — it will only become reactive after another recomposition
/// re-runs the outer build code. Reads return safe fallbacks until
/// the first pager render binds this state to its remembered Kotlin
/// peer.
/// </para>
/// </remarks>
public sealed class PagerState
{
    /// <summary>
    /// Underlying <c>androidx.compose.foundation.pager.PagerState</c>
    /// returned by <c>rememberPagerState</c>. Refreshed every render so
    /// the Kotlin <c>pageCount</c> lambda observes recomposition-time
    /// changes (e.g. an item list growing).
    /// </summary>
    internal AndroidX.Compose.Foundation.Pager.PagerState? Jvm { get; set; }

    /// <summary>
    /// Index of the page closest to the snapped position. Mirrors
    /// Kotlin's <c>PagerState.currentPage</c>. Returns <c>0</c> until
    /// the state is bound by the first pager render.
    /// </summary>
    public int CurrentPage => Jvm?.CurrentPage ?? 0;

    /// <summary>
    /// Index of the page the pager has settled on after a fling or
    /// programmatic scroll completes. Mirrors Kotlin's
    /// <c>PagerState.settledPage</c>. Returns <c>0</c> until bound.
    /// </summary>
    public int SettledPage => Jvm?.SettledPage ?? 0;

    /// <summary>
    /// Index of the page the pager is currently animating toward
    /// (== <see cref="CurrentPage"/> when no scroll is in flight).
    /// Mirrors Kotlin's <c>PagerState.targetPage</c>. Returns <c>0</c>
    /// until bound.
    /// </summary>
    public int TargetPage => Jvm?.TargetPage ?? 0;

    /// <summary>
    /// Fractional offset of the current page in <c>[-0.5, 0.5)</c>,
    /// where <c>0</c> means the page is fully snapped. Mirrors Kotlin's
    /// <c>PagerState.currentPageOffsetFraction</c>. Returns <c>0f</c>
    /// until bound.
    /// </summary>
    public float CurrentPageOffsetFraction => Jvm?.CurrentPageOffsetFraction ?? 0f;

    /// <summary>
    /// Total number of pages reported by the pager's <c>pageCount</c>
    /// lambda. Mirrors Kotlin's <c>PagerState.pageCount</c>. Returns
    /// <c>0</c> until bound.
    /// </summary>
    public int PageCount => Jvm?.PageCount ?? 0;
}
