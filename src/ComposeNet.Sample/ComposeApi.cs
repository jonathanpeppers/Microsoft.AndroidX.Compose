using Android.Runtime;
using Androidx.Compose.Runtime;
using Androidx.Compose.UI;
using Java.Interop;
using Kotlin.Jvm.Functions;

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
        var modifier = Java.Lang.Object.GetObject<IModifier>(instance, JniHandleOwnership.TransferLocalRef);
        System.Diagnostics.Debug.Assert(modifier != null, "Modifier.Companion.$$INSTANCE should always resolve");
        return modifier;
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

    // androidx.compose.material3.ButtonKt.Button(
    //     Function0 onClick, Modifier modifier, boolean enabled, Shape shape,
    //     ButtonColors colors, ButtonElevation elevation, BorderStroke border,
    //     PaddingValues contentPadding, MutableInteractionSource interactionSource,
    //     Function3<RowScope,Composer,Integer,Unit> content,
    //     Composer $composer, int $changed, int $default)
    //   $default bits 0..9 ↔ the 10 user-facing parameters.
    //   bit 0 onClick, bit 9 content → both provided, others defaulted ⇒
    //   $default = 0b1111111110 (everything except onClick=bit0 and content=bit9).
    const string ButtonSig =
        "(Lkotlin/jvm/functions/Function0;Landroidx/compose/ui/Modifier;Z" +
        "Landroidx/compose/ui/graphics/Shape;Landroidx/compose/material3/ButtonColors;" +
        "Landroidx/compose/material3/ButtonElevation;Landroidx/compose/foundation/BorderStroke;" +
        "Landroidx/compose/foundation/layout/PaddingValues;" +
        "Landroidx/compose/foundation/interaction/MutableInteractionSource;" +
        "Lkotlin/jvm/functions/Function3;Landroidx/compose/runtime/Composer;II)V";

    static IntPtr s_buttonClass;
    static IntPtr s_buttonMethod;

    public static unsafe void Button(IFunction0 onClick, IFunction3 content, IComposer composer)
    {
        if (s_buttonClass == IntPtr.Zero)
        {
            s_buttonClass  = JNIEnv.FindClass("androidx/compose/material3/ButtonKt");
            s_buttonMethod = JNIEnv.GetStaticMethodID(s_buttonClass, "Button", ButtonSig);
        }

        // bit 0 onClick (provided), bit 9 content (provided) → mask off bits 0 and 9.
        int defaults = 0b0111111110;

        JValue* args = stackalloc JValue[13];
        args[0]  = new JValue(((Java.Lang.Object)onClick).Handle);
        args[1]  = new JValue(IntPtr.Zero); // modifier
        args[2]  = new JValue(true);        // enabled
        args[3]  = new JValue(IntPtr.Zero); // shape
        args[4]  = new JValue(IntPtr.Zero); // colors
        args[5]  = new JValue(IntPtr.Zero); // elevation
        args[6]  = new JValue(IntPtr.Zero); // border
        args[7]  = new JValue(IntPtr.Zero); // contentPadding
        args[8]  = new JValue(IntPtr.Zero); // interactionSource
        args[9]  = new JValue(((Java.Lang.Object)content).Handle);
        args[10] = new JValue(((Java.Lang.Object)composer).Handle);
        args[11] = new JValue(0);           // $changed
        args[12] = new JValue(defaults);
        JNIEnv.CallStaticVoidMethod(s_buttonClass, s_buttonMethod, args);
    }

    // androidx.compose.material3.TextKt.Text--4IGK_g(
    //     String text, Modifier modifier, long color, long fontSize,
    //     FontStyle, FontWeight, FontFamily, long letterSpacing,
    //     TextDecoration, TextAlign, long lineHeight, int overflow,
    //     boolean softWrap, int maxLines, int minLines, Function1 onTextLayout,
    //     TextStyle style, Composer, int $changed, int $changed1, int $default)
    //   17 user params. Pass text only; set all other $default bits so the
    //   call uses Material defaults. Crucially color=0L (Color.Unspecified)
    //   means "read LocalContentColor from the composition" — inside a
    //   Material Button that resolves to onPrimary (white on the blue
    //   default primary).
    const string MaterialTextSig =
        "(Ljava/lang/String;Landroidx/compose/ui/Modifier;JJ" +
        "Landroidx/compose/ui/text/font/FontStyle;" +
        "Landroidx/compose/ui/text/font/FontWeight;" +
        "Landroidx/compose/ui/text/font/FontFamily;J" +
        "Landroidx/compose/ui/text/style/TextDecoration;" +
        "Landroidx/compose/ui/text/style/TextAlign;JIZII" +
        "Lkotlin/jvm/functions/Function1;Landroidx/compose/ui/text/TextStyle;" +
        "Landroidx/compose/runtime/Composer;III)V";

    static IntPtr s_textClass;
    static IntPtr s_textMethod;

    public static unsafe void Text(string text, IComposer composer)
    {
        if (s_textClass == IntPtr.Zero)
        {
            s_textClass  = JNIEnv.FindClass("androidx/compose/material3/TextKt");
            s_textMethod = JNIEnv.GetStaticMethodID(s_textClass, "Text--4IGK_g", MaterialTextSig);
        }

        // 17 user params, bit 0 = text (provided), bits 1..16 = use default.
        int defaults = 0x1FFFE;

        IntPtr textRef = JNIEnv.NewString(text);
        try
        {
            JValue* args = stackalloc JValue[21];
            args[0]  = new JValue(textRef);
            args[1]  = new JValue(IntPtr.Zero); // modifier
            args[2]  = new JValue(0L);          // color = Unspecified
            args[3]  = new JValue(0L);          // fontSize = Unspecified
            args[4]  = new JValue(IntPtr.Zero); // FontStyle
            args[5]  = new JValue(IntPtr.Zero); // FontWeight
            args[6]  = new JValue(IntPtr.Zero); // FontFamily
            args[7]  = new JValue(0L);          // letterSpacing
            args[8]  = new JValue(IntPtr.Zero); // TextDecoration
            args[9]  = new JValue(IntPtr.Zero); // TextAlign
            args[10] = new JValue(0L);          // lineHeight
            args[11] = new JValue(0);           // overflow
            args[12] = new JValue(true);        // softWrap
            args[13] = new JValue(0);           // maxLines
            args[14] = new JValue(0);           // minLines
            args[15] = new JValue(IntPtr.Zero); // onTextLayout
            args[16] = new JValue(IntPtr.Zero); // style
            args[17] = new JValue(((Java.Lang.Object)composer).Handle);
            args[18] = new JValue(0);           // $changed
            args[19] = new JValue(0);           // $changed1
            args[20] = new JValue(defaults);    // $default
            JNIEnv.CallStaticVoidMethod(s_textClass, s_textMethod, args);
        }
        finally
        {
            JNIEnv.DeleteLocalRef(textRef);
        }
    }

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
