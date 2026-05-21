using Android.Runtime;
using Androidx.Compose.Runtime;
using Androidx.Compose.UI;
using Java.Interop;

namespace ComposeNet.Sample;

// Raw-JNI bridges for Compose APIs that the dotnet/android binding generator
// strips. Background:
//   * Compose @Composable functions don't get $default overloads (the Kotlin
//     compiler plugin handles defaults via a trailing `int $default` mask on
//     the regular method, not via a synthetic *$default sibling).
//   * The .NET-for-Android binding generator therefore can't see "defaults"
//     and surfaces only the full-argument form, which often has inline-class
//     params (TextStyle, ColorProducer, etc.) it then removes anyway.
// So we call directly via JNI.
internal static class ComposeApi
{
    static IModifier? s_modifier;
    public static IModifier ModifierCompanion =>
        s_modifier ??= FetchModifierCompanion();

    static IModifier FetchModifierCompanion()
    {
        // androidx.compose.ui.Modifier.Companion.$$INSTANCE
        // JNIEnv.FindClass returns a *global* ref in .NET-for-Android, so don't
        // DeleteLocalRef on it. GetStaticObjectField returns a real local ref.
        IntPtr classRef = JNIEnv.FindClass("androidx/compose/ui/Modifier$Companion");
        IntPtr fieldId  = JNIEnv.GetStaticFieldID(classRef, "$$INSTANCE", "Landroidx/compose/ui/Modifier$Companion;");
        IntPtr instance = JNIEnv.GetStaticObjectField(classRef, fieldId);
        return Java.Lang.Object.GetObject<IModifier>(instance, JniHandleOwnership.TransferLocalRef)!;
    }

    // androidx.compose.foundation.text.BasicTextKt.BasicText-BpD7jsM(
    //     String text, Modifier modifier, TextStyle style,
    //     Function1<TextLayoutResult,Unit> onTextLayout,
    //     int overflow /* TextOverflow */, boolean softWrap, int maxLines,
    //     Composer $composer, int $changed, int $default)
    // The trailing $default mask lets us pass null/0 for params we want defaulted.
    //   bit 0 text, bit 1 modifier, bit 2 style, bit 3 onTextLayout,
    //   bit 4 overflow, bit 5 softWrap, bit 6 maxLines
    const string BasicTextSig =
        "(Ljava/lang/String;Landroidx/compose/ui/Modifier;Landroidx/compose/ui/text/TextStyle;" +
        "Lkotlin/jvm/functions/Function1;IZILandroidx/compose/runtime/Composer;II)V";

    static IntPtr s_basicTextClass;
    static IntPtr s_basicTextMethod;

    public static unsafe void BasicText(string text, IModifier? modifier, IComposer composer)
    {
        if (s_basicTextClass == IntPtr.Zero)
        {
            s_basicTextClass  = JNIEnv.FindClass("androidx/compose/foundation/text/BasicTextKt");
            s_basicTextMethod = JNIEnv.GetStaticMethodID(s_basicTextClass, "BasicText-BpD7jsM", BasicTextSig);
        }

        int defaults = 0b1111110;  // bits 1..6 set
        if (modifier != null) defaults &= ~0b10;

        IntPtr textRef = JNIEnv.NewString(text);
        try
        {
            JValue* args = stackalloc JValue[10];
            args[0] = new JValue(textRef);
            args[1] = new JValue(modifier != null ? ((Java.Lang.Object)modifier).Handle : IntPtr.Zero);
            args[2] = new JValue(IntPtr.Zero);   // style: default
            args[3] = new JValue(IntPtr.Zero);   // onTextLayout: default
            args[4] = new JValue(0);             // overflow: default
            args[5] = new JValue(false);         // softWrap: default
            args[6] = new JValue(0);             // maxLines: default
            args[7] = new JValue(((Java.Lang.Object)composer).Handle);
            args[8] = new JValue(0);             // $changed
            args[9] = new JValue(defaults);
            JNIEnv.CallStaticVoidMethod(s_basicTextClass, s_basicTextMethod, args);
        }
        finally
        {
            JNIEnv.DeleteLocalRef(textRef);
        }
    }
}
