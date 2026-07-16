using Kotlin.Jvm.Functions;

namespace AndroidX.Compose;

/// <summary>
/// Caller-supplied state holder for the Material 3 carousel facades.
/// The underlying Kotlin state is created by
/// <c>rememberCarouselState</c> when the carousel first composes and
/// is retained by this wrapper across removal and re-entry.
/// </summary>
/// <remarks>
/// Construct the state in a <c>Remember</c> callback and pass the same
/// instance to a carousel. The <c>itemCount</c> callback
/// must report the number of items rendered by that carousel.
/// </remarks>
public sealed class CarouselState
{
    readonly ComposableLambda0Int _itemCount;

    internal AndroidX.Compose.Material3.Carousel.CarouselState? Jvm;
    internal IFunction0 ItemCount => _itemCount;
    internal int InitialItem { get; }

    /// <summary>Creates carousel state with the requested initial item.</summary>
    /// <param name="itemCount">
    /// Callback queried by Compose for the current number of carousel items.
    /// </param>
    /// <param name="initialItem">Zero-based item index selected initially.</param>
    public CarouselState(Func<int> itemCount, int initialItem = 0)
    {
        ArgumentNullException.ThrowIfNull(itemCount);
        if (initialItem < 0)
            throw new ArgumentOutOfRangeException(nameof(initialItem), initialItem, "Initial item must be non-negative.");

        _itemCount = new ComposableLambda0Int(itemCount);
        InitialItem = initialItem;
    }

    /// <summary>
    /// Zero-based item closest to the carousel's snapped position. Before
    /// first composition, returns the constructor's <c>initialItem</c>.
    /// </summary>
    public int CurrentItem => Jvm?.CurrentItem ?? InitialItem;

    /// <summary>
    /// Whether a gesture, fling, or programmatic scroll is currently active.
    /// Returns <c>false</c> before first composition.
    /// </summary>
    public bool IsScrollInProgress => Jvm?.IsScrollInProgress ?? false;

    /// <summary>Snaps immediately to the requested item.</summary>
    /// <param name="item">Zero-based target item index.</param>
    /// <param name="cancellationToken">
    /// Cancels the returned task; the Kotlin operation continues to natural
    /// completion.
    /// </param>
    public Task ScrollToItemAsync(
        int item,
        CancellationToken cancellationToken = default) =>
        SuspendBridge.Invoke(
            cont => ComposeBridges.CarouselStateScrollToItem(
                ((Java.Lang.Object)RequireJvm()).Handle, item, cont),
            cancellationToken);

    /// <summary>
    /// Animates to the requested item using Compose's default spring.
    /// </summary>
    /// <param name="item">Zero-based target item index.</param>
    /// <param name="cancellationToken">
    /// Cancels the returned task; the Kotlin operation continues to natural
    /// completion.
    /// </param>
    public Task AnimateScrollToItemAsync(
        int item,
        CancellationToken cancellationToken = default) =>
        SuspendBridge.Invoke(
            cont => ComposeBridges.CarouselStateAnimateScrollToItem(
                ((Java.Lang.Object)RequireJvm()).Handle, item, null, cont),
            cancellationToken);

    internal void BindJvm(AndroidX.Compose.Material3.Carousel.CarouselState jvm)
    {
        Jvm ??= jvm;
    }

    AndroidX.Compose.Material3.Carousel.CarouselState RequireJvm() =>
        Jvm ?? throw new InvalidOperationException(
            "CarouselState is not bound. Render it with a carousel before scrolling.");
}
