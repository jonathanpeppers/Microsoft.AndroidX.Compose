using Android.OS;
using AndroidX.Compose.Gallery.Registry;

namespace AndroidX.Compose.Gallery.Demos.ListsGrids;

/// <summary>PullToRefreshBox — Material 3 pull-down gesture over any scrollable child.</summary>
public static class PullToRefreshBoxDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "lists-pull-to-refresh",
        CategoryId:  "lists-grids",
        Title:       "PullToRefreshBox",
        Description: "Pull gesture plus programmatic threshold, overshoot, and hide controls.",
        Build:       c =>
        {
            var refreshing = c.MutableStateOf(false);
            var revision   = c.MutableStateOf(0);
            var state = c.Remember(() => new PullToRefreshState());
            return new Column
            {
                new Row(horizontalArrangement: Arrangement.SpacedBy(8.Dp()))
                {
                    new Button(() => _ = state.AnimateToThresholdAsync())
                    {
                        new Text("Threshold"),
                    },
                    new Button(() => _ = state.SnapToAsync(1.25f))
                    {
                        new Text("Overshoot"),
                    },
                    new Button(() => _ = state.AnimateToHiddenAsync())
                    {
                        new Text("Hide"),
                    },
                },
                new Text($"Distance: {state.DistanceFraction:F2}; animating: {state.IsAnimating}"),
                new PullToRefreshBox(
                    isRefreshing: refreshing.Value,
                    onRefresh:    () =>
                    {
                        refreshing.Value = true;
                        var looper = Looper.MainLooper
                            ?? throw new InvalidOperationException(
                                "Main looper was unavailable.");
                        new Handler(looper).PostDelayed(() =>
                        {
                            revision.Value++;
                            refreshing.Value = false;
                        }, 1200);
                    },
                    state: state)
                {
                    Modifier.FillMaxWidth().Height(320),
                    new LazyColumn<int>(
                        items:       Enumerable.Range(0, 40).ToList(),
                        itemContent: i => new Text($"Item {i:D2} (rev {revision})"))
                    {
                        Modifier = Modifier.FillMaxSize(),
                    },
                },
            };
        });
}
