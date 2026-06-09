using global::Android.Runtime;

namespace Microsoft.AndroidX.Compose;

/// <summary>
/// C# wrapper around <c>androidx.compose.ui.text.font.FontFamily</c>.
/// Compose's <c>FontFamily</c> is a real Kotlin class, but Compose
/// 1.11.2.1's <c>ui-text-android</c> package is shipped as a
/// Java-library-only stub with zero exported types — so the .NET
/// binding doesn't expose it yet. We subclass <see cref="Java.Lang.Object"/>
/// directly and resolve the Kotlin <c>Companion</c> instances via JNI;
/// the bridge generator's reference-type code path
/// (<c>x is null ? IntPtr.Zero : x.Handle</c>) passes the handle through
/// to the JNI <c>L</c> slot.
///
/// Will swap to bound <c>global::AndroidX.Compose.UI.Text.Font.FontFamily</c>
/// once <see href="https://github.com/dotnet/android-libraries/pull/1440"/>
/// ships and we adopt the next <c>Xamarin.AndroidX.Compose.UI.Text.Android</c>
/// release.
/// </summary>
public sealed class FontFamily : Java.Lang.Object
{
    FontFamily(IntPtr handle, JniHandleOwnership transfer)
        : base(handle, transfer) { }

    // FontFamily.Companion exposes generic-font-family getters:
    //   FontFamily$Companion.getDefault()    : FontFamily
    //   FontFamily$Companion.getSansSerif()  : FontFamily
    //   FontFamily$Companion.getSerif()      : FontFamily
    //   FontFamily$Companion.getMonospace()  : FontFamily
    //   FontFamily$Companion.getCursive()    : FontFamily
    static IntPtr s_companion;
    static unsafe IntPtr Companion()
    {
        if (s_companion == IntPtr.Zero)
        {
            IntPtr fontFamilyCls = JNIEnv.FindClass("androidx/compose/ui/text/font/FontFamily");
            IntPtr fid = JNIEnv.GetStaticFieldID(fontFamilyCls, "Companion", "Landroidx/compose/ui/text/font/FontFamily$Companion;");
            IntPtr local = JNIEnv.GetStaticObjectField(fontFamilyCls, fid);
            s_companion = JNIEnv.NewGlobalRef(local);
            JNIEnv.DeleteLocalRef(local);
        }
        return s_companion;
    }

    static FontFamily Resolve(string getterName, string returnDescriptor)
    {
        // FontFamily$Companion getters return concrete subtypes:
        //   getDefault()    -> SystemFontFamily
        //   getSansSerif()  -> GenericFontFamily
        //   getSerif()      -> GenericFontFamily
        //   getMonospace()  -> GenericFontFamily
        //   getCursive()    -> GenericFontFamily
        // All of these extend FontFamily, so we wrap as our base type.
        IntPtr cls = JNIEnv.FindClass("androidx/compose/ui/text/font/FontFamily$Companion");
        IntPtr mid = JNIEnv.GetMethodID(cls, getterName, $"(){returnDescriptor}");
        IntPtr companion = Companion();
        IntPtr value = JNIEnv.CallObjectMethod(companion, mid);
        return new FontFamily(value, JniHandleOwnership.TransferLocalRef);
    }

    const string SystemFontFamilyDescriptor  = "Landroidx/compose/ui/text/font/SystemFontFamily;";
    const string GenericFontFamilyDescriptor = "Landroidx/compose/ui/text/font/GenericFontFamily;";

    static FontFamily? s_default, s_sansSerif, s_serif, s_monospace, s_cursive;

    /// <summary>
    /// <c>FontFamily.Default</c> — the platform's system font family.
    /// </summary>
    public static FontFamily Default => s_default ??= Resolve("getDefault", SystemFontFamilyDescriptor);

    /// <summary>Generic sans-serif family (<c>FontFamily.SansSerif</c>).</summary>
    public static FontFamily SansSerif => s_sansSerif ??= Resolve("getSansSerif", GenericFontFamilyDescriptor);

    /// <summary>Generic serif family (<c>FontFamily.Serif</c>).</summary>
    public static FontFamily Serif => s_serif ??= Resolve("getSerif", GenericFontFamilyDescriptor);

    /// <summary>Generic monospace family (<c>FontFamily.Monospace</c>).</summary>
    public static FontFamily Monospace => s_monospace ??= Resolve("getMonospace", GenericFontFamilyDescriptor);

    /// <summary>Generic cursive family (<c>FontFamily.Cursive</c>).</summary>
    public static FontFamily Cursive => s_cursive ??= Resolve("getCursive", GenericFontFamilyDescriptor);
}
