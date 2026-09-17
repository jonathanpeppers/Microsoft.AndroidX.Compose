using AndroidX.Compose;
using Microsoft.AndroidX.Compose.Maui.Loaders;
using MauiPage = Microsoft.Maui.Controls.Page;
using ComposeIcon = AndroidX.Compose.Icon;

namespace Microsoft.AndroidX.Compose.Maui.Handlers;

internal sealed class TabbedPageIcon : IImageSourcePart
{
    static readonly Modifier s_iconSize = Modifier.Size(new Dp(24));

    readonly MauiPage _page;
    readonly ImageSourceLoader _loader;
    bool _active = true;

    public TabbedPageIcon(MauiPage page, IElementHandler handler)
    {
        ArgumentNullException.ThrowIfNull(page);
        ArgumentNullException.ThrowIfNull(handler);

        _page = page;
        _loader = new ImageSourceLoader(handler, () => _active ? this : null);
    }

    public IImageSource? Source => _page.IconImageSource;

    public bool IsAnimationPlaying => false;

    public void UpdateIsLoading(bool isLoading)
    {
    }

    public Task RefreshAsync() => _loader.LoadAsync(Source);

    public ComposableNode BuildNode()
    {
        if (_loader.Painter.Value is { } painter)
            return new ComposeIcon(painter, contentDescription: null) { Modifier = s_iconSize };
        if (_loader.DrawableResourceId.Value is int resourceId)
            return new ComposeIcon(resourceId, contentDescription: null) { Modifier = s_iconSize };
        return new Box { Modifier = s_iconSize };
    }

    public void Reset()
    {
        _active = false;
        _loader.Reset();
    }
}
