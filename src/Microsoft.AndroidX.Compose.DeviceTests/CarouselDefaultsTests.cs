using AndroidX.Compose;

namespace Microsoft.AndroidX.Compose.DeviceTests;

/// <summary>Verifies nullable-Dp carousel defaults against real Compose rendering.</summary>
[TestClass]
[DoNotParallelize]
public class CarouselDefaultsTests
{
    static readonly IReadOnlyList<int> Items = [0];

    [TestMethod]
    public void TreeProperties_DefaultToNull_AndAcceptImplicitDpConversions()
    {
        var uncontained = new HorizontalUncontainedCarousel<int>(
            Items,
            itemWidth: 96,
            static _ => new Text("Item"));
        var multiBrowse = new HorizontalMultiBrowseCarousel<int>(
            Items,
            preferredItemWidth: 120,
            static _ => new Text("Item"));
        var centeredHero = new HorizontalCenteredHeroCarousel<int>(
            Items,
            static _ => new Text("Item"));

        Assert.IsNull(uncontained.ItemSpacing);
        Assert.IsNull(multiBrowse.ItemSpacing);
        Assert.IsNull(centeredHero.MaxItemWidth);
        Assert.IsNull(centeredHero.ItemSpacing);

        uncontained.ItemSpacing = 8;
        multiBrowse.ItemSpacing = 8f;
        centeredHero.MaxItemWidth = 240;
        centeredHero.ItemSpacing = 8f;

        Assert.AreEqual(new Dp(8), uncontained.ItemSpacing);
        Assert.AreEqual(new Dp(8), multiBrowse.ItemSpacing);
        Assert.AreEqual(new Dp(240), centeredHero.MaxItemWidth);
        Assert.AreEqual(new Dp(8), centeredHero.ItemSpacing);
    }

    [TestMethod]
    public async Task TreeCarousels_NullAndExplicitDp_ChangeLayout()
    {
        var omitted = await MeasureScenario(CarouselTestActivity.TreeOmitted);
        var explicitDp = await MeasureScenario(CarouselTestActivity.TreeExplicit);

        AssertNullableDpLayout(omitted, explicitDp);
    }

    [TestMethod]
    public async Task StaticCarousels_NullAndExplicitDp_ChangeLayout()
    {
        var omitted = await MeasureScenario(CarouselTestActivity.StaticOmitted);
        var explicitDp = await MeasureScenario(CarouselTestActivity.StaticExplicit);

        AssertNullableDpLayout(omitted, explicitDp);
    }

    [TestMethod]
    public async Task CarouselState_BindsLiveStateAndReusesPeer()
    {
        CarouselTestActivity.Reset(CarouselTestActivity.StateLifecycle);
        var state = CarouselTestActivity.State
            ?? throw new InvalidOperationException("Carousel state was not initialized.");
        Assert.AreEqual(1, state.CurrentItem);
        Assert.IsFalse(state.IsScrollInProgress);
        Assert.IsNull(state.Jvm);

        var activity = await StartConfiguredActivity();
        try
        {
            var jvm = await WaitFor(
                () => state.Jvm,
                static value => value is not null,
                TimeSpan.FromSeconds(10),
                "CarouselState did not bind to a JVM peer.")
                ?? throw new InvalidOperationException("CarouselState JVM peer was unavailable.");
            Assert.AreEqual(1, state.CurrentItem);

            await RunOnUiThread(activity, () => state.ScrollToItemAsync(8));
            await WaitFor(
                () => state.CurrentItem,
                static value => value == 8,
                TimeSpan.FromSeconds(10),
                "CarouselState did not expose the live snapped item.");

            await SetVisibility(activity, false);
            await SetVisibility(activity, true);
            Assert.AreSame(jvm, state.Jvm, "CarouselState replaced its remembered JVM peer.");

            await Task.Delay(250);
            await RunOnUiThread(activity, () => state.AnimateScrollToItemAsync(0));
            await WaitFor(
                () => state.CurrentItem,
                static value => value == 0,
                TimeSpan.FromSeconds(10),
                "CarouselState animation did not reach the requested item.");
        }
        finally
        {
            await FinishActivity(activity);
        }
    }

    static async Task<CarouselTestActivity.LayoutSnapshot> MeasureScenario(int scenario)
    {
        var activity = await StartActivity(scenario);
        try
        {
            await WaitFor(
                static () => CarouselTestActivity.MeasurementCount,
                static value => value >= CarouselTestActivity.ExpectedMeasurements,
                TimeSpan.FromSeconds(10),
                "The first two items in each carousel were not measured.");
            await Task.Delay(100);
            return CarouselTestActivity.Snapshot();
        }
        finally
        {
            activity.RunOnUiThread(activity.Finish);
            await WaitFor(
                static () => CarouselTestActivity.Current,
                static value => value is null,
                TimeSpan.FromSeconds(5),
                "Carousel test activity did not finish.");
        }
    }

    static void AssertNullableDpLayout(
        CarouselTestActivity.LayoutSnapshot omitted,
        CarouselTestActivity.LayoutSnapshot explicitDp)
    {
        float density = global::Android.App.Application.Context?.Resources?.DisplayMetrics?.Density
            ?? throw new InvalidOperationException(
                "Display density was not available for carousel tests.");
        float expectedSpacing = CarouselTestActivity.ExplicitSpacingDp * density;
        float spacingTolerance = density * 2;

        Assert.AreEqual(
            expectedSpacing,
            Gap(explicitDp.Uncontained0, explicitDp.Uncontained1),
            spacingTolerance,
            "Explicit uncontained-carousel itemSpacing was not forwarded.");
        Assert.AreEqual(
            expectedSpacing,
            Gap(explicitDp.MultiBrowse0, explicitDp.MultiBrowse1),
            spacingTolerance,
            "Explicit multi-browse itemSpacing was not forwarded.");
        Assert.AreEqual(
            expectedSpacing,
            Gap(explicitDp.CenteredHero0, explicitDp.CenteredHero1),
            spacingTolerance,
            "Explicit centered-hero itemSpacing was not forwarded.");

        Assert.IsTrue(
            Gap(omitted.Uncontained0, omitted.Uncontained1) < expectedSpacing / 2,
            "Omitted uncontained-carousel itemSpacing did not use Kotlin's default.");
        Assert.IsTrue(
            Gap(omitted.MultiBrowse0, omitted.MultiBrowse1) < expectedSpacing / 2,
            "Omitted multi-browse itemSpacing did not use Kotlin's default.");
        Assert.IsTrue(
            Gap(omitted.CenteredHero0, omitted.CenteredHero1) < expectedSpacing / 2,
            "Omitted centered-hero itemSpacing did not use Kotlin's default.");

        float maxWidth = CarouselTestActivity.ExplicitMaxItemWidthDp * density;
        Assert.IsTrue(
            explicitDp.CenteredHero0.Width <= maxWidth + density,
            "Explicit centered-hero maxItemWidth was not forwarded.");
        Assert.IsTrue(
            omitted.CenteredHero0.Width > explicitDp.CenteredHero0.Width + density,
            "Omitted centered-hero maxItemWidth did not use Kotlin's default.");
    }

    static float Gap(
        CarouselTestActivity.ItemBounds first,
        CarouselTestActivity.ItemBounds second) =>
        second.X - first.X - first.Width;

    static async Task<CarouselTestActivity> StartActivity(int scenario)
    {
        var context = global::Android.App.Application.Context
            ?? throw new InvalidOperationException(
                "Application.Context not set for carousel tests.");
        CarouselTestActivity.Reset(scenario);
        return await StartConfiguredActivity();
    }

    static async Task<CarouselTestActivity> StartConfiguredActivity()
    {
        var context = global::Android.App.Application.Context
            ?? throw new InvalidOperationException(
                "Application.Context not set for carousel tests.");
        using var intent = new global::Android.Content.Intent(
            context,
            typeof(CarouselTestActivity));
        intent.AddFlags(global::Android.Content.ActivityFlags.NewTask);
        context.StartActivity(intent);

        return await WaitFor(
            static () => CarouselTestActivity.Current,
            static value => value is not null,
            TimeSpan.FromSeconds(5),
            "Carousel test activity did not start.")
            ?? throw new InvalidOperationException(
                "Carousel test activity was not available.");
    }

    static async Task SetVisibility(CarouselTestActivity activity, bool visible)
    {
        int completedPasses = CarouselTestActivity.CompletedRenderPasses;
        activity.RunOnUiThread(() =>
        {
            var visibility = CarouselTestActivity.Visible
                ?? throw new InvalidOperationException(
                    "Visibility state not set on CarouselTestActivity.");
            visibility.Value = visible;
        });
        await WaitFor(
            static () => CarouselTestActivity.CompletedRenderPasses,
            value => value > completedPasses,
            TimeSpan.FromSeconds(10),
            "Carousel visibility change did not complete a render pass.");
    }

    static async Task FinishActivity(CarouselTestActivity activity)
    {
        activity.RunOnUiThread(activity.Finish);
        await WaitFor(
            static () => CarouselTestActivity.Current,
            current => !ReferenceEquals(current, activity),
            TimeSpan.FromSeconds(5),
            "Carousel test activity did not finish.");
    }

    static Task RunOnUiThread(CarouselTestActivity activity, Func<Task> operation)
    {
        var completion = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);
        activity.RunOnUiThread(async () =>
        {
            try
            {
                await operation();
                completion.SetResult();
            }
            catch (Exception ex)
            {
                completion.SetException(ex);
            }
        });
        return completion.Task;
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
