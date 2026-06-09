using global::Android.Runtime;

namespace Microsoft.AndroidX.Compose;

/// <summary>
/// <c>androidx.compose.ui.text.LinkInteractionListener</c> adapter
/// that forwards Compose's link-tap callbacks to a C#
/// <see cref="Action{T}"/>. Allocated once per
/// <see cref="LinkAnnotation.Clickable"/> call and held by the
/// resulting <see cref="LinkAnnotation"/> so the JNI peer stays alive
/// as long as the annotation does.
/// </summary>
[Register("net/compose/LinkClickListener")]
internal sealed class LinkClickListener : Java.Lang.Object, global::AndroidX.Compose.UI.Text.ILinkInteractionListener
{
    readonly string _tag;
    readonly Action<string> _onClick;

    public LinkClickListener(string tag, Action<string> onClick)
    {
        _tag     = tag;
        _onClick = onClick;
    }

    public void OnClick(global::AndroidX.Compose.UI.Text.LinkAnnotation? link) => _onClick(_tag);
}
