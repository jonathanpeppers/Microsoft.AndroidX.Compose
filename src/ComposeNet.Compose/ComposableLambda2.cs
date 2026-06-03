using Android.Runtime;
using AndroidX.Compose.Runtime;
using Kotlin.Jvm.Functions;

namespace ComposeNet;

/// <summary>
/// Function2&lt;Composer, Integer, Unit&gt; — top-level composition +
/// theme/scope content. <c>p0</c> is the composer, <c>p1</c> is
/// <c>$changed</c>.
/// </summary>
[Register("composenet/compose/ComposableLambda2")]
internal sealed class ComposableLambda2 : Java.Lang.Object, IFunction2
{
    readonly System.Action<IComposer> _body;
    public ComposableLambda2(System.Action<IComposer> body) => _body = body;

    public Java.Lang.Object? Invoke(Java.Lang.Object? p0, Java.Lang.Object? p1)
    {
        System.ArgumentNullException.ThrowIfNull(p0);
        var composer = Android.Runtime.Extensions.JavaCast<IComposer>(p0);
        _body(composer);
        return null;
    }
}
