using System;
using System.Threading;
using System.Threading.Tasks;
using Android.Runtime;
using Kotlin.Coroutines.Intrinsics;

namespace ComposeNet;

/// <summary>
/// Shared plumbing for calling Kotlin <c>suspend</c> functions from
/// C# <c>async</c>/<c>await</c>. Each call allocates one
/// <see cref="SuspendContinuation"/>, hands it to the Kotlin function,
/// and either:
/// <list type="bullet">
/// <item><description>completes synchronously when Kotlin returns
/// anything other than the <c>COROUTINE_SUSPENDED</c> sentinel, or</description></item>
/// <item><description>defers to the continuation when Kotlin returns
/// the sentinel (i.e. it will resume the continuation later from its
/// own dispatcher).</description></item>
/// </list>
/// In both cases the boxed <c>kotlin.Result&lt;T&gt;</c> is funneled
/// through <see cref="SuspendContinuation.ResumeWith"/> so failure
/// detection and unboxing live in one place.
/// </summary>
/// <remarks>
/// <para>
/// The returned <see cref="Task"/> may complete on the Kotlin resume
/// thread (usually the Compose main thread for UI-bound suspend
/// functions like <c>ScrollState.scrollTo</c>). Awaiters use
/// <see cref="TaskCreationOptions.RunContinuationsAsynchronously"/> on
/// the underlying TCS, so they won't be inlined onto that thread —
/// they'll resume on whatever <see cref="SynchronizationContext"/> the
/// <c>await</c> captured.
/// </para>
/// <para>
/// v1 does not honour <see cref="CancellationToken"/>. Cancellation
/// support needs a <c>Job</c>-bearing <see cref="Kotlin.Coroutines.ICoroutineContext"/>
/// (~50 LOC); deferred to a follow-up issue.
/// </para>
/// </remarks>
internal static class SuspendBridge
{
    // Cached global ref to the COROUTINE_SUSPENDED singleton. Kotlin
    // identifies "I'll resume you later" by *reference equality* with
    // this exact object, so we cache one global ref and compare via
    // JNIEnv.IsSameObject (raw pointer compares aren't safe across
    // JNI ref types).
    static IntPtr s_suspendedHandle;

    // Cached global ref + field id for kotlin/Result$Failure (the
    // wrapper for failed suspend results). Kotlin.ResultKt's static
    // methods are NOT bound in Xamarin.Kotlin.StdLib 2.3.21, so we
    // detect failures via raw JNI instead.
    static IntPtr s_resultFailureClass;
    static IntPtr s_resultFailureExceptionField;

    /// <summary>
    /// Call a Kotlin <c>suspend</c> function and project its boxed
    /// result through <paramref name="unbox"/> into a typed
    /// <see cref="Task{TResult}"/>.
    /// </summary>
    /// <param name="call">
    /// Invokes the Kotlin <c>suspend</c> function with the supplied
    /// <see cref="SuspendContinuation"/> as the trailing argument and
    /// returns either the boxed result (synchronous completion) or
    /// the <c>COROUTINE_SUSPENDED</c> sentinel.
    /// </param>
    /// <param name="unbox">
    /// Converts the success-value box (e.g. <c>java.lang.Float</c>) to
    /// the target C# type. May be passed <c>null</c> for suspend
    /// functions that return <c>Unit</c>.
    /// </param>
    public static Task<T> Invoke<T>(
        Func<SuspendContinuation, IntPtr> call,
        Func<Java.Lang.Object?, T> unbox)
    {
        if (call is null) throw new ArgumentNullException(nameof(call));
        if (unbox is null) throw new ArgumentNullException(nameof(unbox));

        var cont = new SuspendContinuation();
        IntPtr syncHandle;
        try
        {
            syncHandle = call(cont);
        }
        catch (Exception ex)
        {
            // Exceptions thrown before the suspend function suspends
            // (e.g. JNI lookup failures, Kotlin throwing inline before
            // creating a Result.Failure) shouldn't surface as
            // synchronous throws from an async-style API.
            cont.AbandonPin();
            cont.Dispose();
            return Task.FromException<T>(ex);
        }

        // We work in raw JNI handles here rather than wrapping syncHandle
        // in a Java.Lang.Object up-front. The COROUTINE_SUSPENDED
        // sentinel is a Kotlin singleton, and Mono's peer cache resolves
        // every local ref to it back to a single cached Java.Lang.Object
        // whose Handle is a *global* ref. If we wrap with TransferLocalRef
        // and later dispose, the dispose path calls DeleteLocalRef on a
        // global, which CheckJNI aborts. Raw-handle work-flow side-steps
        // the issue entirely.
        if (syncHandle != IntPtr.Zero)
        {
            if (IsCoroutineSuspended(syncHandle))
            {
                // Kotlin will resume cont later. Free the local ref to
                // the sentinel; the cached global lives in
                // s_suspendedHandle for future comparisons.
                JNIEnv.DeleteLocalRef(syncHandle);
            }
            else
            {
                // Synchronous completion — funnel through the same
                // promote-to-global + TCS path as the Kotlin-async
                // callback (SuspendContinuation.ResumeWith).
                cont.CompleteWithLocalHandle(syncHandle);
            }
        }
        else
        {
            // Suspend returned a Java null. Treat as a successful null
            // result (rare in practice; most suspends either suspend or
            // return a boxed Unit / boxed primitive).
            cont.CompleteWithLocalHandle(IntPtr.Zero);
        }

        return cont.Tcs.Task.ContinueWith(static (t, state) =>
        {
            var (unboxFn, cont) = ((Func<Java.Lang.Object?, T>, SuspendContinuation))state!;
            // Belt-and-suspenders: keep the JCW alive until completion.
            GC.KeepAlive(cont);

            // GetAwaiter().GetResult() rather than t.Result so a faulted
            // TCS surfaces the original exception instead of an
            // AggregateException wrapper.
            var boxed = t.GetAwaiter().GetResult();
            try
            {
                if (boxed is not null && IsResultFailure(boxed.Handle))
                    throw ExtractFailureException(boxed);
                return unboxFn(boxed);
            }
            finally
            {
                // Release the global ref the JNI callback promoted in
                // SuspendContinuation.ResumeWith.
                boxed?.Dispose();
            }
        },
        (unbox, cont),
        CancellationToken.None,
        TaskContinuationOptions.ExecuteSynchronously,
        TaskScheduler.Default);
    }

    /// <summary>
    /// Non-generic overload for suspend functions that return Kotlin
    /// <c>Unit</c> (e.g. <c>ScrollState.animateScrollTo</c>).
    /// </summary>
    public static Task Invoke(Func<SuspendContinuation, IntPtr> call) =>
        Invoke<object?>(call, static _ => null);

    static bool IsCoroutineSuspended(IntPtr handle)
    {
        if (handle == IntPtr.Zero) return false;
        EnsureSuspendedHandle();
        return JNIEnv.IsSameObject(handle, s_suspendedHandle);
    }

    static void EnsureSuspendedHandle()
    {
        if (s_suspendedHandle != IntPtr.Zero) return;
        var inst = IntrinsicsKt.COROUTINE_SUSPENDED;
        var gref = JNIEnv.NewGlobalRef(inst.Handle);
        // CAS so concurrent first-callers don't leak a global ref.
        if (Interlocked.CompareExchange(ref s_suspendedHandle, gref, IntPtr.Zero) != IntPtr.Zero)
            JNIEnv.DeleteGlobalRef(gref);
        GC.KeepAlive(inst);
    }

    static bool IsResultFailure(IntPtr handle)
    {
        if (handle == IntPtr.Zero) return false;
        EnsureResultFailureClass();
        return JNIEnv.IsInstanceOf(handle, s_resultFailureClass);
    }

    static void EnsureResultFailureClass()
    {
        if (s_resultFailureClass != IntPtr.Zero) return;
        // JNIEnv.FindClass in Mono.Android returns a stable, globally
        // registered class ref — no NewGlobalRef/DeleteLocalRef dance.
        // Resolve the field id first, then publish the class ref via
        // CAS with release semantics so readers that observe a non-zero
        // s_resultFailureClass are guaranteed to see a fully
        // initialised s_resultFailureExceptionField.
        var cls = JNIEnv.FindClass("kotlin/Result$Failure");
        var fid = JNIEnv.GetFieldID(cls, "exception", "Ljava/lang/Throwable;");
        s_resultFailureExceptionField = fid;
        Interlocked.CompareExchange(ref s_resultFailureClass, cls, IntPtr.Zero);
    }

    static Exception ExtractFailureException(Java.Lang.Object failure)
    {
        // s_resultFailureExceptionField was set by EnsureResultFailureClass
        // earlier (IsResultFailure path is the only caller).
        IntPtr exHandle = JNIEnv.GetObjectField(failure.Handle, s_resultFailureExceptionField);
        try
        {
            // Java.Lang.Throwable : System.Exception, so callers can
            // `catch (Exception)` or even pattern-match on the Java type.
            var th = global::Java.Lang.Object.GetObject<Java.Lang.Throwable>(
                exHandle, JniHandleOwnership.TransferLocalRef);
            if (th is not null)
                return th;
            return new InvalidOperationException(
                "Kotlin suspend call failed with a null Throwable in Result.Failure");
        }
        finally
        {
            GC.KeepAlive(failure);
        }
    }
}
