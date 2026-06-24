using Android.Runtime;
using Kotlin.Jvm.Functions;

namespace AndroidX.Compose;

/// <summary>
/// JCW adapter implementing <c>Function2&lt;PointerInputChange, Offset,
/// Unit&gt;</c> for the <c>onDrag</c> callback of
/// <c>detectDragGestures</c>. The Kotlin signature passes the live
/// <c>PointerInputChange</c> as <c>p0</c> (which carries pointer id,
/// position, pressure, etc.) and the per-frame drag delta as a
/// <c>@JvmInline value class</c> <c>Offset</c> wrapper in <c>p1</c>.
/// </summary>
/// <remarks>
/// <para>
/// v1 only surfaces the drag delta — <c>PointerInputChange</c> is
/// passed through opaquely (the C# binding doesn't model it) and
/// the user's <see cref="Action{Offset}"/> body receives just the
/// per-frame delta in local layout pixels. To accumulate totals the
/// caller maintains its own running sum across <c>onDragStart</c>
/// / <c>onDrag</c> / <c>onDragEnd</c>.
/// </para>
/// <para>
/// The boxed <c>Offset</c> argument (<c>p1</c>) is unboxed via
/// <see cref="Offset.Unbox"/>.
/// </para>
/// </remarks>
[Register("net/compose/DragCallback")]
internal sealed class DragCallback : Java.Lang.Object, IFunction2
{
    readonly Action<Offset> _body;

    public DragCallback(Action<Offset> body) => _body = body;

    public Java.Lang.Object Invoke(Java.Lang.Object? p0, Java.Lang.Object? p1)
    {
        _body(Offset.Unbox(p1));
        return Kotlin.Unit.Instance!;
    }
}
