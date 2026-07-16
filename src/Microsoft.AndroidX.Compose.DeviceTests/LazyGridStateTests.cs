using AndroidX.Compose;

namespace Microsoft.AndroidX.Compose.DeviceTests;

/// <summary>Verifies managed lazy-grid state binding and Kotlin default masks.</summary>
[TestClass]
[DoNotParallelize]
public class LazyGridStateTests
{
    [TestMethod]
    public async Task TreeGrid_UsesManagedStateAndScrollBridges()
    {
        var activity = await StartActivity(LazyGridStateTestActivity.TreeGridExplicitState);
        try
        {
            var state = LazyGridStateTestActivity.GridState;
            await WaitFor(
                () => state.FirstVisibleItemIndex == LazyGridStateTestActivity.InitialIndex
                    && LazyGridStateTestActivity.RenderedItems > 0,
                "Managed LazyGridState was not attached to the rendered grid.");

            Assert.AreEqual(LazyGridStateTestActivity.InitialIndex, state.FirstVisibleItemIndex);
            await state.ScrollToItemAsync(0);
            Assert.AreEqual(0, state.FirstVisibleItemIndex);
            await state.AnimateScrollToItemAsync(20);
            Assert.AreEqual(20, state.FirstVisibleItemIndex);
        }
        finally
        {
            await FinishActivity(activity);
        }
    }

    [TestMethod]
    public async Task StaticStaggeredGrid_UsesManagedStateAndScrollBridges()
    {
        var activity = await StartActivity(
            LazyGridStateTestActivity.StaticStaggeredExplicitState);
        try
        {
            var state = LazyGridStateTestActivity.StaggeredState;
            await WaitFor(
                () => state.FirstVisibleItemIndex == LazyGridStateTestActivity.InitialIndex
                    && LazyGridStateTestActivity.RenderedItems > 0,
                "Managed LazyStaggeredGridState was not attached to the rendered grid.");

            Assert.AreEqual(LazyGridStateTestActivity.InitialIndex, state.FirstVisibleItemIndex);
            await state.ScrollToItemAsync(0);
            Assert.AreEqual(0, state.FirstVisibleItemIndex);
            await state.AnimateScrollToItemAsync(20);
            Assert.AreEqual(20, state.FirstVisibleItemIndex);
        }
        finally
        {
            await FinishActivity(activity);
        }
    }

    [TestMethod]
    public async Task StaticGrid_OmittedStateUsesKotlinDefault()
    {
        var activity = await StartActivity(LazyGridStateTestActivity.StaticDefaults);
        try
        {
            await WaitFor(
                () => LazyGridStateTestActivity.RenderedItems > 0,
                "The static lazy grid did not render with its state argument omitted.");
        }
        finally
        {
            await FinishActivity(activity);
        }
    }

    [TestMethod]
    public void ManagedCellStrategiesValidateDimensions()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => GridCells.Fixed(0));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => GridCells.Adaptive(Dp.Zero));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => StaggeredGridCells.Fixed(0));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => StaggeredGridCells.Adaptive(Dp.Zero));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => StaggeredGridCells.FixedSize(Dp.Zero));
    }

    [TestMethod]
    public async Task RememberHelpers_PreserveManagedIdentityAcrossRecomposition()
    {
        var activity = await StartActivity(LazyGridStateTestActivity.RememberIdentity);
        try
        {
            await WaitFor(
                () => LazyGridStateTestActivity.RecompositionTrigger is not null,
                "Remember helpers did not complete their first composition.");
            var firstGrid = LazyGridStateTestActivity.FirstRememberedGridState
                ?? throw new InvalidOperationException(
                    "RememberLazyGridState did not return a state.");
            var firstStaggered = LazyGridStateTestActivity.FirstRememberedStaggeredState
                ?? throw new InvalidOperationException(
                    "RememberLazyStaggeredGridState did not return a state.");
            var trigger = LazyGridStateTestActivity.RecompositionTrigger
                ?? throw new InvalidOperationException(
                    "Recomposition trigger was not created.");

            activity.RunOnUiThread(() => trigger.Value++);

            await WaitFor(
                () => LazyGridStateTestActivity.RememberCompositionCount >= 2
                    && ReferenceEquals(
                    firstGrid,
                    LazyGridStateTestActivity.LastRememberedGridState)
                    && ReferenceEquals(
                        firstStaggered,
                        LazyGridStateTestActivity.LastRememberedStaggeredState),
                "Managed lazy-grid state identity changed across recomposition.");
        }
        finally
        {
            await FinishActivity(activity);
        }
    }

    static async Task FinishActivity(LazyGridStateTestActivity activity)
    {
        activity.RunOnUiThread(activity.Finish);
        await WaitFor(
            static () => LazyGridStateTestActivity.Current is null,
            "Lazy-grid test activity did not finish.");
    }

    static async Task<LazyGridStateTestActivity> StartActivity(int scenario)
    {
        var context = global::Android.App.Application.Context
            ?? throw new InvalidOperationException(
                "Application.Context not set for lazy-grid tests.");
        LazyGridStateTestActivity.Reset(scenario);
        using var intent = new global::Android.Content.Intent(
            context,
            typeof(LazyGridStateTestActivity));
        intent.AddFlags(global::Android.Content.ActivityFlags.NewTask);
        context.StartActivity(intent);

        return await WaitFor(
            static () => LazyGridStateTestActivity.Current,
            static value => value is not null,
            "Lazy-grid test activity did not start.")
            ?? throw new InvalidOperationException(
                "Lazy-grid test activity was not available.");
    }

    static async Task WaitFor(Func<bool> predicate, string message)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(10);
        do
        {
            if (predicate())
                return;
            await Task.Delay(20);
        }
        while (DateTime.UtcNow < deadline);

        Assert.Fail(message);
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
