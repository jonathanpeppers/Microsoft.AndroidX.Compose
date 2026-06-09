using global::Android.Runtime;
using global::AndroidX.Compose.Foundation.Shape;

namespace Microsoft.AndroidX.Compose;

/// <summary>
/// C# wrapper around <c>androidx.compose.ui.graphics.Shape</c>.
/// Compose's <c>Shape</c> is an interface; concrete shapes like
/// <c>RoundedCornerShape</c> live in <c>ui-graphics-android</c>, which
/// is bound by <see href="https://github.com/dotnet/android-libraries/commit/da8b14bfd2275ba8c53d999fda48dda97476ee37"/>
/// in newer NuGets. Until we adopt that bump, this thin wrapper holds
/// a Kotlin <c>Shape</c> handle so the bridge generator's reference-
/// type code path can pass it to <c>L</c> JNI slots.
///
/// Use the factory <see cref="RoundedCorners(Dp)"/> to build a
/// <c>RoundedCornerShape</c> from a corner-radius <see cref="Dp"/>;
/// the factory delegates to the existing
/// <c>ComposeBridges.RoundedCornerShape</c> bridge.
/// </summary>
public sealed class Shape : Java.Lang.Object
{
    Shape(IntPtr handle, JniHandleOwnership transfer)
        : base(handle, transfer) { }

    /// <summary>
    /// <c>androidx.compose.foundation.shape.RoundedCornerShape(corner: Dp)</c>
    /// — equal radius on all four corners.
    /// </summary>
    public static Shape RoundedCorners(Dp cornerRadius)
    {
        IntPtr handle = ComposeBridges.RoundedCornerShape(cornerRadius.Value);
        return new Shape(handle, JniHandleOwnership.TransferLocalRef);
    }

    /// <summary>
    /// <c>androidx.compose.foundation.shape.RoundedCornerShape(topStart, topEnd, bottomEnd, bottomStart)</c>
    /// — independent radii per corner expressed in <see cref="Dp"/>. The
    /// argument order matches Kotlin: top-start, top-end, bottom-end,
    /// bottom-start (clockwise from the top-leading corner). Pass
    /// <c>0.Dp()</c> for a square corner.
    /// </summary>
    /// <remarks>
    /// The canonical "chat bubble" shape uses one flattened corner to
    /// visually connect to its avatar tile — e.g.
    /// <c>Shape.RoundedCorners(4, 20, 20, 20)</c> for a bubble whose
    /// top-leading corner is squared off where it meets the speaker's
    /// avatar.
    /// </remarks>
    public static Shape RoundedCorners(Dp topStart, Dp topEnd, Dp bottomEnd, Dp bottomStart)
    {
        var rounded = RoundedCornerShapeKt.RoundedCornerShape(
            topStart.Value, topEnd.Value, bottomEnd.Value, bottomStart.Value);
        IntPtr handle = ((Java.Lang.Object)rounded).Handle;
        return new Shape(JNIEnv.NewLocalRef(handle), JniHandleOwnership.TransferLocalRef);
    }

    /// <summary>
    /// <c>androidx.compose.foundation.shape.RoundedCornerShape(percent: Int)</c>
    /// — equal radius on all four corners, expressed as a percentage of
    /// the smaller dimension. <c>0</c> = rectangle, <c>50</c> = pill /
    /// circle (use <see cref="Circle"/> for the canonical name).
    /// </summary>
    public static Shape RoundedPercent(int percent)
    {
        IntPtr handle = ComposeBridges.RoundedCornerShapePercent(percent);
        return new Shape(handle, JniHandleOwnership.TransferLocalRef);
    }

    /// <summary>
    /// <c>RoundedCornerShape(50)</c> — fully rounded, producing a
    /// circle (when the bounds are square) or a pill (when they're
    /// rectangular).
    /// </summary>
    public static Shape Circle() => RoundedPercent(50);

    /// <summary>
    /// <c>androidx.compose.foundation.shape.CutCornerShape(size: Dp)</c>
    /// — chamfered (straight-line) corners on all four sides.
    /// </summary>
    public static Shape CutCorners(Dp cornerSize)
    {
        IntPtr handle = ComposeBridges.CutCornerShapeDp(cornerSize.Value);
        return new Shape(handle, JniHandleOwnership.TransferLocalRef);
    }

    /// <summary>
    /// <c>androidx.compose.foundation.shape.CutCornerShape(percent: Int)</c>
    /// — chamfered corners expressed as a percentage of the smaller
    /// dimension.
    /// </summary>
    public static Shape CutCornersPercent(int percent)
    {
        IntPtr handle = ComposeBridges.CutCornerShapePercent(percent);
        return new Shape(handle, JniHandleOwnership.TransferLocalRef);
    }
}
