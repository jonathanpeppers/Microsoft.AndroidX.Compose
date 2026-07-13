using Android.Runtime;
using AndroidX.Compose;
using Xamarin.KotlinX.Coroutines;

namespace Microsoft.AndroidX.Compose.DeviceTests;

/// <summary>
/// Verifies the runtime contract at the C# task to Kotlin suspend boundary:
/// job-backed cancellation, synchronous completion, synchronous failure,
/// and defensive handling when both completion paths fire.
/// </summary>
[TestClass]
public class SuspendBridgeTests
{
    [TestMethod]
    public async Task Cancellation_CancelsTaskAndKotlinJob()
    {
        using var cts = new CancellationTokenSource();
        var continuation = new SuspendContinuation(cts.Token);
        var job = JobKt.GetJob(continuation.Context);

        Assert.IsTrue(job.IsActive);

        cts.Cancel();

        try
        {
            await continuation.Tcs.Task;
            Assert.Fail("Expected cancellation.");
        }
        catch (OperationCanceledException ex)
        {
            Assert.AreEqual(cts.Token, ex.CancellationToken);
        }

        Assert.IsTrue(job.IsCancelled);

        var unit = Kotlin.Unit.Instance
            ?? throw new InvalidOperationException("Kotlin.Unit.Instance was not available.");
        continuation.ResumeWith(unit);

        Assert.IsTrue(continuation.Tcs.Task.IsCanceled);
        GC.KeepAlive(job);
    }

    [TestMethod]
    public async Task Cancellation_StopsBoundKotlinDelay()
    {
        using var cts = new CancellationTokenSource();
        var continuation = new SuspendContinuation(cts.Token);
        var suspended = DelayKt.Delay((long)TimeSpan.FromMinutes(1).TotalMilliseconds, continuation)
            ?? throw new InvalidOperationException("Kotlin delay returned null.");

        Assert.IsTrue(SuspendBridge.IsCoroutineSuspended(suspended.Handle));

        cts.Cancel();
        await AssertCanceled(continuation.Tcs.Task, cts.Token);

        for (int i = 0; i < 100 && continuation.Handle != IntPtr.Zero; i++)
            await Task.Delay(20);

        Assert.AreEqual(
            IntPtr.Zero,
            continuation.Handle,
            "Kotlin delay did not unwind and resume its continuation after Job cancellation.");
        GC.KeepAlive(suspended);
    }

    [TestMethod]
    public async Task PreCanceledToken_DoesNotInvokeKotlin()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        bool invoked = false;

        var task = SuspendBridge.Invoke(
            _ =>
            {
                invoked = true;
                return IntPtr.Zero;
            },
            cts.Token);

        await AssertCanceled(task, cts.Token);
        Assert.IsFalse(invoked);
    }

    [TestMethod]
    public async Task SynchronousCompletion_CompletesTask()
    {
        var unit = Kotlin.Unit.Instance
            ?? throw new InvalidOperationException("Kotlin.Unit.Instance was not available.");

        var task = SuspendBridge.Invoke(
            _ => JNIEnv.ToLocalJniHandle(unit));

        await task;
        Assert.IsTrue(task.IsCompletedSuccessfully);
        GC.KeepAlive(unit);
    }

    [TestMethod]
    public async Task SynchronousThrow_FaultsTask()
    {
        var expected = new InvalidOperationException("sync failure");

        var task = SuspendBridge.Invoke(_ => throw expected);

        try
        {
            await task;
            Assert.Fail("Expected failure.");
        }
        catch (InvalidOperationException ex)
        {
            Assert.AreSame(expected, ex);
        }
    }

    [TestMethod]
    public async Task ResumeAndSynchronousReturn_CompletesOnlyOnce()
    {
        var unit = Kotlin.Unit.Instance
            ?? throw new InvalidOperationException("Kotlin.Unit.Instance was not available.");

        var task = SuspendBridge.Invoke(continuation =>
        {
            continuation.ResumeWith(unit);
            return JNIEnv.ToLocalJniHandle(unit);
        });

        await task;
        Assert.IsTrue(task.IsCompletedSuccessfully);
        GC.KeepAlive(unit);
    }

    static async Task AssertCanceled(Task task, CancellationToken expectedToken)
    {
        try
        {
            await task;
            Assert.Fail("Expected cancellation.");
        }
        catch (OperationCanceledException ex)
        {
            Assert.AreEqual(expectedToken, ex.CancellationToken);
        }
    }
}
