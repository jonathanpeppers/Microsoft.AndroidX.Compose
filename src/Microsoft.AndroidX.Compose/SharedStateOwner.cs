using System.Runtime.CompilerServices;
using Android.Runtime;
using AndroidX.Compose.Runtime;

namespace AndroidX.Compose;

// A direct slot value: wrapping this in RememberHolder would hide IRememberObserver.
[Register("net/compose/SharedStateOwner")]
internal sealed class SharedStateOwner : Java.Lang.Object, IRememberObserver
{
    static readonly ConditionalWeakTable<object, SharedStateOwnership> Ownerships = new();
    readonly object? _wrapper;
    readonly SharedStateOwnership _ownership;
    readonly Action _release;

    SharedStateOwner(object? wrapper, Action release)
    {
        _wrapper = wrapper;
        _ownership = wrapper is null ? new() : Ownerships.GetValue(wrapper, static _ => new());
        _release = release;
    }

    internal static SharedStateOwner Remember(IComposer composer, object? wrapper, Action release)
    {
        composer.StartReplaceableGroup(354101);
        try
        {
            if (composer.RememberedValue() is SharedStateOwner existing
                && ReferenceEquals(existing._wrapper, wrapper))
                return existing;

            var owner = new SharedStateOwner(wrapper, release);
            composer.UpdateRememberedValue(owner);
            return owner;
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
            _ = _ownership.Version.Value;
            _ownership.Owner ??= this;
            return ReferenceEquals(_ownership.Owner, this);
        }
    }

    public void OnRemembered() { }
    public void OnForgotten() => Release();
    public void OnAbandoned() => Release();

    void Release()
    {
        if (!ReferenceEquals(_ownership.Owner, this))
            return;

        _release();
        _ownership.Owner = null;
        _ownership.Version.Value++;
    }
}
