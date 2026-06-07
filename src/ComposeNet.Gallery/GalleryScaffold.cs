using AndroidX.Compose.Runtime;
using ComposeNet.Gallery.Registry;
using ComposeNet.Gallery.Screens;

namespace ComposeNet.Gallery;

/// <summary>
/// The <see cref="Scaffold"/> that wraps the gallery's
/// <see cref="NavHost"/>. Hosts the top app bar (hamburger + title +
/// search action) above the routed body.
/// </summary>
public sealed class GalleryScaffold : ComposableNode
{
    readonly NavController _nav;
    readonly DrawerStateHolder _drawer;

    /// <summary>Construct the scaffold bound to <paramref name="nav"/> + <paramref name="drawer"/>.</summary>
    public GalleryScaffold(NavController nav, DrawerStateHolder drawer)
    {
        _nav    = nav;
        _drawer = drawer;
    }

    public override void Render(IComposer composer)
    {
        new Scaffold
        {
            TopBar = new CenterAlignedTopAppBar
            {
                Title = new Text("ComposeNet Gallery"),
                NavigationIcon = new IconButton(onClick: () => _ = _drawer.OpenAsync())
                {
                    new Text("☰"),
                },
                Actions = new Row
                {
                    new IconButton(onClick: () => _nav.Navigate("search"))
                    {
                        new Text("🔍"),
                    },
                },
            },
            Body = BuildNavHost(),
        }.Render(composer);
    }

    NavHost BuildNavHost()
    {
        var host = new NavHost(startDestination: "home", navController: _nav)
        {
            new Composable("home")
            {
                HomeScreen.Build(_nav),
            },
            new Composable("category/{id}", entry =>
            {
                var id = entry.Arguments?.GetString("id");
                var category = Catalog.FindCategory(id);
                return category is null
                    ? MissingScreen($"Unknown category id '{id}'.")
                    : CategoryScreen.Build(category, _nav);
            }),
            new Composable("demo/{id}", entry =>
            {
                var id = entry.Arguments?.GetString("id");
                var demo = Catalog.FindDemo(id);
                return demo is null
                    ? MissingScreen($"Unknown demo id '{id}'.")
                    : DemoScreen.Build(demo);
            }),
            new Composable("search")
            {
                new SearchScreen(_nav),
            },
        };
        return host;
    }

    static ComposableNode MissingScreen(string message) => new Column
    {
        Modifier.Companion.FillMaxSize().Padding(24),
        new Text("404 — not found"),
        new Spacer { Modifier = Modifier.Companion.Height(8) },
        new Text(message),
    };
}
