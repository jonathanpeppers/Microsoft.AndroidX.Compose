using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Android.Runtime;
using Kotlin.Coroutines;

namespace ComposeNet;

/// <summary>
/// Java Callable Wrapper that lets a Kotlin <c>suspend</c> function
/// resume back into managed code via a
/// <see cref="TaskCompletionSource{TResult}"/>. The wrapped
/// <c>kotlin.Result&lt;T&gt;</c> handed to
/// <see cref="ResumeWith(Java.Lang.Object)"/> is stored verbatim and
/// unboxed by <see cref="SuspendBridge"/>.
/// </summary>
/// <remarks>
/// <para>
/// Allocate one instance per suspend call (continuations cannot be
/// reused). The class is internal — call sites should go through
/// <see cref="SuspendBridge.Invoke{T}(System.Func{SuspendContinuation, System.IntPtr}, System.Func{Java.Lang.Object?, T}, CancellationToken)"/>
/// instead of constructing one directly.
/// </para>
/// <para>
/// Lifetime: held by a strong <see cref="GCHandle"/> allocated in
/// the constructor so the managed peer survives until Kotlin invokes
/// <see cref="ResumeWith(Java.Lang.Object)"/> — fire-and-forget callers
/// (no one holding the returned <see cref="Task"/>) would otherwise
/// race the GC. The handle is a normal strong reference, not pinned
/// in memory. <see cref="ReleaseLifetime(bool)"/> is the single
/// cleanup entry point: it frees the pin, disposes the
/// <see cref="CancellationTokenRegistration"/>, and is idempotent
/// (guarded by an interlocked flag) so the overlapping
/// completion / cancel / dispose paths can't double-free.
/// </para>
/// <para>
/// Cancellation: when a non-default <see cref="CancellationToken"/>
/// is supplied, a registration on the token cancels the backing TCS.
/// That propagates to the caller's <c>await</c> as
/// <see cref="System.OperationCanceledException"/> immediately, but
/// the Kotlin suspend function keeps running to its natural
/// completion — we don't wire a <c>Job</c> into <see cref="Context"/>
/// yet. When Kotlin eventually resumes,
/// <see cref="CompleteWithLocalHandle"/> sees the TCS is already
/// completed and disposes the boxed result without surfacing it.
/// </para>
/// </remarks>
[Register("composenet/compose/SuspendContinuation")]
internal sealed class SuspendContinuation : Java.Lang.Object, IContinuation
{
    GCHandle _selfPin;
    CancellationTokenRegistration _ctr;
    int _lifetimeReleased;

    /// <summary>
    /// Backing TCS exposed for <see cref="SuspendBridge"/>.
    /// <see cref="TaskCreationOptions.RunContinuationsAsynchronously"/>
    /// keeps awaiters from running inline on whatever Kotlin dispatcher
    /// fires the resume (usually the Compose main thread).
    /// </summary>
    public TaskCompletionSource<Java.Lang.Object?> Tcs { get; } =
        new TaskCompletionSource<Java.Lang.Object?>(TaskCreationOptions.RunContinuationsAsynchronously);

    public SuspendContinuation(CancellationToken cancellationToken = default)
    {
        _selfPin = GCHandle.Alloc(this);
        if (cancellationToken.CanBeCanceled)
        {
            // Allocation-free (state, token) overload: no closure per
            // suspend call. Disposing the registration in
            // ReleaseLifetime blocks until any in-flight invocation of
            // this callback completes; the callback only touches the
            // (thread-safe) TCS, so there's no deadlock path back into
            // ReleaseLifetime.
            _ctr = cancellationToken.Register(
                static (state, token) =>
                {
                    var self = (SuspendContinuation)state!;
                    self.Tcs.TrySetCanceled(token);
                },
                this);
        }
    }

    /// <summary>
    /// Returns the <c>androidx.compose.ui.platform.AndroidUiDispatcher.Main</c>
    /// context — combines a Compose main-thread dispatcher with a
    /// <c>MonotonicFrameClock</c>. Required so suspend functions that
    /// rely on <c>withFrameNanos</c> (the entire animation family —
    /// <c>animateScrollTo</c>, <c>animateTo</c>, <c>animate</c>) can
    /// drive frames; otherwise their internal
    /// <c>coroutineContext[MonotonicFrameClock]</c> lookup throws.
    /// </summary>
    /// <remarks>
    /// The <c>AndroidUiDispatcher.Companion</c> nested type is bound but
    /// has an internal constructor and no static accessor on
    /// <c>AndroidUiDispatcher</c>, so we resolve <c>Companion.Main</c>
    /// via raw JNI. Replace with a direct binding call once
    /// <c>AndroidUiDispatcher.Companion</c> becomes accessible.
    /// </remarks>
    public ICoroutineContext Context
    {
        get
        {
            var handle = ComposeBridges.AndroidUiDispatcherMain();
            return global::Java.Lang.Object.GetObject<ICoroutineContext>(
                handle, JniHandleOwnership.DoNotTransfer)!;
        }
    }

    /// <summary>
    /// Kotlin's resume callback. The boxed <c>kotlin.Result&lt;T&gt;</c>
    /// is either the raw success value (often a boxed primitive) or a
    /// <c>kotlin.Result$Failure</c> wrapping a <c>Throwable</c>.
    /// </summary>
    /// <remarks>
    /// The argument wraps a borrowed JNI local ref that's only valid
    /// for the duration of this callback frame. We promote it to a
    /// global ref before storing it in the TCS so the continuation
    /// can safely read it after the JNI frame returns.
    /// </remarks>
    public void ResumeWith(Java.Lang.Object boxedResult)
    {
        try
        {
            try
            {
                CompleteWithLocalHandle(boxedResult?.Handle ?? System.IntPtr.Zero, deleteLocal: false);
            }
            catch (System.Exception ex)
            {
                // NewGlobalRef / GetObject can throw; without this catch
                // the TCS would never complete and any awaiter would
                // hang forever. Surface the failure as the task result.
                Tcs.TrySetException(ex);
            }
        }
        finally
        {
            ReleaseLifetime();
        }
    }

    /// <summary>
    /// Funnel a raw JNI local ref into the TCS. Promotes the handle to
    /// a global ref, wraps it in a managed peer with
    /// <see cref="JniHandleOwnership.TransferGlobalRef"/>, and stores
    /// the peer in <see cref="Tcs"/>.
    /// </summary>
    /// <param name="handle">Local ref to the boxed result, or <c>IntPtr.Zero</c>.</param>
    /// <param name="deleteLocal">
    /// When <c>true</c>, the supplied local ref is freed after
    /// promotion. Pass <c>true</c> for the synchronous-completion path
    /// in <see cref="SuspendBridge.Invoke{T}"/> (we own the local that
    /// came back from <c>CallStaticObjectMethod</c>); pass <c>false</c>
    /// when called from the JCW marshaller, which owns its own
    /// argument locals.
    /// </param>
    internal void CompleteWithLocalHandle(System.IntPtr handle, bool deleteLocal = true)
    {
        Java.Lang.Object? owned = null;
        if (handle != System.IntPtr.Zero)
        {
            var gref = JNIEnv.NewGlobalRef(handle);
            owned = global::Java.Lang.Object.GetObject<Java.Lang.Object>(gref, JniHandleOwnership.TransferGlobalRef);
            if (deleteLocal)
                JNIEnv.DeleteLocalRef(handle);
        }
        if (!Tcs.TrySetResult(owned))
        {
            // Double-resume (Kotlin shouldn't, but be defensive):
            // the TCS already has a result, so we own this wrapper
            // and must release the global ref ourselves.
            owned?.Dispose();
        }
    }

    /// <summary>
    /// Single, idempotent cleanup entry point: frees the GCHandle
    /// self-pin and disposes the <see cref="CancellationTokenRegistration"/>.
    /// Called from every completion path (Kotlin resume in
    /// <see cref="ResumeWith"/>, the sync-completion and sync-throw
    /// paths in <see cref="SuspendBridge.Invoke{T}"/>, and
    /// <see cref="Dispose(bool)"/>).
    /// </summary>
    /// <param name="disposing">
    /// <see langword="false"/> when called from the finalizer thread
    /// (via <see cref="Java.Lang.Object.Dispose(bool)"/>): the CTR
    /// dispose is skipped to avoid blocking the finalizer thread, but
    /// the GCHandle (which is the thing that prevented finalization in
    /// the first place) is still released for symmetry. In practice
    /// every call site uses the default <see langword="true"/>.
    /// </param>
    internal void ReleaseLifetime(bool disposing = true)
    {
        if (Interlocked.Exchange(ref _lifetimeReleased, 1) != 0)
            return;
        if (disposing)
            _ctr.Dispose();
        if (_selfPin.IsAllocated)
            _selfPin.Free();
    }

    protected override void Dispose(bool disposing)
    {
        ReleaseLifetime(disposing);
        base.Dispose(disposing);
    }
}
