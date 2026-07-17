using AndroidX.Compose;

namespace Microsoft.AndroidX.Compose.DeviceTests;

/// <summary>Verifies progress adapters and real Material 3 indicator rendering.</summary>
[TestClass]
[DoNotParallelize]
public class ProgressIndicatorTests
{
    [TestMethod]
    public void FloatFunction0_BoxesLatestValueAndPreservesPeerIdentity()
    {
        float value = 0.25f;
        var function = new FloatFunction0(() => value);
        var handle = function.Handle;

        Assert.AreEqual(0.25f, ((Java.Lang.Float)function.Invoke()).FloatValue());
        value = 0.75f;
        Assert.AreEqual(0.75f, ((Java.Lang.Float)function.Invoke()).FloatValue());
        Assert.AreEqual(handle, function.Handle);
    }

    [TestMethod]
    public async Task DeterminateAndIndeterminateIndicators_RenderAndUpdateLiveProgress()
    {
        ProgressIndicatorTestActivity.Reset();
        var activity = await StartActivity();
        try
        {
            await WaitFor(
                static () => ProgressIndicatorTestActivity.CompletedRenderPasses,
                static value => value > 0,
                TimeSpan.FromSeconds(10),
                "Progress indicators did not complete their initial render.");

            int completedPasses = ProgressIndicatorTestActivity.CompletedRenderPasses;
            activity.RunOnUiThread(() =>
            {
                var progress = ProgressIndicatorTestActivity.Progress
                    ?? throw new InvalidOperationException(
                        "Progress state not set on ProgressIndicatorTestActivity.");
                progress.Value = 0.75f;
            });

            await WaitFor(
                static () => ProgressIndicatorTestActivity.CompletedRenderPasses,
                value => value > completedPasses,
                TimeSpan.FromSeconds(10),
                "Progress indicators did not re-render after a live update.");

            Assert.AreEqual(0.75f, ProgressIndicatorTestActivity.Linear?.Progress);
            Assert.AreEqual(0.75f, ProgressIndicatorTestActivity.Circular?.Progress);
        }
        finally
        {
            activity.RunOnUiThread(activity.Finish);
            await WaitFor(
                static () => ProgressIndicatorTestActivity.Current,
                static value => value is null,
                TimeSpan.FromSeconds(5),
                "Progress indicator test activity did not finish.");
        }
    }

    static async Task<ProgressIndicatorTestActivity> StartActivity()
    {
        var context = global::Android.App.Application.Context
            ?? throw new InvalidOperationException(
                "Application.Context not set for progress indicator tests.");
        using var intent = new global::Android.Content.Intent(
            context,
            typeof(ProgressIndicatorTestActivity));
        intent.AddFlags(global::Android.Content.ActivityFlags.NewTask);
        context.StartActivity(intent);

        return await WaitFor(
            static () => ProgressIndicatorTestActivity.Current,
            static value => value is not null,
            TimeSpan.FromSeconds(5),
            "Progress indicator test activity did not start.")
            ?? throw new InvalidOperationException(
                "Progress indicator test activity was not available.");
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
            await Task.Delay(50);
        } while (DateTime.UtcNow < deadline);

        Assert.Fail(message);
        return value;
    }
}
