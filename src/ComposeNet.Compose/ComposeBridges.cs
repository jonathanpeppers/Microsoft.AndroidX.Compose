using Android.Runtime;
using AndroidX.Compose.Runtime;
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
        int defaults = (int)TextDefault.All;

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
        int defaults = (int)ButtonDefault.All;

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

    // androidx.compose.material3.IconButtonKt.IconButton(onClick, modifier,
    //   enabled, colors, interactionSource, content, composer, $changed, $default)
    const string IconButtonSig =
        "(Lkotlin/jvm/functions/Function0;Landroidx/compose/ui/Modifier;Z" +
        "Landroidx/compose/material3/IconButtonColors;" +
        "Landroidx/compose/foundation/interaction/MutableInteractionSource;" +
        "Lkotlin/jvm/functions/Function2;Landroidx/compose/runtime/Composer;II)V";

    static IntPtr s_iconButtonClass;
    static IntPtr s_iconButtonMethod;

    public static unsafe void IconButton(IFunction0 onClick, IFunction2 content, IComposer composer)
    {
        if (s_iconButtonClass == IntPtr.Zero)
        {
            s_iconButtonClass  = JNIEnv.FindClass("androidx/compose/material3/IconButtonKt");
            s_iconButtonMethod = JNIEnv.GetStaticMethodID(s_iconButtonClass, "IconButton", IconButtonSig);
        }

        JValue* args = stackalloc JValue[9];
        args[0] = new JValue(((Java.Lang.Object)onClick).Handle);
        args[1] = new JValue(IntPtr.Zero);
        args[2] = new JValue(true);
        args[3] = new JValue(IntPtr.Zero);
        args[4] = new JValue(IntPtr.Zero);
        args[5] = new JValue(((Java.Lang.Object)content).Handle);
        args[6] = new JValue(((Java.Lang.Object)composer).Handle);
        args[7] = new JValue(0);
        args[8] = new JValue((int)IconButtonDefault.All);
        JNIEnv.CallStaticVoidMethod(s_iconButtonClass, s_iconButtonMethod, args);
    }

    // androidx.compose.material3.FloatingActionButtonKt.FloatingActionButton-X-z6DiA(
    //   onClick, modifier, shape, containerColor, contentColor, elevation,
    //   interactionSource, content, composer, $changed, $default)
    const string FabSig =
        "(Lkotlin/jvm/functions/Function0;Landroidx/compose/ui/Modifier;" +
        "Landroidx/compose/ui/graphics/Shape;JJ" +
        "Landroidx/compose/material3/FloatingActionButtonElevation;" +
        "Landroidx/compose/foundation/interaction/MutableInteractionSource;" +
        "Lkotlin/jvm/functions/Function2;Landroidx/compose/runtime/Composer;II)V";

    static IntPtr s_fabClass;
    static IntPtr s_fabMethod;

    public static unsafe void FloatingActionButton(IFunction0 onClick, IFunction2 content, IComposer composer)
    {
        if (s_fabClass == IntPtr.Zero)
        {
            s_fabClass  = JNIEnv.FindClass("androidx/compose/material3/FloatingActionButtonKt");
            s_fabMethod = JNIEnv.GetStaticMethodID(s_fabClass, "FloatingActionButton-X-z6DiA", FabSig);
        }

        JValue* args = stackalloc JValue[11];
        args[0]  = new JValue(((Java.Lang.Object)onClick).Handle);
        args[1]  = new JValue(IntPtr.Zero); // modifier
        args[2]  = new JValue(IntPtr.Zero); // shape
        args[3]  = new JValue(0L);          // containerColor
        args[4]  = new JValue(0L);          // contentColor
        args[5]  = new JValue(IntPtr.Zero); // elevation
        args[6]  = new JValue(IntPtr.Zero); // interactionSource
        args[7]  = new JValue(((Java.Lang.Object)content).Handle);
        args[8]  = new JValue(((Java.Lang.Object)composer).Handle);
        args[9]  = new JValue(0);
        args[10] = new JValue((int)FloatingActionButtonDefault.All);
        JNIEnv.CallStaticVoidMethod(s_fabClass, s_fabMethod, args);
    }

    // androidx.compose.material3.SurfaceKt.Surface-T9BRK9s (non-interactive):
    //   (modifier, shape, color, contentColor, tonalElevation, shadowElevation,
    //    border, content, composer, $changed, $default)
    const string SurfaceSig =
        "(Landroidx/compose/ui/Modifier;Landroidx/compose/ui/graphics/Shape;JJFF" +
        "Landroidx/compose/foundation/BorderStroke;" +
        "Lkotlin/jvm/functions/Function2;Landroidx/compose/runtime/Composer;II)V";

    static IntPtr s_surfaceClass;
    static IntPtr s_surfaceMethod;

    public static unsafe void Surface(IFunction2 content, IComposer composer)
    {
        if (s_surfaceClass == IntPtr.Zero)
        {
            s_surfaceClass  = JNIEnv.FindClass("androidx/compose/material3/SurfaceKt");
            s_surfaceMethod = JNIEnv.GetStaticMethodID(s_surfaceClass, "Surface-T9BRK9s", SurfaceSig);
        }

        JValue* args = stackalloc JValue[11];
        args[0]  = new JValue(IntPtr.Zero); // modifier
        args[1]  = new JValue(IntPtr.Zero); // shape
        args[2]  = new JValue(0L);          // color
        args[3]  = new JValue(0L);          // contentColor
        args[4]  = new JValue(0f);          // tonalElevation
        args[5]  = new JValue(0f);          // shadowElevation
        args[6]  = new JValue(IntPtr.Zero); // border
        args[7]  = new JValue(((Java.Lang.Object)content).Handle);
        args[8]  = new JValue(((Java.Lang.Object)composer).Handle);
        args[9]  = new JValue(0);
        args[10] = new JValue((int)SurfaceDefault.All);
        JNIEnv.CallStaticVoidMethod(s_surfaceClass, s_surfaceMethod, args);
    }

    // androidx.compose.material3.TextFieldKt.TextField (String overload):
    //   (value, onValueChange, modifier, enabled, readOnly, textStyle, label,
    //    placeholder, leadingIcon, trailingIcon, prefix, suffix, supportingText,
    //    isError, visualTransformation, keyboardOptions, keyboardActions,
    //    singleLine, maxLines, minLines, interactionSource, shape, colors,
    //    composer, $changed, $changed1, $changed2, $default) — 23 user params,
    //    3x $changed, 1x $default.
    const string TextFieldStringSig =
        "(Ljava/lang/String;Lkotlin/jvm/functions/Function1;Landroidx/compose/ui/Modifier;ZZ" +
        "Landroidx/compose/ui/text/TextStyle;" +
        "Lkotlin/jvm/functions/Function2;Lkotlin/jvm/functions/Function2;" +
        "Lkotlin/jvm/functions/Function2;Lkotlin/jvm/functions/Function2;" +
        "Lkotlin/jvm/functions/Function2;Lkotlin/jvm/functions/Function2;" +
        "Lkotlin/jvm/functions/Function2;Z" +
        "Landroidx/compose/ui/text/input/VisualTransformation;" +
        "Landroidx/compose/foundation/text/KeyboardOptions;" +
        "Landroidx/compose/foundation/text/KeyboardActions;ZII" +
        "Landroidx/compose/foundation/interaction/MutableInteractionSource;" +
        "Landroidx/compose/ui/graphics/Shape;" +
        "Landroidx/compose/material3/TextFieldColors;" +
        "Landroidx/compose/runtime/Composer;IIII)V";

    static IntPtr s_textFieldClass;
    static IntPtr s_textFieldMethod;
    static IntPtr s_outlinedTextFieldClass;
    static IntPtr s_outlinedTextFieldMethod;

    public static unsafe void TextField(string value, IFunction1 onValueChange, IComposer composer)
    {
        if (s_textFieldClass == IntPtr.Zero)
        {
            s_textFieldClass  = JNIEnv.FindClass("androidx/compose/material3/TextFieldKt");
            s_textFieldMethod = JNIEnv.GetStaticMethodID(s_textFieldClass, "TextField", TextFieldStringSig);
        }
        InvokeTextField(s_textFieldClass, s_textFieldMethod, value, onValueChange, composer, (int)TextFieldDefault.All);
    }

    public static unsafe void OutlinedTextField(string value, IFunction1 onValueChange, IComposer composer)
    {
        if (s_outlinedTextFieldClass == IntPtr.Zero)
        {
            s_outlinedTextFieldClass  = JNIEnv.FindClass("androidx/compose/material3/OutlinedTextFieldKt");
            s_outlinedTextFieldMethod = JNIEnv.GetStaticMethodID(s_outlinedTextFieldClass, "OutlinedTextField", TextFieldStringSig);
        }
        InvokeTextField(s_outlinedTextFieldClass, s_outlinedTextFieldMethod, value, onValueChange, composer, (int)TextFieldDefault.All);
    }

    static unsafe void InvokeTextField(IntPtr cls, IntPtr method, string value, IFunction1 onValueChange, IComposer composer, int defaults)
    {
        IntPtr valueRef = JNIEnv.NewString(value);
        try
        {
            JValue* args = stackalloc JValue[28];
            args[0]  = new JValue(valueRef);
            args[1]  = new JValue(((Java.Lang.Object)onValueChange).Handle);
            args[2]  = new JValue(IntPtr.Zero); // modifier
            args[3]  = new JValue(true);        // enabled
            args[4]  = new JValue(false);       // readOnly
            args[5]  = new JValue(IntPtr.Zero); // textStyle
            args[6]  = new JValue(IntPtr.Zero); // label
            args[7]  = new JValue(IntPtr.Zero); // placeholder
            args[8]  = new JValue(IntPtr.Zero); // leadingIcon
            args[9]  = new JValue(IntPtr.Zero); // trailingIcon
            args[10] = new JValue(IntPtr.Zero); // prefix
            args[11] = new JValue(IntPtr.Zero); // suffix
            args[12] = new JValue(IntPtr.Zero); // supportingText
            args[13] = new JValue(false);       // isError
            args[14] = new JValue(IntPtr.Zero); // visualTransformation
            args[15] = new JValue(IntPtr.Zero); // keyboardOptions
            args[16] = new JValue(IntPtr.Zero); // keyboardActions
            args[17] = new JValue(false);       // singleLine
            args[18] = new JValue(0);           // maxLines
            args[19] = new JValue(0);           // minLines
            args[20] = new JValue(IntPtr.Zero); // interactionSource
            args[21] = new JValue(IntPtr.Zero); // shape
            args[22] = new JValue(IntPtr.Zero); // colors
            args[23] = new JValue(((Java.Lang.Object)composer).Handle);
            args[24] = new JValue(0);           // $changed
            args[25] = new JValue(0);           // $changed1
            args[26] = new JValue(0);           // $changed2
            args[27] = new JValue(defaults);
            JNIEnv.CallStaticVoidMethod(cls, method, args);
        }
        finally
        {
            JNIEnv.DeleteLocalRef(valueRef);
        }
    }
}
