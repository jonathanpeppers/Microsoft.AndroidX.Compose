using AndroidX.Compose;
using AndroidX.Compose.Animation.Core;
using Color = AndroidX.Compose.Color;
using CoreTransition = AndroidX.Compose.Animation.Core.Transition;

namespace Microsoft.AndroidX.Compose.DeviceTests;

/// <summary>Native synchronized values, spec routing, identity, interruption and composition ownership.</summary>
[TestClass]
[DoNotParallelize]
public class TransitionValueTests
{
    [TestMethod]
    public void FiniteSpecs_PreserveParametersAndRejectInvalidInputs()
    {
        using var spring = AnimationSpecs.Spring(Spring.DampingRatioMediumBouncy, Spring.StiffnessLow);
        using var tween = AnimationSpecs.Tween(345, 67, EasingKt.LinearEasing);
        Assert.AreEqual(Spring.DampingRatioMediumBouncy, spring.DampingRatio);
        Assert.AreEqual(Spring.StiffnessLow, spring.Stiffness);
        Assert.IsNull(spring.VisibilityThreshold);
        Assert.AreEqual(345, tween.DurationMillis);
        Assert.AreEqual(67, tween.Delay);
        Assert.AreEqual(EasingKt.LinearEasing, tween.Easing);
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => AnimationSpecs.Spring(0));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => AnimationSpecs.Spring(float.NaN));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => AnimationSpecs.Spring(stiffness: float.PositiveInfinity));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => AnimationSpecs.Tween(-1));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => AnimationSpecs.Tween(delayMillis: -1));
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task TypedValues_CompleteTogetherAndKeepPeersAcrossTargetsAndSpecs(bool direct)
    {
        var host = await Start(direct);
        try
        {
            var initial = host.Latest ?? throw new InvalidOperationException("Initial transition snapshot missing.");
            AssertValues(initial, TransitionTestState.Idle, 1f, 0f, Color.Black);
            Assert.IsFalse(initial.Running);
            var recording = await Animate(host, 1);
            AssertValues(recording, TransitionTestState.Recording, 2f, 1f, Color.Red);
            AssertIdentity(initial, recording);
            Assert.IsInstanceOfType<SpringSpec>(Native(recording.Scale).AnimationSpec);
            Assert.IsInstanceOfType<TweenSpec>(Native(recording.Alpha).AnimationSpec);
            Assert.IsInstanceOfType<TweenSpec>(Native(recording.Color).AnimationSpec);
            Assert.IsTrue(host.Observations.Any(x => x.Phase == 1 && x.Running && !x.Idle &&
                x.AlphaValue > 0f && x.AlphaValue < 1f),
                "The tween must actually interpolate, not jump directly to its target.");

            await OnUi(host, () =>
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                var runtime = Java.Lang.Runtime.GetRuntime()
                    ?? throw new InvalidOperationException("Java runtime unavailable during transition identity test.");
                runtime.Gc();
                host.ChangePhase(2);
            });
            var unchanged = await host.Committed.Task.WaitAsync(TimeSpan.FromSeconds(15));
            AssertIdentity(initial, unchanged);
            AssertValues(unchanged, TransitionTestState.Recording, 2f, 1f, Color.Red);

            var cancelled = await Animate(host, 3);
            AssertIdentity(initial, cancelled);
            AssertValues(cancelled, TransitionTestState.Cancelled, 0.5f, 0.25f, Color.Blue);
            var defaults = await Animate(host, 4);
            AssertIdentity(initial, defaults);
            AssertValues(defaults, TransitionTestState.Idle, 1f, 0f, Color.Black);
            Assert.IsInstanceOfType<SpringSpec>(Native(defaults.Scale).AnimationSpec);
            Assert.IsInstanceOfType<SpringSpec>(Native(defaults.Alpha).AnimationSpec);
            Assert.IsInstanceOfType<SpringSpec>(Native(defaults.Color).AnimationSpec);
            var tween = await Animate(host, 5);
            AssertIdentity(initial, tween);
            Assert.IsInstanceOfType<TweenSpec>(Native(tween.Scale).AnimationSpec);
            AssertValues(tween, TransitionTestState.Recording, 2f, 1f, Color.Red);

            var remapped = await Animate(host, 6);
            AssertIdentity(initial, remapped);
            AssertValues(remapped, TransitionTestState.Recording, 3f, 1f, Color.Green);
        }
        finally
        {
            await Finish(host);
        }
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task MappingLocalSnapshotReads_RetargetTheParentTransition(bool direct)
    {
        var host = await Start(direct);
        try
        {
            var recording = await Animate(host, 1);
            AssertValues(recording, TransitionTestState.Recording, 2f, 1f, Color.Red);
            await OnUi(host, host.ChangeMapping);
            await host.Started.Task.WaitAsync(TimeSpan.FromSeconds(15));
            var changed = await host.Settled.Task.WaitAsync(TimeSpan.FromSeconds(15));
            AssertIdentity(recording, changed);
            AssertValues(changed, TransitionTestState.Recording, 4f, 1f, Color.Cyan);
        }
        finally
        {
            await Finish(host);
        }
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task InterruptedAndRemovedTransitions_UseNativeLifecycle(bool direct)
    {
        var host = await Start(direct);
        try
        {
            var initial = host.Latest ?? throw new InvalidOperationException("Initial transition snapshot missing.");
            await OnUi(host, () => host.ChangePhase(1));
            var running = await host.Started.Task.WaitAsync(TimeSpan.FromSeconds(15));
            Assert.IsFalse(running.Idle);
            var reverse = await Animate(host, 3);
            AssertIdentity(initial, reverse);
            AssertValues(reverse, TransitionTestState.Cancelled, 0.5f, 0.25f, Color.Blue);

            await OnUi(host, () => host.ChangePhase(5));
            await host.Started.Task.WaitAsync(TimeSpan.FromSeconds(15));
            await OnUi(host, () => host.ChangePhase(8));
            await host.Removed.Task.WaitAsync(TimeSpan.FromSeconds(15));
            Assert.IsFalse(initial.Transition.IsRunning, "Native disposal must terminate the transition frame loop.");
            Assert.IsFalse(host.Settled.Task.IsCompleted, "Removal is not successful value completion.");
            await OnUi(host, () => host.ChangePhase(9));
            var readded = await host.Committed.Task.WaitAsync(TimeSpan.FromSeconds(15));
            Assert.AreNotSame(initial.Transition, readded.Transition);
            Assert.AreNotSame(initial.Transition.Jvm, readded.Transition.Jvm);
            Assert.AreNotSame(initial.Scale.Jvm, readded.Scale.Jvm);
            AssertValues(readded, TransitionTestState.Recording, 2f, 1f, Color.Red);
        }
        finally
        {
            await Finish(host);
        }
    }

    static CoreTransition.TransitionAnimationState Native<T>(TransitionAnimation<T> state) =>
        global::Android.Runtime.Extensions.JavaCast<CoreTransition.TransitionAnimationState>((Java.Lang.Object)state.Jvm);

    static void AssertIdentity(TransitionValueSnapshot first, TransitionValueSnapshot next)
    {
        Assert.AreSame(first.Transition, next.Transition);
        Assert.AreSame(first.Transition.Jvm, next.Transition.Jvm);
        Assert.AreSame(first.Scale, next.Scale);
        Assert.AreSame(first.Alpha, next.Alpha);
        Assert.AreSame(first.Color, next.Color);
        Assert.AreSame(first.Scale.Jvm, next.Scale.Jvm);
        Assert.AreSame(first.Color.Jvm, next.Color.Jvm);
        Assert.AreSame(first.Scale.TargetCallback, next.Scale.TargetCallback);
        Assert.AreSame(first.Scale.SpecCallback, next.Scale.SpecCallback);
        Assert.AreSame(first.Color.TargetCallback, next.Color.TargetCallback);
        Assert.AreSame(first.Color.SpecCallback, next.Color.SpecCallback);
        Assert.AreSame(first.TailIdentity, next.TailIdentity);
        Assert.AreNotSame(next.Scale, next.Alpha);
        Assert.AreNotSame(next.Scale.TargetCallback, next.Alpha.TargetCallback);
    }

    static void AssertValues(TransitionValueSnapshot snapshot, TransitionTestState state,
        float scale, float alpha, Color color)
    {
        Assert.IsTrue(snapshot.Idle);
        Assert.AreEqual(state, snapshot.Current);
        Assert.AreEqual(state, snapshot.Target);
        Assert.AreEqual(scale, snapshot.ScaleValue);
        Assert.AreEqual(alpha, snapshot.AlphaValue);
        Assert.AreEqual(color, snapshot.ColorValue);
    }

    static async Task<TransitionValueSnapshot> Animate(TransitionValueTestActivity host, int phase)
    {
        await OnUi(host, () => host.ChangePhase(phase));
        await host.Started.Task.WaitAsync(TimeSpan.FromSeconds(15));
        return await host.Settled.Task.WaitAsync(TimeSpan.FromSeconds(15));
    }

    static async Task<TransitionValueTestActivity> Start(bool direct)
    {
        TransitionValueTestActivity.Ready = new(TaskCreationOptions.RunContinuationsAsynchronously);
        var context = global::Android.App.Application.Context;
        using var intent = new global::Android.Content.Intent(context, typeof(TransitionValueTestActivity));
        intent.AddFlags(global::Android.Content.ActivityFlags.NewTask);
        intent.PutExtra("direct", direct);
        context.StartActivity(intent);
        return await TransitionValueTestActivity.Ready.Task.WaitAsync(TimeSpan.FromSeconds(15));
    }

    static async Task Finish(TransitionValueTestActivity host)
    {
        await OnUi(host, host.Finish);
        await host.Destroyed.Task.WaitAsync(TimeSpan.FromSeconds(15));
    }

    static Task OnUi(TransitionValueTestActivity host, Action action)
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
