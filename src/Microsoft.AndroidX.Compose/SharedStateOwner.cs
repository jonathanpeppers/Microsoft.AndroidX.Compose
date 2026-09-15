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
    bool _releasing;
    SharedStatePhase _phase;
    Java.Lang.Object? _peer;

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

    internal bool IsLive => IsLiveFor(null);

    bool IsLiveFor(IControlledComposition? consumer)
    {
        var ownership = _ownership;
        if (ownership is null)
            return false;
        IControlledComposition? composition;
        IRecomposeScope? scope;
        Java.Util.Concurrent.Atomic.AtomicReference? registrationOrigin, ownershipOrigin;
        lock (ownership.Gate)
        {
            if (!ReferenceEquals(_ownership, ownership) || Handle == IntPtr.Zero)
                return false;
            composition = _composition;
            scope = _scope;
            registrationOrigin = _registrationOrigin;
            ownershipOrigin = _ownershipOrigin;
        }
        var live = composition is null
            || ComposeBridges.SharedStateIsLive(composition, this, scope, registrationOrigin, ownershipOrigin, consumer);
        lock (ownership.Gate)
            return live && ReferenceEquals(_ownership, ownership) && !_releasing;
    }

    internal SharedStateAcquisition Acquire()
    {
        // Readers must execute again if their owner is forgotten during applyChanges.
        var ownership = _ownership
            ?? throw new InvalidOperationException("SharedStateOwner no longer has an active lifetime.");
        _ = ownership.Version.Value;
        while (true)
        {
            WeakReference<SharedStateOwner>? registration;
            SharedStateOwner? previous;
            lock (ownership.Gate)
            {
                if (!ReferenceEquals(_ownership, ownership))
                    throw new InvalidOperationException("SharedStateOwner no longer has an active lifetime.");
                registration = ownership.Registration;
                previous = ownership.Owner;
            }
            // The native wait must never hold the managed arbitration gate.
            var live = previous?.IsLiveFor(_composition) == true;
            lock (ownership.Gate)
            {
                if (!ReferenceEquals(registration, ownership.Registration))
                    continue;
                if (previous?._releasing == true)
                {
                    Monitor.Wait(ownership.Gate);
                    continue;
                }
                if (live)
                {
                    var owns = ReferenceEquals(previous, this);
                    var current = registration
                        ?? throw new InvalidOperationException("Shared state owner has no claim registration.");
                    if (!owns && (previous?._phase != SharedStatePhase.Published || previous._peer is null))
                        throw new InvalidOperationException("Shared state acquisition reached an owner whose native peer has not finished initializing.");
                    return new(this, current, owns, owns && _phase == SharedStatePhase.Initializing, previous?._peer);
                }
            }
            if (ReferenceEquals(previous, this))
            {
                Release();
                throw new InvalidOperationException("SharedStateOwner no longer has an active lifetime.");
            }

            var origin = _composition is { } composition
                ? ComposeBridges.SharedStatePausedOrigin(composition)
                : null;
            var claim = new WeakReference<SharedStateOwner>(this, trackResurrection: true);
            var acquisition = new SharedStateAcquisition(this, claim, true, true, null);
            Action? retiredRelease;
            lock (ownership.Gate)
            {
                if (!ReferenceEquals(registration, ownership.Registration))
                    continue;
                if (previous?._releasing == true)
                {
                    Monitor.Wait(ownership.Gate);
                    continue;
                }
                if (!ReferenceEquals(_ownership, ownership))
                    throw new InvalidOperationException("SharedStateOwner no longer has an active lifetime.");
                retiredRelease = registration is null ? null
                    : (previous is null ? _release : previous._release)
                        ?? throw new InvalidOperationException("SharedStateOwner has no release callback.");
                previous?.Detach();
                _ownershipOrigin = origin;
                _phase = SharedStatePhase.Initializing;
                // Publish before cleanup/factory work: foreign borrowers must
                // wait on this composition until its peer is bound or abandoned.
                ownership.Registration = claim;
                Monitor.PulseAll(ownership.Gate);
            }
            try
            {
                Exception? releaseFailure = null;
                try
                {
                    retiredRelease?.Invoke();
                }
                catch (Exception error)
                {
                    releaseFailure = error;
                    throw;
                }
                finally
                {
                    try
                    {
                        if (registration is not null)
                            ownership.Version.Value++;
                    }
                    catch (Exception notificationFailure) when (releaseFailure is not null)
                    {
                        throw new AggregateException("Shared state cleanup and invalidation failed.",
                            releaseFailure, notificationFailure);
                    }
                }
            }
            catch
            {
                lock (ownership.Gate)
                {
                    if (ReferenceEquals(ownership.Registration, claim))
                        ownership.Registration = null;
                    Detach();
                    Monitor.PulseAll(ownership.Gate);
                }
                throw;
            }
            return acquisition;
        }
    }

    internal void Publish(SharedStateAcquisition acquisition, Java.Lang.Object peer)
    {
        ArgumentNullException.ThrowIfNull(peer);
        var ownership = _ownership
            ?? throw new InvalidOperationException("Shared state acquisition was already retired.");
        lock (ownership.Gate)
        {
            if (!acquisition.IsOwner || !ReferenceEquals(acquisition.Registration, ownership.Registration)
                || !ReferenceEquals(ownership.Owner, this) || _releasing
                || _phase is not (SharedStatePhase.Initializing or SharedStatePhase.Published))
                throw new InvalidOperationException("Shared state acquisition no longer owns its registration.");
            _peer = peer;
            _phase = SharedStatePhase.Published;
            Monitor.PulseAll(ownership.Gate);
        }
    }

    internal void Abort(SharedStateAcquisition acquisition, Exception failure)
    {
        var ownership = _ownership;
        if (ownership is null || !acquisition.IsOwner || !acquisition.Initializes)
            return;
        lock (ownership.Gate)
        {
            if (!ReferenceEquals(acquisition.Registration, ownership.Registration)
                || !ReferenceEquals(ownership.Owner, this) || _phase != SharedStatePhase.Initializing)
                return;
            _phase = SharedStatePhase.Failed;
        }
        try
        {
            Release();
        }
        catch (Exception cleanupFailure)
        {
            throw new AggregateException("Shared state initialization and cleanup failed.", failure, cleanupFailure);
        }
    }

    public void OnRemembered() { }
    public void OnForgotten() => Release();
    public void OnAbandoned() => Release();

    void Release()
    {
        var ownership = _ownership;
        if (ownership is null)
            return;
        Action? release;
        lock (ownership.Gate)
        {
            if (!ReferenceEquals(_ownership, ownership) || _releasing)
                return;
            if (!ReferenceEquals(ownership.Owner, this))
            {
                Detach();
                return;
            }
            _releasing = true;
            release = _release;
        }
        Exception? releaseFailure = null;
        try
        {
            (release ?? throw new InvalidOperationException("SharedStateOwner has no release callback."))();
        }
        catch (Exception error)
        {
            releaseFailure = error;
            throw;
        }
        finally
        {
            // Keep the native wait target available until cleanup has finished.
            lock (ownership.Gate)
            {
                if (ReferenceEquals(ownership.Owner, this))
                    ownership.Registration = null;
                Detach();
                Monitor.PulseAll(ownership.Gate);
            }
            try
            {
                ownership.Version.Value++;
            }
            catch (Exception notificationFailure) when (releaseFailure is not null)
            {
                throw new AggregateException("Shared state cleanup and invalidation failed.",
                    releaseFailure, notificationFailure);
            }
        }
    }

    void Detach()
    {
        _wrapper = null;
        _ownership = null;
        _release = null;
        _scope = null;
        _composition = null;
        _registrationOrigin = null;
        _ownershipOrigin = null;
        _peer = null;
        if (_phase != SharedStatePhase.Failed)
            _phase = SharedStatePhase.Retired;
    }
}
