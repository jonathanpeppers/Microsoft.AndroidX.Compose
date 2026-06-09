using Android.OS;
using AndroidX.Compose.Gallery.Registry;

namespace AndroidX.Compose.Gallery.Demos.ListsGrids;

/// <summary>LazyColumn with 1000 rows inside a PullToRefreshBox.</summary>
public static class LazyColumnLongDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "lists-lazy-column-long",
        CategoryId:  "lists-grids",
        Title:       "LazyColumn — 1000 rows + pull-to-refresh",
        Description: "Only the visible window is composed; PullToRefreshBox surfaces the Material 3 pull gesture.",
        Build:       c =>
        {
            var refreshing  = c.Remember(() => new MutableState<bool>(false));
            var refreshTick = c.Remember(() => new MutableNumberState<int>(0));
            return new Column
            {
                new Text($"LazyColumn (1000 rows) — pull to refresh, rev {refreshTick}"),
                new PullToRefreshBox(
                    isRefreshing: refreshing.Value,
                    onRefresh:    () =>
                    {
                        refreshing.Value = true;
                        new Handler(Looper.MainLooper!).PostDelayed(() =>
                        {
                            refreshTick.Value++;
                            refreshing.Value = false;
                        }, 1200);
                    })
                {
                    Modifier.Companion.FillMaxWidth().Height(320),

                    new LazyColumn<int>(
                        items:       Enumerable.Range(0, 1000).ToList(),
                        itemContent: i => new Text($"Row {i:D4} (rev {refreshTick})"))
                    {
                        Modifier = Modifier.Companion.FillMaxSize(),
                    },
                },
            };
        });
}
