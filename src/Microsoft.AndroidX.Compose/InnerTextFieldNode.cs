using Android.Runtime;
using AndroidX.Compose.Runtime;
using Kotlin.Jvm.Functions;

namespace AndroidX.Compose;

internal sealed class InnerTextFieldNode(IFunction2 content) : ComposableNode
{
    internal static InnerTextFieldNode FromPeer(Java.Lang.Object? peer) =>
        new((peer ?? throw new InvalidOperationException("BasicTextField decoration received no inner editor."))
            .JavaCast<IFunction2>());

    public override void Render(IComposer composer)
    {
        ArgumentNullException.ThrowIfNull(composer);
        using var changed = Java.Lang.Integer.ValueOf(0);
        content.Invoke((Java.Lang.Object)composer, changed);
    }
}
