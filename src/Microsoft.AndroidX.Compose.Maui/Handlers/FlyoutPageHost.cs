using Microsoft.Maui.Platform;

namespace Microsoft.AndroidX.Compose.Maui.Handlers;

sealed class FlyoutPageHost : global::Android.Widget.FrameLayout
{
    IView? _page;

    internal FlyoutPageHost(global::Android.Content.Context context) : base(context)
    {
    }

    internal void UpdatePage(IView? page, IMauiContext context)
    {
        if (ReferenceEquals(_page, page))
            return;

        if (_page?.Handler is IPlatformViewHandler oldHandler)
            oldHandler.DisconnectHandler();

        RemoveAllViews();
        _page = page;
        if (page is null)
            return;

        var platform = page.ToPlatform(context);
        if (platform.Parent is global::Android.Views.ViewGroup oldParent)
            oldParent.RemoveView(platform);

        AddView(platform, new LayoutParams(
            LayoutParams.MatchParent,
            LayoutParams.MatchParent));
    }
}
