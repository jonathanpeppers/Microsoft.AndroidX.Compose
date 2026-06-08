using System;
using AndroidX.Compose.Runtime;

namespace ComposeNet.Gallery;

/// <summary>
/// A single tap target inside the gallery's <see cref="GalleryDrawer"/>:
/// a glyph and a label rendered as a clickable
/// <see cref="ListItem"/>. Kept simple deliberately — the full Material
/// <c>NavigationDrawerItem</c> control isn't bound yet, and a plain
/// list row matches the rest of the gallery's visual language.
/// </summary>
public sealed class GalleryDrawerItem : ComposableNode
{
    readonly string _glyph;
    readonly string _label;
    readonly Action _onClick;

    /// <summary>Create a drawer item that fires <paramref name="onClick"/> when tapped.</summary>
    public GalleryDrawerItem(string glyph, string label, Action onClick)
    {
        _glyph   = glyph;
        _label   = label;
        _onClick = onClick;
    }

    public override void Render(IComposer composer)
    {
        new ListItem
        {
            Modifier = Modifier.Companion
                .FillMaxWidth()
                .Clickable(_onClick),
            Headline = new Text($"{_glyph}  {_label}"),
        }.Render(composer);
    }
}
