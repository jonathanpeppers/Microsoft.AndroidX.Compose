using Android.Runtime;
using AndroidX.Compose.Runtime;
using Kotlin.Jvm.Functions;

namespace ComposeNet;

/// <summary>
/// JCW for Compose's <c>DisposableEffect</c> body —
/// <c>Function1&lt;DisposableEffectScope, DisposableEffectResult&gt;</c>.
/// Kotlin invokes the body once per (key change | enter composition)
/// and stores the returned <see cref="IDisposableEffectResult"/>; when
/// keys change or the call site leaves the composition Compose calls
/// <see cref="IDisposableEffectResult.Dispose"/> on the stored result.
/// </summary>
/// <remarks>
/// The C# body returns a <see cref="System.Action"/> "onDispose"
/// callback. We wrap that callback in a fresh
/// <see cref="ComposableLambda0"/> and hand it to
/// <see cref="DisposableEffectScope.OnDispose(IFunction0)"/>, which
/// is the only way to construct an <see cref="IDisposableEffectResult"/>
/// without subclassing the interface in Java.
/// </remarks>
[Register("composenet/compose/DisposableEffectBody")]
internal sealed class DisposableEffectBody : Java.Lang.Object, IFunction1
{
    readonly System.Func<DisposableEffectScope, System.Action> _body;

    public DisposableEffectBody(System.Func<DisposableEffectScope, System.Action> body)
    {
        _body = body;
    }

    public Java.Lang.Object Invoke(Java.Lang.Object? p0)
    {
        // Compose always passes a non-null DisposableEffectScope. The
        // ?? fallback keeps the static analyzer happy and gives a
        // recognisable NRE message if Compose ever changes that.
        var scope = p0 as DisposableEffectScope
            ?? throw new System.InvalidOperationException(
                "DisposableEffect body received a non-DisposableEffectScope argument: "
                + (p0?.Class?.Name ?? "<null>"));
        var onDispose = _body(scope)
            ?? throw new System.InvalidOperationException(
                "DisposableEffect body returned a null onDispose callback. "
                + "Return `() => { }` if there's nothing to clean up.");
        return (Java.Lang.Object)scope.OnDispose(new ComposableLambda0(onDispose));
    }
}
