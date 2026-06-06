using Android.Runtime;

namespace ComposeNet;

/// <summary>
/// C# wrapper around <c>androidx.compose.ui.text.font.FontStyle</c>.
/// In Kotlin source, <c>FontStyle</c> is a <c>@JvmInline value class</c>
/// wrapping an <c>Int</c>, but every <c>@Composable</c> function in
/// Material 3 declares it as <em>nullable</em> (<c>fontStyle: FontStyle?
/// = null</c>) — so at the JNI boundary it travels as a boxed
/// <c>androidx/compose/ui/text/font/FontStyle;</c> reference, not a
/// packed <c>int</c>. The bridge generator's reference-type path
/// passes the handle through to the JNI <c>L</c> slot.
///
/// Same trick as <see cref="TextAlign"/>: call the mangled
/// <c>Companion.getNormal-_-LCdwA()I</c> for the packed int, then
/// route through static <c>FontStyle.box-impl(I)LFontStyle;</c>.
///
/// Will swap to bound <c>AndroidX.Compose.UI.Text.Font.FontStyle</c>
/// once <see href="https://github.com/dotnet/android-libraries/pull/1440"/>
/// ships.
/// </summary>
public sealed class FontStyle : Java.Lang.Object
{
    FontStyle(IntPtr handle, JniHandleOwnership transfer)
        : base(handle, transfer) { }

    static IntPtr s_companion;
    static IntPtr s_box;

    static unsafe IntPtr Companion()
    {
        if (s_companion == IntPtr.Zero)
        {
            IntPtr cls = JNIEnv.FindClass("androidx/compose/ui/text/font/FontStyle");
            IntPtr fid = JNIEnv.GetStaticFieldID(cls, "Companion", "Landroidx/compose/ui/text/font/FontStyle$Companion;");
            IntPtr local = JNIEnv.GetStaticObjectField(cls, fid);
            s_companion = JNIEnv.NewGlobalRef(local);
            JNIEnv.DeleteLocalRef(local);
        }
        return s_companion;
    }

    static IntPtr BoxMethod()
    {
        if (s_box == IntPtr.Zero)
        {
            IntPtr cls = JNIEnv.FindClass("androidx/compose/ui/text/font/FontStyle");
            s_box = JNIEnv.GetStaticMethodID(cls, "box-impl", "(I)Landroidx/compose/ui/text/font/FontStyle;");
        }
        return s_box;
    }

    static unsafe FontStyle Resolve(string mangledGetter)
    {
        IntPtr companionCls = JNIEnv.FindClass("androidx/compose/ui/text/font/FontStyle$Companion");
        IntPtr getter = JNIEnv.GetMethodID(companionCls, mangledGetter, "()I");
        int packed = JNIEnv.CallIntMethod(Companion(), getter);

        IntPtr cls = JNIEnv.FindClass("androidx/compose/ui/text/font/FontStyle");
        JValue* args = stackalloc JValue[1];
        args[0] = new JValue(packed);
        IntPtr boxed = JNIEnv.CallStaticObjectMethod(cls, BoxMethod(), args);
        return new FontStyle(boxed, JniHandleOwnership.TransferLocalRef);
    }

    static FontStyle? s_normal, s_italic;

    /// <summary>Upright glyphs (the default).</summary>
    public static FontStyle Normal => s_normal ??= Resolve("getNormal-_-LCdwA");

    /// <summary>Italic / slanted glyphs.</summary>
    public static FontStyle Italic => s_italic ??= Resolve("getItalic-_-LCdwA");
}

