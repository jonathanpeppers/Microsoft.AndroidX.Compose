using Microsoft.AndroidX.Compose.Gallery.Registry;

namespace Microsoft.AndroidX.Compose.Gallery.Screens;

/// <summary>
/// The gallery's landing screen. Shows a welcome card followed by a
/// scrollable list of <see cref="Catalog.Categories"/>; tapping a
/// category navigates to <c>category/{id}</c>.
/// </summary>
public static class HomeScreen
{
    /// <summary>
    /// Build the home screen tree. <paramref name="nav"/> is invoked
    /// when the user taps a category row.
    /// </summary>
    public static ComposableNode Build(NavController nav) => new Column
    {
        Modifier.Companion.FillMaxSize().Padding(16),

        new Text("Welcome to .NET Compose Gallery"),
        new Text("Every facade — buttons, lists, dialogs, navigation, animation — laid out by category. Tap a row to jump in, or use the search action in the top bar to look one up."),
        new Spacer { Modifier = Modifier.Companion.Height(16) },

        new LazyColumn<Category>(
            items:       Catalog.Categories.ToList(),
            itemContent: cat => new CategoryRow(cat, nav))
        {
            Modifier = Modifier.Companion.FillMaxSize(),
        },
    };
}

