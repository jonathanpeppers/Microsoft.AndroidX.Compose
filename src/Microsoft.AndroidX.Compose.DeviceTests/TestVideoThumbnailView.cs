using Android.Content;
using AndroidX.Compose.Samples.Jetchat;

namespace Microsoft.AndroidX.Compose.DeviceTests;

internal sealed class TestVideoThumbnailView : VideoThumbnailView
{
    internal TestVideoThumbnailView(Context context, string videoUri)
        : base(context, videoUri) { }

    internal void Attach() => OnAttachedToWindow();

    internal void Detach() => OnDetachedFromWindow();
}
