using System.Diagnostics;
using Android.Runtime;
using Kotlin.Coroutines;
using Kotlin.Coroutines.Intrinsics;
using Kotlin.Jvm.Functions;
using Xamarin.KotlinX.Coroutines;

namespace AndroidX.Compose;

/// <summary>
/// JCW for the suspend lambda Kotlin's <c>LaunchedEffect</c> expects:
/// <c>Function2&lt;CoroutineScope, Continuation&lt;in Unit&gt;, Any?&gt;</c>.
/// Bridges the Kotlin coroutine to a C# <c>async Task</c> body that
/// accepts a <see cref="CancellationToken"/>.
/// </summary>
/// <remarks>
/// <para>
/// The coroutine protocol contract: <c>Invoke</c> must either return
/// the final boxed result (success / failure) <em>synchronously</em>,
/// or return the
/// <see cref="IntrinsicsKt.COROUTINE_SUSPENDED"/> sentinel and resume
/// the supplied <see cref="IContinuation"/> later. This implementation
/// always returns the sentinel and resumes from a
/// <see cref="Task.ContinueWith(Action{Task})"/>
/// once the C# <see cref="Task"/> completes,
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
/// <c>Result.Failure(throwable)</c> on fault. Contract failures before
/// the body starts use the same failure-resume path.</description></item>
/// </list>
/// </remarks>
[Register("net/compose/LaunchedEffectBody")]
internal sealed class LaunchedEffectBody : Java.Lang.Object, IFunction2
{
    readonly Func<CancellationToken, Task> _body;
    readonly Func<IContinuation, JobCompletionHandler, IDisposable> _registerCancellation;

    public LaunchedEffectBody(
        Func<CancellationToken, Task> body)
        : this(body, RegisterCancellation)
    {
    }

    internal LaunchedEffectBody(
        Func<CancellationToken, Task> body,
        Func<IContinuation, JobCompletionHandler, IDisposable> registerCancellation)
    {
        ArgumentNullException.ThrowIfNull(body);
        ArgumentNullException.ThrowIfNull(registerCancellation);
        _body = body;
        _registerCancellation = registerCancellation;
    }

    public Java.Lang.Object? Invoke(Java.Lang.Object? p0, Java.Lang.Object? p1)
    {
        // p0 is CoroutineScope (unused — we have a managed Task).
        // p1 is the Continuation<Unit> we resume when the Task ends.
        // Kotlin hands us an anonymous synthetic class (e.g.
        // `IntrinsicsKt__IntrinsicsJvmKt$createCoroutineUnintercepted$$inlined$...$4`)
        // that does implement `kotlin.coroutines.Continuation` but isn't
        // in Mono.Android's peer registry, so a plain `as IContinuation`
        // cast returns null. JavaCast<T> bypasses the managed-type
        // cache and synthesizes an interface peer from the raw handle,
        // which is exactly what we need.
        if (p1 is null)
            throw new InvalidOperationException(
                "LaunchedEffect Invoke received a null Continuation in slot 1");

        IContinuation continuation;
        try
        {
            continuation = p1.JavaCast<IContinuation>();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                "LaunchedEffect Invoke could not project slot 1 ("
                + (p1.Class?.Name ?? "<unknown>")
                + ") as kotlin.coroutines.Continuation", ex);
        }

        var cts = new CancellationTokenSource();
        // Bind the Job's cancellation → our CTS so the user's Task can
        // observe ct.IsCancellationRequested and bail cooperatively.
        // The IDisposableHandle is held strongly through `state` below
        // so it doesn't get yanked by GC before completion.
        var handler = new JobCompletionHandler(cts);
        IDisposable? completionRegistration = null;
        Exception? registrationFailure = null;
        try
        {
            completionRegistration = _registerCancellation(continuation, handler)
                ?? throw new InvalidOperationException(
                    "Kotlin Job registration returned no disposal handle.");
        }
        catch (Exception ex)
        {
            registrationFailure = new InvalidOperationException(
                "LaunchedEffect could not observe its Kotlin Job; "
                + "the managed body was not started because lifecycle cancellation "
                + "could not be guaranteed.",
                ex);
        }

        Task task;
        if (registrationFailure is not null)
        {
            task = Task.FromException(registrationFailure);
        }
        else
        {
            try
            {
                task = _body(cts.Token)
                    ?? Task.FromException(
                        new InvalidOperationException(
                            "LaunchedEffect body returned a null Task."));
            }
            catch (Exception ex)
            {
                task = Task.FromException(ex);
            }
        }

        // Always go through ContinueWith so the resume runs on the
        // default TaskScheduler rather than inlining on whatever thread
        // happened to complete the Task. This keeps lifetime tracking
        // simple and matches what Kotlin's own `kotlinx-coroutines`
        // does when handed an external future.
        var ctx = new ResumeContext(
            continuation,
            cts,
            handler,
            completionRegistration,
            completionRegistration is not null);
        _ = task.ContinueWith(
            static (t, state) =>
            {
                var c = state as ResumeContext
                    ?? throw new InvalidOperationException(
                        "LaunchedEffect continuation state was invalid.");
                c.Resume(t);
            },
            ctx,
            CancellationToken.None,
            TaskContinuationOptions.None,
            TaskScheduler.Default);

        return IntrinsicsKt.COROUTINE_SUSPENDED;
    }

    static IDisposableHandle RegisterCancellation(
        IContinuation continuation,
        JobCompletionHandler handler)
    {
        var job = JobKt.GetJob(continuation.Context);
        return job.InvokeOnCompletion(
            onCancelling: true,
            invokeImmediately: true,
            handler);
    }

    // Holds everything needed to resume the continuation and release
    // cancellation resources after the managed Task completes.
    sealed class ResumeContext
    {
        readonly IContinuation _continuation;
        readonly CancellationTokenSource _cts;
        readonly JobCompletionHandler _handler;
        readonly IDisposable? _completionRegistration;
        readonly bool _handlerWasRegistered;

        public ResumeContext(
            IContinuation continuation,
            CancellationTokenSource cts,
            JobCompletionHandler handler,
            IDisposable? completionRegistration,
            bool handlerWasRegistered)
        {
            _continuation = continuation;
            _cts = cts;
            _handler = handler;
            _completionRegistration = completionRegistration;
            _handlerWasRegistered = handlerWasRegistered;
        }

        public void Resume(Task t)
        {
            Exception? cleanupFailure = null;
            Java.Lang.Throwable? throwable = null;
            try
            {
                try
                {
                    _completionRegistration?.Dispose();
                }
                catch (Exception ex)
                {
                    cleanupFailure = new InvalidOperationException(
                        "LaunchedEffect failed to detach its Kotlin Job cancellation handler.",
                        ex);
                }

                Java.Lang.Object? result;
                if (cleanupFailure is not null)
                {
                    throwable = ToThrowable(cleanupFailure);
                    result = KotlinResult.CreateFailure(throwable);
                }
                else if (t.IsFaulted)
                {
                    var inner = t.Exception?.GetBaseException()
                        ?? new Exception("LaunchedEffect Task faulted with no exception");
                    throwable = ToThrowable(inner);
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
                    throwable = new Java.Util.Concurrent.CancellationException(
                        "LaunchedEffect Task was cancelled");
                    result = KotlinResult.CreateFailure(throwable);
                }
                else
                {
                    result = Kotlin.Unit.Instance
                        ?? throw new InvalidOperationException(
                            "Kotlin.Unit.Instance was not available.");
                }

                try
                {
                    _continuation.ResumeWith(result);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(
                        "AndroidX.Compose.LaunchedEffect: continuation resume failed: " + ex);
                }
                finally
                {
                    GC.KeepAlive(result);
                    GC.KeepAlive(throwable);
                }
            }
            finally
            {
                _cts.Dispose();
                if (!_handlerWasRegistered)
                    _handler.Dispose();
                else
                    GC.KeepAlive(_handler);
                GC.KeepAlive(_continuation);
            }
        }

        internal static Java.Lang.Throwable ToThrowable(Exception ex) =>
            ex switch
            {
                Java.Lang.Throwable th => th,
                OperationCanceledException =>
                    new Java.Util.Concurrent.CancellationException(ex.Message ?? "cancelled"),
                _ => new Java.Lang.RuntimeException(ex.ToString()),
            };
    }
}
