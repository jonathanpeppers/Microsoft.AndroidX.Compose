using ComposeNet.Gallery.Registry;

namespace ComposeNet.Gallery.Screens;

/// <summary>
/// Lists every demo in a single category. Empty categories render a
/// friendly placeholder explaining the slot exists but is not wired up
/// yet — gives users (and contributors) a clear "what's missing" hint.
/// </summary>
public static class CategoryScreen
{
    /// <summary>
    /// Build the screen for <paramref name="category"/>. Taps on a
    /// demo row call <paramref name="nav"/> with <c>demo/{id}</c>.
    /// </summary>
    public static ComposableNode Build(Category category, NavController nav)
    {
        var demos = Catalog.DemosByCategory(category.Id).ToList();
        if (demos.Count == 0)
        {
            return new Column
            {
                Modifier.Companion.FillMaxSize().Padding(24),
                new Text(category.Title),
                new Spacer { Modifier = Modifier.Companion.Height(8) },
                new Text("No demos in this category yet."),
                new Text("(See README.md for the porting status.)"),
            };
        }

        return new LazyColumn<Demo>(
            items:       demos,
            itemContent: d => new DemoRow(d, nav))
        {
            Modifier = Modifier.Companion.FillMaxSize(),
        };
    }
}

