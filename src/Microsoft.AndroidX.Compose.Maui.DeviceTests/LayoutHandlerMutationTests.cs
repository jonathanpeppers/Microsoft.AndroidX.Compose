namespace Microsoft.AndroidX.Compose.Maui.DeviceTests;

/// <summary>Exercises MAUI stack child commands against an attached production composition.</summary>
[TestClass]
[DoNotParallelize]
public class LayoutHandlerMutationTests
{
    static TestInstrumentation Runner => TestInstrumentation.Current
        ?? throw new InvalidOperationException("Layout mutation tests require native instrumentation.");

    /// <summary>Checks add, insert, remove, replace, and clear in both stack directions.</summary>
    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task ChildMutations_RecomposeInOrderAndPreserveSurvivorState(bool vertical)
    {
        var activity = await Start(vertical);
        try
        {
            object aState = new();
            object bState = new();
            Runner.RunOnMainSync(() =>
            {
                AssertOrder(activity.Order, "A", "B");
                aState = activity.Observed["A"];
                bState = activity.Observed["B"];
            });

            await AssertUnrelatedStateDoesNotRecompose(activity);

            object cState = new();
            await Mutate(activity, "Add", () =>
            {
                AssertOrder(activity.Order, "A", "B", "C");
                Assert.AreSame(aState, activity.Observed["A"]);
                Assert.AreSame(bState, activity.Observed["B"]);
                cState = activity.Observed["C"];
            });

            object xState = new();
            await Mutate(activity, "Insert", () =>
            {
                AssertOrder(activity.Order, "A", "X", "B", "C");
                Assert.AreSame(aState, activity.Observed["A"]);
                Assert.AreSame(bState, activity.Observed["B"]);
                Assert.AreSame(cState, activity.Observed["C"]);
                xState = activity.Observed["X"];
            });

            await Mutate(activity, "Remove", () =>
            {
                AssertOrder(activity.Order, "A", "X", "C");
                Assert.AreSame(aState, activity.Observed["A"]);
                Assert.AreSame(xState, activity.Observed["X"]);
                Assert.AreSame(cState, activity.Observed["C"]);
                Assert.AreEqual(1, activity.Disposals["B"]);
            });

            await Mutate(activity, "Update", () =>
            {
                AssertOrder(activity.Order, "A", "R", "C");
                Assert.AreSame(aState, activity.Observed["A"]);
                Assert.AreSame(cState, activity.Observed["C"]);
                Assert.AreNotSame(xState, activity.Observed["R"]);
                Assert.AreEqual(1, activity.Disposals["X"]);
            });

            await Mutate(activity, "Clear", () =>
            {
                Assert.AreEqual(0, activity.Order.Count);
                Assert.AreEqual(1, activity.Disposals["A"]);
                Assert.AreEqual(1, activity.Disposals["C"]);
                Assert.AreEqual(1, activity.Disposals["R"]);
            });
        }
        finally
        {
            Runner.RunOnMainSync(activity.Finish);
            await activity.Destroyed.Task.WaitAsync(TimeSpan.FromSeconds(15));
            Runner.WaitForIdleSync();
        }
    }

    static async Task<LayoutHandlerMutationTestActivity> Start(bool vertical)
    {
        LayoutHandlerMutationTestActivity.Reset(vertical);
        var context = global::Android.App.Application.Context
            ?? throw new InvalidOperationException("Android application context is unavailable.");
        using var intent = new global::Android.Content.Intent(
            context,
            typeof(LayoutHandlerMutationTestActivity));
        intent.AddFlags(global::Android.Content.ActivityFlags.NewTask);
        var started = Runner.StartActivitySync(intent);
        if (started is not LayoutHandlerMutationTestActivity activity)
        {
            if (started is not null)
                Runner.RunOnMainSync(started.Finish);
            throw new InvalidOperationException("Layout mutation activity did not start.");
        }

        try
        {
            await activity.InitialApplied.WaitAsync(TimeSpan.FromSeconds(15));
            await Settle(activity);
            return activity;
        }
        catch
        {
            Runner.RunOnMainSync(activity.Finish);
            await activity.Destroyed.Task.WaitAsync(TimeSpan.FromSeconds(15));
            Runner.WaitForIdleSync();
            throw;
        }
    }

    static async Task AssertUnrelatedStateDoesNotRecompose(LayoutHandlerMutationTestActivity activity)
    {
        await Settle(activity);
        int baseline = activity.AppliedPasses;
        Runner.RunOnMainSync(activity.WriteUnrelatedState);
        await Settle(activity);
        Assert.AreEqual(
            baseline,
            activity.AppliedPasses,
            "A state that the layout never observed triggered an applied composition pass.");
    }

    static async Task Mutate(
        LayoutHandlerMutationTestActivity activity,
        string command,
        Action assertions)
    {
        await Settle(activity);
        int baseline = activity.AppliedPasses;
        int previousVersion = 0;
        int currentVersion = 0;
        int capturedPass = 0;
        Task<int>? applied = null;
        Runner.RunOnMainSync(() =>
        {
            (previousVersion, currentVersion, capturedPass, applied) = activity.Mutate(command);
        });

        Assert.AreEqual(
            previousVersion + 1,
            currentVersion,
            $"Layout mutation '{command}' did not dispatch through the handler command mapper.");
        Assert.AreEqual(
            baseline + 1,
            capturedPass,
            $"Layout mutation '{command}' did not capture the next owned composition pass.");

        int actualPass = await (applied
            ?? throw new InvalidOperationException($"Layout mutation '{command}' did not create an applied-pass signal."))
            .WaitAsync(TimeSpan.FromSeconds(15));
        Assert.AreEqual(capturedPass, actualPass);

        await Settle(activity);
        Runner.RunOnMainSync(() =>
        {
            Assert.AreEqual(
                capturedPass,
                activity.AppliedPasses,
                $"Layout mutation '{command}' produced an unexpected extra composition pass.");
            assertions();
        });
    }

    static async Task Settle(LayoutHandlerMutationTestActivity activity)
    {
        Runner.WaitForIdleSync();
        var frame = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var secondFrame = new Java.Lang.Runnable(() => frame.TrySetResult());
        using var firstFrame = new Java.Lang.Runnable(() =>
            Require(activity.Window?.DecorView).PostOnAnimation(secondFrame));
        Runner.RunOnMainSync(() => Require(activity.Window?.DecorView).PostOnAnimation(firstFrame));
        await frame.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Runner.WaitForIdleSync();
        Assert.IsFalse(
            activity.IsFinishing || activity.IsDestroyed,
            "The layout mutation activity left its lifecycle unexpectedly.");
    }

    static T Require<T>(T? value) where T : class =>
        value ?? throw new InvalidOperationException($"{typeof(T).Name} is unavailable.");

    static void AssertOrder(IList<string> actual, params string[] expected) =>
        CollectionAssert.AreEqual(expected, actual.ToArray());
}
