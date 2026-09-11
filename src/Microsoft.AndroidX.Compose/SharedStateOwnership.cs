namespace AndroidX.Compose;

internal sealed class SharedStateOwnership
{
    internal SharedStateOwner? Owner;
    internal MutableState<int> Version { get; } = new(0);
}
