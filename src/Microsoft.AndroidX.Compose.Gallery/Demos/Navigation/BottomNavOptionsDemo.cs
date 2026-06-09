using AndroidX.Compose.Runtime;
using AndroidX.Compose.Gallery.Registry;

namespace AndroidX.Compose.Gallery.Demos.Navigation;

/// <summary>
/// Bottom-navigation demo that exercises the
/// <see cref="NavOptions"/> overload of
/// <see cref="NavController.Navigate(string, NavOptions)"/>.
/// Three top-level tabs — Home, Search, Profile — share a single
/// <see cref="NavController"/>; each tab tap pops back to the start
/// destination with <c>saveState = true</c>, launches single-top, and
/// restores any state previously saved for the target tab.
///
/// <para>
/// Per-tab counters use <see cref="ComposeExtensions.RememberSaveable{T}(Func{T}, int, string)"/>
/// so a reviewer can prove the saved-state round-trip: tap Increment
/// a few times, switch tabs, come back — the count should still be
/// there.
/// </para>
/// </summary>
public static class BottomNavOptionsDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "navigation-bottom-nav-options",
        CategoryId:  "navigation",
        Title:       "NavOptions — bottom nav (popUpTo + saveState)",
        Description: "Three tabs share one NavController; tapping pops to start with saveState/restoreState so per-tab counters survive switches.",
        Build:       c =>
        {
            const string startRoute = "home";

            var nav      = c.Remember(() => new NavController());
            var selected = c.Remember(() => new MutableState<string>(startRoute));

            return new Column
            {
                Modifier.Companion.FillMaxWidth(),

                new Text("Tap Search or Profile, hit Increment a few times, switch to another tab and come back — the counter survives via popUpTo(saveState) + restoreState. (Selection follows taps; back-stack changes via the system Back button don't update the highlight in this v1 demo.)"),

                // Bounded NavHost so the inner subcomposition has a
                // finite slot to lay out into; the NavigationBar below
                // it picks up its natural M3 height.
                new Box
                {
                    Modifier.Companion.FillMaxWidth().Height(260),
                    new NavHost(startDestination: startRoute, navController: nav)
                    {
                        new Composable(startRoute,    _ => new CounterPane("🏠 Home")),
                        new Composable("search",      _ => new CounterPane("🔍 Search")),
                        new Composable("profile",     _ => new CounterPane("👤 Profile")),
                    },
                },

                new NavigationBar
                {
                    new NavigationBarItem(
                        selected: selected.Value == startRoute,
                        onClick:  () => GoToTab(nav, selected, startRoute, startRoute))
                    {
                        Icon  = new Text("🏠"),
                        Label = new Text("Home"),
                    },
                    new NavigationBarItem(
                        selected: selected.Value == "search",
                        onClick:  () => GoToTab(nav, selected, "search", startRoute))
                    {
                        Icon  = new Text("🔍"),
                        Label = new Text("Search"),
                    },
                    new NavigationBarItem(
                        selected: selected.Value == "profile",
                        onClick:  () => GoToTab(nav, selected, "profile", startRoute))
                    {
                        Icon  = new Text("👤"),
                        Label = new Text("Profile"),
                    },
                },
            };
        });

    static void GoToTab(NavController nav, MutableState<string> selected, string route, string startRoute)
    {
        selected.Value = route;
        nav.Navigate(route, new NavOptions
        {
            PopUpToRoute     = startRoute,
            PopUpToSaveState = true,
            LaunchSingleTop  = true,
            RestoreState     = true,
        });
    }

    // Private per-destination node — needs to be a ComposableNode (not
    // an inline tree built in the demo's Build lambda) so the
    // RememberSaveable call happens INSIDE the destination's own
    // subcomposition. That's what scopes the saved counter to the
    // NavBackStackEntry rather than the outer demo screen, so the
    // saveState/restoreState round-trip actually has work to do.
    sealed class CounterPane : ComposableNode
    {
        readonly string _title;

        public CounterPane(string title) => _title = title;

        public override void Render(IComposer composer)
        {
            var count = composer.RememberSaveable(() => new MutableNumberState<int>(0));

            new Column(verticalArrangement: Arrangement.SpacedBy(8))
            {
                Modifier.Companion.Padding(16),
                new Text(_title),
                new Text($"Tapped {count.Value} times"),
                new Button(onClick: () => count++)
                {
                    new Text("Increment"),
                },
            }.Render(composer);
        }
    }
}
