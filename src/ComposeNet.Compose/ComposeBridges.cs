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
            GC.KeepAlive(composer);
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
        try
        {
            JNIEnv.CallStaticVoidMethod(s_buttonClass, s_buttonMethod, args);
        }
        finally
        {
            GC.KeepAlive(onClick);
            GC.KeepAlive(content);
            GC.KeepAlive(composer);
        }
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
        try
        {
            JNIEnv.CallStaticVoidMethod(s_iconButtonClass, s_iconButtonMethod, args);
        }
        finally
        {
            GC.KeepAlive(onClick);
            GC.KeepAlive(content);
            GC.KeepAlive(composer);
        }
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
        try
        {
            JNIEnv.CallStaticVoidMethod(s_fabClass, s_fabMethod, args);
        }
        finally
        {
            GC.KeepAlive(onClick);
            GC.KeepAlive(content);
            GC.KeepAlive(composer);
        }
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
        try
        {
            JNIEnv.CallStaticVoidMethod(s_surfaceClass, s_surfaceMethod, args);
        }
        finally
        {
            GC.KeepAlive(content);
            GC.KeepAlive(composer);
        }
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
        try
        {
            JNIEnv.CallStaticVoidMethod(s_alertDialogClass, s_alertDialogMethod, args);
        }
        finally
        {
            GC.KeepAlive(onDismissRequest);
            GC.KeepAlive(confirmButton);
            GC.KeepAlive(dismissButton);
            GC.KeepAlive(icon);
            GC.KeepAlive(title);
            GC.KeepAlive(text);
            GC.KeepAlive(composer);
        }
    }

    // androidx.compose.material3.ModalBottomSheet_androidKt.ModalBottomSheet-dYc4hso(
    //   onDismissRequest, modifier, sheetState, sheetMaxWidth, shape,
    //   containerColor, contentColor, tonalElevation, scrimColor, dragHandle,
    //   windowInsets, properties, content, composer, $changed, $changed1, $default)
    //
    // 13 user params; bits 0 (onDismissRequest), 2 (sheetState), 12 (content)
    // are always provided. dragHandle (bit 9) is the only optional slot.
    const string ModalBottomSheetSig =
        "(Lkotlin/jvm/functions/Function0;Landroidx/compose/ui/Modifier;" +
        "Landroidx/compose/material3/SheetState;FLandroidx/compose/ui/graphics/Shape;JJFJ" +
        "Lkotlin/jvm/functions/Function2;Landroidx/compose/foundation/layout/WindowInsets;" +
        "Landroidx/compose/material3/ModalBottomSheetProperties;" +
        "Lkotlin/jvm/functions/Function3;Landroidx/compose/runtime/Composer;III)V";

    static IntPtr s_modalBottomSheetClass;
    static IntPtr s_modalBottomSheetMethod;

    public static unsafe void ModalBottomSheet(
        IFunction0  onDismissRequest,
        IntPtr      sheetState,
        IFunction2? dragHandle,
        IFunction3  content,
        int         defaults,
        IComposer   composer)
    {
        if (s_modalBottomSheetClass == IntPtr.Zero)
        {
            s_modalBottomSheetClass  = JNIEnv.FindClass("androidx/compose/material3/ModalBottomSheet_androidKt");
            s_modalBottomSheetMethod = JNIEnv.GetStaticMethodID(s_modalBottomSheetClass, "ModalBottomSheet-dYc4hso", ModalBottomSheetSig);
        }

        JValue* args = stackalloc JValue[17];
        args[0]  = new JValue(((Java.Lang.Object)onDismissRequest).Handle);
        args[1]  = new JValue(IntPtr.Zero); // modifier
        args[2]  = new JValue(sheetState);
        args[3]  = new JValue(0f);          // sheetMaxWidth
        args[4]  = new JValue(IntPtr.Zero); // shape
        args[5]  = new JValue(0L);          // containerColor
        args[6]  = new JValue(0L);          // contentColor
        args[7]  = new JValue(0f);          // tonalElevation
        args[8]  = new JValue(0L);          // scrimColor
        args[9]  = new JValue(dragHandle is null ? IntPtr.Zero : ((Java.Lang.Object)dragHandle).Handle);
        args[10] = new JValue(IntPtr.Zero); // windowInsets
        args[11] = new JValue(IntPtr.Zero); // properties
        args[12] = new JValue(((Java.Lang.Object)content).Handle);
        args[13] = new JValue(((Java.Lang.Object)composer).Handle);
        args[14] = new JValue(0);           // $changed
        args[15] = new JValue(0);           // $changed1
        args[16] = new JValue(defaults);    // $default
        try
        {
            JNIEnv.CallStaticVoidMethod(s_modalBottomSheetClass, s_modalBottomSheetMethod, args);
        }
        finally
        {
            GC.KeepAlive(onDismissRequest);
            GC.KeepAlive(dragHandle);
            GC.KeepAlive(content);
            GC.KeepAlive(composer);
        }
    }

    // androidx.compose.material3.BottomSheetScaffoldKt.BottomSheetScaffold-sdMYb0k(
    //   sheetContent, modifier, scaffoldState, sheetPeekHeight, sheetMaxWidth,
    //   sheetShape, sheetContainerColor, sheetContentColor, sheetTonalElevation,
    //   sheetShadowElevation, sheetDragHandle, sheetSwipeEnabled, topBar,
    //   snackbarHost, containerColor, contentColor, content, composer,
    //   $changed, $changed1, $default)
    //
    // 17 user params; provided bits: 0 (sheetContent), 2 (scaffoldState),
    // 16 (content). Optional slots: 10 (sheetDragHandle), 12 (topBar),
    // 13 (snackbarHost).
    const string BottomSheetScaffoldSig =
        "(Lkotlin/jvm/functions/Function3;Landroidx/compose/ui/Modifier;" +
        "Landroidx/compose/material3/BottomSheetScaffoldState;FF" +
        "Landroidx/compose/ui/graphics/Shape;JJFF" +
        "Lkotlin/jvm/functions/Function2;Z" +
        "Lkotlin/jvm/functions/Function2;Lkotlin/jvm/functions/Function3;JJ" +
        "Lkotlin/jvm/functions/Function3;Landroidx/compose/runtime/Composer;III)V";

    static IntPtr s_bottomSheetScaffoldClass;
    static IntPtr s_bottomSheetScaffoldMethod;

    public static unsafe void BottomSheetScaffold(
        IFunction3  sheetContent,
        IntPtr      scaffoldState,
        IFunction2? sheetDragHandle,
        IFunction2? topBar,
        IFunction3? snackbarHost,
        IFunction3  content,
        int         defaults,
        IComposer   composer)
    {
        if (s_bottomSheetScaffoldClass == IntPtr.Zero)
        {
            s_bottomSheetScaffoldClass  = JNIEnv.FindClass("androidx/compose/material3/BottomSheetScaffoldKt");
            s_bottomSheetScaffoldMethod = JNIEnv.GetStaticMethodID(s_bottomSheetScaffoldClass, "BottomSheetScaffold-sdMYb0k", BottomSheetScaffoldSig);
        }

        JValue* args = stackalloc JValue[21];
        args[0]  = new JValue(((Java.Lang.Object)sheetContent).Handle);
        args[1]  = new JValue(IntPtr.Zero); // modifier
        args[2]  = new JValue(scaffoldState);
        args[3]  = new JValue(0f);          // sheetPeekHeight
        args[4]  = new JValue(0f);          // sheetMaxWidth
        args[5]  = new JValue(IntPtr.Zero); // sheetShape
        args[6]  = new JValue(0L);          // sheetContainerColor
        args[7]  = new JValue(0L);          // sheetContentColor
        args[8]  = new JValue(0f);          // sheetTonalElevation
        args[9]  = new JValue(0f);          // sheetShadowElevation
        args[10] = new JValue(sheetDragHandle is null ? IntPtr.Zero : ((Java.Lang.Object)sheetDragHandle).Handle);
        args[11] = new JValue(true);        // sheetSwipeEnabled
        args[12] = new JValue(topBar       is null ? IntPtr.Zero : ((Java.Lang.Object)topBar).Handle);
        args[13] = new JValue(snackbarHost is null ? IntPtr.Zero : ((Java.Lang.Object)snackbarHost).Handle);
        args[14] = new JValue(0L);          // containerColor
        args[15] = new JValue(0L);          // contentColor
        args[16] = new JValue(((Java.Lang.Object)content).Handle);
        args[17] = new JValue(((Java.Lang.Object)composer).Handle);
        args[18] = new JValue(0);           // $changed
        args[19] = new JValue(0);           // $changed1
        args[20] = new JValue(defaults);    // $default
        try
        {
            JNIEnv.CallStaticVoidMethod(s_bottomSheetScaffoldClass, s_bottomSheetScaffoldMethod, args);
        }
        finally
        {
            GC.KeepAlive(sheetContent);
            GC.KeepAlive(sheetDragHandle);
            GC.KeepAlive(topBar);
            GC.KeepAlive(snackbarHost);
            GC.KeepAlive(content);
            GC.KeepAlive(composer);
        }
    }

    // androidx.compose.material3.DatePickerDialog_androidKt.DatePickerDialog-GmEhDVc(
    //   onDismissRequest, confirmButton, modifier, dismissButton, shape,
    //   tonalElevation, colors, properties, content, composer, $changed, $default)
    //
    // 9 user params; bits 0 (onDismissRequest), 1 (confirmButton),
    // 8 (content) always provided. dismissButton (bit 3) is the only
    // optional Function2 slot.
    const string DatePickerDialogSig =
        "(Lkotlin/jvm/functions/Function0;Lkotlin/jvm/functions/Function2;" +
        "Landroidx/compose/ui/Modifier;Lkotlin/jvm/functions/Function2;" +
        "Landroidx/compose/ui/graphics/Shape;F" +
        "Landroidx/compose/material3/DatePickerColors;" +
        "Landroidx/compose/ui/window/DialogProperties;" +
        "Lkotlin/jvm/functions/Function3;Landroidx/compose/runtime/Composer;II)V";

    static IntPtr s_datePickerDialogClass;
    static IntPtr s_datePickerDialogMethod;

    public static unsafe void DatePickerDialog(
        IFunction0  onDismissRequest,
        IFunction2  confirmButton,
        IFunction2? dismissButton,
        IFunction3  content,
        int         defaults,
        IComposer   composer)
    {
        if (s_datePickerDialogClass == IntPtr.Zero)
        {
            s_datePickerDialogClass  = JNIEnv.FindClass("androidx/compose/material3/DatePickerDialog_androidKt");
            s_datePickerDialogMethod = JNIEnv.GetStaticMethodID(s_datePickerDialogClass, "DatePickerDialog-GmEhDVc", DatePickerDialogSig);
        }

        JValue* args = stackalloc JValue[12];
        args[0]  = new JValue(((Java.Lang.Object)onDismissRequest).Handle);
        args[1]  = new JValue(((Java.Lang.Object)confirmButton).Handle);
        args[2]  = new JValue(IntPtr.Zero); // modifier
        args[3]  = new JValue(dismissButton is null ? IntPtr.Zero : ((Java.Lang.Object)dismissButton).Handle);
        args[4]  = new JValue(IntPtr.Zero); // shape
        args[5]  = new JValue(0f);          // tonalElevation
        args[6]  = new JValue(IntPtr.Zero); // colors
        args[7]  = new JValue(IntPtr.Zero); // properties
        args[8]  = new JValue(((Java.Lang.Object)content).Handle);
        args[9]  = new JValue(((Java.Lang.Object)composer).Handle);
        args[10] = new JValue(0);           // $changed
        args[11] = new JValue(defaults);    // $default
        try
        {
            JNIEnv.CallStaticVoidMethod(s_datePickerDialogClass, s_datePickerDialogMethod, args);
        }
        finally
        {
            GC.KeepAlive(onDismissRequest);
            GC.KeepAlive(confirmButton);
            GC.KeepAlive(dismissButton);
            GC.KeepAlive(content);
            GC.KeepAlive(composer);
        }
    }

    // androidx.compose.material3.TimePickerKt.TimePicker-mT9BvqQ(
    //   state, modifier, colors, layoutType, composer, $changed, $default)
    //
    // 4 user params; bit 0 (state) always provided.
    const string TimePickerSig =
        "(Landroidx/compose/material3/TimePickerState;Landroidx/compose/ui/Modifier;" +
        "Landroidx/compose/material3/TimePickerColors;ILandroidx/compose/runtime/Composer;II)V";

    static IntPtr s_timePickerClass;
    static IntPtr s_timePickerMethod;

    public static unsafe void TimePicker(IntPtr state, int defaults, IComposer composer)
    {
        if (s_timePickerClass == IntPtr.Zero)
        {
            s_timePickerClass  = JNIEnv.FindClass("androidx/compose/material3/TimePickerKt");
            s_timePickerMethod = JNIEnv.GetStaticMethodID(s_timePickerClass, "TimePicker-mT9BvqQ", TimePickerSig);
        }

        JValue* args = stackalloc JValue[7];
        args[0] = new JValue(state);
        args[1] = new JValue(IntPtr.Zero); // modifier
        args[2] = new JValue(IntPtr.Zero); // colors
        args[3] = new JValue(0);           // layoutType
        args[4] = new JValue(((Java.Lang.Object)composer).Handle);
        args[5] = new JValue(0);           // $changed
        args[6] = new JValue(defaults);    // $default
        try
        {
            JNIEnv.CallStaticVoidMethod(s_timePickerClass, s_timePickerMethod, args);
        }
        finally
        {
            GC.KeepAlive(composer);
        }
    }

    // androidx.compose.material3.TimePickerDialogKt.TimePickerDialog-FItCLgY(
    //   onDismissRequest, confirmButton, dismissButton, modifier, properties,
    //   title, modeToggleButton, shape, containerColor, content, composer,
    //   $changed, $default)
    //
    // 10 user params. confirmButton (bit 1), dismissButton (bit 2), and
    // content (bit 9) are required slots. title (bit 5) and
    // modeToggleButton (bit 6) are optional.
    const string TimePickerDialogSig =
        "(Lkotlin/jvm/functions/Function0;Lkotlin/jvm/functions/Function2;" +
        "Lkotlin/jvm/functions/Function2;Landroidx/compose/ui/Modifier;" +
        "Landroidx/compose/ui/window/DialogProperties;" +
        "Lkotlin/jvm/functions/Function2;Lkotlin/jvm/functions/Function2;" +
        "Landroidx/compose/ui/graphics/Shape;J" +
        "Lkotlin/jvm/functions/Function3;Landroidx/compose/runtime/Composer;II)V";

    static IntPtr s_timePickerDialogClass;
    static IntPtr s_timePickerDialogMethod;

    public static unsafe void TimePickerDialog(
        IFunction0  onDismissRequest,
        IFunction2  confirmButton,
        IFunction2  dismissButton,
        IFunction2? title,
        IFunction2? modeToggleButton,
        IFunction3  content,
        int         defaults,
        IComposer   composer)
    {
        if (s_timePickerDialogClass == IntPtr.Zero)
        {
            s_timePickerDialogClass  = JNIEnv.FindClass("androidx/compose/material3/TimePickerDialogKt");
            s_timePickerDialogMethod = JNIEnv.GetStaticMethodID(s_timePickerDialogClass, "TimePickerDialog-FItCLgY", TimePickerDialogSig);
        }

        JValue* args = stackalloc JValue[13];
        args[0]  = new JValue(((Java.Lang.Object)onDismissRequest).Handle);
        args[1]  = new JValue(((Java.Lang.Object)confirmButton).Handle);
        args[2]  = new JValue(((Java.Lang.Object)dismissButton).Handle);
        args[3]  = new JValue(IntPtr.Zero); // modifier
        args[4]  = new JValue(IntPtr.Zero); // properties
        args[5]  = new JValue(title            is null ? IntPtr.Zero : ((Java.Lang.Object)title).Handle);
        args[6]  = new JValue(modeToggleButton is null ? IntPtr.Zero : ((Java.Lang.Object)modeToggleButton).Handle);
        args[7]  = new JValue(IntPtr.Zero); // shape
        args[8]  = new JValue(0L);          // containerColor
        args[9]  = new JValue(((Java.Lang.Object)content).Handle);
        args[10] = new JValue(((Java.Lang.Object)composer).Handle);
        args[11] = new JValue(0);           // $changed
        args[12] = new JValue(defaults);    // $default
        try
        {
            JNIEnv.CallStaticVoidMethod(s_timePickerDialogClass, s_timePickerDialogMethod, args);
        }
        finally
        {
            GC.KeepAlive(onDismissRequest);
            GC.KeepAlive(confirmButton);
            GC.KeepAlive(dismissButton);
            GC.KeepAlive(title);
            GC.KeepAlive(modeToggleButton);
            GC.KeepAlive(content);
            GC.KeepAlive(composer);
        }
    }

    // androidx.compose.material3.TooltipKt.TooltipBox (7-user-param overload):
    //   (positionProvider, tooltip, state, modifier, focusable, enableUserInput,
    //    content, composer, $changed, $default)
    //
    // Bits 0 (positionProvider), 1 (tooltip), 2 (state), 6 (content) provided.
    const string TooltipBoxSig =
        "(Landroidx/compose/ui/window/PopupPositionProvider;" +
        "Lkotlin/jvm/functions/Function3;" +
        "Landroidx/compose/material3/TooltipState;" +
        "Landroidx/compose/ui/Modifier;ZZ" +
        "Lkotlin/jvm/functions/Function2;Landroidx/compose/runtime/Composer;II)V";

    static IntPtr s_tooltipBoxClass;
    static IntPtr s_tooltipBoxMethod;

    public static unsafe void TooltipBox(
        IntPtr     positionProvider,
        IFunction3 tooltip,
        IntPtr     state,
        IFunction2 content,
        int        defaults,
        IComposer  composer)
    {
        if (s_tooltipBoxClass == IntPtr.Zero)
        {
            s_tooltipBoxClass  = JNIEnv.FindClass("androidx/compose/material3/TooltipKt");
            s_tooltipBoxMethod = JNIEnv.GetStaticMethodID(s_tooltipBoxClass, "TooltipBox", TooltipBoxSig);
        }

        JValue* args = stackalloc JValue[10];
        args[0] = new JValue(positionProvider);
        args[1] = new JValue(((Java.Lang.Object)tooltip).Handle);
        args[2] = new JValue(state);
        args[3] = new JValue(IntPtr.Zero); // modifier
        args[4] = new JValue(true);        // focusable
        args[5] = new JValue(true);        // enableUserInput
        args[6] = new JValue(((Java.Lang.Object)content).Handle);
        args[7] = new JValue(((Java.Lang.Object)composer).Handle);
        args[8] = new JValue(0);           // $changed
        args[9] = new JValue(defaults);    // $default
        try
        {
            JNIEnv.CallStaticVoidMethod(s_tooltipBoxClass, s_tooltipBoxMethod, args);
        }
        finally
        {
            GC.KeepAlive(tooltip);
            GC.KeepAlive(content);
            GC.KeepAlive(composer);
        }
    }

    // androidx.compose.material3.DatePickerKt.DatePicker(
    //   state, modifier, dateFormatter, colors, title, headline,
    //   showModeToggle, requestFocus, composer, $changed, $default)
    //
    // 8 user params; bit 0 (state) provided. requestFocus is the
    // optional focus requester (defaultable).
    const string DatePickerSig =
        "(Landroidx/compose/material3/DatePickerState;Landroidx/compose/ui/Modifier;" +
        "Landroidx/compose/material3/DatePickerFormatter;" +
        "Landroidx/compose/material3/DatePickerColors;" +
        "Lkotlin/jvm/functions/Function2;Lkotlin/jvm/functions/Function2;Z" +
        "Landroidx/compose/ui/focus/FocusRequester;" +
        "Landroidx/compose/runtime/Composer;II)V";

    static IntPtr s_datePickerClass;
    static IntPtr s_datePickerMethod;

    public static unsafe void DatePicker(IntPtr state, int defaults, IComposer composer)
    {
        if (s_datePickerClass == IntPtr.Zero)
        {
            s_datePickerClass  = JNIEnv.FindClass("androidx/compose/material3/DatePickerKt");
            s_datePickerMethod = JNIEnv.GetStaticMethodID(s_datePickerClass, "DatePicker", DatePickerSig);
        }

        JValue* args = stackalloc JValue[11];
        args[0]  = new JValue(state);
        args[1]  = new JValue(IntPtr.Zero); // modifier
        args[2]  = new JValue(IntPtr.Zero); // dateFormatter
        args[3]  = new JValue(IntPtr.Zero); // colors
        args[4]  = new JValue(IntPtr.Zero); // title
        args[5]  = new JValue(IntPtr.Zero); // headline
        args[6]  = new JValue(true);        // showModeToggle
        args[7]  = new JValue(IntPtr.Zero); // requestFocus
        args[8]  = new JValue(((Java.Lang.Object)composer).Handle);
        args[9]  = new JValue(0);           // $changed
        args[10] = new JValue(defaults);    // $default
        try
        {
            JNIEnv.CallStaticVoidMethod(s_datePickerClass, s_datePickerMethod, args);
        }
        finally
        {
            GC.KeepAlive(composer);
        }
    }

    // ---- State-holder bridges ----
    //
    // Every `remember*State` builder is itself @Composable so it takes a
    // trailing Composer + $changed + $default. The dotnet/android-libraries
    // binding generator strips the mangled overloads (DatePickerState's
    // `-EU0dCGE`), so we go through raw JNI for those. We always return
    // the IntPtr — the caller threads it back into a composable bridge
    // as a JValue argument without ever materialising a managed wrapper.

    // androidx.compose.material3.DatePickerKt.rememberDatePickerState-EU0dCGE(
    //   initialSelectedDateMillis, initialDisplayedMonthMillis, yearRange,
    //   initialDisplayMode, selectableDates, composer, $changed, $default)
    const string RememberDatePickerStateSig =
        "(Ljava/lang/Long;Ljava/lang/Long;Lkotlin/ranges/IntRange;I" +
        "Landroidx/compose/material3/SelectableDates;" +
        "Landroidx/compose/runtime/Composer;II)Landroidx/compose/material3/DatePickerState;";

    static IntPtr s_rememberDatePickerStateClass;
    static IntPtr s_rememberDatePickerStateMethod;

    public static unsafe IntPtr RememberDatePickerState(IComposer composer)
    {
        if (s_rememberDatePickerStateClass == IntPtr.Zero)
        {
            s_rememberDatePickerStateClass  = JNIEnv.FindClass("androidx/compose/material3/DatePickerKt");
            s_rememberDatePickerStateMethod = JNIEnv.GetStaticMethodID(s_rememberDatePickerStateClass, "rememberDatePickerState-EU0dCGE", RememberDatePickerStateSig);
        }

        JValue* args = stackalloc JValue[8];
        args[0] = new JValue(IntPtr.Zero); // initialSelectedDateMillis
        args[1] = new JValue(IntPtr.Zero); // initialDisplayedMonthMillis
        args[2] = new JValue(IntPtr.Zero); // yearRange
        args[3] = new JValue(0);           // initialDisplayMode
        args[4] = new JValue(IntPtr.Zero); // selectableDates
        args[5] = new JValue(((Java.Lang.Object)composer).Handle);
        args[6] = new JValue(0);           // $changed
        args[7] = new JValue(0b11111);     // $default — all 5 user params defaulted
        try
        {
            return JNIEnv.CallStaticObjectMethod(s_rememberDatePickerStateClass, s_rememberDatePickerStateMethod, args);
        }
        finally
        {
            GC.KeepAlive(composer);
        }
    }

    // androidx.compose.material3.TimePickerKt.rememberTimePickerState(
    //   initialHour, initialMinute, is24Hour, composer, $changed, $default)
    const string RememberTimePickerStateSig =
        "(IIZLandroidx/compose/runtime/Composer;II)Landroidx/compose/material3/TimePickerState;";

    static IntPtr s_rememberTimePickerStateClass;
    static IntPtr s_rememberTimePickerStateMethod;

    public static unsafe IntPtr RememberTimePickerState(int initialHour, int initialMinute, bool is24Hour, IComposer composer)
    {
        if (s_rememberTimePickerStateClass == IntPtr.Zero)
        {
            s_rememberTimePickerStateClass  = JNIEnv.FindClass("androidx/compose/material3/TimePickerKt");
            s_rememberTimePickerStateMethod = JNIEnv.GetStaticMethodID(s_rememberTimePickerStateClass, "rememberTimePickerState", RememberTimePickerStateSig);
        }

        JValue* args = stackalloc JValue[6];
        args[0] = new JValue(initialHour);
        args[1] = new JValue(initialMinute);
        args[2] = new JValue(is24Hour);
        args[3] = new JValue(((Java.Lang.Object)composer).Handle);
        args[4] = new JValue(0);           // $changed
        args[5] = new JValue(0);           // $default — all 3 provided
        try
        {
            return JNIEnv.CallStaticObjectMethod(s_rememberTimePickerStateClass, s_rememberTimePickerStateMethod, args);
        }
        finally
        {
            GC.KeepAlive(composer);
        }
    }

    // androidx.compose.material3.TooltipKt.rememberTooltipState(
    //   initialIsVisible, isPersistent, mutatorMutex, composer, $changed, $default)
    const string RememberTooltipStateSig =
        "(ZZLandroidx/compose/foundation/MutatorMutex;Landroidx/compose/runtime/Composer;II)Landroidx/compose/material3/TooltipState;";

    static IntPtr s_rememberTooltipStateClass;
    static IntPtr s_rememberTooltipStateMethod;

    public static unsafe IntPtr RememberTooltipState(bool isPersistent, IComposer composer)
    {
        if (s_rememberTooltipStateClass == IntPtr.Zero)
        {
            s_rememberTooltipStateClass  = JNIEnv.FindClass("androidx/compose/material3/TooltipKt");
            s_rememberTooltipStateMethod = JNIEnv.GetStaticMethodID(s_rememberTooltipStateClass, "rememberTooltipState", RememberTooltipStateSig);
        }

        JValue* args = stackalloc JValue[6];
        args[0] = new JValue(false);       // initialIsVisible
        args[1] = new JValue(isPersistent);
        args[2] = new JValue(IntPtr.Zero); // mutatorMutex
        args[3] = new JValue(((Java.Lang.Object)composer).Handle);
        args[4] = new JValue(0);           // $changed
        args[5] = new JValue(0b101);       // $default — bits 0 and 2 (initialIsVisible, mutatorMutex)
        try
        {
            return JNIEnv.CallStaticObjectMethod(s_rememberTooltipStateClass, s_rememberTooltipStateMethod, args);
        }
        finally
        {
            GC.KeepAlive(composer);
        }
    }

    // androidx.compose.material3.TooltipDefaults.INSTANCE.rememberPlainTooltipPositionProvider-kHDZbjc(
    //   spacingBetweenTooltipAndAnchor, composer, $changed, $default)
    //
    // Instance method on the Kotlin object singleton. We resolve INSTANCE
    // once and reuse it across calls.
    const string RememberPlainTooltipPositionProviderSig =
        "(FLandroidx/compose/runtime/Composer;II)Landroidx/compose/ui/window/PopupPositionProvider;";

    static IntPtr s_tooltipDefaultsInstance;
    static IntPtr s_rememberPlainTooltipPositionProviderMethod;

    public static unsafe IntPtr RememberPlainTooltipPositionProvider(IComposer composer)
    {
        if (s_tooltipDefaultsInstance == IntPtr.Zero)
        {
            IntPtr cls         = JNIEnv.FindClass("androidx/compose/material3/TooltipDefaults");
            IntPtr instanceFid = JNIEnv.GetStaticFieldID(cls, "INSTANCE", "Landroidx/compose/material3/TooltipDefaults;");
            s_tooltipDefaultsInstance = JNIEnv.NewGlobalRef(JNIEnv.GetStaticObjectField(cls, instanceFid));
            s_rememberPlainTooltipPositionProviderMethod = JNIEnv.GetMethodID(cls, "rememberPlainTooltipPositionProvider-kHDZbjc", RememberPlainTooltipPositionProviderSig);
        }

        JValue* args = stackalloc JValue[4];
        args[0] = new JValue(0f);          // spacing
        args[1] = new JValue(((Java.Lang.Object)composer).Handle);
        args[2] = new JValue(0);
        args[3] = new JValue(1);           // $default — spacing defaulted
        try
        {
            return JNIEnv.CallObjectMethod(s_tooltipDefaultsInstance, s_rememberPlainTooltipPositionProviderMethod, args);
        }
        finally
        {
            GC.KeepAlive(composer);
        }
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
            GC.KeepAlive(onValueChange);
            GC.KeepAlive(composer);
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
        try
        {
            JNIEnv.CallStaticVoidMethod(s_cardClass, s_cardMethod, args);
        }
        finally
        {
            GC.KeepAlive(content);
            GC.KeepAlive(composer);
        }
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
        try
        {
            JNIEnv.CallStaticVoidMethod(s_assistChipClass, s_assistChipMethod, args);
        }
        finally
        {
            GC.KeepAlive(onClick);
            GC.KeepAlive(label);
            GC.KeepAlive(leadingIcon);
            GC.KeepAlive(trailingIcon);
            GC.KeepAlive(composer);
        }
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
        try
        {
            JNIEnv.CallStaticVoidMethod(s_filterChipClass, s_filterChipMethod, args);
        }
        finally
        {
            GC.KeepAlive(onClick);
            GC.KeepAlive(label);
            GC.KeepAlive(leadingIcon);
            GC.KeepAlive(trailingIcon);
            GC.KeepAlive(composer);
        }
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
        try
        {
            JNIEnv.CallStaticVoidMethod(s_inputChipClass, s_inputChipMethod, args);
        }
        finally
        {
            GC.KeepAlive(onClick);
            GC.KeepAlive(label);
            GC.KeepAlive(leadingIcon);
            GC.KeepAlive(avatar);
            GC.KeepAlive(trailingIcon);
            GC.KeepAlive(composer);
        }
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
        try
        {
            JNIEnv.CallStaticVoidMethod(s_suggestionChipClass, s_suggestionChipMethod, args);
        }
        finally
        {
            GC.KeepAlive(onClick);
            GC.KeepAlive(label);
            GC.KeepAlive(icon);
            GC.KeepAlive(composer);
        }
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
        try
        {
            JNIEnv.CallStaticVoidMethod(s_navBarClass, s_navBarMethod, args);
        }
        finally
        {
            GC.KeepAlive(content);
            GC.KeepAlive(composer);
        }
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
        try
        {
            JNIEnv.CallStaticVoidMethod(s_navBarItemClass, s_navBarItemMethod, args);
        }
        finally
        {
            GC.KeepAlive(onClick);
            GC.KeepAlive(icon);
            GC.KeepAlive(label);
            GC.KeepAlive(composer);
        }
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
        try
        {
            JNIEnv.CallStaticVoidMethod(s_navRailClass, s_navRailMethod, args);
        }
        finally
        {
            GC.KeepAlive(content);
            GC.KeepAlive(composer);
        }
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
        try
        {
            JNIEnv.CallStaticVoidMethod(s_navRailItemClass, s_navRailItemMethod, args);
        }
        finally
        {
            GC.KeepAlive(onClick);
            GC.KeepAlive(icon);
            GC.KeepAlive(label);
            GC.KeepAlive(composer);
        }
    }
}
