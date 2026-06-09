using Android.Runtime;

namespace ComposeNet;

/// <summary>
/// Cached accessor for <c>androidx.compose.ui.text.TextStyle.Companion</c> —
/// the singleton that exposes <c>Default</c> (and via that
/// <c>Default.toSpanStyle()</c>). The C# binding doesn't expose a static
/// accessor for the Companion field, so we read it through JNI once and
/// cache the wrapper for both <see cref="TextStyle"/> and
/// <see cref="SpanStyle"/> to share.
/// </summary>
static class TextStyleCompanion
{
    static IntPtr s_companion_ref;
    static AndroidX.Compose.UI.Text.TextStyle.Companion? s_companion;
    static AndroidX.Compose.UI.Text.TextStyle? s_default;
    static AndroidX.Compose.UI.Text.SpanStyle? s_defaultSpan;

    public static AndroidX.Compose.UI.Text.TextStyle.Companion Get()
    {
        if (s_companion is not null) return s_companion;
        if (s_companion_ref == IntPtr.Zero)
        {
            IntPtr cls   = JNIEnv.FindClass("androidx/compose/ui/text/TextStyle");
            IntPtr fid   = JNIEnv.GetStaticFieldID(cls, "Companion", "Landroidx/compose/ui/text/TextStyle$Companion;");
            IntPtr local = JNIEnv.GetStaticObjectField(cls, fid);
            s_companion_ref = JNIEnv.NewGlobalRef(local);
            JNIEnv.DeleteLocalRef(local);
        }
        s_companion = Java.Lang.Object.GetObject<AndroidX.Compose.UI.Text.TextStyle.Companion>(
            s_companion_ref, JniHandleOwnership.DoNotTransfer)!;
        return s_companion;
    }

    public static AndroidX.Compose.UI.Text.TextStyle Default =>
        s_default ??= Get().Default;

    public static AndroidX.Compose.UI.Text.SpanStyle DefaultSpan =>
        s_defaultSpan ??= Default.ToSpanStyle();
}
