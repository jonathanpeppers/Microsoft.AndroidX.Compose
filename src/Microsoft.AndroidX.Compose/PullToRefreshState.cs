using AndroidX.Compose.Material3.PullToRefresh;

namespace AndroidX.Compose;

/// <summary>
/// Caller-supplied state holder for <see cref="PullToRefreshBox"/>. The
/// underlying JVM
/// <c>androidx.compose.material3.pulltorefresh.PullToRefreshState</c>
/// is created lazily the first time a <see cref="PullToRefreshBox"/>
/// bound to this state is rendered; reads of <see cref="DistanceFraction"/>
/// or <see cref="IsAnimating"/> before that point return safe defaults
/// (<c>0f</c> / <c>false</c>).
/// </summary>
/// <remarks>
/// Same shape as <see cref="DateRangePickerState"/>: a thin wrapper
/// whose <c>Jvm</c> field is populated by the generator-emitted state-
/// holder facade on first render. Typical usage is to <c>Remember</c>
/// an instance, pass it as the <c>state</c> ctor arg of a
/// <see cref="PullToRefreshBox"/>, and (optionally) drive UI off the
/// progress properties — e.g. a custom indicator that fades in as
/// <see cref="DistanceFraction"/> approaches <c>1</c>.
///
/// <code>
/// var refreshing = Remember(() =&gt; new MutableState&lt;bool&gt;(false));
/// var ptrState   = Remember(() =&gt; new PullToRefreshState());
///
/// new PullToRefreshBox(
///     isRefreshing: refreshing.Value,
///     onRefresh:    () =&gt; StartReload(refreshing),
///     state:        ptrState)
/// {
///     new LazyColumn&lt;int&gt;(items: rows, itemContent: i =&gt; new Text($"Row {i}"))
///     {
///         Modifier = Modifier.FillMaxSize(),
///     },
/// }
/// </code>
/// </remarks>
public sealed class PullToRefreshState
{
    internal IPullToRefreshState? Jvm;

    /// <summary>
    /// Current pull progress, in the range <c>[0f, 1f+]</c>. <c>0f</c>
    /// at rest; reaches <c>1f</c> at the refresh threshold; can exceed
    /// <c>1f</c> on over-pull. Returns <c>0f</c> until the first
    /// <see cref="PullToRefreshBox"/> render binds this state to the
    /// JVM picker. Read inside a composable to drive a custom
    /// indicator's visual feedback.
    /// </summary>
    public float DistanceFraction => Jvm?.DistanceFraction ?? 0f;

    /// <summary>
    /// <c>true</c> while the indicator is animating into / out of its
    /// rest position (typically right after the user releases past the
    /// threshold, before <c>onRefresh</c> fires, and while the indicator
    /// retracts after the caller clears the busy flag). Returns
    /// <c>false</c> until the state is bound.
    /// </summary>
    public bool IsAnimating => Jvm?.IsAnimating ?? false;

    /// <summary>Animates the indicator back to its hidden resting position.</summary>
    /// <param name="cancellationToken">Cancels the returned task and stops the Kotlin animation at its next cancellable suspend point.</param>
    public Task AnimateToHiddenAsync(CancellationToken cancellationToken = default) =>
        SuspendBridge.Invoke(
            cont => ComposeBridges.PullToRefreshStateAnimateToHidden(
                ((Java.Lang.Object)RequireJvm()).Handle, cont),
            cancellationToken);

    /// <summary>Animates the indicator to the refresh threshold.</summary>
    /// <param name="cancellationToken">Cancels the returned task and stops the Kotlin animation at its next cancellable suspend point.</param>
    public Task AnimateToThresholdAsync(CancellationToken cancellationToken = default) =>
        SuspendBridge.Invoke(
            cont => ComposeBridges.PullToRefreshStateAnimateToThreshold(
                ((Java.Lang.Object)RequireJvm()).Handle, cont),
            cancellationToken);

    /// <summary>Snaps the indicator to a non-negative threshold fraction.</summary>
    /// <param name="targetValue"><c>0</c> hides the indicator, <c>1</c> places it at the threshold, and values greater than <c>1</c> represent overshoot.</param>
    /// <param name="cancellationToken">Cancels the returned task and the underlying Kotlin operation.</param>
    public Task SnapToAsync(
        float targetValue,
        CancellationToken cancellationToken = default)
    {
        if (!(targetValue >= 0f))
            throw new ArgumentOutOfRangeException(
                nameof(targetValue),
                targetValue,
                "Target value must be greater than or equal to zero.");
        return SuspendBridge.Invoke(
            cont => ComposeBridges.PullToRefreshStateSnapTo(
                ((Java.Lang.Object)RequireJvm()).Handle, targetValue, cont),
            cancellationToken);
    }

    IPullToRefreshState RequireJvm() =>
        Jvm ?? throw new InvalidOperationException(
            "PullToRefreshState is not bound. Render it with PullToRefreshBox before controlling it.");
}
