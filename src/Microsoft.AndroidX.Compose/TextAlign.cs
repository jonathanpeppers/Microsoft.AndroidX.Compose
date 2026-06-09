using global::Android.Runtime;

namespace Microsoft.AndroidX.Compose;

/// <summary>
/// C# wrapper around <c>androidx.compose.ui.text.style.TextAlign</c>.
/// In Kotlin source, <c>TextAlign</c> is a <c>@JvmInline value class</c>
/// wrapping an <c>Int</c>, but every <c>@Composable</c> function in
/// Material 3 declares it as <em>nullable</em> (<c>textAlign: TextAlign?
/// = null</c>) — so at the JNI boundary it travels as a boxed
/// <c>androidx/compose/ui/text/style/TextAlign;</c> reference, not a
/// packed <c>int</c>. The bridge generator's reference-type path
/// passes the handle through to the JNI <c>L</c> slot.
///
/// To produce that boxed reference the constants here call the
/// inline-class's mangled <c>Companion.getCenter-e0LSkKk()I</c>
/// (and friends) for the packed int, then route it through the
/// synthesized static <c>TextAlign.box-impl(I)LTextAlign;</c> to wrap.
///
/// Will swap to bound <c>global::AndroidX.Compose.UI.Text.Style.TextAlign</c>
/// once <see href="https://github.com/dotnet/android-libraries/pull/1440"/>
/// ships and we adopt the next <c>Xamarin.AndroidX.Compose.UI.Text.Android</c>
/// release.
/// </summary>
public sealed class TextAlign : Java.Lang.Object
{
    TextAlign(IntPtr handle, JniHandleOwnership transfer)
        : base(handle, transfer) { }

    static IntPtr s_companion;
    static IntPtr s_box;

    static unsafe IntPtr Companion()
    {
        if (s_companion == IntPtr.Zero)
        {
            IntPtr cls = JNIEnv.FindClass("androidx/compose/ui/text/style/TextAlign");
            IntPtr fid = JNIEnv.GetStaticFieldID(cls, "Companion", "Landroidx/compose/ui/text/style/TextAlign$Companion;");
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
            IntPtr cls = JNIEnv.FindClass("androidx/compose/ui/text/style/TextAlign");
            s_box = JNIEnv.GetStaticMethodID(cls, "box-impl", "(I)Landroidx/compose/ui/text/style/TextAlign;");
        }
        return s_box;
    }

    static unsafe TextAlign Resolve(string mangledGetter)
    {
        // 1. Companion.<getter>()I returns the packed int.
        IntPtr companionCls = JNIEnv.FindClass("androidx/compose/ui/text/style/TextAlign$Companion");
        IntPtr getter = JNIEnv.GetMethodID(companionCls, mangledGetter, "()I");
        int packed = JNIEnv.CallIntMethod(Companion(), getter);

        // 2. TextAlign.box-impl(int) -> Landroidx/.../TextAlign;
        IntPtr cls = JNIEnv.FindClass("androidx/compose/ui/text/style/TextAlign");
        JValue* args = stackalloc JValue[1];
        args[0] = new JValue(packed);
        IntPtr boxed = JNIEnv.CallStaticObjectMethod(cls, BoxMethod(), args);
        return new TextAlign(boxed, JniHandleOwnership.TransferLocalRef);
    }

    static TextAlign? s_left, s_right, s_center, s_justify, s_start, s_end, s_unspecified;

    /// <summary>Align text to the left edge of the container.</summary>
    public static TextAlign Left => s_left ??= Resolve("getLeft-e0LSkKk");

    /// <summary>Align text to the right edge of the container.</summary>
    public static TextAlign Right => s_right ??= Resolve("getRight-e0LSkKk");

    /// <summary>Center text within the container.</summary>
    public static TextAlign Center => s_center ??= Resolve("getCenter-e0LSkKk");

    /// <summary>Stretch lines to fill the container width.</summary>
    public static TextAlign Justify => s_justify ??= Resolve("getJustify-e0LSkKk");

    /// <summary>Align text to the layout-direction start edge.</summary>
    public static TextAlign Start => s_start ??= Resolve("getStart-e0LSkKk");

    /// <summary>Align text to the layout-direction end edge.</summary>
    public static TextAlign End => s_end ??= Resolve("getEnd-e0LSkKk");

    /// <summary>The unspecified-alignment sentinel value (Kotlin's default for nullable slots).</summary>
    public static TextAlign Unspecified => s_unspecified ??= Resolve("getUnspecified-e0LSkKk");
}

