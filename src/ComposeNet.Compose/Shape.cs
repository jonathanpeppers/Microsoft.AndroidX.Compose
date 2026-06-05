using Android.Runtime;

namespace ComposeNet;

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
}
