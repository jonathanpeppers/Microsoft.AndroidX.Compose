using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Android.Runtime;
using AndroidX.Compose.Runtime;
using Kotlin.Coroutines.Intrinsics;
using Xamarin.KotlinX.Coroutines;
using Xamarin.KotlinX.Coroutines.Flow;

namespace ComposeNet;

/// <summary>
/// Async iterator for <see cref="Compose.SnapshotFlow{T}(System.Func{T})"/>.
/// On first <see cref="MoveNextAsync"/> it starts a Kotlin
/// <c>snapshotFlow(producer).collect(...)</c> coroutine on the
/// Compose main dispatcher, with a Kotlin <see cref="IJob"/> in the
/// continuation context so the consumer's
/// <see cref="CancellationToken"/> (or
/// <see cref="DisposeAsync"/>) can tear the flow down for real.
/// </summary>
internal sealed class SnapshotFlowEnumerator<T> : IAsyncEnumerator<T>
{
    readonly System.Func<T> _producer;
    readonly CancellationToken _userToken;
    readonly Channel<T> _channel;

    // JCWs we have to keep rooted for the lifetime of the Kotlin
    // coroutine: dropping any of these while Kotlin is still holding
    // its JNI ref would leave the runtime pointing at a freed peer.
    ObjectFunction0? _producerJcw;
    SnapshotFlowCollectorAdapter<T>? _collectorJcw;
    SnapshotFlowContinuation? _continuation;
    ICompletableJob? _job;
    GCHandle _pin;
    int _pinReleased;

    CancellationTokenRegistration _ctr;
    bool _started;
    bool _disposed;
    T? _current;

    public SnapshotFlowEnumerator(System.Func<T> producer, CancellationToken userToken)
    {
        _producer = producer;
        _userToken = userToken;
        // SingleWriter intentionally false: Emit (Kotlin dispatcher
        // thread), OnResumed (resume thread), and DisposeAsync
        // (caller thread) can each call TryComplete / TryWrite.
        _channel = Channel.CreateBounded<T>(new BoundedChannelOptions(1)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
            SingleWriter = false,
        });
    }

    public T Current => _current!;

    public async ValueTask<bool> MoveNextAsync()
    {
        if (!_started)
        {
            _started = true;
            try
            {
                StartCollection();
            }
            catch (System.Exception ex)
            {
                // Setup failure — surface to the awaiter and stop
                // the iteration. Mark started so a subsequent call
                // sees the now-completed channel instead of trying
                // again.
                _channel.Writer.TryComplete(ex);
            }
        }

        try
        {
            if (await _channel.Reader.WaitToReadAsync(_userToken).ConfigureAwait(false))
            {
                if (_channel.Reader.TryRead(out var value))
                {
                    _current = value;
                    return true;
                }
            }
        }
        catch (ChannelClosedException ex) when (ex.InnerException is not null)
        {
            // WaitToReadAsync surfaces the completion exception as
            // its own ChannelClosedException wrapper; unwrap so the
            // awaiter sees the original error from the producer or
            // collector.
            throw ex.InnerException;
        }

        return false;
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        _ctr.Dispose();

        // Cancel Kotlin-side so snapshotFlow's apply observer
        // unregisters and its internal channel closes. If Kotlin
        // never started, _job is null and there's nothing to cancel.
        try { _job?.Cancel(null); } catch { }

        // Close the channel from our side too in case Kotlin never
        // got far enough to drive ResumeWith (e.g. setup failure).
        _channel.Writer.TryComplete();

        if (_continuation is { } cont)
        {
            try
            {
                // Wait — bounded — for Kotlin to actually unwind.
                // Without this, the JCWs (and their JNI peers) could
                // be GC'd while Kotlin still holds references, which
                // tends to crash with "expected Local but found ..."
                // from CheckJNI.
                var completedFirst = await Task.WhenAny(
                    cont.Completion,
                    Task.Delay(System.TimeSpan.FromSeconds(2))).ConfigureAwait(false);
                if (completedFirst != cont.Completion)
                {
                    // Kotlin didn't acknowledge cancellation within the
                    // grace window. Log and keep going; the JCWs stay
                    // rooted via _pin until the resume eventually
                    // fires, which is leak-not-crash.
                    Android.Util.Log.Warn(
                        "ComposeNet",
                        "SnapshotFlow: Kotlin coroutine didn't unwind within 2s after cancellation; JCWs will stay rooted until the eventual resume.");
                    return;
                }
            }
            catch
            {
                // Completion task can fault if ResumeWith fails;
                // we've already cancelled and best-effort cleaned up.
            }
        }

        ReleasePin();
    }

    void StartCollection()
    {
        // 1. Fresh Kotlin Job so we can cancel the coroutine for
        //    real (vs. just abandoning the C# awaiter).
        _job = JobKt.Job(null);

        // 2. JCWs. Allocate them once and root via _pin so neither
        //    Kotlin nor we drop them while collect is running.
        _producerJcw = new ObjectFunction0(() => MutableState<T>.ToJava(_producer()));
        _collectorJcw = new SnapshotFlowCollectorAdapter<T>(_channel.Writer);
        _continuation = new SnapshotFlowContinuation(_job);

        // Self-pin so fire-and-forget consumers (no one holding the
        // enumerator) can't have GC reclaim the JCWs while the
        // coroutine is still emitting.
        _pin = GCHandle.Alloc(this);

        // 3. Hook channel completion to the continuation's resume.
        //    Closing the writer wakes the awaiter; passing the
        //    optional exception faults the await foreach.
        //    Also release the self-pin once Kotlin has actually
        //    unwound — if the consumer disposed first and
        //    DisposeAsync timed out, this is the only place the
        //    GCHandle gets freed.
        _continuation.OnResumed = error =>
        {
            if (error is not null)
                _channel.Writer.TryComplete(error);
            else
                _channel.Writer.TryComplete();
            ReleasePin();
        };

        // 4. User cancellation → Kotlin job cancel. The job's
        //    cancellation propagates into the flow's snapshot-apply
        //    waiter, which throws CancellationException, which
        //    surfaces via the continuation's ResumeWith path.
        if (_userToken.CanBeCanceled)
        {
            _ctr = _userToken.Register(static state =>
            {
                var self = (SnapshotFlowEnumerator<T>)state!;
                try { self._job?.Cancel(null); } catch { }
            }, this);

            if (_userToken.IsCancellationRequested)
            {
                _job.Cancel(null);
                _channel.Writer.TryComplete();
                _continuation.MarkSynchronouslyCompleted();
                return;
            }
        }

        // 5. Drive the collect. snapshotFlow().collect() suspends
        //    immediately — the first frame's snapshot tracking work
        //    runs on the dispatcher we hand it via _continuation's
        //    context, and emit fires from there on each apply.
        //    Returning anything other than COROUTINE_SUSPENDED would
        //    be the synchronous-completion path (only happens if
        //    Kotlin throws inline before the first suspend).
        var flow = SnapshotStateKt.SnapshotFlow(_producerJcw);
        bool synchronouslyCompleted = false;
        try
        {
            var boxed = flow.Collect(_collectorJcw, _continuation);
            var handle = boxed?.Handle ?? IntPtr.Zero;
            if (handle != IntPtr.Zero && !IsCoroutineSuspended(handle))
            {
                synchronouslyCompleted = true;
                // Only dispose the non-sentinel wrapper. The
                // COROUTINE_SUSPENDED sentinel is a Kotlin singleton
                // whose Java.Lang.Object peer in Mono's cache holds
                // a *global* ref; calling Dispose on it would
                // DeleteLocalRef a global handle and CheckJNI would
                // abort. The cached wrapper outlives every call to
                // collect by design, so leaking the local ref isn't
                // a concern here. (See SuspendBridge for the same
                // pattern.)
                boxed!.Dispose();
            }
        }
        catch (System.Exception ex)
        {
            _channel.Writer.TryComplete(ex);
            _continuation.MarkSynchronouslyCompleted();
            return;
        }

        if (synchronouslyCompleted)
        {
            // Synchronous completion (rare for snapshotFlow — it's
            // designed to be infinite). Treat as immediate end-of-
            // stream. Kotlin won't invoke our continuation in this
            // case, so manually drive OnResumed so DisposeAsync's
            // wait on Completion can finish.
            _continuation.MarkSynchronouslyCompleted();
        }
    }

    void ReleasePin()
    {
        // Idempotent: DisposeAsync and OnResumed can race to be
        // first; whichever loses sees _pinReleased already non-zero
        // and skips the Free.
        if (Interlocked.Exchange(ref _pinReleased, 1) != 0)
            return;
        if (_pin.IsAllocated)
            _pin.Free();
    }

    // Cached global ref to the COROUTINE_SUSPENDED sentinel — same
    // pattern as SuspendBridge. snapshotFlow's collect always
    // suspends on first call (it has to register an apply observer
    // first), so this branch is essentially dead in practice; we
    // still need it to be correct for the synchronous-fault edge
    // case where Kotlin throws inline.
    static IntPtr s_suspendedHandle;
    static bool IsCoroutineSuspended(IntPtr handle)
    {
        if (handle == IntPtr.Zero) return false;
        if (s_suspendedHandle == IntPtr.Zero)
        {
            var inst = IntrinsicsKt.COROUTINE_SUSPENDED;
            var gref = JNIEnv.NewGlobalRef(inst.Handle);
            if (Interlocked.CompareExchange(ref s_suspendedHandle, gref, IntPtr.Zero) != IntPtr.Zero)
                JNIEnv.DeleteGlobalRef(gref);
            System.GC.KeepAlive(inst);
        }
        return JNIEnv.IsSameObject(handle, s_suspendedHandle);
    }
}
