using AndroidX.Compose.Runtime;
using AndroidX.Compose.Gallery.Registry;
using AndroidX.Compose.Gallery.Screens;

namespace AndroidX.Compose.Gallery;

/// <summary>
/// The <see cref="Scaffold"/> that wraps the gallery's
/// <see cref="NavHost"/>. Hosts the top app bar (hamburger + title +
/// search action) above the routed body. When the back stack is not
/// at <c>home</c>, the hamburger is replaced with a back arrow that
/// calls <see cref="NavController.NavigateUp"/>.
/// </summary>
public sealed class GalleryScaffold : ComposableNode
{
    readonly NavController _nav;
    readonly DrawerStateHolder _drawer;
    readonly MutableState<string> _currentRoute;

    /// <summary>Construct the scaffold bound to <paramref name="nav"/> + <paramref name="drawer"/>.</summary>
    /// <param name="nav">The shared <see cref="NavController"/>.</param>
    /// <param name="drawer">The shared <see cref="DrawerStateHolder"/>.</param>
    /// <param name="currentRoute">
    /// The shared route <see cref="MutableState{T}"/> owned by
    /// <see cref="GalleryApp"/>. Each route's body updates it in a
    /// <see cref="DisposableEffect"/>; the top app bar reads it to
    /// decide between hamburger (home) and back arrow (anywhere else),
    /// and the surrounding drawer reads it to toggle the edge-swipe
    /// gesture.
    /// </param>
    public GalleryScaffold(NavController nav, DrawerStateHolder drawer, MutableState<string> currentRoute)
    {
        _nav          = nav;
        _drawer       = drawer;
        _currentRoute = currentRoute;
    }

    public override void Render(IComposer composer)
    {
        var atRoot = _currentRoute.Value == "home";

        new Scaffold
        {
            TopBar = new CenterAlignedTopAppBar
            {
                Title = new Text(TitleFor(_currentRoute.Value)),
                NavigationIcon = atRoot
                    ? new IconButton(onClick: () => _ = _drawer.OpenAsync())
                    {
                        new Text("☰"),
                    }
                    : new IconButton(onClick: () => _nav.NavigateUp())
                    {
                        new Text("←"),
                    },
                Actions = new Row
                {
                    new IconButton(onClick: () => _nav.Navigate("search"))
                    {
                        new Text("🔍"),
                    },
                },
            },
            Body = BuildNavHost(_currentRoute),
        }.Render(composer);
    }

    static string TitleFor(string route) => route switch
    {
        "home"   => ".NET Compose Gallery",
        "search" => "Search",
        var r when r.StartsWith("category/") => Catalog.FindCategory(r["category/".Length..])?.Title ?? "Category",
        var r when r.StartsWith("demo/")     => Catalog.FindDemo(r["demo/".Length..])?.Title ?? "Demo",
        _ => ".NET Compose Gallery",
    };

    NavHost BuildNavHost(MutableState<string> currentRoute)
    {
        var host = new NavHost(startDestination: "home", navController: _nav)
        {
            new Composable("home", _ => new Column
            {
                new DisposableEffect("home", _ => { currentRoute.Value = "home"; return () => { }; }),
                HomeScreen.Build(_nav),
            }),
            new Composable("category/{id}", entry =>
            {
                var id = entry.Arguments?.GetString("id");
                var category = Catalog.FindCategory(id);
                return new Column
                {
                    new DisposableEffect($"category/{id}", _ =>
                    {
                        currentRoute.Value = $"category/{id}";
                        return () => { };
                    }),
                    category is null
                        ? MissingScreen($"Unknown category id '{id}'.")
                        : CategoryScreen.Build(category, _nav),
                };
            }),
            new Composable("demo/{id}", entry =>
            {
                var id = entry.Arguments?.GetString("id");
                var demo = Catalog.FindDemo(id);
                return new Column
                {
                    new DisposableEffect($"demo/{id}", _ =>
                    {
                        currentRoute.Value = $"demo/{id}";
                        return () => { };
                    }),
                    demo is null
                        ? MissingScreen($"Unknown demo id '{id}'.")
                        : DemoScreen.Build(demo),
                };
            }),
            new Composable("search", _ => new Column
            {
                new DisposableEffect("search", _ => { currentRoute.Value = "search"; return () => { }; }),
                new SearchScreen(_nav),
            }),
        };
        return host;
    }

    static ComposableNode MissingScreen(string message) => new Column
    {
        Modifier.FillMaxSize().Padding(24),
        new Text("404 — not found"),
        new Spacer { Modifier = Modifier.Height(8) },
        new Text(message),
    };
}
