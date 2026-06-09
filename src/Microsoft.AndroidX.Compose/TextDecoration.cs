using global::Android.Runtime;

namespace Microsoft.AndroidX.Compose;

/// <summary>
/// C# wrapper around <c>androidx.compose.ui.text.style.TextDecoration</c>.
/// Compose's <c>TextDecoration</c> is a real Kotlin class (not a value
/// class), with companion constants for <c>None</c>, <c>Underline</c>,
/// and <c>LineThrough</c>. Same plumbing story as <see cref="FontWeight"/>
/// — <c>ui-text-android</c> ships as a Java-library-only stub today, so
/// we resolve via JNI. Will swap to bound
/// <c>global::AndroidX.Compose.UI.Text.Style.TextDecoration</c> once
/// <see href="https://github.com/dotnet/android-libraries/issues/1439"/>
/// ships.
/// </summary>
public sealed class TextDecoration : Java.Lang.Object
{
    TextDecoration(IntPtr handle, JniHandleOwnership transfer)
        : base(handle, transfer) { }

    // Companion fields on TextDecoration are private — the public
    // surface is the `getXxx()` getters on the Companion class. (Same
    // story as FontWeight.)
    static IntPtr s_companion;
    static unsafe IntPtr Companion()
    {
        if (s_companion == IntPtr.Zero)
        {
            IntPtr outerCls = JNIEnv.FindClass("androidx/compose/ui/text/style/TextDecoration");
            IntPtr fid = JNIEnv.GetStaticFieldID(outerCls, "Companion", "Landroidx/compose/ui/text/style/TextDecoration$Companion;");
            IntPtr local = JNIEnv.GetStaticObjectField(outerCls, fid);
            s_companion = JNIEnv.NewGlobalRef(local);
            JNIEnv.DeleteLocalRef(local);
        }
        return s_companion;
    }

    static TextDecoration Resolve(string getterName)
    {
        IntPtr cls = JNIEnv.FindClass("androidx/compose/ui/text/style/TextDecoration$Companion");
        IntPtr mid = JNIEnv.GetMethodID(cls, getterName, "()Landroidx/compose/ui/text/style/TextDecoration;");
        IntPtr companion = Companion();
        IntPtr value = JNIEnv.CallObjectMethod(companion, mid);
        return new TextDecoration(value, JniHandleOwnership.TransferLocalRef);
    }

    static TextDecoration? s_none, s_underline, s_lineThrough;

    /// <summary><c>TextDecoration.None</c>.</summary>
    public static TextDecoration None => s_none ??= Resolve("getNone");
    /// <summary><c>TextDecoration.Underline</c>.</summary>
    public static TextDecoration Underline => s_underline ??= Resolve("getUnderline");
    /// <summary><c>TextDecoration.LineThrough</c>.</summary>
    public static TextDecoration LineThrough => s_lineThrough ??= Resolve("getLineThrough");
}
