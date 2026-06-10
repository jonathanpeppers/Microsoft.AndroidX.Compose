using AndroidX.Compose.Gallery.Registry;

namespace AndroidX.Compose.Gallery.Demos.Navigation;

/// <summary>NavHost demo — three routes including <c>user/{id}</c> with an argument.</summary>
public static class NavHostRouteArgsDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "navigation-navhost",
        CategoryId:  "navigation",
        Title:       "NavHost — route arguments",
        Description: "Three routes; user/{id} reads its parameter from NavBackStackEntry.Arguments.",
        Build:       c =>
        {
            var nav = c.Remember(() => new NavController());
            return new Column
            {
                new Text("Tap to navigate. Up uses navController.NavigateUp()."),
                new NavHost(startDestination: "home", navController: nav)
                {
                    Modifier.Companion.FillMaxWidth().Height(360),

                    new NavDestination("home")
                    {
                        new Column
                        {
                            Modifier.Companion.Padding(16),
                            new Text("🏠 Home"),
                            new Button(onClick: () => nav.Navigate("detail")) { new Text("Go to detail") },
                            new Button(onClick: () => nav.Navigate("user/42")) { new Text("Open user 42") },
                        },
                    },
                    new NavDestination("detail")
                    {
                        new Column
                        {
                            Modifier.Companion.Padding(16),
                            new Text("📄 Detail"),
                            new Button(onClick: () => nav.Navigate("user/7")) { new Text("Drill down to user 7") },
                            new Button(onClick: () => nav.PopBackStack()) { new Text("Back") },
                        },
                    },
                    new NavDestination("user/{id}", entry => new Column
                    {
                        Modifier.Companion.Padding(16),
                        new Text($"👤 User #{entry.Arguments?.GetString("id") ?? "?"}"),
                        new Text($"Route: {entry.Route ?? "(unknown)"}"),
                        new Button(onClick: () => nav.NavigateUp()) { new Text("Up") },
                    }),
                },
            };
        });
}
