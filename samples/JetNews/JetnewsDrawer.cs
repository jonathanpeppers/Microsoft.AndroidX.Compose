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
    public static ComposableNode Build(
        NavController        nav,
        MutableState<string> currentRoute,
        DrawerStateHolder    drawerState) =>
        new Composed(c =>
        {
            var typography = c.Typography();
            string home = c.StringResource(Resource.String.home_title);
            string interests = c.StringResource(Resource.String.interests_title);
            string navigateHome = c.StringResource(Resource.String.cd_navigate_home);
            string navigateInterests = c.StringResource(Resource.String.cd_navigate_interests);
            string logo = c.StringResource(Resource.String.drawer_logo);
            string appName = c.StringResource(Resource.String.app_name);
            return new ModalDrawerSheet
            {
                new Column
                {
                    Modifier.FillMaxWidth(),
                    BuildHeader(logo, appName),
                    BuildItem(
                        label:        home,
                        description:  navigateHome,
                        iconRes:      Resource.Drawable.ic_home,
                        route:        Routes.Home,
                        nav:          nav,
                        currentRoute: currentRoute,
                        drawerState:  drawerState,
                        typography:   typography),
                    BuildItem(
                        label:        interests,
                        description:  navigateInterests,
                        iconRes:      Resource.Drawable.ic_interests,
                        route:        Routes.Interests,
                        nav:          nav,
                        currentRoute: currentRoute,
                        drawerState:  drawerState,
                        typography:   typography),
                },
            };
        });

    static Row BuildHeader(string logo, string appName) =>
        new()
        {
            Modifier.FillMaxWidth().Padding(horizontal: 28, vertical: 24),
            new Icon(Resource.Drawable.ic_jetnews_logo, logo),
            Spacer.Width(8),
            new Icon(Resource.Drawable.ic_jetnews_wordmark, appName),
        };

    static NavigationDrawerItem BuildItem(string label, string description, int iconRes, string route,
                                          NavController nav, MutableState<string> currentRoute,
                                          DrawerStateHolder drawerState,
                                          AndroidX.Compose.Material3.Typography typography)
    {
        bool selected = currentRoute.Value == route;
        var labelText = new Text(label).WithTypography(typography.LabelLarge);
        if (selected)
            labelText.FontWeight = FontWeight.SemiBold;

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
            Modifier = Modifier.Padding(horizontal: 12),
            Label = labelText,
            Icon = new Icon(iconRes, description),
        };
    }
}
