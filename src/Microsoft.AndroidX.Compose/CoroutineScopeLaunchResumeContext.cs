using System.Diagnostics;
using Kotlin.Coroutines;
using Xamarin.KotlinX.Coroutines;

namespace AndroidX.Compose;

/// <summary>
/// Owns one managed Task to Kotlin continuation handoff.
/// </summary>
internal sealed class CoroutineScopeLaunchResumeContext
{
    readonly CoroutineScopeLaunchBody _operation;
    readonly IContinuation _continuation;
    readonly CancellationTokenSource _cts;
    readonly JobCompletionHandler _cancellationHandler;
    readonly IDisposableHandle _cancellationRegistration;

    public CoroutineScopeLaunchResumeContext(
        CoroutineScopeLaunchBody operation,
        IContinuation continuation,
        CancellationTokenSource cts,
        JobCompletionHandler cancellationHandler,
        IDisposableHandle cancellationRegistration)
    {
        _operation = operation;
        _continuation = continuation;
        _cts = cts;
        _cancellationHandler = cancellationHandler;
        _cancellationRegistration = cancellationRegistration;
    }

    public void Resume(Task task)
    {
        _operation.Complete(task);
        try
        {
            var unit = Kotlin.Unit.Instance
                ?? throw new InvalidOperationException(
                    "Kotlin.Unit.Instance was not available.");
            _continuation.ResumeWith(unit);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(
                "AndroidX.Compose.CoroutineScope: continuation resume failed: "
                + ex);
        }
        finally
        {
            try
            {
                _cancellationRegistration.Dispose();
            }
            catch (Java.Lang.Throwable ex)
            {
                Debug.WriteLine(
                    "AndroidX.Compose.CoroutineScope: Job registration disposal failed: "
                    + ex);
            }
            finally
            {
                _cts.Dispose();
            }
        }
    }
}
