namespace AndroidX.Compose;

internal sealed class SharedStateOwnership
{
    WeakReference<SharedStateOwner>? _owner;

    internal bool HasOwner => _owner is not null;
    internal SharedStateOwner? Owner
    {
        get => _owner?.TryGetTarget(out var owner) == true ? owner : null;
        set => _owner = value is null ? null : new(value, trackResurrection: true);
    }

    internal MutableState<int> Version { get; } = new(0);
}
