namespace Microsoft.AndroidX.Compose.Maui.Handlers;

internal sealed class LazyListScrollSnapshot
{
    public LazyListScrollSnapshot(
        LazyListVisibleItemSnapshot[] visibleItems)
    {
        System.ArgumentNullException.ThrowIfNull(visibleItems);
        VisibleItems = visibleItems;
    }

    public LazyListVisibleItemSnapshot[] VisibleItems { get; }
}
