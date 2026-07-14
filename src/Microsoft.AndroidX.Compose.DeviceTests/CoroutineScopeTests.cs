using AndroidX.Compose;

namespace Microsoft.AndroidX.Compose.DeviceTests;

/// <summary>
/// Verifies the composition-owned coroutine scope and its managed Task bridge.
/// </summary>
[TestClass]
[DoNotParallelize]
public class CoroutineScopeTests
{
    [TestMethod]
    public async Task Launch_ProjectsCompletionAndFaultWithoutCancellingScope()
    {
        var activity = await StartActivity();
        try
        {
            var scope = await WaitForScope();

            await scope.Launch(static _ => Task.CompletedTask);

            var expected = new InvalidOperationException("managed launch failure");
            var failed = scope.Launch(_ => Task.FromException(expected));
            try
            {
                await failed;
                Assert.Fail("Expected the managed launch to fault.");
            }
            catch (InvalidOperationException ex)
            {
                Assert.AreSame(expected, ex);
            }

            await scope.Launch(static _ => Task.CompletedTask);
        }
        finally
        {
            activity.RunOnUiThread(activity.Finish);
        }
    }

    [TestMethod]
    public async Task LeavingComposition_CancelsRunningKotlinSuspend()
    {
        var activity = await StartActivity();
        var state = LazyListCancellationTestActivity.State;
        try
        {
            var scope = await WaitForScope();
            await WaitFor(
                () => state.CanScrollForward,
                static value => value,
                TimeSpan.FromSeconds(5),
                "LazyColumn did not finish its first layout.");

            var launch = scope.Launch(async ct =>
            {
                while (true)
                {
                    await state.AnimateScrollToItemAsync(
                        9_999,
                        cancellationToken: ct);
                    await state.AnimateScrollToItemAsync(
                        0,
                        cancellationToken: ct);
                }
            });
            await WaitFor(
                () => state.IsScrollInProgress,
                static value => value,
                TimeSpan.FromSeconds(2),
                "Scoped LazyColumn animation did not start.");

            activity.RunOnUiThread(activity.Finish);

            await AssertCanceled(launch);
            await WaitFor(
                () => state.IsScrollInProgress,
                static value => !value,
                TimeSpan.FromSeconds(2),
                "Scoped LazyColumn animation did not stop after composition disposal.");

            int stoppedIndex = state.FirstVisibleItemIndex;
            int stoppedOffset = state.FirstVisibleItemScrollOffset;

            await Task.Delay(300);

            Assert.AreEqual(stoppedIndex, state.FirstVisibleItemIndex);
            Assert.AreEqual(stoppedOffset, state.FirstVisibleItemScrollOffset);
        }
        finally
        {
            if (!activity.IsFinishing)
                activity.RunOnUiThread(activity.Finish);
        }
    }

    [TestMethod]
    public async Task LeavingComposition_CancelsNonCooperativeBodyUnderGcPressure()
    {
        var activity = await StartActivity();
        var releaseBody = new TaskCompletionSource<object?>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        try
        {
            var scope = await WaitForScope();
            var bodyStarted = new TaskCompletionSource<CancellationToken>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            var launch = scope.Launch(ct =>
            {
                bodyStarted.TrySetResult(ct);
                return releaseBody.Task;
            });
            var token = await bodyStarted.Task.WaitAsync(TimeSpan.FromSeconds(2));

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            activity.RunOnUiThread(activity.Finish);

            await AssertCanceled(launch);
            Assert.IsTrue(token.IsCancellationRequested);
        }
        finally
        {
            releaseBody.TrySetResult(null);
            if (!activity.IsFinishing)
                activity.RunOnUiThread(activity.Finish);
        }
    }

    [TestMethod]
    public async Task Launch_AfterCompositionDisposalIsCanceledWithoutInvokingBody()
    {
        var activity = await StartActivity();
        var scope = await WaitForScope();
        activity.RunOnUiThread(activity.Finish);
        await WaitFor(
            static () => LazyListCancellationTestActivity.Current,
            static value => value is null,
            TimeSpan.FromSeconds(5),
            "Coroutine-scope test activity did not finish.");

        bool invoked = false;
        var launch = scope.Launch(_ =>
        {
            invoked = true;
            return Task.CompletedTask;
        });

        await AssertCanceled(launch);
        Assert.IsFalse(invoked);
    }

    static async Task<LazyListCancellationTestActivity> StartActivity()
    {
        var context = global::Android.App.Application.Context
            ?? throw new InvalidOperationException(
                "Application.Context not set for coroutine-scope tests.");
        LazyListCancellationTestActivity.Reset();
        using var intent = new global::Android.Content.Intent(
            context,
            typeof(LazyListCancellationTestActivity));
        intent.AddFlags(global::Android.Content.ActivityFlags.NewTask);
        context.StartActivity(intent);

        return await WaitFor(
            static () => LazyListCancellationTestActivity.Current,
            static value => value is not null,
            TimeSpan.FromSeconds(5),
            "Coroutine-scope test activity did not start.")
            ?? throw new InvalidOperationException(
                "Coroutine-scope test activity was not available.");
    }

    static async Task<CoroutineScope> WaitForScope()
    {
        var scope = await WaitFor(
            static () => LazyListCancellationTestActivity.Scope,
            static value => value is not null,
            TimeSpan.FromSeconds(5),
            "RememberCoroutineScope did not produce a scope.");
        return scope
            ?? throw new InvalidOperationException(
                "RememberCoroutineScope returned a null scope.");
    }

    static async Task AssertCanceled(Task task)
    {
        try
        {
            await task.WaitAsync(TimeSpan.FromSeconds(2));
            Assert.Fail("Expected cancellation.");
        }
        catch (OperationCanceledException)
        {
        }
    }

    static async Task<T> WaitFor<T>(
        Func<T> read,
        Func<T, bool> predicate,
        TimeSpan timeout,
        string message)
    {
        var deadline = DateTime.UtcNow + timeout;
        T value;
        do
        {
            value = read();
            if (predicate(value))
                return value;
            await Task.Delay(20);
        }
        while (DateTime.UtcNow < deadline);

        Assert.Fail(message);
        return value;
    }
}
