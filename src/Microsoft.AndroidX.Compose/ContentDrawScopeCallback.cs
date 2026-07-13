using Android.Runtime;
using AndroidX.Compose.UI.Graphics.Drawscope;
using Kotlin.Jvm.Functions;

namespace AndroidX.Compose;

[Register("net/compose/ContentDrawScopeCallback")]
internal sealed class ContentDrawScopeCallback : Java.Lang.Object, IFunction1
{
    readonly Action<ContentDrawScope> _body;

    public ContentDrawScopeCallback(Action<ContentDrawScope> body) => _body = body;

    public Java.Lang.Object Invoke(Java.Lang.Object? p0)
    {
        var value = p0 ?? throw new InvalidOperationException("Compose supplied a null ContentDrawScope.");
        var jvm = value.JavaCast<IContentDrawScope>()
            ?? throw new InvalidOperationException("Compose ContentDrawScope could not be cast.");
        _body(new ContentDrawScope(jvm));
        return Kotlin.Unit.Instance
            ?? throw new InvalidOperationException("Kotlin Unit singleton was unavailable.");
    }
}
