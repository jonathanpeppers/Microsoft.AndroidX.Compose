using AndroidX.Compose;

namespace Microsoft.AndroidX.Compose.DeviceTests;

/// <summary>Verifies managed disposable-effect contracts on Android.</summary>
[TestClass]
[DoNotParallelize]
public class DisposableEffectLifecycleTests
{
    [TestMethod]
    public void PublicSurfaces_RejectNullSetupDelegates()
    {
#pragma warning disable CS8625
        Assert.ThrowsExactly<ArgumentNullException>(
            () => new DisposableEffect("key", null));
        Assert.ThrowsExactly<ArgumentNullException>(
            () => new DisposableEffect("key1", "key2", null));
        Assert.ThrowsExactly<ArgumentNullException>(
            () => new DisposableEffect("key1", "key2", "key3", null));
        Assert.ThrowsExactly<ArgumentNullException>(
            () => Composables.DisposableEffect("key", null));
        Assert.ThrowsExactly<ArgumentNullException>(
            () => new DisposableEffectBody(null));
#pragma warning restore CS8625
    }

    [TestMethod]
    public async Task SetupAndCleanup_FollowKeysAndCompositionLifetime()
    {
        var activity = await StartActivity();
        try
        {
            await WaitFor(
                static () => (
                    EffectsAndLifecycleTestActivity.Setups,
                    EffectsAndLifecycleTestActivity.Cleanups),
                static value => value == (1, 0),
                "DisposableEffect setup did not run on entering composition.");

            int priorPass = EffectsAndLifecycleTestActivity.CompletedRenderPasses;
            activity.RunOnUiThread(() =>
            {
                var tick = EffectsAndLifecycleTestActivity.RecompositionTick
                    ?? throw new InvalidOperationException(
                        "Recomposition tick was not initialized.");
                tick.Value++;
            });
            await WaitFor(
                static () => EffectsAndLifecycleTestActivity.CompletedRenderPasses,
                value => value > priorPass,
                "Unrelated state change did not recompose the test content.");
            Assert.AreEqual(1, EffectsAndLifecycleTestActivity.Setups);
            Assert.AreEqual(0, EffectsAndLifecycleTestActivity.Cleanups);

            activity.RunOnUiThread(() =>
            {
                var key = EffectsAndLifecycleTestActivity.EffectKey
                    ?? throw new InvalidOperationException(
                        "Effect key was not initialized.");
                key.Value++;
            });
            await WaitFor(
                static () => (
                    EffectsAndLifecycleTestActivity.Setups,
                    EffectsAndLifecycleTestActivity.Cleanups),
                static value => value == (2, 1),
                "Key change did not clean up the old effect before restarting it.");

            activity.RunOnUiThread(() =>
            {
                var visible = EffectsAndLifecycleTestActivity.EffectVisible
                    ?? throw new InvalidOperationException(
                        "Effect visibility was not initialized.");
                visible.Value = false;
            });
            await WaitFor(
                static () => (
                    EffectsAndLifecycleTestActivity.Setups,
                    EffectsAndLifecycleTestActivity.Cleanups),
                static value => value == (2, 2),
                "DisposableEffect did not clean up when leaving composition.");
        }
        finally
        {
            await FinishActivity(activity);
        }
    }

    static async Task<EffectsAndLifecycleTestActivity> StartActivity()
    {
        var context = global::Android.App.Application.Context
            ?? throw new InvalidOperationException(
                "Application.Context not set for disposable-effect tests.");
        EffectsAndLifecycleTestActivity.Reset();
        using var intent = new global::Android.Content.Intent(
            context,
            typeof(EffectsAndLifecycleTestActivity));
        intent.AddFlags(global::Android.Content.ActivityFlags.NewTask);
        context.StartActivity(intent);

        return await WaitFor(
            static () => EffectsAndLifecycleTestActivity.Current,
            static value => value is not null,
            "Effects test activity did not start.")
            ?? throw new InvalidOperationException(
                "Effects test activity was unavailable.");
    }

    static async Task FinishActivity(EffectsAndLifecycleTestActivity activity)
    {
        activity.RunOnUiThread(activity.Finish);
        await WaitFor(
            static () => EffectsAndLifecycleTestActivity.Current,
            current => !ReferenceEquals(current, activity),
            "Effects test activity did not finish.");
    }

    static async Task<T> WaitFor<T>(
        Func<T> read,
        Func<T, bool> predicate,
        string message)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(10);
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
