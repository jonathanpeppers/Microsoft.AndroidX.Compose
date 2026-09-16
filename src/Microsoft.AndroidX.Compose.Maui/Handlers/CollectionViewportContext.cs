namespace Microsoft.AndroidX.Compose.Maui.Handlers;

internal static class CollectionViewportContext
{
    static readonly System.Threading.AsyncLocal<CollectionViewportObserver?>
        s_current = new();

    public static CollectionViewportObserver? Current => s_current.Value;

    public static T BuildItem<T>(
        CollectionViewportObserver observer,
        Func<T> factory)
    {
        ArgumentNullException.ThrowIfNull(observer);
        ArgumentNullException.ThrowIfNull(factory);
        var previous = s_current.Value;
        s_current.Value = observer;
        try
        {
            return factory();
        }
        finally
        {
            s_current.Value = previous;
        }
    }
}
