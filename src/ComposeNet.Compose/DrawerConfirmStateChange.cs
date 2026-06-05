using Android.Runtime;
using AndroidX.Compose.Material3;
using Kotlin.Jvm.Functions;

namespace ComposeNet;

/// <summary>
/// <c>Function1&lt;DrawerValue, Boolean&gt;</c> adapter used as the
/// <c>confirmStateChange</c> callback when constructing a
/// <c>DrawerState</c> via <c>rememberDrawerState</c>. The callback is
/// part of the <c>remember</c> cache key in Kotlin, so this type is
/// allocated <strong>once per <see cref="ComposableNode"/> instance</strong>
/// and reused across recompositions — a fresh adapter each pass would
/// invalidate the cached state holder and the drawer would forget
/// whether it's open. <see cref="Callback"/> is read on every JNI
/// invocation, so callers may mutate it freely without re-allocating.
/// When <see cref="Callback"/> is <c>null</c> the adapter behaves as
/// the Kotlin default <c>{ true }</c> — every transition is allowed.
/// </summary>
[Register("composenet/compose/DrawerConfirmStateChange")]
internal sealed class DrawerConfirmStateChange : Java.Lang.Object, IFunction1
{
    public System.Func<DrawerValue, bool>? Callback { get; set; }

    public Java.Lang.Object? Invoke(Java.Lang.Object? p0)
    {
        var cb = Callback;
        if (cb is null)
            return Java.Lang.Boolean.True;
        var value = Android.Runtime.Extensions.JavaCast<DrawerValue>(p0!);
        return cb(value) ? Java.Lang.Boolean.True : Java.Lang.Boolean.False;
    }
}
