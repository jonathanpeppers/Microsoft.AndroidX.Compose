using System.Runtime.CompilerServices;
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
    IControlledComposition? _composition;
    IRecomposeScope? _scope;
    Java.Util.Concurrent.Atomic.AtomicReference? _registrationOrigin;
    Java.Util.Concurrent.Atomic.AtomicReference? _ownershipOrigin;

    SharedStateOwner(object? wrapper, Action release)
    {
        _wrapper = wrapper;
        _ownership = wrapper is null ? new() : Ownerships.GetValue(wrapper, static _ => new());
        _release = release;
    }

    internal SharedStateOwner(IntPtr handle, JniHandleOwnership transfer)
        : base(handle, transfer)
    {
        // Native slot reachability preserves the original JCW through the GC bridge.
        // An empty activation would lose arbitration and the release callback.
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
            {
                if (existing.IsLive)
                    return existing;
                existing.Release();
            }

            var composition = composer.Composition;
            var origin = ComposeBridges.SharedStatePausedOrigin(composition);
            return Publish(wrapper, release, owner =>
            {
                owner._composition = composition;
                owner._registrationOrigin = origin;
                composer.UpdateRememberedValue(owner);
            });
        }
        finally
        {
            composer.EndReplaceableGroup();
        }
    }

    internal void TrackScope(IComposer composer)
    {
        // This keyed group contains only the marker, never native rememberSaveable.
        composer.StartMovableGroup(354103, this);
        try
        {
            var inner = composer.StartRestartGroup(354104);
            try
            {
                _scope = inner.RecomposeScope
                    ?? throw new InvalidOperationException("SharedStateOwner lifetime scope was unavailable.");
                inner.RecordUsed(_scope);
            }
            finally
            {
                inner.EndRestartGroup();
            }
        }
        finally
        {
            composer.EndMovableGroup();
        }
    }

    internal bool IsLive
    {
        get
        {
            if (_ownership is null || Handle == IntPtr.Zero)
                return false;
            return _composition is not { } composition
                || ComposeBridges.SharedStateIsLive(composition, this, _scope, _registrationOrigin, _ownershipOrigin);
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
            var previous = ownership.Owner;
            if (previous is not null && !previous.IsLive)
            {
                previous.Release();
                if (ReferenceEquals(previous, this))
                    throw new InvalidOperationException("SharedStateOwner no longer has an active lifetime.");
                previous = null;
            }
            if (previous is null)
            {
                if (ownership.HasOwner)
                {
                    // The native token died without a callback. The incoming wrapper's
                    // cleanup captures the last peer values before creating a successor.
                    try
                    {
                        (_release ?? throw new InvalidOperationException("SharedStateOwner has no release callback."))();
                    }
                    finally
                    {
                        ownership.Owner = null;
                        ownership.Version.Value++;
                    }
                }
                // A committed borrower can acquire its first peer during paused work.
                // Ordinary owning rerenders must retain the original acquisition.
                _ownershipOrigin = _composition is { } composition
                    ? ComposeBridges.SharedStatePausedOrigin(composition)
                    : null;
                ownership.Owner = this;
            }
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
        _scope = null;
        _composition = null;
        _registrationOrigin = null;
        _ownershipOrigin = null;
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
}
