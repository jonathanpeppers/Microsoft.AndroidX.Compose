namespace Microsoft.AndroidX.Compose.Maui.Handlers;

internal static class ViewportObserverBinding
{
    public static CollectionViewportObserver? Resolve(
        CollectionViewportObserver? current,
        CollectionViewportObserver? bound) =>
        current ?? bound;
}
