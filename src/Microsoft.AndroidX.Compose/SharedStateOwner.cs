using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Android.Runtime;
using AndroidX.Compose.Runtime;

namespace AndroidX.Compose;

// A direct slot value: wrapping this in RememberHolder would hide IRememberObserver.
[Register("net/compose/SharedStateOwner")]
internal sealed class SharedStateOwner : Java.Lang.Object, IRememberObserver
{
    static readonly ConditionalWeakTable<object, SharedStateOwnership> Ownerships = new();
    object? _wrapper;
    SharedStateOwnership? _ownership;
    Action? _release;
    GCHandle _selfRoot;

    SharedStateOwner(object? wrapper, Action release)
    {
        _wrapper = wrapper;
        _ownership = wrapper is null ? new() : Ownerships.GetValue(wrapper, static _ => new());
        _release = release;
        // Preserve the stateful peer even before OnRemembered. Every token, including
        // non-owning siblings, releases this root on forgotten/abandoned or failed publication.
        _selfRoot = GCHandle.Alloc(this);
    }

    internal SharedStateOwner(IntPtr handle, JniHandleOwnership transfer)
        : base(handle, transfer)
    {
        // A live token is rooted, so activation cannot be its normal GC path.
        // An empty replacement would lose both arbitration and the release callback.
        throw new InvalidOperationException("SharedStateOwner activation lost its original managed lifetime state.");
    }

    internal static SharedStateOwner Publish(object? wrapper, Action release, Action<SharedStateOwner> publish)
    {
        var owner = new SharedStateOwner(wrapper, release);
        try
        {
            publish(owner);
            return owner;
        }
        catch
        {
            owner.Release();
            throw;
        }
    }

    internal static SharedStateOwner Remember(IComposer composer, object? wrapper, Action release)
    {
        composer.StartReplaceableGroup(354101);
        try
        {
            if (composer.RememberedValue() is SharedStateOwner existing
                && existing._ownership is not null
                && ReferenceEquals(existing._wrapper, wrapper))
                return existing;

            return Publish(wrapper, release, composer.UpdateRememberedValue);
        }
        finally
        {
            composer.EndReplaceableGroup();
        }
    }

    internal bool IsOwner
    {
        get
        {
            // Readers must execute again if their owner is forgotten during applyChanges.
            var ownership = _ownership
                ?? throw new InvalidOperationException("SharedStateOwner no longer has an active lifetime.");
            _ = ownership.Version.Value;
            ownership.Owner ??= this;
            return ReferenceEquals(ownership.Owner, this);
        }
    }

    public void OnRemembered() { }
    public void OnForgotten() => Release();
    public void OnAbandoned() => Release();

    void Release()
    {
        var ownership = _ownership;
        var release = _release;
        _wrapper = null;
        _ownership = null;
        _release = null;
        try
        {
            if (ownership is null || !ReferenceEquals(ownership.Owner, this))
                return;

            try
            {
                (release ?? throw new InvalidOperationException("SharedStateOwner has no release callback."))();
            }
            finally
            {
                ownership.Owner = null;
                ownership.Version.Value++;
            }
        }
        finally
        {
            if (_selfRoot.IsAllocated)
                _selfRoot.Free();
        }
    }
}
