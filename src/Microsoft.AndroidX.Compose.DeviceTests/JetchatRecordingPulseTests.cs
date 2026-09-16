namespace Microsoft.AndroidX.Compose.DeviceTests;

/// <summary>Jetchat J06/J07 recording pulse timing, removal, and re-entry evidence.</summary>
[TestClass]
[DoNotParallelize]
public class JetchatRecordingPulseTests
{
    [TestMethod]
    public async Task RecordingIndicator_UsesPinnedNativeTimingAndStopsWhenRemoved()
    {
        var host = await Start();
        try
        {
            var first = await host.Committed.Task.WaitAsync(TimeSpan.FromSeconds(5));
            var animationTimeout = TimeSpan.FromSeconds(Math.Max(15, host.DurationScale * 6));
            var low = await host.Low.Task.WaitAsync(animationTimeout);

            if (host.DurationScale == 0f)
            {
                Assert.AreEqual(0.2f, low.Value, 0.001f,
                    "Disabled Jetchat pulse must snap to its target and suspend native frame work.");
            }
            else
            {
                var returned = await host.Returned.Task.WaitAsync(animationTimeout);
                AssertDirectionDuration(
                    2000f * host.DurationScale,
                    low.ElapsedMilliseconds - first.ElapsedMilliseconds,
                    "Jetchat initial-to-target");
                AssertDirectionDuration(
                    2000f * host.DurationScale,
                    returned.ElapsedMilliseconds - low.ElapsedMilliseconds,
                    "Jetchat target-to-initial");
                Assert.IsTrue(host.Observations.Any(x => x.Value > 0.3f && x.Value < 0.9f),
                    "The actual recording indicator must receive interpolated native values.");
            }

            await OnUi(host, () => host.ChangePhase(1));
            await host.Removed.Task.WaitAsync(TimeSpan.FromSeconds(5));
            int removedCount = host.Observations.Count;
            await Task.Delay(500);
            Assert.AreEqual(removedCount, host.Observations.Count,
                "The removed recording indicator must stop publishing native pulse frames.");

            await OnUi(host, () => host.ChangePhase(2));
            var readded = await host.Committed.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.IsTrue(readded.Value >= 0.95f,
                $"Re-entered Jetchat pulse must restart near 1.0; observed {readded.Value:R}.");
            var readdedLow = await host.Low.Task.WaitAsync(animationTimeout);
            Assert.IsTrue(readdedLow.ElapsedMilliseconds > readded.ElapsedMilliseconds);
        }
        finally
        {
            await Finish(host);
        }
    }

    static void AssertDirectionDuration(float expectedMilliseconds, long actualMilliseconds, string leg)
    {
        float tolerance = MathF.Max(600f, expectedMilliseconds * 0.5f);
        Assert.IsTrue(
            actualMilliseconds >= expectedMilliseconds - tolerance &&
            actualMilliseconds <= expectedMilliseconds + tolerance,
            $"{leg} took {actualMilliseconds} ms; expected {expectedMilliseconds:F0} +/- {tolerance:F0} ms.");
    }

    static async Task<JetchatRecordingPulseTestActivity> Start()
    {
        JetchatRecordingPulseTestActivity.Ready =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        var context = global::Android.App.Application.Context;
        using var intent = new global::Android.Content.Intent(
            context, typeof(JetchatRecordingPulseTestActivity));
        intent.AddFlags(global::Android.Content.ActivityFlags.NewTask);
        context.StartActivity(intent);
        return await JetchatRecordingPulseTestActivity.Ready.Task.WaitAsync(TimeSpan.FromSeconds(15));
    }

    static async Task Finish(JetchatRecordingPulseTestActivity host)
    {
        await OnUi(host, host.Finish);
        await host.Destroyed.Task.WaitAsync(TimeSpan.FromSeconds(15));
    }

    static Task OnUi(JetchatRecordingPulseTestActivity host, Action action)
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        host.RunOnUiThread(() =>
        {
            try
            {
                action();
                completion.SetResult();
            }
            catch (Exception ex)
            {
                completion.SetException(ex);
            }
        });
        return completion.Task.WaitAsync(TimeSpan.FromSeconds(15));
    }
}
