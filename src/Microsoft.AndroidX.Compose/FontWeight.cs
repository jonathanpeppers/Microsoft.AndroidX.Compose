using global::Android.Runtime;

namespace Microsoft.AndroidX.Compose;

/// <summary>
/// C# wrapper around <c>androidx.compose.ui.text.font.FontWeight</c>.
/// Compose's <c>FontWeight</c> is a real Kotlin class (not a value
/// class), but Compose 1.11.1's <c>ui-text-android</c> package is
/// shipped as a Java-library-only stub with zero exported types — so
/// the .NET binding doesn't expose it yet. We subclass
/// <see cref="Java.Lang.Object"/> directly and resolve the Kotlin
/// <c>Companion</c> instances via JNI; the bridge generator's
/// reference-type code path (<c>x is null ? IntPtr.Zero : x.Handle</c>)
/// passes the handle through to the JNI <c>L</c> slot.
///
/// Will swap to bound <c>global::AndroidX.Compose.UI.Text.Font.FontWeight</c>
/// once <see href="https://github.com/dotnet/android-libraries/issues/1439"/>
/// ships and we adopt <c>Xamarin.AndroidX.Compose.UI.Text.Android</c>
/// 1.11.2.1+.
/// </summary>
public sealed class FontWeight : Java.Lang.Object
{
    FontWeight(IntPtr handle, JniHandleOwnership transfer)
        : base(handle, transfer) { }

    // Companion fields on FontWeight are private — the public surface
    // is the `getXxx()` getters on the Companion class. (Decompiled the
    // 1.11.1 jar with javap to confirm.) The pattern is:
    //   FontWeight$Companion.getThin() : FontWeight
    static IntPtr s_companion;
    static unsafe IntPtr Companion()
    {
        if (s_companion == IntPtr.Zero)
        {
            IntPtr fontWeightCls = JNIEnv.FindClass("androidx/compose/ui/text/font/FontWeight");
            IntPtr fid = JNIEnv.GetStaticFieldID(fontWeightCls, "Companion", "Landroidx/compose/ui/text/font/FontWeight$Companion;");
            IntPtr local = JNIEnv.GetStaticObjectField(fontWeightCls, fid);
            s_companion = JNIEnv.NewGlobalRef(local);
            JNIEnv.DeleteLocalRef(local);
        }
        return s_companion;
    }

    static FontWeight Resolve(string getterName)
    {
        IntPtr cls = JNIEnv.FindClass("androidx/compose/ui/text/font/FontWeight$Companion");
        IntPtr mid = JNIEnv.GetMethodID(cls, getterName, "()Landroidx/compose/ui/text/font/FontWeight;");
        IntPtr companion = Companion();
        IntPtr value = JNIEnv.CallObjectMethod(companion, mid);
        return new FontWeight(value, JniHandleOwnership.TransferLocalRef);
    }

    static FontWeight? s_thin, s_extraLight, s_light, s_normal, s_medium, s_semiBold, s_bold, s_extraBold, s_black;

    /// <summary><c>FontWeight.Thin</c> (W100).</summary>
    public static FontWeight Thin => s_thin ??= Resolve("getThin");
    /// <summary><c>FontWeight.ExtraLight</c> (W200).</summary>
    public static FontWeight ExtraLight => s_extraLight ??= Resolve("getExtraLight");
    /// <summary><c>FontWeight.Light</c> (W300).</summary>
    public static FontWeight Light => s_light ??= Resolve("getLight");
    /// <summary><c>FontWeight.Normal</c> (W400). The default.</summary>
    public static FontWeight Normal => s_normal ??= Resolve("getNormal");
    /// <summary><c>FontWeight.Medium</c> (W500).</summary>
    public static FontWeight Medium => s_medium ??= Resolve("getMedium");
    /// <summary><c>FontWeight.SemiBold</c> (W600).</summary>
    public static FontWeight SemiBold => s_semiBold ??= Resolve("getSemiBold");
    /// <summary><c>FontWeight.Bold</c> (W700).</summary>
    public static FontWeight Bold => s_bold ??= Resolve("getBold");
    /// <summary><c>FontWeight.ExtraBold</c> (W800).</summary>
    public static FontWeight ExtraBold => s_extraBold ??= Resolve("getExtraBold");
    /// <summary><c>FontWeight.Black</c> (W900).</summary>
    public static FontWeight Black => s_black ??= Resolve("getBlack");
}
