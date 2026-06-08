using System.Linq;
using ComposeNet.Gallery.Registry;

namespace ComposeNet.Gallery.Demos.AppBars;

/// <summary>
/// <see cref="TopAppBarDefaults.PinnedScrollBehavior(TopAppBarState, int, string)"/>
/// paired with a long <c>LazyColumn</c>. The bar stays in place but
/// elevates its surface as the list scrolls underneath it.
/// </summary>
public static class PinnedScrollBehaviorDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "appbars-pinned-scroll-behavior",
        CategoryId:  "app-bars-tabs",
        Title:       "TopAppBar — pinned scroll behavior",
        Description: "Bar stays pinned but elevates as the LazyColumn scrolls underneath.",
        Build:       () =>
        {
            var state    = Compose.Remember(() => new TopAppBarState());
            var behavior = TopAppBarDefaults.PinnedScrollBehavior(state);
            return new Column
            {
                Modifier.Companion.Height(420),

                new TopAppBar
                {
                    Title          = new Text("Pinned bar"),
                    NavigationIcon = new IconButton(onClick: () => { }) { new Text("☰") },
                    Actions        = new Row
                    {
                        new IconButton(onClick: () => { }) { new Text("⋮") },
                    },
                    ScrollBehavior = behavior,
                },
                new LazyColumn<int>(
                    items:       Enumerable.Range(0, 100).ToList(),
                    itemContent: i => new Text($"Row {i:D3}"))
                {
                    Modifier = Modifier.Companion
                        .FillMaxSize()
                        .NestedScroll(behavior.NestedScrollConnection),
                },
            };
        });
}
