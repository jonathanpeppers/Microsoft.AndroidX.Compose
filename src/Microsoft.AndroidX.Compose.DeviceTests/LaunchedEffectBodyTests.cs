using AndroidX.Compose;
using Xamarin.KotlinX.Coroutines;

namespace Microsoft.AndroidX.Compose.DeviceTests;

/// <summary>Verifies managed Task and Kotlin Job contracts for launched effects.</summary>
[TestClass]
[DoNotParallelize]
public class LaunchedEffectBodyTests
{
    [TestMethod]
    public void Constructor_RejectsNullDelegates()
    {
#pragma warning disable CS8625
        Assert.ThrowsExactly<ArgumentNullException>(
            () => new LaunchedEffectBody(null));
        Assert.ThrowsExactly<ArgumentNullException>(
            () => new LaunchedEffectBody(
                static _ => Task.CompletedTask,
                null));
#pragma warning restore CS8625
    }

    [TestMethod]
    public async Task NullTask_ResumesWithClearFailure()
    {
        using var owner = new CancellationTokenSource();
        using var continuation = new SuspendContinuation(owner.Token);
        using var body = new LaunchedEffectBody(static _ => ReturnNullTask());

        _ = body.Invoke(null, continuation);

        var exception = await ReadCompletion(continuation);
        Assert.IsNotNull(exception);
        StringAssert.Contains(
            Describe(exception),
            "LaunchedEffect body returned a null Task.");
    }

    [TestMethod]
    public async Task RegistrationFailure_DoesNotStartBodyAndResumesFailure()
    {
        bool invoked = false;
        using var owner = new CancellationTokenSource();
        using var continuation = new SuspendContinuation(owner.Token);
        using var body = new LaunchedEffectBody(
            _ =>
            {
                invoked = true;
                return Task.CompletedTask;
            },
            static (_, _) => throw new InvalidOperationException(
                "registration failure"));

        _ = body.Invoke(null, continuation);

        var exception = await ReadCompletion(continuation);
        Assert.IsFalse(invoked);
        Assert.IsNotNull(exception);
        StringAssert.Contains(
            Describe(exception),
            "LaunchedEffect could not observe its Kotlin Job");
        StringAssert.Contains(Describe(exception), "registration failure");
    }

    [TestMethod]
    public async Task SynchronousException_ResumesFailure()
    {
        using var owner = new CancellationTokenSource();
        using var continuation = new SuspendContinuation(owner.Token);
        using var body = new LaunchedEffectBody(
            static _ => throw new InvalidOperationException("synchronous failure"));

        _ = body.Invoke(null, continuation);

        var exception = await ReadCompletion(continuation);
        Assert.IsNotNull(exception);
        StringAssert.Contains(Describe(exception), "synchronous failure");
    }

    [TestMethod]
    public async Task JobCancellation_CancelsManagedBody()
    {
        using var owner = new CancellationTokenSource();
        using var continuation = new SuspendContinuation(owner.Token);
        var job = JobKt.GetJob(continuation.Context);
        var tokenReady = new TaskCompletionSource<CancellationToken>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        using var body = new LaunchedEffectBody(async token =>
        {
            tokenReady.TrySetResult(token);
            await Task.Delay(Timeout.InfiniteTimeSpan, token);
        });

        _ = body.Invoke(null, continuation);
        var token = await tokenReady.Task.WaitAsync(TimeSpan.FromSeconds(2));
        job.Cancel(null);

        var exception = await ReadCompletion(continuation);
        Assert.IsTrue(token.IsCancellationRequested);
        Assert.IsInstanceOfType<Java.Util.Concurrent.CancellationException>(exception);
    }

    [TestMethod]
    public async Task SuccessfulCompletion_DetachesJobAndDisposesCancellationSource()
    {
        using var owner = new CancellationTokenSource();
        using var continuation = new SuspendContinuation(owner.Token);
        using var registration = new CancellationTokenSource();
        CancellationToken bodyToken = default;
        var registrationToken = registration.Token;
        using var body = new LaunchedEffectBody(
            value =>
            {
                bodyToken = value;
                return Task.CompletedTask;
            },
            (_, _) => registration);

        _ = body.Invoke(null, continuation);

        var exception = await ReadCompletion(continuation);
        Assert.IsNull(exception);
        Assert.IsFalse(bodyToken.IsCancellationRequested);
        Assert.ThrowsExactly<ObjectDisposedException>(
            () => _ = bodyToken.WaitHandle);
        Assert.ThrowsExactly<ObjectDisposedException>(
            () => _ = registrationToken.WaitHandle);
    }

    static async Task<Exception?> ReadCompletion(SuspendContinuation continuation)
    {
        using var result = await continuation.Tcs.Task.WaitAsync(TimeSpan.FromSeconds(2));
        return result is not null && KotlinResult.IsFailure(result.Handle)
            ? KotlinResult.ExtractException(result)
            : null;
    }

    static string Describe(Exception exception) =>
        exception is Java.Lang.Throwable throwable
            ? throwable.LocalizedMessage ?? throwable.ToString()
            : exception.ToString();

#pragma warning disable CS8603
    static Task ReturnNullTask() => null;
#pragma warning restore CS8603
}
