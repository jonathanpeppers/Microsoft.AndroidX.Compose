using System.Collections.Generic;
using System.Linq;

namespace ComposeNet.Gallery.Registry;

/// <summary>
/// Global catalog: every <see cref="Demo"/> in the gallery and every
/// <see cref="Category"/> they belong to. Adding a new demo is two
/// edits — a new file under <c>Demos/&lt;Category&gt;/</c> and a new
/// line in <see cref="Demos"/>.
/// </summary>
public static class Catalog
{
    /// <summary>
    /// All categories, in the order they appear on the home screen
    /// and in the navigation drawer. Order matters; reordering here
    /// reorders the UI.
    /// </summary>
    public static readonly IReadOnlyList<Category> Categories =
    [
        new("text-inputs",         "Text & inputs",          "Text styling, TextField variants",             "✏️"),
        new("buttons",             "Buttons",                "Filled, icon, chip, FAB, tooltip",             "🔘"),
        new("selection",           "Selection",              "Checkbox, switch, slider, segmented",          "☑️"),
        new("containers",          "Containers",             "Card, Surface, Box, Column, Row, Flow",        "📦"),
        new("lists-grids",         "Lists & grids",          "LazyColumn, LazyGrid, PullToRefresh",          "📋"),
        new("carousels-paging",    "Carousels & paging",     "HorizontalPager, M3 carousels",                "🎠"),
        new("app-bars-tabs",       "App bars & tabs",        "TopAppBar variants, BottomAppBar, TabRow",     "🧭"),
        new("navigation",          "Navigation",             "NavHost, drawers, rails",                      "🗺️"),
        new("dialogs-sheets",      "Dialogs & sheets",       "AlertDialog, ModalSheet, pickers, menus",      "💬"),
        new("search",              "Search",                 "SearchBar, ExpandedFullScreen, Docked",        "🔍"),
        new("modifiers",           "Modifiers",              "Shapes, transforms, gestures, semantics",      "🎛️"),
        new("state-effects",       "State, effects, anim.",  "Remember, side effects, animation",            "✨"),
        new("locals-misc",         "CompositionLocal & misc","Locals, progress, image, icon",                "🧩"),
    ];

    /// <summary>
    /// All registered demos. Order within each category is preserved
    /// when rendered on the category screen — keep related demos
    /// adjacent.
    /// </summary>
    public static readonly IReadOnlyList<Demo> Demos =
    [
        // Demos register themselves here as they're ported. Empty
        // categories render an explanatory placeholder on the
        // category screen.
    ];

    /// <summary>
    /// Look up a category by <see cref="Category.Id"/>. Returns
    /// <c>null</c> when no such category exists (e.g. a stale deep
    /// link).
    /// </summary>
    public static Category? FindCategory(string? id) =>
        id is null ? null : Categories.FirstOrDefault(c => c.Id == id);

    /// <summary>
    /// Look up a demo by <see cref="Demo.Id"/>. Returns <c>null</c>
    /// when no such demo exists.
    /// </summary>
    public static Demo? FindDemo(string? id) =>
        id is null ? null : Demos.FirstOrDefault(d => d.Id == id);

    /// <summary>
    /// Every demo declared with <see cref="Demo.CategoryId"/> equal to
    /// <paramref name="categoryId"/>, in registration order.
    /// </summary>
    public static IEnumerable<Demo> DemosByCategory(string categoryId) =>
        Demos.Where(d => d.CategoryId == categoryId);

    /// <summary>
    /// Case-insensitive <c>Contains</c> match across each demo's
    /// title and description plus its parent category title. An
    /// empty / whitespace-only query returns every demo.
    /// </summary>
    public static IEnumerable<Demo> Search(string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return Demos;

        var needle = query.Trim();
        return Demos.Where(d =>
            d.Title.Contains(needle, System.StringComparison.OrdinalIgnoreCase) ||
            d.Description.Contains(needle, System.StringComparison.OrdinalIgnoreCase) ||
            (FindCategory(d.CategoryId)?.Title.Contains(needle, System.StringComparison.OrdinalIgnoreCase) ?? false));
    }
}
