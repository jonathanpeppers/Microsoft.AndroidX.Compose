namespace Microsoft.AndroidX.Compose.Maui.Handlers;

internal sealed class LazyListScrollSnapshot
{
    public LazyListScrollSnapshot(
        LazyListVisibleItemSnapshot[] visibleItems,
        int viewportStart,
        int viewportEnd)
    {
        System.ArgumentNullException.ThrowIfNull(visibleItems);
        VisibleItems = visibleItems;
        ViewportStart = viewportStart;
        ViewportEnd = viewportEnd;
    }

    public LazyListVisibleItemSnapshot[] VisibleItems { get; }

    public int ViewportStart { get; }

    public int ViewportEnd { get; }
}
