using System.Runtime.InteropServices;
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
/// <see cref="SuspendBridge.Invoke{T}(System.Func{SuspendContinuation, Java.Lang.Object?}, System.Func{Java.Lang.Object?, T})"/>
/// instead of constructing one directly.
/// </para>
/// <para>
/// Lifetime: pinned with a strong <see cref="GCHandle"/> in the
/// constructor so the managed peer survives until Kotlin invokes
/// <see cref="ResumeWith(Java.Lang.Object)"/> — fire-and-forget callers
/// (no one holding the returned <see cref="Task"/>) would otherwise
/// race the GC. The pin is released in <see cref="ResumeWith"/>'s
/// finally, or by <see cref="Dispose(bool)"/> if the suspend call
/// throws before suspension.
/// </para>
/// </remarks>
[Register("composenet/compose/SuspendContinuation")]
internal sealed class SuspendContinuation : Java.Lang.Object, IContinuation
{
    GCHandle _selfPin;

    /// <summary>
    /// Backing TCS exposed for <see cref="SuspendBridge"/>.
    /// <see cref="TaskCreationOptions.RunContinuationsAsynchronously"/>
    /// keeps awaiters from running inline on whatever Kotlin dispatcher
    /// fires the resume (usually the Compose main thread).
    /// </summary>
    public TaskCompletionSource<Java.Lang.Object?> Tcs { get; } =
        new TaskCompletionSource<Java.Lang.Object?>(TaskCreationOptions.RunContinuationsAsynchronously);

    public SuspendContinuation()
    {
        _selfPin = GCHandle.Alloc(this);
    }

    /// <summary>
    /// Always returns <see cref="EmptyCoroutineContext.Instance"/> —
    /// v1 doesn't propagate cancellation or dispatcher overrides into
    /// the suspend call.
    /// </summary>
    public ICoroutineContext Context => EmptyCoroutineContext.Instance!;

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
        Java.Lang.Object? owned = null;
        try
        {
            if (boxedResult is { Handle: var handle } && handle != System.IntPtr.Zero)
            {
                var gref = JNIEnv.NewGlobalRef(handle);
                owned = global::Java.Lang.Object.GetObject<Java.Lang.Object>(gref, JniHandleOwnership.TransferGlobalRef);
            }
            Tcs.TrySetResult(owned);
        }
        finally
        {
            if (_selfPin.IsAllocated)
                _selfPin.Free();
        }
    }

    /// <summary>
    /// Release the self-pin when the suspend call fails before
    /// suspension (the Kotlin runtime will never invoke
    /// <see cref="ResumeWith"/>). Called from <see cref="SuspendBridge"/>
    /// on the sync-throw path.
    /// </summary>
    internal void AbandonPin()
    {
        if (_selfPin.IsAllocated)
            _selfPin.Free();
    }

    protected override void Dispose(bool disposing)
    {
        if (_selfPin.IsAllocated)
            _selfPin.Free();
        base.Dispose(disposing);
    }
}
