using AndroidX.Compose;

namespace Microsoft.AndroidX.Compose.DeviceTests;

/// <summary>Verifies managed state mutation and synchronization behavior.</summary>
[TestClass]
[DoNotParallelize]
public class MutableManagedStateTests
{
    [TestMethod]
    public void Value_ExposesInitialAndAssignedValues()
    {
        var state = new MutableManagedState<string>("initial");

        Assert.AreEqual("initial", state.Value);

        state.Value = "updated";

        Assert.AreEqual("updated", state.Value);
    }

    [TestMethod]
    public void TrySet_ReportsWhetherValueChanged()
    {
        var state = new MutableManagedState<int>(1);

        Assert.IsFalse(state.TrySet(1));
        Assert.IsTrue(state.TrySet(2));
        Assert.AreEqual(2, state.Value);
    }

    [TestMethod]
    public void Update_ReturnsAndStoresTransformedValue()
    {
        var state = new MutableManagedState<int>(2);

        var result = state.Update(static value => value * 3);

        Assert.AreEqual(6, result);
        Assert.AreEqual(6, state.Value);
    }

    [TestMethod]
    public void ConcurrentUpdates_DoNotLoseWrites()
    {
        const int updateCount = 100;
        var state = new MutableManagedState<int>(0);

        Parallel.For(0, updateCount, _ => state.Update(static value => value + 1));

        Assert.AreEqual(updateCount, state.Value);
    }

    [TestMethod]
    public async Task BackgroundUpdate_RecomposesCompositionReader()
    {
        var activity = await StartActivity();
        try
        {
            await WaitFor(
                static () => MutableManagedStateTestActivity.RenderCount,
                static count => count > 0,
                "Managed-state composition did not render.");
            int initialRenderCount = MutableManagedStateTestActivity.RenderCount;

            bool changed = await Task.Run(
                static () => MutableManagedStateTestActivity.State.TrySet(1));

            Assert.IsTrue(changed);
            await WaitFor(
                static () => (
                    MutableManagedStateTestActivity.ObservedValue,
                    MutableManagedStateTestActivity.RenderCount),
                result => result.ObservedValue == 1 &&
                    result.RenderCount > initialRenderCount,
                "Managed-state update did not recompose its reader.");
        }
        finally
        {
            activity.RunOnUiThread(activity.Finish);
        }
    }

    static async Task<MutableManagedStateTestActivity> StartActivity()
    {
        var context = global::Android.App.Application.Context
            ?? throw new InvalidOperationException(
                "Application.Context not set for managed-state tests.");
        MutableManagedStateTestActivity.Reset();
        using var intent = new global::Android.Content.Intent(
            context,
            typeof(MutableManagedStateTestActivity));
        intent.AddFlags(global::Android.Content.ActivityFlags.NewTask);
        context.StartActivity(intent);

        return await WaitFor(
            static () => MutableManagedStateTestActivity.Current,
            static activity => activity is not null,
            "Managed-state test activity did not start.")
            ?? throw new InvalidOperationException(
                "Managed-state test activity was not available.");
    }

    static async Task<T> WaitFor<T>(
        Func<T> read,
        Func<T, bool> predicate,
        string message)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(5);
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
