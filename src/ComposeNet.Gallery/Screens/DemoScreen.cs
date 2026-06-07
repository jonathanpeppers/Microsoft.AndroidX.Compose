using ComposeNet.Gallery.Registry;

namespace ComposeNet.Gallery.Screens;

/// <summary>
/// Single-demo screen — a header card summarising the demo plus the
/// demo's own composable tree (wrapped in <see cref="DemoContent"/> so
/// <c>Compose.Remember</c> calls inside the demo factory resolve
/// against the active composer).
/// </summary>
public static class DemoScreen
{
    /// <summary>
    /// Build the screen for <paramref name="demo"/>. The category
    /// label in the header is resolved via
    /// <see cref="Catalog.FindCategory(string?)"/>; an orphaned demo
    /// (category id not in the catalog) just omits the breadcrumb.
    /// </summary>
    public static ComposableNode Build(Demo demo)
    {
        var category = Catalog.FindCategory(demo.CategoryId);
        var breadcrumb = category is null
            ? demo.Title
            : $"{category.Glyph}  {category.Title}  ›  {demo.Title}";

        return new Column
        {
            Modifier.Companion.FillMaxSize(),

            // Header sits above the demo body so the demo doesn't have
            // to repeat its own description.
            new Surface
            {
                Modifier.Companion.FillMaxWidth(),
                new Column
                {
                    Modifier.Companion.Padding(16),
                    new Text(breadcrumb),
                    new Spacer { Modifier = Modifier.Companion.Height(4) },
                    new Text(demo.Description),
                },
            },
            new HorizontalDivider(),

            // Demo body. Wrapping in a Column with vertical scroll
            // would be tempting, but many demos need full available
            // height (e.g. LazyColumn, Carousels) — let the demo own
            // its own scrolling.
            new Box
            {
                Modifier.Companion.FillMaxSize().Padding(16),
                new DemoContent(demo.Build),
            },
        };
    }
}
