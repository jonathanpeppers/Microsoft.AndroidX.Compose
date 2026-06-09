using Android.Runtime;

namespace ComposeNet;

/// <summary>
/// <c>androidx.compose.ui.text.LinkInteractionListener</c> adapter
/// that forwards Compose's link-tap callbacks to a C#
/// <see cref="System.Action{T}"/>. Allocated once per
/// <see cref="LinkAnnotation.Clickable"/> call and held by the
/// resulting <see cref="LinkAnnotation"/> so the JNI peer stays alive
/// as long as the annotation does.
/// </summary>
[Register("composenet/compose/LinkClickListener")]
internal sealed class LinkClickListener : Java.Lang.Object, AndroidX.Compose.UI.Text.ILinkInteractionListener
{
    readonly string _tag;
    readonly System.Action<string> _onClick;

    public LinkClickListener(string tag, System.Action<string> onClick)
    {
        _tag     = tag;
        _onClick = onClick;
    }

    public void OnClick(AndroidX.Compose.UI.Text.LinkAnnotation? link) => _onClick(_tag);
}
