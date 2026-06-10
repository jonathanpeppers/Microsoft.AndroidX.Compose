using Android.Runtime;
using Kotlin.Jvm.Functions;

namespace AndroidX.Compose;

/// <summary>
/// JCW <c>Function3&lt;MeasureScope, List&lt;Measurable&gt;, Long, MeasureResult&gt;</c>
/// passed to <c>composenet.compose.MeasurePolicyFactory.create</c> to
/// build the proxy that implements <c>androidx.compose.ui.layout.MeasurePolicy</c>.
/// One instance per <see cref="Layout"/> facade — <see cref="Body"/> is
/// rewritten on every recomposition (the user's lambda captures may change),
/// but the <em>JNI identity</em> of the lambda + factory-built proxy is
/// stable so Compose's <c>remember</c>-keyed measure cache survives.
/// </summary>
[Register("composenet/compose/MeasurePolicyLambda")]
internal sealed class MeasurePolicyLambda : Java.Lang.Object, IFunction3
{
    /// <summary>
    /// Developer-supplied measure callback. Assigned by
    /// <see cref="Layout"/> in its <c>Render</c> preamble; never null
    /// during a measure pass.
    /// </summary>
    public Func<MeasureScope, IReadOnlyList<Measurable>, Constraints, MeasureResult>? Body { get; set; }

    /// <summary>
    /// Required no-arg ctor for the JNI runtime to materialise the type
    /// when Java code instantiates the JCW. Not used by managed callers.
    /// </summary>
    public MeasurePolicyLambda() { }

    /// <summary>
    /// Kotlin <c>Function3.invoke</c> entry point. <paramref name="p0"/>
    /// is the <c>MeasureScope</c>, <paramref name="p1"/> is a
    /// <c>List&lt;Measurable&gt;</c>, <paramref name="p2"/> is the
    /// boxed <c>Long</c> Constraints value. Routes to <see cref="Body"/>
    /// and returns the underlying <c>MeasureResult</c> handle wrapped
    /// as a <see cref="Java.Lang.Object"/>.
    /// </summary>
    public Java.Lang.Object Invoke(Java.Lang.Object? p0, Java.Lang.Object? p1, Java.Lang.Object? p2)
    {
        if (Body is null)
            throw new InvalidOperationException(
                "MeasurePolicyLambda.Body is null — Layout invoked before Render set it.");
        ArgumentNullException.ThrowIfNull(p0);
        ArgumentNullException.ThrowIfNull(p1);
        ArgumentNullException.ThrowIfNull(p2);

        var constraints = new Constraints(((Java.Lang.Long)p2).LongValue());
        var scope = new MeasureScope(p0.Handle);
        var list = p1.JavaCast<Java.Util.IList>()!;
        int count = list.Size();
        var measurables = new Measurable[count];
        // Hold the JCW peers for the duration of the call so the global
        // refs they own outlive the loop below — Measurable just snapshots
        // the handle.
        var peers = new Java.Lang.Object?[count];
        for (int i = 0; i < count; i++)
        {
            peers[i] = (Java.Lang.Object?)list.Get(i);
            measurables[i] = new Measurable(peers[i]!.Handle);
        }

        try
        {
            return Body(scope, measurables, constraints);
        }
        finally
        {
            GC.KeepAlive(p0);
            GC.KeepAlive(p1);
            GC.KeepAlive(p2);
            GC.KeepAlive(peers);
        }
    }
}
