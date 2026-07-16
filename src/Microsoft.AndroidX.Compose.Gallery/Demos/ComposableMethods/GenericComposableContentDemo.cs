using AndroidX.Compose.Gallery.Registry;
using static AndroidX.Compose.Composables;

namespace AndroidX.Compose.Gallery.Demos.ComposableMethods;

/// <summary>Exercises generic composable-method lowering for animation, pagers, and carousels.</summary>
public static class GenericComposableContentDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "composable-generic-content",
        CategoryId:  "composable-methods",
        Title:       "Generic content adapters",
        Description: "Animated content, pagers, and carousels through generic composerless APIs.",
        Build:       static _ => new ComposableDemoAdapter(() => GenericContent()));

    /// <summary>Renders bounded examples of every non-lazy generic composable adapter.</summary>
    [Composable]
    public static void GenericContent()
    {
        IReadOnlyList<int> items = [1, 2, 3, 4, 5, 6];
        var viewport = Modifier.FillMaxWidth().Height(140);

        Column(() =>
        {
            Text("AnimatedContent");
            AnimatedContent(1, value => Text($"Animated state {value}"));

            Text("Crossfade");
            Crossfade(2, value => Text($"Crossfade state {value}"));

            Text("HorizontalPager");
            HorizontalPager(items, item => Text($"Page {item}"), modifier: viewport);

            Text("VerticalPager");
            VerticalPager(items, item => Text($"Page {item}"), modifier: viewport);

            Text("HorizontalUncontainedCarousel");
            HorizontalUncontainedCarousel(
                items,
                itemWidth: 96,
                item => Text($"Item {item}"),
                modifier: viewport);

            Text("HorizontalMultiBrowseCarousel");
            HorizontalMultiBrowseCarousel(
                items,
                preferredItemWidth: 120,
                item => Text($"Item {item}"),
                modifier: viewport);

            Text("HorizontalCenteredHeroCarousel");
            HorizontalCenteredHeroCarousel(
                items,
                item => Text($"Hero {item}"),
                modifier: viewport);
        });
    }
}
