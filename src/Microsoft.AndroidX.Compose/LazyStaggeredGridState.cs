using BindingLazyStaggeredGridState =
    AndroidX.Compose.Foundation.Lazy.Staggeredgrid.LazyStaggeredGridState;

namespace AndroidX.Compose;

/// <summary>
/// Managed scroll state for <see cref="LazyVerticalStaggeredGrid{T}"/> and
/// <see cref="LazyHorizontalStaggeredGrid{T}"/>.
/// </summary>
/// <remarks>
/// Obtain a composition-owned instance with
/// <see cref="ComposeExtensions.RememberLazyStaggeredGridState(AndroidX.Compose.Runtime.IComposer, int, int, int, string)"/>.
/// The remembered state survives recomposition but not process death.
/// </remarks>
public sealed class LazyStaggeredGridState
{
    internal BindingLazyStaggeredGridState Jvm { get; }

    /// <summary>Creates staggered-grid state with an initial visible item and pixel offset.</summary>
    /// <param name="initialFirstVisibleItemIndex">Initial first-visible item index.</param>
    /// <param name="initialFirstVisibleItemScrollOffset">Initial first-visible item offset in pixels.</param>
    public LazyStaggeredGridState(
        int initialFirstVisibleItemIndex = 0,
        int initialFirstVisibleItemScrollOffset = 0)
    {
        Jvm = new BindingLazyStaggeredGridState(
            initialFirstVisibleItemIndex,
            initialFirstVisibleItemScrollOffset);
    }

    internal LazyStaggeredGridState(BindingLazyStaggeredGridState jvm)
    {
        Jvm = jvm;
    }

    /// <summary>Index of the first currently visible item.</summary>
    public int FirstVisibleItemIndex => Jvm.FirstVisibleItemIndex;

    /// <summary>Pixel offset of the first currently visible item.</summary>
    public int FirstVisibleItemScrollOffset => Jvm.FirstVisibleItemScrollOffset;

    /// <summary>Whether a gesture, fling, or programmatic scroll is in progress.</summary>
    public bool IsScrollInProgress => Jvm.IsScrollInProgress;

    /// <summary>Whether the staggered grid can scroll backward.</summary>
    public bool CanScrollBackward => Jvm.CanScrollBackward;

    /// <summary>Whether the staggered grid can scroll forward.</summary>
    public bool CanScrollForward => Jvm.CanScrollForward;

    /// <summary>Whether the most recent scroll motion was backward.</summary>
    public bool LastScrolledBackward => Jvm.LastScrolledBackward;

    /// <summary>Whether the most recent scroll motion was forward.</summary>
    public bool LastScrolledForward => Jvm.LastScrolledForward;

    /// <summary>Snaps immediately to an item.</summary>
    /// <param name="index">Target item index.</param>
    /// <param name="scrollOffset">Additional pixel offset from the viewport start.</param>
    /// <param name="cancellationToken">Cancels the returned task and Kotlin scroll operation.</param>
    public Task ScrollToItemAsync(
        int index,
        int scrollOffset = 0,
        CancellationToken cancellationToken = default) =>
        SuspendBridge.Invoke(
            cont => ComposeBridges.LazyStaggeredGridStateScrollToItem(
                ((Java.Lang.Object)Jvm).Handle,
                index,
                scrollOffset,
                cont),
            cancellationToken);

    /// <summary>Animates to an item using Compose's default scroll animation.</summary>
    /// <param name="index">Target item index.</param>
    /// <param name="scrollOffset">Additional pixel offset from the viewport start.</param>
    /// <param name="cancellationToken">Cancels the returned task and Kotlin scroll operation.</param>
    public Task AnimateScrollToItemAsync(
        int index,
        int scrollOffset = 0,
        CancellationToken cancellationToken = default) =>
        SuspendBridge.Invoke(
            cont => ComposeBridges.LazyStaggeredGridStateAnimateScrollToItem(
                ((Java.Lang.Object)Jvm).Handle,
                index,
                scrollOffset,
                cont),
            cancellationToken);
}
