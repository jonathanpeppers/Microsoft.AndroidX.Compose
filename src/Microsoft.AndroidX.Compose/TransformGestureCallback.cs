using Android.Runtime;
using Kotlin.Jvm.Functions;

namespace AndroidX.Compose;

/// <summary>
/// JCW adapter implementing
/// <c>Function4&lt;Offset, Offset, Float, Float, Unit&gt;</c> for the
/// <c>onGesture</c> callback of <c>detectTransformGestures</c>. Kotlin
/// passes the gesture centroid in <c>p0</c>, the cumulative pan in
/// <c>p1</c>, the cumulative zoom multiplier in <c>p2</c>, and the
/// rotation in degrees in <c>p3</c>.
/// </summary>
/// <remarks>
/// Both <see cref="Offset"/> args (<c>p0</c> centroid, <c>p1</c> pan)
/// arrive as boxed <c>androidx.compose.ui.geometry.Offset</c>
/// wrappers around a packed <c>long</c>; we unbox via
/// <see cref="Offset.Unbox"/>. <c>p2</c> / <c>p3</c> are
/// <see cref="Java.Lang.Float"/> boxed primitives.
/// </remarks>
[Register("net/compose/TransformGestureCallback")]
internal sealed class TransformGestureCallback : Java.Lang.Object, IFunction4
{
    readonly Action<Offset, Offset, float, float> _body;

    public TransformGestureCallback(Action<Offset, Offset, float, float> body) => _body = body;

    public Java.Lang.Object Invoke(
        Java.Lang.Object? p0, Java.Lang.Object? p1,
        Java.Lang.Object? p2, Java.Lang.Object? p3)
    {
        // Kotlin Float => Java.Lang.Float; FloatValue() unwraps.
        var zoom     = (p2 as Java.Lang.Float)?.FloatValue() ?? 1f;
        var rotation = (p3 as Java.Lang.Float)?.FloatValue() ?? 0f;
        _body(Offset.Unbox(p0), Offset.Unbox(p1), zoom, rotation);
        return Kotlin.Unit.Instance!;
    }
}
