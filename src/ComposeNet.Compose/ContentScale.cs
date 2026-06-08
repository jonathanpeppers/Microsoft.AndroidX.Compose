using Android.Runtime;

namespace ComposeNet;

/// <summary>
/// C# wrapper around the <c>androidx.compose.ui.layout.ContentScale</c>
/// singletons exposed by Kotlin's <c>ContentScale.Companion</c> object.
/// Compose's <c>ContentScale</c> is a real Kotlin interface (not a
/// <c>@JvmInline value class</c>) — same plumbing story as
/// <see cref="TextDecoration"/>. The <c>Xamarin.AndroidX.Compose.UI</c>
/// binding currently doesn't surface a managed <c>IContentScale</c>
/// type, so we resolve the companion getters via JNI directly.
///
/// Pass instances to facades that accept an optional <c>contentScale</c>
/// slot (e.g. <see cref="Image"/>) to control how source content is
/// scaled to fit its destination bounds. Left unset (null) on those
/// facades, Kotlin's default (<c>ContentScale.Fit</c>) applies.
/// </summary>
public sealed class ContentScale : Java.Lang.Object
{
    ContentScale(IntPtr handle, JniHandleOwnership transfer)
        : base(handle, transfer) { }

    static ContentScale Resolve(string getterName)
    {
        IntPtr cls = JNIEnv.FindClass("androidx/compose/ui/layout/ContentScale$Companion");
        IntPtr mid = JNIEnv.GetMethodID(cls, getterName, "()Landroidx/compose/ui/layout/ContentScale;");
        IntPtr companion = Companion();
        IntPtr value = JNIEnv.CallObjectMethod(companion, mid);
        return new ContentScale(value, JniHandleOwnership.TransferLocalRef);
    }

    // The outer ContentScale interface has a Companion holder object;
    // the public surface lives on the Companion's getXxx() methods
    // (the val fields on the outer class are private, same story as
    // TextDecoration and FontWeight).
    static IntPtr s_companion;
    static IntPtr Companion()
    {
        if (s_companion == IntPtr.Zero)
        {
            IntPtr outerCls = JNIEnv.FindClass("androidx/compose/ui/layout/ContentScale");
            IntPtr fid = JNIEnv.GetStaticFieldID(outerCls, "Companion", "Landroidx/compose/ui/layout/ContentScale$Companion;");
            IntPtr local = JNIEnv.GetStaticObjectField(outerCls, fid);
            s_companion = JNIEnv.NewGlobalRef(local);
            JNIEnv.DeleteLocalRef(local);
        }
        return s_companion;
    }

    static ContentScale? s_fit, s_crop, s_fillHeight, s_fillWidth, s_fillBounds, s_inside, s_none;

    /// <summary>
    /// <c>ContentScale.Fit</c> — scale the source uniformly (maintaining
    /// the aspect ratio) so both dimensions fit within the destination.
    /// Comparable to <c>ImageView.ScaleType.FIT_CENTER</c>.
    /// </summary>
    public static ContentScale Fit => s_fit ??= Resolve("getFit");

    /// <summary>
    /// <c>ContentScale.Crop</c> — scale the source uniformly so it fully
    /// fills the destination, cropping any overflow. Comparable to
    /// <c>ImageView.ScaleType.CENTER_CROP</c>.
    /// </summary>
    public static ContentScale Crop => s_crop ??= Resolve("getCrop");

    /// <summary>
    /// <c>ContentScale.FillHeight</c> — scale the source uniformly to
    /// match the destination's height; the width may overflow or be
    /// clipped depending on the parent.
    /// </summary>
    public static ContentScale FillHeight => s_fillHeight ??= Resolve("getFillHeight");

    /// <summary>
    /// <c>ContentScale.FillWidth</c> — scale the source uniformly to
    /// match the destination's width; the height may overflow or be
    /// clipped depending on the parent.
    /// </summary>
    public static ContentScale FillWidth => s_fillWidth ??= Resolve("getFillWidth");

    /// <summary>
    /// <c>ContentScale.FillBounds</c> — scale the source non-uniformly
    /// to exactly match the destination bounds. Distorts the aspect
    /// ratio. Comparable to <c>ImageView.ScaleType.FIT_XY</c>.
    /// </summary>
    public static ContentScale FillBounds => s_fillBounds ??= Resolve("getFillBounds");

    /// <summary>
    /// <c>ContentScale.Inside</c> — like <see cref="Fit"/> when the
    /// source is larger than the destination; otherwise the source is
    /// drawn at its intrinsic size (no upscaling). Comparable to
    /// <c>ImageView.ScaleType.CENTER_INSIDE</c>.
    /// </summary>
    public static ContentScale Inside => s_inside ??= Resolve("getInside");

    /// <summary>
    /// <c>ContentScale.None</c> — draw the source at its intrinsic size
    /// (no scaling). Comparable to <c>ImageView.ScaleType.CENTER</c>.
    /// </summary>
    public static ContentScale None => s_none ??= Resolve("getNone");
}
