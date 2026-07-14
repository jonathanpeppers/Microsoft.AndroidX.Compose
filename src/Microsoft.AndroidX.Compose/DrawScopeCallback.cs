using Android.Runtime;
using AndroidX.Compose.UI.Graphics.Drawscope;
using Kotlin.Jvm.Functions;

namespace AndroidX.Compose;

[Register("net/compose/DrawScopeCallback")]
internal sealed class DrawScopeCallback : Java.Lang.Object, IFunction1
{
    readonly Action<DrawScope> _body;

    public DrawScopeCallback(Action<DrawScope> body) => _body = body;

    public Java.Lang.Object Invoke(Java.Lang.Object? p0)
    {
        var value = p0 ?? throw new InvalidOperationException("Compose supplied a null DrawScope.");
        var jvm = value.JavaCast<IDrawScope>()
            ?? throw new InvalidOperationException("Compose DrawScope could not be cast to IDrawScope.");
        _body(new DrawScope(jvm));
        return Kotlin.Unit.Instance
            ?? throw new InvalidOperationException("Kotlin Unit singleton was unavailable.");
    }
}
