using AndroidX.Compose.Runtime;
using ComposeNet;

namespace ComposeNet.Samples.JetNews;

/// <summary>
/// Builds the JetNews navigation drawer — a logo header plus two
/// top-level destination rows (Home, Interests). Mirrors upstream's
/// <c>AppDrawer</c>.
/// </summary>
/// <remarks>
/// <para>
/// Tapping a row navigates to that route AND updates the
/// <see cref="MutableState{T}"/> tracking the active route so the row
/// can highlight itself.
/// </para>
/// <para>
/// The drawer doesn't auto-close on selection: programmatic
/// <c>DrawerState.Close()</c> hasn't been bridged yet (the
/// <c>SuspendBridge</c> plumbing for it would mirror what <c>ScrollState</c>
/// already does). Users can swipe the drawer closed by hand —
/// functionality, not finesse.
/// </para>
/// </remarks>
public static class JetnewsDrawer
{
    static readonly Color SelectedColor = Color.FromHex("#D0E4FF");

    /// <summary>Materialize the drawer sheet.</summary>
    public static ModalDrawerSheet Build(NavController nav, MutableState<string> currentRoute) =>
        new()
        {
            new Column
            {
                Modifier.Companion.FillMaxWidth(),
                BuildHeader(),
                new HorizontalDivider(),
                BuildItem(
                    label:     "Home",
                    iconRes:   Resource.Drawable.ic_home,
                    route:     Routes.Home,
                    nav:       nav,
                    currentRoute: currentRoute),
                BuildItem(
                    label:     "Interests",
                    iconRes:   Resource.Drawable.ic_interests,
                    route:     Routes.Interests,
                    nav:       nav,
                    currentRoute: currentRoute),
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
                         NavController nav, MutableState<string> currentRoute)
    {
        bool selected = currentRoute.Value == route;

        var modifier = Modifier.Companion
            .FillMaxWidth()
            .Height(56)
            .Padding(horizontal: 12, vertical: 4)
            .Clip(28)
            .Clickable(() =>
            {
                if (currentRoute.Value == route) return;
                currentRoute.Value = route;
                nav.Navigate(route);
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
