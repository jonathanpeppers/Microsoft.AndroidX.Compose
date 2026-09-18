using AndroidX.Compose;
using AndroidX.Compose.Runtime;

namespace Microsoft.AndroidX.Compose.Maui.Platform;

sealed class FallbackViewNode : ComposableNode
{
    const int LifetimeKey = 0;

    readonly IView _view;
    readonly IMauiContext _context;
    readonly Modifier? _modifier;

    internal FallbackViewNode(IView view, IMauiContext context, Modifier? modifier)
    {
        _view = view;
        _context = context;
        _modifier = modifier;
    }

    public override void Render(IComposer composer)
    {
        FallbackViewHost? host = null;
        new AndroidView(
            factory: context => host = new FallbackViewHost(context),
            update: platformHost => ((FallbackViewHost)platformHost).Update(_view, _context))
        {
            Modifier = Modifier ?? _modifier,
        }.Render(composer);

        // The supported, stable primitive key retains the first effect
        // closure, whose factory capture points at this slot's host.
        new DisposableEffect(LifetimeKey, () => () =>
        {
            host?.RemoveAllViews();
            host = null;
        }).Render(composer);
    }
}
