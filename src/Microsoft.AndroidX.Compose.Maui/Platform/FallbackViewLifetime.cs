using Android.Content;
using AView = Android.Views.View;

namespace Microsoft.AndroidX.Compose.Maui.Platform;

sealed class FallbackViewLifetime
{
    FallbackViewHost? _host;

    internal FallbackViewHost Create(Context context) =>
        _host = new FallbackViewHost(context);

    internal void Update(AView platformHost, IView view, IMauiContext context)
    {
        var host = (FallbackViewHost)platformHost;
        _host = host;
        host.Update(view, context);
    }

    internal Action RegisterRelease() => Release;

    void Release()
    {
        _host?.RemoveAllViews();
        _host = null;
    }
}
