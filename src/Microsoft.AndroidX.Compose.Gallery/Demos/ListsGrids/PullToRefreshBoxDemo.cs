using global::Android.OS;
using Microsoft.AndroidX.Compose.Gallery.Registry;

namespace Microsoft.AndroidX.Compose.Gallery.Demos.ListsGrids;

/// <summary>PullToRefreshBox — Material 3 pull-down gesture over any scrollable child.</summary>
public static class PullToRefreshBoxDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "lists-pull-to-refresh",
        CategoryId:  "lists-grids",
        Title:       "PullToRefreshBox",
        Description: "Wraps a scrollable child and surfaces the M3 pull gesture; faked 1.2s reload.",
        Build:       c =>
        {
            var refreshing = c.Remember(() => new MutableState<bool>(false));
            var revision   = c.Remember(() => new MutableNumberState<int>(0));
            return new PullToRefreshBox(
                isRefreshing: refreshing.Value,
                onRefresh:    () =>
                {
                    refreshing.Value = true;
                    new Handler(Looper.MainLooper!).PostDelayed(() =>
                    {
                        revision.Value++;
                        refreshing.Value = false;
                    }, 1200);
                })
            {
                Modifier.Companion.FillMaxWidth().Height(320),

                new LazyColumn<int>(
                    items:       Enumerable.Range(0, 40).ToList(),
                    itemContent: i => new Text($"Item {i:D2} (rev {revision})"))
                {
                    Modifier = Modifier.Companion.FillMaxSize(),
                },
            };
        });
}
