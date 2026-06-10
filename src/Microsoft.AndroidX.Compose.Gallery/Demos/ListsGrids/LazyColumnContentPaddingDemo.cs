using AndroidX.Compose.Gallery.Registry;

namespace AndroidX.Compose.Gallery.Demos.ListsGrids;

/// <summary>
/// LazyColumn comparing no content padding vs an asymmetric
/// <see cref="PaddingValues"/> so the inset is visible on the first
/// and last items.
/// </summary>
public static class LazyColumnContentPaddingDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "lists-lazy-column-content-padding",
        CategoryId:  "lists-grids",
        Title:       "LazyColumn — ContentPadding",
        Description: "Asymmetric PaddingValues(start: 16, top: 24, end: 16, bottom: 24) vs no padding.",
        Build:       c =>
        {
            var items   = Enumerable.Range(0, 20).ToList();
            var padding = c.Remember(() => new PaddingValues(start: 16, top: 24, end: 16, bottom: 24));
            return new Column
            {
                new Text("No ContentPadding (items hug the edges):"),
                new Box
                {
                    Modifier.FillMaxWidth().Height(140).Background(Color.FromRgb(0xFF, 0xE0, 0xB2)),

                    new LazyColumn<int>(
                        items:       items,
                        itemContent: i => new Text($"Row {i:D2}") { Color = Color.Black })
                    {
                        Modifier = Modifier.FillMaxSize(),
                    },
                },

                new Spacer { Modifier = Modifier.Height(12) },

                new Text("ContentPadding = PaddingValues(start: 16, top: 24, end: 16, bottom: 24):"),
                new Box
                {
                    Modifier.FillMaxWidth().Height(140).Background(Color.FromRgb(0xC8, 0xE6, 0xC9)),

                    new LazyColumn<int>(
                        items:       items,
                        itemContent: i => new Text($"Row {i:D2}") { Color = Color.Black })
                    {
                        Modifier       = Modifier.FillMaxSize(),
                        ContentPadding = padding,
                    },
                },

                new Spacer { Modifier = Modifier.Height(12) },

                new Text(
                    $"Top inset {padding.Top.Value}dp, bottom inset {padding.Bottom.Value}dp " +
                    "are reachable by scrolling — first and last items respect the inset."),
            };
        });
}
