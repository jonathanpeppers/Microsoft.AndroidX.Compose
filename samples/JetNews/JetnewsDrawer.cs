using AndroidX.Compose.Runtime;
using ComposeNet;

namespace ComposeNet.Samples.JetNews;

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
    static readonly Color SelectedColor = Color.FromHex("#D0E4FF");

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
                new HorizontalDivider(),
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
            Modifier.Companion.FillMaxWidth().Padding(16),
            new Icon(Resource.Drawable.ic_jetnews_logo, "JetNews logo")
            {
                Modifier = Modifier.Companion.Size(28),
            },
            new Spacer(Modifier.Companion.Width(12)),
            new Text("JetNews")
            {
                FontSize   = 22,
                FontWeight = FontWeight.SemiBold,
            },
        };

    static Row BuildItem(string label, int iconRes, string route,
                         NavController nav, MutableState<string> currentRoute,
                         DrawerStateHolder drawerState)
    {
        bool selected = currentRoute.Value == route;

        var modifier = Modifier.Companion
            .FillMaxWidth()
            .Height(56)
            .Padding(horizontal: 12, vertical: 4)
            .Clip(28)
            .Clickable(() =>
            {
                if (currentRoute.Value != route)
                {
                    currentRoute.Value = route;
                    nav.Navigate(route);
                }
                // Fire-and-forget close even on a re-tap of the
                // already-active item — matches upstream behaviour
                // and Jetchat's drawer.
                _ = drawerState.CloseAsync();
            });
        if (selected)
            modifier = modifier.Background(SelectedColor);

        return new Row
        {
            modifier,
            new Icon(iconRes, label)
            {
                Modifier = Modifier.Companion.Padding(16),
            },
            new Spacer(Modifier.Companion.Width(12)),
            new Text(label)
            {
                FontSize   = 16,
                FontWeight = selected ? FontWeight.SemiBold : FontWeight.Normal,
                Modifier   = Modifier.Companion.Padding(top: 16, bottom: 16, start: 0, end: 0),
            },
        };
    }
}
