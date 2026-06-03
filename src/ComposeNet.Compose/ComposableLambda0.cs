using Android.Runtime;
using Kotlin.Jvm.Functions;

namespace ComposeNet;

/// <summary>
/// Function0&lt;Unit&gt; — zero-arg callbacks (Button.onClick, etc.).
/// A <c>[Register]</c>'d Java.Lang.Object ACW so Compose's bytecode-typed
/// lambda parameter accepts it.
/// </summary>
[Register("composenet/compose/ComposableLambda0")]
internal sealed class ComposableLambda0 : Java.Lang.Object, IFunction0
{
    readonly System.Action _body;
    public ComposableLambda0(System.Action body) => _body = body;
    public Java.Lang.Object? Invoke() { _body(); return null; }
}
