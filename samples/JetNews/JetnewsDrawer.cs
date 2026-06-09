namespace AndroidX.Compose.Samples.JetNews;

/// <summary>
/// Builds the JetNews navigation drawer — a logo header plus two
/// top-level destination rows (Home, Interests). Mirrors upstream's
/// <c>AppDrawer</c>.
/// </summary>
/// <remarks>
/// Tapping a row navigates to that route, updates the
/// <see cref="MutableState{T}"/> tracking the active route so the row
/// can highlight itself, then fires
/// <see cref="DrawerStateHolder.CloseAsync"/> so the drawer slides
/// shut behind the navigation — same fire-and-forget pattern Jetchat
/// uses for its hamburger menu.
/// </remarks>
public static class JetnewsDrawer
{
    /// <summary>Materialize the drawer sheet.</summary>
    public static ModalDrawerSheet Build(
        NavController        nav,
        MutableState<string> currentRoute,
        DrawerStateHolder    drawerState) =>
        new()
        {
            new Column
            {
                Modifier.Companion.FillMaxWidth(),
                BuildHeader(),
                BuildItem(
                    label:        "Home",
                    iconRes:      Resource.Drawable.ic_home,
                    route:        Routes.Home,
                    nav:          nav,
                    currentRoute: currentRoute,
                    drawerState:  drawerState),
                BuildItem(
                    label:        "Interests",
                    iconRes:      Resource.Drawable.ic_interests,
                    route:        Routes.Interests,
                    nav:          nav,
                    currentRoute: currentRoute,
                    drawerState:  drawerState),
            },
        };

    static Row BuildHeader() =>
        new()
        {
            Modifier.Companion.FillMaxWidth().Padding(horizontal: 28, vertical: 24),
            new Icon(Resource.Drawable.ic_jetnews_logo, "JetNews logo"),
            new Spacer(Modifier.Companion.Width(8)),
            new Icon(Resource.Drawable.ic_jetnews_wordmark, "JetNews"),
        };

    static NavigationDrawerItem BuildItem(string label, int iconRes, string route,
                                          NavController nav, MutableState<string> currentRoute,
                                          DrawerStateHolder drawerState)
    {
        bool selected = currentRoute.Value == route;
        return new NavigationDrawerItem(
            selected: selected,
            onClick:  () =>
            {
                if (currentRoute.Value != route)
                {
                    currentRoute.Value = route;
                    nav.Navigate(route);
                }
                _ = drawerState.CloseAsync();
            })
        {
            Modifier = Modifier.Companion.Padding(horizontal: 12, vertical: 0),
            Label    = new Text(label)
            {
                FontSize   = 16,
                FontWeight = selected ? FontWeight.SemiBold : FontWeight.Normal,
            },
            Icon = new Icon(iconRes, label),
        };
    }
}
