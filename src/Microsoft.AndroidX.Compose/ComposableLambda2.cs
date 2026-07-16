using Android.Runtime;
using AndroidX.Compose.Runtime;
using Kotlin.Jvm.Functions;

namespace AndroidX.Compose;

/// <summary>
/// Function2&lt;Composer, Integer, Unit&gt; — top-level composition +
/// theme/scope content. <c>p0</c> is the composer, <c>p1</c> is
/// <c>$changed</c>.
/// </summary>
[Register("net/compose/ComposableLambda2")]
public sealed class ComposableLambda2 : Java.Lang.Object, IFunction2
{
    readonly Action<IComposer>? _body;
    readonly Action<IComposer, int>? _bodyWithChanged;

    /// <summary>
    /// Body that ignores the runtime's <c>$changed</c> hint. Use for
    /// scope/content lambdas where the wrapper doesn't need to thread
    /// the force flag back into a restart.
    /// </summary>
    public ComposableLambda2(Action<IComposer> body)
    {
        ArgumentNullException.ThrowIfNull(body);
        _body = body;
    }

    /// <summary>
    /// Body that observes the runtime's <c>$changed</c> hint. Used by
    /// <c>EndRestartGroup().UpdateScope(...)</c> emissions on generated
    /// composable methods so the recompose pass can OR the runtime's force
    /// bit into <c>$changed</c> and propagate it down.
    /// </summary>
    public ComposableLambda2(Action<IComposer, int> body)
    {
        ArgumentNullException.ThrowIfNull(body);
        _bodyWithChanged = body;
    }

    // Kotlin Function2<Composer, Int, Unit> contractually returns
    // Unit.INSTANCE. See ComposableLambda0 / issue #43 for the rationale.
    public Java.Lang.Object Invoke(Java.Lang.Object? p0, Java.Lang.Object? p1)
    {
        ArgumentNullException.ThrowIfNull(p0);
        var composer = Android.Runtime.Extensions.JavaCast<IComposer>(p0);
        if (_bodyWithChanged is not null)
        {
            int changed = p1 is Java.Lang.Integer i ? i.IntValue() : 0;
            _bodyWithChanged(composer, changed);
        }
        else
        {
            var body = _body
                ?? throw new InvalidOperationException(
                    "ComposableLambda2 has neither body delegate set.");
            body(composer);
        }
        return Kotlin.Unit.Instance
            ?? throw new InvalidOperationException(
                "Kotlin.Unit.Instance was unavailable after invoking ComposableLambda2.");
    }
}
