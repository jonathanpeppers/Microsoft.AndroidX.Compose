using Android.Runtime;
using AndroidX.Compose.UI.Graphics;
using AndroidX.Compose.UI.Unit;
using Kotlin.Jvm.Functions;

namespace AndroidX.Compose;

// A retained non-composable Function3<Path, Size, LayoutDirection, Unit>;
// ComposableLambda3 instead interprets its second argument as a Composer.
[Register("net/compose/ShapePathCallback")]
internal sealed class ShapePathCallback(Action<Path, Size, LayoutDirection> builder)
    : Java.Lang.Object, IFunction3
{
    public Java.Lang.Object Invoke(Java.Lang.Object? p0, Java.Lang.Object? p1, Java.Lang.Object? p2)
    {
        var pathPeer = p0 ?? throw new InvalidOperationException("GenericShape supplied a null Path.");
        var sizePeer = p1 ?? throw new InvalidOperationException("GenericShape supplied a null Size.");
        var directionPeer = p2 ?? throw new InvalidOperationException("GenericShape supplied a null LayoutDirection.");
        var path = pathPeer.JavaCast<IPath>()
            ?? throw new InvalidOperationException("GenericShape Path could not be cast to IPath.");
        var direction = directionPeer.JavaCast<LayoutDirection>()
            ?? throw new InvalidOperationException("GenericShape LayoutDirection could not be cast.");
        try
        {
            using var borrowed = new Path(path);
            builder(borrowed, Size.FromPacked(ComposeBridges.ShapeSizeUnbox(sizePeer.Handle)), direction);
            return Kotlin.Unit.Instance
                ?? throw new InvalidOperationException("Kotlin Unit was unavailable in GenericShape.");
        }
        finally
        {
            GC.KeepAlive(sizePeer);
        }
    }
}
