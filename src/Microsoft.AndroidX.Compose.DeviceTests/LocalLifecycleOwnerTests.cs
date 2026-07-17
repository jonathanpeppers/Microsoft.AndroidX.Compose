namespace Microsoft.AndroidX.Compose.DeviceTests;

/// <summary>Verifies lifecycle-runtime-compose local access and ownership.</summary>
[TestClass]
[DoNotParallelize]
public class LocalLifecycleOwnerTests
{
    [TestMethod]
    public async Task CurrentAndProvides_PreserveLiveJavaPeers()
    {
        var activity = await StartActivity();
        try
        {
            await WaitFor(
                static () => EffectsAndLifecycleTestActivity.CompletedRenderPasses,
                static value => value > 0,
                "Lifecycle owner locals were not read during composition.");

            var defaultOwner = EffectsAndLifecycleTestActivity.DefaultOwner
                ?? throw new InvalidOperationException(
                    "Default lifecycle owner was not captured.");
            var providedOwner = EffectsAndLifecycleTestActivity.ProvidedOwner
                ?? throw new InvalidOperationException(
                    "Provided lifecycle owner was not captured.");
            var expectedProvidedOwner =
                EffectsAndLifecycleTestActivity.ExpectedProvidedOwner
                ?? throw new InvalidOperationException(
                    "Expected provided lifecycle owner was not initialized.");

            Assert.AreSame(activity, defaultOwner);
            Assert.AreEqual(
                ((Java.Lang.Object)expectedProvidedOwner).Handle,
                ((Java.Lang.Object)providedOwner).Handle);
            Assert.AreNotEqual(IntPtr.Zero, ((Java.Lang.Object)defaultOwner).Handle);
            Assert.AreNotEqual(IntPtr.Zero, ((Java.Lang.Object)providedOwner).Handle);

            int priorPass = EffectsAndLifecycleTestActivity.CompletedRenderPasses;
            activity.RunOnUiThread(() =>
            {
                var key = EffectsAndLifecycleTestActivity.EffectKey
                    ?? throw new InvalidOperationException(
                        "Effect key was not initialized.");
                key.Value++;
            });
            await WaitFor(
                static () => EffectsAndLifecycleTestActivity.CompletedRenderPasses,
                value => value > priorPass,
                "Lifecycle owner locals were not read after recomposition.");

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var recomposedDefaultOwner =
                EffectsAndLifecycleTestActivity.DefaultOwner
                ?? throw new InvalidOperationException(
                    "Default lifecycle owner was lost after recomposition.");
            var recomposedProvidedOwner =
                EffectsAndLifecycleTestActivity.ProvidedOwner
                ?? throw new InvalidOperationException(
                    "Provided lifecycle owner was lost after recomposition.");
            Assert.AreEqual(
                ((Java.Lang.Object)defaultOwner).Handle,
                ((Java.Lang.Object)recomposedDefaultOwner).Handle);
            Assert.AreEqual(
                ((Java.Lang.Object)providedOwner).Handle,
                ((Java.Lang.Object)recomposedProvidedOwner).Handle);
            Assert.IsNotNull(recomposedDefaultOwner.Lifecycle.CurrentState);
            Assert.IsNotNull(recomposedProvidedOwner.Lifecycle.CurrentState);
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
                "Application.Context not set for lifecycle-owner tests.");
        EffectsAndLifecycleTestActivity.Reset();
        using var intent = new global::Android.Content.Intent(
            context,
            typeof(EffectsAndLifecycleTestActivity));
        intent.AddFlags(global::Android.Content.ActivityFlags.NewTask);
        context.StartActivity(intent);

        return await WaitFor(
            static () => EffectsAndLifecycleTestActivity.Current,
            static value => value is not null,
            "Lifecycle-owner test activity did not start.")
            ?? throw new InvalidOperationException(
                "Lifecycle-owner test activity was unavailable.");
    }

    static async Task FinishActivity(EffectsAndLifecycleTestActivity activity)
    {
        activity.RunOnUiThread(activity.Finish);
        await WaitFor(
            static () => EffectsAndLifecycleTestActivity.Current,
            current => !ReferenceEquals(current, activity),
            "Lifecycle-owner test activity did not finish.");
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
