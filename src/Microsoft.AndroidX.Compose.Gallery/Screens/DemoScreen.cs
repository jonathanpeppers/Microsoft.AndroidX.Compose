using AndroidX.Compose.Runtime;
using AndroidX.Compose.Gallery.Registry;

namespace AndroidX.Compose.Gallery.Screens;

/// <summary>
/// Single-demo screen — a header card summarising the demo plus the
/// demo's own composable tree (wrapped in <see cref="DemoContent"/> so
/// <c>composer.Remember</c> calls inside the demo factory resolve
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
            Modifier.FillMaxSize(),

            // Header sits above the demo body so the demo doesn't have
            // to repeat its own description.
            new Surface
            {
                Modifier.FillMaxWidth(),
                new Column
                {
                    Modifier.Padding(16),
                    new Text(breadcrumb),
                    new Spacer { Modifier = Modifier.Height(4) },
                    new Text(demo.Description),
                },
            },
            new HorizontalDivider(),

            // Vertical scroll wrapper around the demo body so demos that
            // are taller than the viewport stay reachable, with consistent
            // gutter padding across the catalog. Demos that need full
            // height (LazyColumn, Carousels) supply their own Modifier.
            new ScrollableDemoBody(demo.Build),
        };
    }

    sealed class ScrollableDemoBody : ComposableNode
    {
        readonly Func<IComposer, ComposableNode> _build;

        public ScrollableDemoBody(Func<IComposer, ComposableNode> build) => _build = build;

        public override void Render(IComposer composer)
        {
            var scroll = composer.Remember(() => new ScrollState());
            new Column(verticalArrangement: Arrangement.SpacedBy(12.Dp()))
            {
                Modifier
                    .FillMaxSize()
                    .VerticalScroll(scroll)
                    .Padding(16),
                new DemoContent(_build),
            }.Render(composer);
        }
    }
}
