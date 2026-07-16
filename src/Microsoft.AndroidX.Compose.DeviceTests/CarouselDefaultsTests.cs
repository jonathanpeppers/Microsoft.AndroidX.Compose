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
    public Task TreeCarousels_OmittedDpDefaults_RenderAllItems() =>
        AssertScenarioRenders(CarouselTestActivity.TreeOmitted);

    [TestMethod]
    public Task TreeCarousels_ExplicitDpValues_RenderAllItems() =>
        AssertScenarioRenders(CarouselTestActivity.TreeExplicit);

    [TestMethod]
    public Task StaticCarousels_OmittedDpDefaults_RenderAllItems() =>
        AssertScenarioRenders(CarouselTestActivity.StaticOmitted);

    [TestMethod]
    public Task StaticCarousels_ExplicitDpValues_RenderAllItems() =>
        AssertScenarioRenders(CarouselTestActivity.StaticExplicit);

    static async Task AssertScenarioRenders(int scenario)
    {
        var activity = await StartActivity(scenario);
        try
        {
            await WaitFor(
                static () => CarouselTestActivity.RenderedCarousels,
                static value => value == CarouselTestActivity.AllCarouselsRendered,
                TimeSpan.FromSeconds(10),
                "Not all carousel item-content lambdas were composed.");
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

    static async Task<CarouselTestActivity> StartActivity(int scenario)
    {
        var context = global::Android.App.Application.Context
            ?? throw new InvalidOperationException(
                "Application.Context not set for carousel tests.");
        CarouselTestActivity.Reset(scenario);
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
