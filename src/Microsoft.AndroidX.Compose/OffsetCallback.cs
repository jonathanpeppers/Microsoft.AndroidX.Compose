using Android.Runtime;
using Kotlin.Jvm.Functions;

namespace AndroidX.Compose;

/// <summary>
/// JCW adapter implementing <c>Function1&lt;Offset, Unit&gt;</c> for
/// gesture-detector callbacks like <c>onTap</c> / <c>onLongPress</c> /
/// <c>onDoubleTap</c>. Because Kotlin's <c>Offset</c> is a
/// <c>@JvmInline value class</c>, the boxed argument <c>p0</c> arrives
/// as an <c>androidx.compose.ui.geometry.Offset</c> wrapper, not a
/// <c>java.lang.Long</c>; we unbox via <see cref="Offset.Unbox"/>.
/// </summary>
[Register("net/compose/OffsetCallback")]
internal sealed class OffsetCallback : Java.Lang.Object, IFunction1
{
    readonly Action<Offset> _body;

    public OffsetCallback(Action<Offset> body) => _body = body;

    public Java.Lang.Object Invoke(Java.Lang.Object? p0)
    {
        _body(Offset.Unbox(p0));
        return Kotlin.Unit.Instance!;
    }
}
