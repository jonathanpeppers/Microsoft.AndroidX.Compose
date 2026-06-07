using Android.Runtime;
using Kotlin.Coroutines;
using Kotlin.Coroutines.Intrinsics;
using Kotlin.Jvm.Functions;
using Xamarin.KotlinX.Coroutines;

namespace ComposeNet;

/// <summary>
/// JCW for the suspend lambda Kotlin's <c>LaunchedEffect</c> expects:
/// <c>Function2&lt;CoroutineScope, Continuation&lt;in Unit&gt;, Any?&gt;</c>.
/// Bridges the Kotlin coroutine to a C# <c>async Task</c> body that
/// accepts a <see cref="System.Threading.CancellationToken"/>.
/// </summary>
/// <remarks>
/// <para>
/// The coroutine protocol contract: <c>Invoke</c> must either return
/// the final boxed result (success / failure) <em>synchronously</em>,
/// or return the
/// <see cref="IntrinsicsKt.COROUTINE_SUSPENDED"/> sentinel and resume
/// the supplied <see cref="IContinuation"/> later. This implementation
/// always returns the sentinel and resumes from a
/// <see cref="System.Threading.Tasks.Task.ContinueWith(System.Action{System.Threading.Tasks.Task})"/>
/// once the C# <see cref="System.Threading.Tasks.Task"/> completes,
/// even when the body completes synchronously — that keeps the
/// resume protocol uniform and lets Kotlin's dispatcher own thread
/// affinity for the resume side.
/// </para>
/// <para>
/// Cancellation flow:
/// </para>
/// <list type="number">
/// <item><description>Allocate a CTS for this invocation.</description></item>
/// <item><description>Register a 3-arg <c>InvokeOnCompletion(onCancelling: true, invokeImmediately: true, handler)</c>
/// hook on the job — the 1-arg overload only fires after completion
/// and is too late to cancel a still-suspended Task.</description></item>
/// <item><description>Invoke the user's
/// <c>Func&lt;CancellationToken, Task&gt;</c> with <c>cts.Token</c>.</description></item>
/// <item><description>Once the resulting Task completes (sync or async),
/// resume the continuation with <c>Unit</c> on success /
/// <c>Result.Failure(throwable)</c> on fault. An
/// <see cref="System.Threading.Interlocked.CompareExchange{T}(ref T, T, T)"/>
/// once-gate keeps the resume from firing twice if the Job is
/// cancelled while the ContinueWith is still racing.</description></item>
/// </list>
/// </remarks>
[Register("composenet/compose/LaunchedEffectBody")]
internal sealed class LaunchedEffectBody : Java.Lang.Object, IFunction2
{
    readonly System.Func<System.Threading.CancellationToken, System.Threading.Tasks.Task> _body;

    public LaunchedEffectBody(
        System.Func<System.Threading.CancellationToken, System.Threading.Tasks.Task> body)
    {
        _body = body;
    }

    public Java.Lang.Object? Invoke(Java.Lang.Object? p0, Java.Lang.Object? p1)
    {
        // p0 is CoroutineScope (unused — we have a managed Task).
        // p1 is the Continuation<Unit> we resume when the Task ends.
        var continuation = p1 as IContinuation
            ?? throw new System.InvalidOperationException(
                "LaunchedEffect Invoke expected a Continuation arg in slot 1, got "
                + (p1?.Class?.Name ?? "<null>"));

        var cts = new System.Threading.CancellationTokenSource();
        var resumed = new ResumeOnceGate();

        // Bind the Job's cancellation → our CTS so the user's Task can
        // observe ct.IsCancellationRequested and bail cooperatively.
        // The IDisposableHandle is held strongly through `state` below
        // so it doesn't get yanked by GC before completion.
        var handler = new JobCompletionHandler(cts);
        Xamarin.KotlinX.Coroutines.IDisposableHandle? completionRegistration = null;
        try
        {
            var job = JobKt.GetJob(continuation.Context);
            completionRegistration = job.InvokeOnCompletion(
                onCancelling: true, invokeImmediately: true, handler);
        }
        catch (System.Exception ex)
        {
            // Without a Job in the context we can't wire cancellation
            // — but the user's Task can still run. Log and continue.
            System.Diagnostics.Debug.WriteLine(
                "ComposeNet.LaunchedEffect: failed to register Job completion handler: " + ex);
        }

        System.Threading.Tasks.Task task;
        try
        {
            task = _body(cts.Token) ?? System.Threading.Tasks.Task.CompletedTask;
        }
        catch (System.Exception ex)
        {
            // Synchronous fault from the body (rare — Func<T, Task> bodies
            // typically wrap their work in async). Throw a Java
            // RuntimeException; Kotlin's coroutine machinery converts it
            // into a Result.Failure for any waiting awaiters of the job
            // and the InvokeOnCompletion hook still fires. Include
            // ex.ToString() so the original stack trace + type stay
            // visible in logcat / coroutine debug output.
            completionRegistration?.Dispose();
            cts.Dispose();
            handler.Dispose();
            throw new Java.Lang.RuntimeException(
                "LaunchedEffect body threw synchronously: " + ex);
        }

        // Always go through ContinueWith so the resume runs on the
        // default TaskScheduler rather than inlining on whatever thread
        // happened to complete the Task. This keeps lifetime tracking
        // simple and matches what Kotlin's own `kotlinx-coroutines`
        // does when handed an external future.
        var ctx = new ResumeContext(
            continuation, cts, handler, completionRegistration, resumed);
        task.ContinueWith(
            static (t, state) =>
            {
                var c = (ResumeContext)state!;
                c.Resume(t);
            },
            ctx,
            System.Threading.CancellationToken.None,
            System.Threading.Tasks.TaskContinuationOptions.None,
            System.Threading.Tasks.TaskScheduler.Default);

        return IntrinsicsKt.COROUTINE_SUSPENDED;
    }

    // Holds everything we need to safely resume the continuation
    // exactly once and release the JCWs / handler registration after.
    sealed class ResumeContext
    {
        readonly IContinuation _continuation;
        readonly System.Threading.CancellationTokenSource _cts;
        readonly JobCompletionHandler _handler;
        readonly Xamarin.KotlinX.Coroutines.IDisposableHandle? _completionRegistration;
        readonly ResumeOnceGate _gate;

        public ResumeContext(
            IContinuation continuation,
            System.Threading.CancellationTokenSource cts,
            JobCompletionHandler handler,
            Xamarin.KotlinX.Coroutines.IDisposableHandle? completionRegistration,
            ResumeOnceGate gate)
        {
            _continuation = continuation;
            _cts = cts;
            _handler = handler;
            _completionRegistration = completionRegistration;
            _gate = gate;
        }

        public void Resume(System.Threading.Tasks.Task t)
        {
            if (!_gate.TryEnter())
                return;

            try
            {
                Java.Lang.Object? result;
                if (t.IsFaulted)
                {
                    var inner = t.Exception?.GetBaseException()
                        ?? new System.Exception("LaunchedEffect Task faulted with no exception");
                    var throwable = ToThrowable(inner);
                    result = KotlinResult.CreateFailure(throwable);
                }
                else if (t.IsCanceled)
                {
                    // Surface cancellation as Result.Failure(CancellationException)
                    // so Kotlin's coroutine machinery records this as a
                    // cancellation rather than treating it as success.
                    // kotlinx.coroutines.CancellationException is a
                    // typealias for java.util.concurrent.CancellationException
                    // on JVM, so the simpler Java type is fine.
                    var ce = new Java.Util.Concurrent.CancellationException(
                        "LaunchedEffect Task was cancelled");
                    result = KotlinResult.CreateFailure(ce);
                }
                else
                {
                    result = global::Kotlin.Unit.Instance!;
                }

                try
                {
                    _continuation.ResumeWith(result);
                }
                catch (System.Exception ex)
                {
                    // ResumeWith on an already-cancelled continuation
                    // may throw IllegalStateException from Kotlin's
                    // dispatched continuation impl. Log and swallow —
                    // there's no caller to surface this to.
                    System.Diagnostics.Debug.WriteLine(
                        "ComposeNet.LaunchedEffect: continuation resume failed: " + ex);
                }
            }
            finally
            {
                try { _completionRegistration?.Dispose(); } catch { }
                try { _cts.Dispose(); } catch { }
                // Don't dispose _handler — Kotlin's job machinery may
                // still hold a reference to it briefly. Let GC reclaim
                // the JCW once Kotlin drops it.
                System.GC.KeepAlive(_handler);
            }
        }

        static Java.Lang.Throwable ToThrowable(System.Exception ex) =>
            ex switch
            {
                Java.Lang.Throwable th => th,
                System.OperationCanceledException =>
                    new Java.Util.Concurrent.CancellationException(ex.Message ?? "cancelled"),
                _ => new Java.Lang.RuntimeException(ex.GetType().Name + ": " + ex.Message),
            };
    }

    sealed class ResumeOnceGate
    {
        int _state; // 0 = pending, 1 = resumed
        public bool TryEnter() =>
            System.Threading.Interlocked.CompareExchange(ref _state, 1, 0) == 0;
    }
}
