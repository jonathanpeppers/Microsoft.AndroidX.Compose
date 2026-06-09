using AndroidX.Compose.Runtime;

namespace AndroidX.Compose.Gallery;

/// <summary>
/// A single tap target inside the gallery's <see cref="GalleryDrawer"/>:
/// a glyph and a label rendered as a Material 3
/// <see cref="NavigationDrawerItem"/>. The gallery's drawer doesn't
/// track per-tile selection state, so <c>selected</c> is always
/// <c>false</c> — the indicator pill is reserved for app-level state.
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
        new NavigationDrawerItem(selected: false, onClick: _onClick)
        {
            Label = new Text(_label),
            Icon  = new Text(_glyph),
        }.Render(composer);
    }
}
