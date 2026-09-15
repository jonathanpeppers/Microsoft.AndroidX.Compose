namespace AndroidX.Compose;

internal sealed class SharedStateOwnership
{
    WeakReference<SharedStateOwner>? _owner;

    internal object Gate { get; } = new();
    internal WeakReference<SharedStateOwner>? Registration
    {
        get => Volatile.Read(ref _owner);
        set => Volatile.Write(ref _owner, value);
    }
    internal SharedStateOwner? Owner => Registration?.TryGetTarget(out var owner) == true ? owner : null;

    internal MutableState<int> Version { get; } = new(0);
}
