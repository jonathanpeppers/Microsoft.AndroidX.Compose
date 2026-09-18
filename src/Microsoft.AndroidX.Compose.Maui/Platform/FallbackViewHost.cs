using Android.Content;
using Android.Views;
using Android.Widget;
using Microsoft.Maui.Platform;

namespace Microsoft.AndroidX.Compose.Maui.Platform;

sealed class FallbackViewHost : FrameLayout
{
    internal FallbackViewHost(Context context) : base(context)
    {
        LayoutParameters = new ViewGroup.LayoutParams(
            ViewGroup.LayoutParams.MatchParent,
            ViewGroup.LayoutParams.MatchParent);
    }

    internal void Update(IView view, IMauiContext context)
    {
        var platformView = view.ToPlatform(context);
        if (ChildCount == 1 && ReferenceEquals(GetChildAt(0), platformView))
            return;

        RemoveAllViews();
        if (platformView.Parent is ViewGroup oldParent)
            oldParent.RemoveView(platformView);

        AddView(platformView, new LayoutParams(
            LayoutParams.MatchParent,
            LayoutParams.MatchParent));
    }
}
