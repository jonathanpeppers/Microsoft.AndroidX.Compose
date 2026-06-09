using global::AndroidX.Compose.Runtime;
using Microsoft.AndroidX.Compose.Gallery.Registry;

namespace Microsoft.AndroidX.Compose.Gallery.Screens;

/// <summary>
/// One clickable row on <see cref="HomeScreen"/>: shows the category's
/// glyph, title, subtitle, and the number of demos it contains.
/// Tapping pushes <c>category/{id}</c> on the supplied
/// <see cref="NavController"/>.
/// </summary>
public sealed class CategoryRow : ComposableNode
{
    readonly Category _category;
    readonly NavController _nav;

    /// <summary>Create a row for <paramref name="category"/>.</summary>
    public CategoryRow(Category category, NavController nav)
    {
        _category = category;
        _nav      = nav;
    }

    public override void Render(IComposer composer)
    {
        var count = Enumerable.Count(Catalog.DemosByCategory(_category.Id));
        new Card
        {
            Modifier.Companion
                .FillMaxWidth()
                .Padding(0, 4)
                .Clickable(() => _nav.Navigate($"category/{_category.Id}")),
            new ListItem
            {
                Headline   = new Text($"{_category.Glyph}  {_category.Title}"),
                Supporting = new Text($"{_category.Subtitle}  ·  {count} demo{(count == 1 ? "" : "s")}"),
            },
        }.Render(composer);
    }
}
