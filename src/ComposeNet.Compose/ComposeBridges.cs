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

    // androidx.compose.material3.AndroidAlertDialog_androidKt.AlertDialog-Oix01E0(
    //   onDismissRequest, confirmButton, modifier, dismissButton, icon, title,
    //   text, shape, containerColor, iconContentColor, titleContentColor,
    //   textContentColor, tonalElevation, properties,
    //   composer, $changed, $changed1, $default)
    //
    // 14 user params, bit 0 = onDismissRequest, bit 1 = confirmButton
    // (both always provided); the four slot Function2s (dismissButton, icon,
    // title, text) are user-supplied — if any is null, set its $default bit
    // so Compose substitutes the real Kotlin default (also null).
    const string AlertDialogSig =
        "(Lkotlin/jvm/functions/Function0;Lkotlin/jvm/functions/Function2;" +
        "Landroidx/compose/ui/Modifier;" +
        "Lkotlin/jvm/functions/Function2;Lkotlin/jvm/functions/Function2;" +
        "Lkotlin/jvm/functions/Function2;Lkotlin/jvm/functions/Function2;" +
        "Landroidx/compose/ui/graphics/Shape;JJJJF" +
        "Landroidx/compose/ui/window/DialogProperties;" +
        "Landroidx/compose/runtime/Composer;III)V";

    static IntPtr s_alertDialogClass;
    static IntPtr s_alertDialogMethod;

    public static unsafe void AlertDialog(
        IFunction0  onDismissRequest,
        IFunction2  confirmButton,
        IFunction2? dismissButton,
        IFunction2? icon,
        IFunction2? title,
        IFunction2? text,
        int         defaults,
        IComposer   composer)
    {
        if (s_alertDialogClass == IntPtr.Zero)
        {
            s_alertDialogClass  = JNIEnv.FindClass("androidx/compose/material3/AndroidAlertDialog_androidKt");
            s_alertDialogMethod = JNIEnv.GetStaticMethodID(s_alertDialogClass, "AlertDialog-Oix01E0", AlertDialogSig);
        }

        JValue* args = stackalloc JValue[18];
        args[0]  = new JValue(((Java.Lang.Object)onDismissRequest).Handle);
        args[1]  = new JValue(((Java.Lang.Object)confirmButton).Handle);
        args[2]  = new JValue(IntPtr.Zero); // modifier
        args[3]  = new JValue(dismissButton is null ? IntPtr.Zero : ((Java.Lang.Object)dismissButton).Handle);
        args[4]  = new JValue(icon          is null ? IntPtr.Zero : ((Java.Lang.Object)icon).Handle);
        args[5]  = new JValue(title         is null ? IntPtr.Zero : ((Java.Lang.Object)title).Handle);
        args[6]  = new JValue(text          is null ? IntPtr.Zero : ((Java.Lang.Object)text).Handle);
        args[7]  = new JValue(IntPtr.Zero); // shape
        args[8]  = new JValue(0L);          // containerColor
        args[9]  = new JValue(0L);          // iconContentColor
        args[10] = new JValue(0L);          // titleContentColor
        args[11] = new JValue(0L);          // textContentColor
        args[12] = new JValue(0f);          // tonalElevation
        args[13] = new JValue(IntPtr.Zero); // properties
        args[14] = new JValue(((Java.Lang.Object)composer).Handle);
        args[15] = new JValue(0);           // $changed
        args[16] = new JValue(0);           // $changed1
        args[17] = new JValue(defaults);    // $default
        JNIEnv.CallStaticVoidMethod(s_alertDialogClass, s_alertDialogMethod, args);
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

    // androidx.compose.material3.CardKt.Card (non-clickable):
    //   (modifier, shape, colors, elevation, border, content,
    //    composer, $changed, $default)
    // 6 user params, only bit 5 (content) provided.
    const string CardSig =
        "(Landroidx/compose/ui/Modifier;Landroidx/compose/ui/graphics/Shape;" +
        "Landroidx/compose/material3/CardColors;Landroidx/compose/material3/CardElevation;" +
        "Landroidx/compose/foundation/BorderStroke;" +
        "Lkotlin/jvm/functions/Function3;Landroidx/compose/runtime/Composer;II)V";

    static IntPtr s_cardClass;
    static IntPtr s_cardMethod;

    public static unsafe void Card(IFunction3 content, IComposer composer)
    {
        if (s_cardClass == IntPtr.Zero)
        {
            s_cardClass  = JNIEnv.FindClass("androidx/compose/material3/CardKt");
            s_cardMethod = JNIEnv.GetStaticMethodID(s_cardClass, "Card", CardSig);
        }

        JValue* args = stackalloc JValue[9];
        args[0] = new JValue(IntPtr.Zero); // modifier
        args[1] = new JValue(IntPtr.Zero); // shape
        args[2] = new JValue(IntPtr.Zero); // colors
        args[3] = new JValue(IntPtr.Zero); // elevation
        args[4] = new JValue(IntPtr.Zero); // border
        args[5] = new JValue(((Java.Lang.Object)content).Handle);
        args[6] = new JValue(((Java.Lang.Object)composer).Handle);
        args[7] = new JValue(0);
        args[8] = new JValue((int)CardDefault.All);
        JNIEnv.CallStaticVoidMethod(s_cardClass, s_cardMethod, args);
    }

    // androidx.compose.material3.ChipKt.AssistChip:
    //   (onClick, label, modifier, enabled, leadingIcon, trailingIcon,
    //    shape, colors, elevation, border, interactionSource,
    //    composer, $changed, $changed1, $default)
    // 11 user params; bit 0 (onClick), bit 1 (label) always provided.
    // bits 4 (leadingIcon) + 5 (trailingIcon) toggled per-call.
    const string AssistChipSig =
        "(Lkotlin/jvm/functions/Function0;Lkotlin/jvm/functions/Function2;" +
        "Landroidx/compose/ui/Modifier;Z" +
        "Lkotlin/jvm/functions/Function2;Lkotlin/jvm/functions/Function2;" +
        "Landroidx/compose/ui/graphics/Shape;" +
        "Landroidx/compose/material3/ChipColors;Landroidx/compose/material3/ChipElevation;" +
        "Landroidx/compose/foundation/BorderStroke;" +
        "Landroidx/compose/foundation/interaction/MutableInteractionSource;" +
        "Landroidx/compose/runtime/Composer;III)V";

    static IntPtr s_assistChipClass;
    static IntPtr s_assistChipMethod;

    public static unsafe void AssistChip(
        IFunction0  onClick,
        IFunction2  label,
        IFunction2? leadingIcon,
        IFunction2? trailingIcon,
        int         defaults,
        IComposer   composer)
    {
        if (s_assistChipClass == IntPtr.Zero)
        {
            s_assistChipClass  = JNIEnv.FindClass("androidx/compose/material3/ChipKt");
            s_assistChipMethod = JNIEnv.GetStaticMethodID(s_assistChipClass, "AssistChip", AssistChipSig);
        }

        JValue* args = stackalloc JValue[15];
        args[0]  = new JValue(((Java.Lang.Object)onClick).Handle);
        args[1]  = new JValue(((Java.Lang.Object)label).Handle);
        args[2]  = new JValue(IntPtr.Zero); // modifier
        args[3]  = new JValue(true);        // enabled
        args[4]  = new JValue(leadingIcon  is null ? IntPtr.Zero : ((Java.Lang.Object)leadingIcon).Handle);
        args[5]  = new JValue(trailingIcon is null ? IntPtr.Zero : ((Java.Lang.Object)trailingIcon).Handle);
        args[6]  = new JValue(IntPtr.Zero); // shape
        args[7]  = new JValue(IntPtr.Zero); // colors
        args[8]  = new JValue(IntPtr.Zero); // elevation
        args[9]  = new JValue(IntPtr.Zero); // border
        args[10] = new JValue(IntPtr.Zero); // interactionSource
        args[11] = new JValue(((Java.Lang.Object)composer).Handle);
        args[12] = new JValue(0);           // $changed
        args[13] = new JValue(0);           // $changed1
        args[14] = new JValue(defaults);    // $default
        JNIEnv.CallStaticVoidMethod(s_assistChipClass, s_assistChipMethod, args);
    }

    // androidx.compose.material3.ChipKt.FilterChip:
    //   (selected, onClick, label, modifier, enabled, leadingIcon, trailingIcon,
    //    shape, colors, elevation, border, interactionSource,
    //    composer, $changed, $changed1, $default)
    // 12 user params; bits 0 (selected), 1 (onClick), 2 (label) always provided.
    const string FilterChipSig =
        "(ZLkotlin/jvm/functions/Function0;Lkotlin/jvm/functions/Function2;" +
        "Landroidx/compose/ui/Modifier;Z" +
        "Lkotlin/jvm/functions/Function2;Lkotlin/jvm/functions/Function2;" +
        "Landroidx/compose/ui/graphics/Shape;" +
        "Landroidx/compose/material3/SelectableChipColors;" +
        "Landroidx/compose/material3/SelectableChipElevation;" +
        "Landroidx/compose/foundation/BorderStroke;" +
        "Landroidx/compose/foundation/interaction/MutableInteractionSource;" +
        "Landroidx/compose/runtime/Composer;III)V";

    static IntPtr s_filterChipClass;
    static IntPtr s_filterChipMethod;

    public static unsafe void FilterChip(
        bool        selected,
        IFunction0  onClick,
        IFunction2  label,
        IFunction2? leadingIcon,
        IFunction2? trailingIcon,
        int         defaults,
        IComposer   composer)
    {
        if (s_filterChipClass == IntPtr.Zero)
        {
            s_filterChipClass  = JNIEnv.FindClass("androidx/compose/material3/ChipKt");
            s_filterChipMethod = JNIEnv.GetStaticMethodID(s_filterChipClass, "FilterChip", FilterChipSig);
        }

        JValue* args = stackalloc JValue[16];
        args[0]  = new JValue(selected);
        args[1]  = new JValue(((Java.Lang.Object)onClick).Handle);
        args[2]  = new JValue(((Java.Lang.Object)label).Handle);
        args[3]  = new JValue(IntPtr.Zero); // modifier
        args[4]  = new JValue(true);        // enabled
        args[5]  = new JValue(leadingIcon  is null ? IntPtr.Zero : ((Java.Lang.Object)leadingIcon).Handle);
        args[6]  = new JValue(trailingIcon is null ? IntPtr.Zero : ((Java.Lang.Object)trailingIcon).Handle);
        args[7]  = new JValue(IntPtr.Zero); // shape
        args[8]  = new JValue(IntPtr.Zero); // colors
        args[9]  = new JValue(IntPtr.Zero); // elevation
        args[10] = new JValue(IntPtr.Zero); // border
        args[11] = new JValue(IntPtr.Zero); // interactionSource
        args[12] = new JValue(((Java.Lang.Object)composer).Handle);
        args[13] = new JValue(0);
        args[14] = new JValue(0);
        args[15] = new JValue(defaults);
        JNIEnv.CallStaticVoidMethod(s_filterChipClass, s_filterChipMethod, args);
    }

    // androidx.compose.material3.ChipKt.InputChip:
    //   (selected, onClick, label, modifier, enabled, leadingIcon, avatar,
    //    trailingIcon, shape, colors, elevation, border, interactionSource,
    //    composer, $changed, $changed1, $default)
    // 13 user params; bits 0/1/2 always provided.
    const string InputChipSig =
        "(ZLkotlin/jvm/functions/Function0;Lkotlin/jvm/functions/Function2;" +
        "Landroidx/compose/ui/Modifier;Z" +
        "Lkotlin/jvm/functions/Function2;Lkotlin/jvm/functions/Function2;" +
        "Lkotlin/jvm/functions/Function2;Landroidx/compose/ui/graphics/Shape;" +
        "Landroidx/compose/material3/SelectableChipColors;" +
        "Landroidx/compose/material3/SelectableChipElevation;" +
        "Landroidx/compose/foundation/BorderStroke;" +
        "Landroidx/compose/foundation/interaction/MutableInteractionSource;" +
        "Landroidx/compose/runtime/Composer;III)V";

    static IntPtr s_inputChipClass;
    static IntPtr s_inputChipMethod;

    public static unsafe void InputChip(
        bool        selected,
        IFunction0  onClick,
        IFunction2  label,
        IFunction2? leadingIcon,
        IFunction2? avatar,
        IFunction2? trailingIcon,
        int         defaults,
        IComposer   composer)
    {
        if (s_inputChipClass == IntPtr.Zero)
        {
            s_inputChipClass  = JNIEnv.FindClass("androidx/compose/material3/ChipKt");
            s_inputChipMethod = JNIEnv.GetStaticMethodID(s_inputChipClass, "InputChip", InputChipSig);
        }

        JValue* args = stackalloc JValue[17];
        args[0]  = new JValue(selected);
        args[1]  = new JValue(((Java.Lang.Object)onClick).Handle);
        args[2]  = new JValue(((Java.Lang.Object)label).Handle);
        args[3]  = new JValue(IntPtr.Zero); // modifier
        args[4]  = new JValue(true);        // enabled
        args[5]  = new JValue(leadingIcon  is null ? IntPtr.Zero : ((Java.Lang.Object)leadingIcon).Handle);
        args[6]  = new JValue(avatar       is null ? IntPtr.Zero : ((Java.Lang.Object)avatar).Handle);
        args[7]  = new JValue(trailingIcon is null ? IntPtr.Zero : ((Java.Lang.Object)trailingIcon).Handle);
        args[8]  = new JValue(IntPtr.Zero); // shape
        args[9]  = new JValue(IntPtr.Zero); // colors
        args[10] = new JValue(IntPtr.Zero); // elevation
        args[11] = new JValue(IntPtr.Zero); // border
        args[12] = new JValue(IntPtr.Zero); // interactionSource
        args[13] = new JValue(((Java.Lang.Object)composer).Handle);
        args[14] = new JValue(0);
        args[15] = new JValue(0);
        args[16] = new JValue(defaults);
        JNIEnv.CallStaticVoidMethod(s_inputChipClass, s_inputChipMethod, args);
    }

    // androidx.compose.material3.ChipKt.SuggestionChip:
    //   (onClick, label, modifier, enabled, icon, shape, colors, elevation,
    //    border, interactionSource, composer, $changed, $default)
    // 10 user params; bits 0 (onClick), 1 (label) always provided.
    const string SuggestionChipSig =
        "(Lkotlin/jvm/functions/Function0;Lkotlin/jvm/functions/Function2;" +
        "Landroidx/compose/ui/Modifier;Z" +
        "Lkotlin/jvm/functions/Function2;Landroidx/compose/ui/graphics/Shape;" +
        "Landroidx/compose/material3/ChipColors;Landroidx/compose/material3/ChipElevation;" +
        "Landroidx/compose/foundation/BorderStroke;" +
        "Landroidx/compose/foundation/interaction/MutableInteractionSource;" +
        "Landroidx/compose/runtime/Composer;II)V";

    static IntPtr s_suggestionChipClass;
    static IntPtr s_suggestionChipMethod;

    public static unsafe void SuggestionChip(
        IFunction0  onClick,
        IFunction2  label,
        IFunction2? icon,
        int         defaults,
        IComposer   composer)
    {
        if (s_suggestionChipClass == IntPtr.Zero)
        {
            s_suggestionChipClass  = JNIEnv.FindClass("androidx/compose/material3/ChipKt");
            s_suggestionChipMethod = JNIEnv.GetStaticMethodID(s_suggestionChipClass, "SuggestionChip", SuggestionChipSig);
        }

        JValue* args = stackalloc JValue[13];
        args[0]  = new JValue(((Java.Lang.Object)onClick).Handle);
        args[1]  = new JValue(((Java.Lang.Object)label).Handle);
        args[2]  = new JValue(IntPtr.Zero); // modifier
        args[3]  = new JValue(true);        // enabled
        args[4]  = new JValue(icon is null ? IntPtr.Zero : ((Java.Lang.Object)icon).Handle);
        args[5]  = new JValue(IntPtr.Zero); // shape
        args[6]  = new JValue(IntPtr.Zero); // colors
        args[7]  = new JValue(IntPtr.Zero); // elevation
        args[8]  = new JValue(IntPtr.Zero); // border
        args[9]  = new JValue(IntPtr.Zero); // interactionSource
        args[10] = new JValue(((Java.Lang.Object)composer).Handle);
        args[11] = new JValue(0);
        args[12] = new JValue(defaults);
        JNIEnv.CallStaticVoidMethod(s_suggestionChipClass, s_suggestionChipMethod, args);
    }

    // androidx.compose.material3.NavigationBarKt.NavigationBar-HsRjFd4:
    //   (modifier, containerColor, contentColor, tonalElevation, windowInsets,
    //    content, composer, $changed, $default)
    // 6 user params; only bit 5 (content) provided.
    const string NavigationBarSig =
        "(Landroidx/compose/ui/Modifier;JJF" +
        "Landroidx/compose/foundation/layout/WindowInsets;" +
        "Lkotlin/jvm/functions/Function3;Landroidx/compose/runtime/Composer;II)V";

    static IntPtr s_navBarClass;
    static IntPtr s_navBarMethod;

    public static unsafe void NavigationBar(IFunction3 content, IComposer composer)
    {
        if (s_navBarClass == IntPtr.Zero)
        {
            s_navBarClass  = JNIEnv.FindClass("androidx/compose/material3/NavigationBarKt");
            s_navBarMethod = JNIEnv.GetStaticMethodID(s_navBarClass, "NavigationBar-HsRjFd4", NavigationBarSig);
        }

        JValue* args = stackalloc JValue[9];
        args[0] = new JValue(IntPtr.Zero); // modifier
        args[1] = new JValue(0L);          // containerColor
        args[2] = new JValue(0L);          // contentColor
        args[3] = new JValue(0f);          // tonalElevation
        args[4] = new JValue(IntPtr.Zero); // windowInsets
        args[5] = new JValue(((Java.Lang.Object)content).Handle);
        args[6] = new JValue(((Java.Lang.Object)composer).Handle);
        args[7] = new JValue(0);
        args[8] = new JValue((int)NavigationBarDefault.All);
        JNIEnv.CallStaticVoidMethod(s_navBarClass, s_navBarMethod, args);
    }

    // androidx.compose.material3.NavigationBarKt.NavigationBarItem:
    //   (RowScope, selected, onClick, icon, modifier, enabled, label,
    //    alwaysShowLabel, colors, interactionSource, composer, $changed, $default)
    // RowScope is the Kotlin extension receiver (first param). 9 user params
    // (after the receiver); bits 0 (selected), 1 (onClick), 2 (icon) provided.
    const string NavigationBarItemSig =
        "(Landroidx/compose/foundation/layout/RowScope;Z" +
        "Lkotlin/jvm/functions/Function0;Lkotlin/jvm/functions/Function2;" +
        "Landroidx/compose/ui/Modifier;Z" +
        "Lkotlin/jvm/functions/Function2;Z" +
        "Landroidx/compose/material3/NavigationBarItemColors;" +
        "Landroidx/compose/foundation/interaction/MutableInteractionSource;" +
        "Landroidx/compose/runtime/Composer;II)V";

    static IntPtr s_navBarItemClass;
    static IntPtr s_navBarItemMethod;

    public static unsafe void NavigationBarItem(
        IntPtr      rowScope,
        bool        selected,
        IFunction0  onClick,
        IFunction2  icon,
        IFunction2? label,
        int         defaults,
        IComposer   composer)
    {
        if (s_navBarItemClass == IntPtr.Zero)
        {
            s_navBarItemClass  = JNIEnv.FindClass("androidx/compose/material3/NavigationBarKt");
            s_navBarItemMethod = JNIEnv.GetStaticMethodID(s_navBarItemClass, "NavigationBarItem", NavigationBarItemSig);
        }

        if (rowScope == IntPtr.Zero)
            throw new System.InvalidOperationException(
                "NavigationBarItem must be a child of NavigationBar (no RowScope receiver in scope).");

        JValue* args = stackalloc JValue[13];
        args[0]  = new JValue(rowScope);
        args[1]  = new JValue(selected);
        args[2]  = new JValue(((Java.Lang.Object)onClick).Handle);
        args[3]  = new JValue(((Java.Lang.Object)icon).Handle);
        args[4]  = new JValue(IntPtr.Zero); // modifier
        args[5]  = new JValue(true);        // enabled
        args[6]  = new JValue(label is null ? IntPtr.Zero : ((Java.Lang.Object)label).Handle);
        args[7]  = new JValue(true);        // alwaysShowLabel
        args[8]  = new JValue(IntPtr.Zero); // colors
        args[9]  = new JValue(IntPtr.Zero); // interactionSource
        args[10] = new JValue(((Java.Lang.Object)composer).Handle);
        args[11] = new JValue(0);
        args[12] = new JValue(defaults);
        JNIEnv.CallStaticVoidMethod(s_navBarItemClass, s_navBarItemMethod, args);
    }

    // androidx.compose.material3.NavigationRailKt.NavigationRail-qi6gXK8:
    //   (modifier, containerColor, contentColor, header, windowInsets,
    //    content, composer, $changed, $default)
    // 6 user params; only bit 5 (content) provided. (`header` is a slot we
    // don't surface — defaulted via bit 3.)
    const string NavigationRailSig =
        "(Landroidx/compose/ui/Modifier;JJ" +
        "Lkotlin/jvm/functions/Function3;" +
        "Landroidx/compose/foundation/layout/WindowInsets;" +
        "Lkotlin/jvm/functions/Function3;Landroidx/compose/runtime/Composer;II)V";

    static IntPtr s_navRailClass;
    static IntPtr s_navRailMethod;

    public static unsafe void NavigationRail(IFunction3 content, IComposer composer)
    {
        if (s_navRailClass == IntPtr.Zero)
        {
            s_navRailClass  = JNIEnv.FindClass("androidx/compose/material3/NavigationRailKt");
            s_navRailMethod = JNIEnv.GetStaticMethodID(s_navRailClass, "NavigationRail-qi6gXK8", NavigationRailSig);
        }

        JValue* args = stackalloc JValue[9];
        args[0] = new JValue(IntPtr.Zero); // modifier
        args[1] = new JValue(0L);          // containerColor
        args[2] = new JValue(0L);          // contentColor
        args[3] = new JValue(IntPtr.Zero); // header
        args[4] = new JValue(IntPtr.Zero); // windowInsets
        args[5] = new JValue(((Java.Lang.Object)content).Handle);
        args[6] = new JValue(((Java.Lang.Object)composer).Handle);
        args[7] = new JValue(0);
        args[8] = new JValue((int)NavigationRailDefault.All);
        JNIEnv.CallStaticVoidMethod(s_navRailClass, s_navRailMethod, args);
    }

    // androidx.compose.material3.NavigationRailKt.NavigationRailItem:
    //   (selected, onClick, icon, modifier, enabled, label, alwaysShowLabel,
    //    colors, interactionSource, composer, $changed, $default)
    // 9 user params (no scope receiver despite parent NavigationRail
    // exposing a ColumnScope content lambda — NavigationRailItem is a
    // top-level static, not a ColumnScope extension).
    const string NavigationRailItemSig =
        "(ZLkotlin/jvm/functions/Function0;Lkotlin/jvm/functions/Function2;" +
        "Landroidx/compose/ui/Modifier;Z" +
        "Lkotlin/jvm/functions/Function2;Z" +
        "Landroidx/compose/material3/NavigationRailItemColors;" +
        "Landroidx/compose/foundation/interaction/MutableInteractionSource;" +
        "Landroidx/compose/runtime/Composer;II)V";

    static IntPtr s_navRailItemClass;
    static IntPtr s_navRailItemMethod;

    public static unsafe void NavigationRailItem(
        bool        selected,
        IFunction0  onClick,
        IFunction2  icon,
        IFunction2? label,
        int         defaults,
        IComposer   composer)
    {
        if (s_navRailItemClass == IntPtr.Zero)
        {
            s_navRailItemClass  = JNIEnv.FindClass("androidx/compose/material3/NavigationRailKt");
            s_navRailItemMethod = JNIEnv.GetStaticMethodID(s_navRailItemClass, "NavigationRailItem", NavigationRailItemSig);
        }

        JValue* args = stackalloc JValue[12];
        args[0]  = new JValue(selected);
        args[1]  = new JValue(((Java.Lang.Object)onClick).Handle);
        args[2]  = new JValue(((Java.Lang.Object)icon).Handle);
        args[3]  = new JValue(IntPtr.Zero); // modifier
        args[4]  = new JValue(true);        // enabled
        args[5]  = new JValue(label is null ? IntPtr.Zero : ((Java.Lang.Object)label).Handle);
        args[6]  = new JValue(true);        // alwaysShowLabel
        args[7]  = new JValue(IntPtr.Zero); // colors
        args[8]  = new JValue(IntPtr.Zero); // interactionSource
        args[9]  = new JValue(((Java.Lang.Object)composer).Handle);
        args[10] = new JValue(0);
        args[11] = new JValue(defaults);
        JNIEnv.CallStaticVoidMethod(s_navRailItemClass, s_navRailItemMethod, args);
    }
}
