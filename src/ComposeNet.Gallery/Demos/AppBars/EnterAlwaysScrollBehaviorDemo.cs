using AndroidX.Compose.Material3;
using ComposeNet.Gallery.Registry;

namespace ComposeNet.Gallery.Demos.AppBars;

/// <summary>
/// <see cref="TopAppBarDefaults.EnterAlwaysScrollBehavior(TopAppBarState, int, string)"/>
/// paired with a long <c>LazyColumn</c>. The bar collapses out of
/// view as the list scrolls up and immediately re-enters the moment
/// the user starts to scroll back down.
/// </summary>
public static class EnterAlwaysScrollBehaviorDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "appbars-enter-always-scroll-behavior",
        CategoryId:  "app-bars-tabs",
        Title:       "TopAppBar — enter-always scroll behavior",
        Description: "Bar collapses on scroll up and snaps back the moment the user scrolls down.",
        Build:       () =>
        {
            var state    = TopAppBarDefaults.RememberTopAppBarState();
            var behavior = TopAppBarDefaults.EnterAlwaysScrollBehavior(state);
            return new Column
            {
                Modifier.Companion.Height(420),

                new TopAppBar
                {
                    Title          = new Text("Enter-always bar"),
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
