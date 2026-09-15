namespace AndroidX.Compose;

internal sealed class SharedStateAcquisition(
    SharedStateOwner owner,
    WeakReference<SharedStateOwner> registration,
    bool isOwner,
    bool initializes,
    Java.Lang.Object? peer) : IDisposable
{
    internal WeakReference<SharedStateOwner> Registration { get; } = registration;
    internal bool IsOwner { get; } = isOwner;
    internal bool Initializes { get; } = initializes;
    internal Java.Lang.Object? Peer { get; private set; } = peer;

    internal void Publish(Java.Lang.Object value)
    {
        owner.Publish(this, value);
        Peer = value;
    }

    internal void Abort(Exception failure) => owner.Abort(this, failure);

    public void Dispose() => GC.KeepAlive(Peer);
}
