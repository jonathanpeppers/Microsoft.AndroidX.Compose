using AndroidX.Compose;

namespace Microsoft.AndroidX.Compose.DeviceTests;

/// <summary>Verifies LocalDensity reads, provided density and font scale, and native measurement.</summary>
[TestClass]
[DoNotParallelize]
public class LocalDensityTests
{
    [TestMethod]
    public void Provides_RejectsNullDensity()
    {
#pragma warning disable CS8625 // Exercise the public runtime null guard.
        Assert.ThrowsExactly<ArgumentNullException>(() => LocalDensity.Provides(null));
#pragma warning restore CS8625
    }

    [TestMethod]
    public async Task Current_ReadsHostDensityAndFontScale()
    {
        var activity = await StartActivity();
        try
        {
            var inherited = await WaitFor(
                static () => LocalDensityTestActivity.Inherited,
                static value => value is not null,
                "LocalDensity was not read during composition.")
                ?? throw new InvalidOperationException("Inherited density was not captured.");
            var resources = activity.Resources
                ?? throw new InvalidOperationException("Resources not set on LocalDensityTestActivity.");
            var metrics = resources.DisplayMetrics
                ?? throw new InvalidOperationException("DisplayMetrics not set on LocalDensityTestActivity.");
            var configuration = resources.Configuration
                ?? throw new InvalidOperationException("Configuration not set on LocalDensityTestActivity.");

            Assert.AreEqual(metrics.Density, inherited.Density);
            Assert.AreEqual(configuration.FontScale, inherited.FontScale);

            var sibling = await WaitFor(
                static () => LocalDensityTestActivity.Sibling,
                static value => value is not null,
                "LocalDensity was not read after the provider.");
            Assert.AreEqual(inherited, sibling, "A sibling after the provider must read the inherited density.");
        }
        finally
        {
            await FinishActivity(activity);
        }
    }

    [TestMethod]
    public async Task ProvidedDensityAndFontScale_ReachReadsAndMeasurement()
    {
        var activity = await StartActivity();
        try
        {
            var expected = new LocalDensitySnapshot(
                LocalDensityTestActivity.ProvidedDensity,
                LocalDensityTestActivity.InitialFontScale);
            await WaitFor(
                static () => LocalDensityTestActivity.Provided,
                value => value == expected,
                "The provided density was not read inside the provider.");
            await WaitFor(
                static () => LocalDensityTestActivity.ProvidedMeasure,
                value => value == expected,
                "Native measurement did not receive the provided density.");
            Assert.AreEqual(20, LocalDensityTestActivity.ProvidedTenDpPx, "10dp must measure as 20px at density 2.");

            const float UpdatedFontScale = 1.25f;
            var updated = expected with { FontScale = UpdatedFontScale };
            activity.RunOnUiThread(() =>
            {
                var fontScale = LocalDensityTestActivity.ProvidedFontScale
                    ?? throw new InvalidOperationException("Provided font scale was not initialized.");
                fontScale.Value = UpdatedFontScale;
            });
            await WaitFor(
                static () => LocalDensityTestActivity.Provided,
                value => value == updated,
                "A changed provided font scale was not read after recomposition.");
            await WaitFor(
                static () => LocalDensityTestActivity.ProvidedMeasure,
                value => value == updated,
                "Native measurement did not receive the changed font scale.");
        }
        finally
        {
            await FinishActivity(activity);
        }
    }

    [TestMethod]
    public async Task ProvidedDensityPeer_KeepsJavaIdentityAndReachesMeasurement()
    {
        var activity = await StartActivity();
        try
        {
            var expected = new LocalDensitySnapshot(
                LocalDensityTestActivity.PeerDensity,
                LocalDensityTestActivity.PeerFontScale);
            await WaitFor(
                static () => LocalDensityTestActivity.PeerIdentity,
                static value => value >= 0,
                "The provided density peer was not read inside the provider.");
            Assert.AreEqual(1, LocalDensityTestActivity.PeerIdentity,
                "LocalDensity.Current must return the provided Java density object.");
            Assert.AreEqual(expected, LocalDensityTestActivity.Peer);
            await WaitFor(
                static () => LocalDensityTestActivity.PeerMeasure,
                value => value == expected,
                "Native measurement did not receive the provided density peer.");
        }
        finally
        {
            await FinishActivity(activity);
        }
    }

    [TestMethod]
    public async Task ProvidedFontScale_ScalesTextMeasurement()
    {
        var activity = await StartActivity();
        try
        {
            int baseline = await WaitFor(
                static () => LocalDensityTestActivity.BaselineTextHeight,
                static value => value > 0,
                "Text was not measured at font scale 1.");
            int large = await WaitFor(
                static () => LocalDensityTestActivity.LargeTextHeight,
                static value => value > 0,
                "Text was not measured at font scale 2.");

            // Android 14+ scales large text nonlinearly, so require clear growth, not exactly 2x.
            Assert.IsGreaterThan(baseline * 1.5, (double)large,
                $"14sp text at font scale 2 measured {large}px; font scale 1 measured {baseline}px.");
        }
        finally
        {
            await FinishActivity(activity);
        }
    }

    static async Task<LocalDensityTestActivity> StartActivity()
    {
        var context = global::Android.App.Application.Context
            ?? throw new InvalidOperationException(
                "Application.Context not set for LocalDensity tests.");
        LocalDensityTestActivity.Reset();
        using var intent = new global::Android.Content.Intent(
            context,
            typeof(LocalDensityTestActivity));
        intent.AddFlags(global::Android.Content.ActivityFlags.NewTask);
        context.StartActivity(intent);

        return await WaitFor(
            static () => LocalDensityTestActivity.Current,
            static value => value is not null,
            "LocalDensity test activity did not start.")
            ?? throw new InvalidOperationException(
                "LocalDensity test activity was unavailable.");
    }

    static async Task FinishActivity(LocalDensityTestActivity activity)
    {
        activity.RunOnUiThread(activity.Finish);
        await WaitFor(
            static () => LocalDensityTestActivity.Current,
            current => !ReferenceEquals(current, activity),
            "LocalDensity test activity did not finish.");
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
