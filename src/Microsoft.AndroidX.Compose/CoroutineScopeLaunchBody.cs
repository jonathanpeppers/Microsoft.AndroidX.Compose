using Android.Runtime;
using Kotlin.Coroutines;
using Kotlin.Coroutines.Intrinsics;
using Kotlin.Jvm.Functions;
using Xamarin.KotlinX.Coroutines;

namespace AndroidX.Compose;

/// <summary>
/// Kotlin suspend-lambda adapter used by <see cref="CoroutineScope.Launch"/>.
/// </summary>
[Register("net/compose/CoroutineScopeLaunchBody")]
internal sealed class CoroutineScopeLaunchBody : Java.Lang.Object, IFunction2
{
    readonly Func<CancellationToken, Task> _body;
    readonly TaskCompletionSource<object?> _completion =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    public CoroutineScopeLaunchBody(Func<CancellationToken, Task> body)
    {
        _body = body;
    }

    public Task Task => _completion.Task;

    public Java.Lang.Object? Invoke(Java.Lang.Object? p0, Java.Lang.Object? p1)
    {
        if (p1 is null)
            throw new InvalidOperationException(
                "CoroutineScope.Launch received a null Continuation.");

        IContinuation continuation;
        try
        {
            continuation = p1.JavaCast<IContinuation>();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                "CoroutineScope.Launch could not project the Kotlin continuation.",
                ex);
        }

        var cts = new CancellationTokenSource();
        var cancellationHandler = new JobCompletionHandler(cts);
        IDisposableHandle? cancellationRegistration = null;
        try
        {
            var job = JobKt.GetJob(continuation.Context);
            cancellationRegistration = job.InvokeOnCompletion(
                onCancelling: true,
                invokeImmediately: true,
                cancellationHandler);
        }
        catch (Exception ex)
        {
            cts.Dispose();
            cancellationHandler.Dispose();
            _completion.TrySetException(ex);
            throw new Java.Lang.RuntimeException(
                "CoroutineScope.Launch could not observe its Kotlin child Job: "
                + ex);
        }

        Task bodyTask;
        try
        {
            bodyTask = _body(cts.Token) ?? Task.CompletedTask;
        }
        catch (Exception ex)
        {
            bodyTask = System.Threading.Tasks.Task.FromException(ex);
        }

        var resume = new CoroutineScopeLaunchResumeContext(
            this,
            continuation,
            cts,
            cancellationHandler,
            cancellationRegistration);
        _ = bodyTask.ContinueWith(
            static (task, state) =>
            {
                var context = state as CoroutineScopeLaunchResumeContext
                    ?? throw new InvalidOperationException(
                        "CoroutineScope.Launch continuation state was invalid.");
                context.Resume(task);
            },
            resume,
            CancellationToken.None,
            TaskContinuationOptions.None,
            TaskScheduler.Default);

        return IntrinsicsKt.COROUTINE_SUSPENDED;
    }

    internal void AttachJob(IJob job)
    {
        ArgumentNullException.ThrowIfNull(job);
        var handler = new CoroutineScopeJobCompletionHandler(this);
        bool attached = false;
        try
        {
            _ = job.InvokeOnCompletion(
                onCancelling: true,
                invokeImmediately: true,
                handler);
            attached = true;
        }
        finally
        {
            if (!attached)
                handler.Release();
        }
    }

    internal void CancelFromScope()
    {
        _completion.TrySetCanceled();
    }

    internal void Complete(Task task)
    {
        if (task.IsCanceled)
        {
            _completion.TrySetCanceled();
        }
        else if (task.IsFaulted)
        {
            var exception = task.Exception;
            if (exception is null)
            {
                _completion.TrySetException(
                    new InvalidOperationException(
                        "CoroutineScope.Launch body faulted without an exception."));
            }
            else
            {
                _completion.TrySetException(exception.InnerExceptions);
            }
        }
        else
        {
            _completion.TrySetResult(null);
        }
    }
}
