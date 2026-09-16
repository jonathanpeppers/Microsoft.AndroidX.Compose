using AndroidX.Compose;
using AndroidX.Compose.Animation.Core;

namespace Microsoft.AndroidX.Compose.DeviceTests;

/// <summary>Native infinite float timing, disabled policy, identity, removal, and re-entry.</summary>
[TestClass]
[DoNotParallelize]
public class InfiniteTransitionValueTests
{
    [TestMethod]
    public void InfiniteSpec_PreservesTweenAndReverseMode()
    {
        using var tween = AnimationSpecs.Tween(345, 67, EasingKt.LinearEasing);
        var reverse = RepeatMode.Reverse
            ?? throw new InvalidOperationException("Compose RepeatMode.Reverse was unavailable.");
        using var spec = AnimationSpecs.InfiniteRepeatable(tween, reverse);
        Assert.AreSame(tween, spec.Animation);
        Assert.AreEqual(reverse, spec.RepeatMode);
        Assert.AreEqual(0L, spec.InitialStartOffset);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task NativeTimingRemovalAndReentry_FollowDurationScaleAndComposition(bool direct)
    {
        var host = await Start(direct);
        try
        {
            var first = await host.Committed.Task.WaitAsync(TimeSpan.FromSeconds(5));
            var animationTimeout = TimeSpan.FromSeconds(Math.Max(15, host.DurationScale * 6));
            var low = await host.Low.Task.WaitAsync(animationTimeout);
            AssertIdentity(first, low);

            if (host.DurationScale == 0f)
            {
                Assert.AreEqual(0.2f, low.Value, 0.001f,
                    "Disabled animations must snap to the target and suspend native frame work.");
            }
            else
            {
                var returned = await host.Returned.Task.WaitAsync(animationTimeout);
                AssertIdentity(first, returned);
                AssertDirectionDuration(
                    2000f * host.DurationScale,
                    low.ElapsedMilliseconds - first.ElapsedMilliseconds,
                    "initial-to-target");
                AssertDirectionDuration(
                    2000f * host.DurationScale,
                    returned.ElapsedMilliseconds - low.ElapsedMilliseconds,
                    "target-to-initial");
                Assert.IsTrue(host.Observations.Any(x => x.Value > 0.3f && x.Value < 0.9f),
                    "Native frames must interpolate rather than jump between endpoints.");
            }

            await OnUi(host, () => host.ChangePhase(1));
            await host.Removed.Task.WaitAsync(TimeSpan.FromSeconds(5));
            await OnUi(host, () =>
                Assert.AreEqual(0, first.Transition.Jvm.Animations.Count,
                    "Removing the call must unregister its native child animation."));

            await OnUi(host, () => host.ChangePhase(2));
            var readded = await host.Committed.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.AreNotSame(first.Transition, readded.Transition);
            Assert.AreNotSame(first.Transition.Jvm, readded.Transition.Jvm);
            Assert.AreNotSame(first.Animation, readded.Animation);
            Assert.AreNotSame(first.Animation.Jvm, readded.Animation.Jvm);
        }
        finally
        {
            await Finish(host);
        }
    }

    static void AssertIdentity(
        InfiniteTransitionValueSnapshot first,
        InfiniteTransitionValueSnapshot next)
    {
        Assert.AreSame(first.Transition, next.Transition);
        Assert.AreSame(first.Transition.Jvm, next.Transition.Jvm);
        Assert.AreSame(first.Animation, next.Animation);
        Assert.AreSame(first.Animation.Jvm, next.Animation.Jvm);
    }

    static void AssertDirectionDuration(float expectedMilliseconds, long actualMilliseconds, string leg)
    {
        float tolerance = MathF.Max(600f, expectedMilliseconds * 0.5f);
        Assert.IsTrue(
            actualMilliseconds >= expectedMilliseconds - tolerance &&
            actualMilliseconds <= expectedMilliseconds + tolerance,
            $"{leg} took {actualMilliseconds} ms; expected {expectedMilliseconds:F0} +/- {tolerance:F0} ms.");
    }

    static async Task<InfiniteTransitionValueTestActivity> Start(bool direct)
    {
        InfiniteTransitionValueTestActivity.Ready =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        var context = global::Android.App.Application.Context;
        using var intent = new global::Android.Content.Intent(
            context, typeof(InfiniteTransitionValueTestActivity));
        intent.AddFlags(global::Android.Content.ActivityFlags.NewTask);
        intent.PutExtra("direct", direct);
        context.StartActivity(intent);
        return await InfiniteTransitionValueTestActivity.Ready.Task.WaitAsync(TimeSpan.FromSeconds(15));
    }

    static async Task Finish(InfiniteTransitionValueTestActivity host)
    {
        await OnUi(host, host.Finish);
        await host.Destroyed.Task.WaitAsync(TimeSpan.FromSeconds(15));
    }

    static Task OnUi(InfiniteTransitionValueTestActivity host, Action action)
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
