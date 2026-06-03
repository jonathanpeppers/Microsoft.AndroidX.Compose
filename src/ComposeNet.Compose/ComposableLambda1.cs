using Android.Runtime;
using Kotlin.Jvm.Functions;

namespace ComposeNet;

/// <summary>
/// Function1&lt;T, Unit&gt; — single-arg callbacks (TextField's
/// onValueChange, onCheckedChange, etc.). The body receives the raw
/// Java arg and is responsible for unboxing it.
/// </summary>
[Register("composenet/compose/ComposableLambda1")]
internal sealed class ComposableLambda1 : Java.Lang.Object, IFunction1
{
    readonly System.Action<Java.Lang.Object?> _body;
    public ComposableLambda1(System.Action<Java.Lang.Object?> body) => _body = body;

    public Java.Lang.Object? Invoke(Java.Lang.Object? p0)
    {
        _body(p0);
        return null;
    }
}
