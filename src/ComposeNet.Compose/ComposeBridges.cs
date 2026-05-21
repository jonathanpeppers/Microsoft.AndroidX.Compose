using Android.Runtime;
using Androidx.Compose.Runtime;
using Java.Interop;
using Kotlin.Jvm.Functions;

namespace ComposeNet;

// Raw-JNI bridges to Compose functions the .NET-for-Android binding generator
// can't see (Compose @Composable functions don't get $default sibling overloads
// — the trailing $default bitmask lives on the regular method). Same machinery
// the sample used in ComposeApi.cs, just moved into the facade and made
// internal — user code never touches these directly.
internal static class ComposeBridges
{
    // androidx.compose.material3.TextKt.Text--4IGK_g(text, modifier, color,
    //   fontSize, fontStyle, fontWeight, fontFamily, letterSpacing, decoration,
    //   align, lineHeight, overflow, softWrap, maxLines, minLines, onTextLayout,
    //   style, composer, $changed, $changed1, $default)
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

        // Everything but `text` is defaulted.
        int defaults = (int)(TextDefault.Modifier | TextDefault.Color | TextDefault.FontSize
                           | TextDefault.FontStyle | TextDefault.FontWeight | TextDefault.FontFamily
                           | TextDefault.LetterSpacing | TextDefault.Decoration | TextDefault.Align
                           | TextDefault.LineHeight | TextDefault.Overflow | TextDefault.SoftWrap
                           | TextDefault.MaxLines | TextDefault.MinLines | TextDefault.OnTextLayout
                           | TextDefault.Style);

        IntPtr textRef = JNIEnv.NewString(text);
        try
        {
            JValue* args = stackalloc JValue[21];
            args[0]  = new JValue(textRef);
            args[1]  = new JValue(IntPtr.Zero); // modifier
            args[2]  = new JValue(0L);          // color = Unspecified
            args[3]  = new JValue(0L);          // fontSize
            args[4]  = new JValue(IntPtr.Zero); // fontStyle
            args[5]  = new JValue(IntPtr.Zero); // fontWeight
            args[6]  = new JValue(IntPtr.Zero); // fontFamily
            args[7]  = new JValue(0L);          // letterSpacing
            args[8]  = new JValue(IntPtr.Zero); // decoration
            args[9]  = new JValue(IntPtr.Zero); // align
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

    // androidx.compose.material3.ButtonKt.Button(onClick, modifier, enabled,
    //   shape, colors, elevation, border, contentPadding, interactionSource,
    //   content, composer, $changed, $default)
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

        // onClick (bit 0) and content (bit 9) are provided; default everything else.
        int defaults = (int)(ButtonDefault.Modifier | ButtonDefault.Enabled
                           | ButtonDefault.Shape | ButtonDefault.Colors
                           | ButtonDefault.Elevation | ButtonDefault.Border
                           | ButtonDefault.ContentPadding | ButtonDefault.InteractionSource);

        JValue* args = stackalloc JValue[13];
        args[0]  = new JValue(((Java.Lang.Object)onClick).Handle);
        args[1]  = new JValue(IntPtr.Zero);
        args[2]  = new JValue(true);
        args[3]  = new JValue(IntPtr.Zero);
        args[4]  = new JValue(IntPtr.Zero);
        args[5]  = new JValue(IntPtr.Zero);
        args[6]  = new JValue(IntPtr.Zero);
        args[7]  = new JValue(IntPtr.Zero);
        args[8]  = new JValue(IntPtr.Zero);
        args[9]  = new JValue(((Java.Lang.Object)content).Handle);
        args[10] = new JValue(((Java.Lang.Object)composer).Handle);
        args[11] = new JValue(0);
        args[12] = new JValue(defaults);
        JNIEnv.CallStaticVoidMethod(s_buttonClass, s_buttonMethod, args);
    }
}
