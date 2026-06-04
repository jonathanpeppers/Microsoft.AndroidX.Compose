using Android.Runtime;
using AndroidX.Compose.Runtime;
using AndroidX.Compose.UI;
using Java.Interop;
using Kotlin.Jvm.Functions;

namespace ComposeNet;

// Raw-JNI bridges to Compose functions the .NET-for-Android binding generator
// can't see (Compose @Composable functions don't get $default sibling overloads
// — the trailing $default bitmask lives on the regular method). The bodies
// for every method tagged with [ComposeBridge] are emitted by
// ComposeNet.SourceGenerators.ComposeBridgeGenerator from the attribute
// metadata + the matching [ComposeDefaults] enum names.
//
// A few helpers stay hand-written: the Modifier-chain helpers (no $default,
// they wrap plain Kotlin static functions, not @Composable), and the
// Modifier.Companion.$$INSTANCE field lookup used by Modifier.Build().
internal static partial class ComposeBridges
{
    // Convert a managed Modifier wrapper (from `Modifier.Build()`) to a
    // raw JNI handle, or IntPtr.Zero when null. Each bridge that takes
    // a modifier param uses this + KeepAlive's the wrapper across the
    // JNI call so its handle stays alive.
    internal static IntPtr ModifierHandle(IModifier? modifier) =>
        modifier is null ? IntPtr.Zero : ((Java.Lang.Object)modifier).Handle;

    // androidx.compose.ui.Modifier$Companion.$$INSTANCE — the empty
    // Modifier that every chain builds on top of. Cached as a global
    // ref so the chain builder doesn't pay the FindClass +
    // GetStaticObjectField cost on every recomposition.
    static IntPtr s_modifierCompanionInstance;

    internal static unsafe IntPtr ModifierCompanionInstance()
    {
        if (s_modifierCompanionInstance == IntPtr.Zero)
        {
            IntPtr cls = JNIEnv.FindClass("androidx/compose/ui/Modifier$Companion");
            IntPtr fid = JNIEnv.GetStaticFieldID(cls, "$$INSTANCE", "Landroidx/compose/ui/Modifier$Companion;");
            IntPtr local = JNIEnv.GetStaticObjectField(cls, fid);
            s_modifierCompanionInstance = JNIEnv.NewGlobalRef(local);
            JNIEnv.DeleteLocalRef(local);
        }
        // Returning a NEW local ref each call so callers can DeleteLocalRef
        // it uniformly while walking the op chain.
        return JNIEnv.NewLocalRef(s_modifierCompanionInstance);
    }

    // androidx.compose.foundation.layout.PaddingKt — the Dp-taking
    // overloads have hashed JVM names from the inline-class compiler
    // mangling (`@JvmInline value class Dp(val value: Float)`).
    // The signatures below are stable across Compose UI 1.x.
    const string ModifierDpSig =
        "(Landroidx/compose/ui/Modifier;F)Landroidx/compose/ui/Modifier;";
    const string ModifierDp2Sig =
        "(Landroidx/compose/ui/Modifier;FF)Landroidx/compose/ui/Modifier;";
    const string ModifierDp4Sig =
        "(Landroidx/compose/ui/Modifier;FFFF)Landroidx/compose/ui/Modifier;";

    static IntPtr s_paddingKtClass;
    static IntPtr s_paddingAllMethod;
    static IntPtr s_paddingHVMethod;
    static IntPtr s_paddingLTRBMethod;

    internal static unsafe IntPtr ModifierPaddingAll(IntPtr modifier, float dp)
    {
        if (s_paddingKtClass == IntPtr.Zero)
            s_paddingKtClass = JNIEnv.FindClass("androidx/compose/foundation/layout/PaddingKt");
        if (s_paddingAllMethod == IntPtr.Zero)
            s_paddingAllMethod = JNIEnv.GetStaticMethodID(s_paddingKtClass, "padding-3ABfNKs", ModifierDpSig);

        JValue* args = stackalloc JValue[2];
        args[0] = new JValue(modifier);
        args[1] = new JValue(dp);
        return JNIEnv.CallStaticObjectMethod(s_paddingKtClass, s_paddingAllMethod, args);
    }

    internal static unsafe IntPtr ModifierPaddingHV(IntPtr modifier, float horizontal, float vertical)
    {
        if (s_paddingKtClass == IntPtr.Zero)
            s_paddingKtClass = JNIEnv.FindClass("androidx/compose/foundation/layout/PaddingKt");
        if (s_paddingHVMethod == IntPtr.Zero)
            s_paddingHVMethod = JNIEnv.GetStaticMethodID(s_paddingKtClass, "padding-VpY3zN4", ModifierDp2Sig);

        JValue* args = stackalloc JValue[3];
        args[0] = new JValue(modifier);
        args[1] = new JValue(horizontal);
        args[2] = new JValue(vertical);
        return JNIEnv.CallStaticObjectMethod(s_paddingKtClass, s_paddingHVMethod, args);
    }

    internal static unsafe IntPtr ModifierPaddingLTRB(IntPtr modifier, float start, float top, float end, float bottom)
    {
        if (s_paddingKtClass == IntPtr.Zero)
            s_paddingKtClass = JNIEnv.FindClass("androidx/compose/foundation/layout/PaddingKt");
        if (s_paddingLTRBMethod == IntPtr.Zero)
            s_paddingLTRBMethod = JNIEnv.GetStaticMethodID(s_paddingKtClass, "padding-qDBjuR0", ModifierDp4Sig);

        JValue* args = stackalloc JValue[5];
        args[0] = new JValue(modifier);
        args[1] = new JValue(start);
        args[2] = new JValue(top);
        args[3] = new JValue(end);
        args[4] = new JValue(bottom);
        return JNIEnv.CallStaticObjectMethod(s_paddingKtClass, s_paddingLTRBMethod, args);
    }

    // androidx.compose.foundation.layout.SizeKt — fillMax* take a plain
    // Float fraction, not Dp, so the JVM names are NOT mangled.
    static IntPtr s_sizeKtClass;
    static IntPtr s_fillMaxWidthMethod;
    static IntPtr s_fillMaxHeightMethod;
    static IntPtr s_fillMaxSizeMethod;

    internal static unsafe IntPtr ModifierFillMaxWidth(IntPtr modifier, float fraction)
    {
        if (s_sizeKtClass == IntPtr.Zero)
            s_sizeKtClass = JNIEnv.FindClass("androidx/compose/foundation/layout/SizeKt");
        if (s_fillMaxWidthMethod == IntPtr.Zero)
            s_fillMaxWidthMethod = JNIEnv.GetStaticMethodID(s_sizeKtClass, "fillMaxWidth", ModifierDpSig);

        JValue* args = stackalloc JValue[2];
        args[0] = new JValue(modifier);
        args[1] = new JValue(fraction);
        return JNIEnv.CallStaticObjectMethod(s_sizeKtClass, s_fillMaxWidthMethod, args);
    }

    internal static unsafe IntPtr ModifierFillMaxHeight(IntPtr modifier, float fraction)
    {
        if (s_sizeKtClass == IntPtr.Zero)
            s_sizeKtClass = JNIEnv.FindClass("androidx/compose/foundation/layout/SizeKt");
        if (s_fillMaxHeightMethod == IntPtr.Zero)
            s_fillMaxHeightMethod = JNIEnv.GetStaticMethodID(s_sizeKtClass, "fillMaxHeight", ModifierDpSig);

        JValue* args = stackalloc JValue[2];
        args[0] = new JValue(modifier);
        args[1] = new JValue(fraction);
        return JNIEnv.CallStaticObjectMethod(s_sizeKtClass, s_fillMaxHeightMethod, args);
    }

    internal static unsafe IntPtr ModifierFillMaxSize(IntPtr modifier, float fraction)
    {
        if (s_sizeKtClass == IntPtr.Zero)
            s_sizeKtClass = JNIEnv.FindClass("androidx/compose/foundation/layout/SizeKt");
        if (s_fillMaxSizeMethod == IntPtr.Zero)
            s_fillMaxSizeMethod = JNIEnv.GetStaticMethodID(s_sizeKtClass, "fillMaxSize", ModifierDpSig);

        JValue* args = stackalloc JValue[2];
        args[0] = new JValue(modifier);
        args[1] = new JValue(fraction);
        return JNIEnv.CallStaticObjectMethod(s_sizeKtClass, s_fillMaxSizeMethod, args);
    }

    // androidx.compose.foundation.layout.WindowInsetsPadding_androidKt —
    // Modifier extensions that read WindowInsets from CompositionLocals
    // and apply them as padding. Take only Modifier (no Dp), so JVM
    // names are unmangled.
    const string ModifierToModifierSig =
        "(Landroidx/compose/ui/Modifier;)Landroidx/compose/ui/Modifier;";

    // androidx.compose.ui.res.PainterResources_androidKt.painterResource —
    // returns a NEW local Painter ref the caller is responsible for
    // DeleteLocalRef'ing once it's been handed to the consuming
    // Image/Icon JNI call.
    [ComposeBridge(
        Class     = "androidx/compose/ui/res/PainterResources_androidKt",
        JvmName   = "painterResource",
        Signature = "(ILandroidx/compose/runtime/Composer;I)Landroidx/compose/ui/graphics/painter/Painter;")]
    public static partial IntPtr PainterResource(int id, IComposer composer);


    static IntPtr s_windowInsetsPaddingAndroidKtClass;
    static IntPtr s_safeDrawingPaddingMethod;
    static IntPtr s_systemBarsPaddingMethod;

    internal static unsafe IntPtr ModifierSafeDrawingPadding(IntPtr modifier)
    {
        if (s_windowInsetsPaddingAndroidKtClass == IntPtr.Zero)
            s_windowInsetsPaddingAndroidKtClass = JNIEnv.FindClass("androidx/compose/foundation/layout/WindowInsetsPadding_androidKt");
        if (s_safeDrawingPaddingMethod == IntPtr.Zero)
            s_safeDrawingPaddingMethod = JNIEnv.GetStaticMethodID(s_windowInsetsPaddingAndroidKtClass, "safeDrawingPadding", ModifierToModifierSig);

        JValue* args = stackalloc JValue[1];
        args[0] = new JValue(modifier);
        return JNIEnv.CallStaticObjectMethod(s_windowInsetsPaddingAndroidKtClass, s_safeDrawingPaddingMethod, args);
    }

    internal static unsafe IntPtr ModifierSystemBarsPadding(IntPtr modifier)
    {
        if (s_windowInsetsPaddingAndroidKtClass == IntPtr.Zero)
            s_windowInsetsPaddingAndroidKtClass = JNIEnv.FindClass("androidx/compose/foundation/layout/WindowInsetsPadding_androidKt");
        if (s_systemBarsPaddingMethod == IntPtr.Zero)
            s_systemBarsPaddingMethod = JNIEnv.GetStaticMethodID(s_windowInsetsPaddingAndroidKtClass, "systemBarsPadding", ModifierToModifierSig);

        JValue* args = stackalloc JValue[1];
        args[0] = new JValue(modifier);
        return JNIEnv.CallStaticObjectMethod(s_windowInsetsPaddingAndroidKtClass, s_systemBarsPaddingMethod, args);
    }

    // Source-generated bridges below. Each [ComposeBridge] partial
    // declaration is paired with a matching [ComposeDefaults] in
    // ComposeDefaults.cs; the generator reads bit positions and parameter
    // names from the enum and emits the cache fields, lazy class/method
    // ID resolution, JValue array fill, $default bitmask, and try/finally
    // with GC.KeepAlive.

    // androidx.compose.material3.TextKt.Text--4IGK_g
    [ComposeBridge(
        Class     = "androidx/compose/material3/TextKt",
        JvmName   = "Text--4IGK_g",
        Signature = "(Ljava/lang/String;Landroidx/compose/ui/Modifier;JJ" +
                    "Landroidx/compose/ui/text/font/FontStyle;" +
                    "Landroidx/compose/ui/text/font/FontWeight;" +
                    "Landroidx/compose/ui/text/font/FontFamily;J" +
                    "Landroidx/compose/ui/text/style/TextDecoration;" +
                    "Landroidx/compose/ui/text/style/TextAlign;JIZII" +
                    "Lkotlin/jvm/functions/Function1;Landroidx/compose/ui/text/TextStyle;" +
                    "Landroidx/compose/runtime/Composer;III)V",
        Defaults  = typeof(TextDefault))]
    public static partial void Text(string text, IModifier? modifier, IComposer composer);

    // androidx.compose.material3.ButtonKt.Button
    [ComposeBridge(
        Class     = "androidx/compose/material3/ButtonKt",
        JvmName   = "Button",
        Signature = "(Lkotlin/jvm/functions/Function0;Landroidx/compose/ui/Modifier;Z" +
                    "Landroidx/compose/ui/graphics/Shape;Landroidx/compose/material3/ButtonColors;" +
                    "Landroidx/compose/material3/ButtonElevation;Landroidx/compose/foundation/BorderStroke;" +
                    "Landroidx/compose/foundation/layout/PaddingValues;" +
                    "Landroidx/compose/foundation/interaction/MutableInteractionSource;" +
                    "Lkotlin/jvm/functions/Function3;Landroidx/compose/runtime/Composer;II)V",
        Defaults  = typeof(ButtonDefault))]
    public static partial void Button(IFunction0 onClick, IModifier? modifier,
                                      IFunction3 content, IComposer composer);

    // androidx.compose.material3.IconButtonKt.IconButton
    [ComposeBridge(
        Class     = "androidx/compose/material3/IconButtonKt",
        JvmName   = "IconButton",
        Signature = "(Lkotlin/jvm/functions/Function0;Landroidx/compose/ui/Modifier;Z" +
                    "Landroidx/compose/material3/IconButtonColors;" +
                    "Landroidx/compose/foundation/interaction/MutableInteractionSource;" +
                    "Lkotlin/jvm/functions/Function2;Landroidx/compose/runtime/Composer;II)V",
        Defaults  = typeof(IconButtonDefault))]
    public static partial void IconButton(IFunction0 onClick, IModifier? modifier,
                                          IFunction2 content, IComposer composer);

    // androidx.compose.material3.FloatingActionButtonKt.FloatingActionButton-X-z6DiA
    [ComposeBridge(
        Class     = "androidx/compose/material3/FloatingActionButtonKt",
        JvmName   = "FloatingActionButton-X-z6DiA",
        Signature = "(Lkotlin/jvm/functions/Function0;Landroidx/compose/ui/Modifier;" +
                    "Landroidx/compose/ui/graphics/Shape;JJ" +
                    "Landroidx/compose/material3/FloatingActionButtonElevation;" +
                    "Landroidx/compose/foundation/interaction/MutableInteractionSource;" +
                    "Lkotlin/jvm/functions/Function2;Landroidx/compose/runtime/Composer;II)V",
        Defaults  = typeof(FloatingActionButtonDefault))]
    public static partial void FloatingActionButton(IFunction0 onClick, IModifier? modifier,
                                                    IFunction2 content, IComposer composer);

    // androidx.compose.material3.SurfaceKt.Surface-T9BRK9s (non-interactive)
    [ComposeBridge(
        Class     = "androidx/compose/material3/SurfaceKt",
        JvmName   = "Surface-T9BRK9s",
        Signature = "(Landroidx/compose/ui/Modifier;Landroidx/compose/ui/graphics/Shape;JJFF" +
                    "Landroidx/compose/foundation/BorderStroke;" +
                    "Lkotlin/jvm/functions/Function2;Landroidx/compose/runtime/Composer;II)V",
        Defaults  = typeof(SurfaceDefault))]
    public static partial void Surface(IModifier? modifier, IFunction2 content, IComposer composer);

    // androidx.compose.foundation.ImageKt.Image (Painter overload) — all
    // four `Image` Kotlin overloads share the JVM name `Image`, so the
    // binder strips them. The Painter type itself isn't bound either
    // (inline-class methods like getIntrinsicSize-NH-jbRc), so callers
    // pass a raw IntPtr Painter handle obtained from
    // ComposeBridges.PainterResource.
    [ComposeBridge(
        Class     = "androidx/compose/foundation/ImageKt",
        JvmName   = "Image",
        Signature = "(Landroidx/compose/ui/graphics/painter/Painter;Ljava/lang/String;" +
                    "Landroidx/compose/ui/Modifier;Landroidx/compose/ui/Alignment;" +
                    "Landroidx/compose/ui/layout/ContentScale;F" +
                    "Landroidx/compose/ui/graphics/ColorFilter;" +
                    "Landroidx/compose/runtime/Composer;II)V",
        Defaults  = typeof(ImageDefault))]
    public static partial void Image(
        IntPtr     painter,
        string?    contentDescription,
        IModifier? modifier,
        int        defaults,
        IComposer  composer);

    // androidx.compose.material3.IconKt.Icon-ww6aTOc (Painter overload) —
    // the Painter and ImageBitmap overloads share the mangled JVM name
    // `Icon-ww6aTOc` with the bound ImageVector overload and are
    // stripped. Painter handles come from ComposeBridges.PainterResource.
    [ComposeBridge(
        Class     = "androidx/compose/material3/IconKt",
        JvmName   = "Icon-ww6aTOc",
        Signature = "(Landroidx/compose/ui/graphics/painter/Painter;Ljava/lang/String;" +
                    "Landroidx/compose/ui/Modifier;JLandroidx/compose/runtime/Composer;II)V",
        Defaults  = typeof(IconPainterDefault))]
    public static partial void IconPainter(
        IntPtr     painter,
        string?    contentDescription,
        IModifier? modifier,
        long       tint,
        int        defaults,
        IComposer  composer);

    // androidx.compose.material3.TextFieldKt.TextField (String overload)
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

    [ComposeBridge(
        Class     = "androidx/compose/material3/TextFieldKt",
        JvmName   = "TextField",
        Signature = TextFieldStringSig,
        Defaults  = typeof(TextFieldDefault))]
    public static partial void TextField(string value, IFunction1 onValueChange,
                                         IModifier? modifier, IComposer composer);

    [ComposeBridge(
        Class     = "androidx/compose/material3/OutlinedTextFieldKt",
        JvmName   = "OutlinedTextField",
        Signature = TextFieldStringSig,
        Defaults  = typeof(TextFieldDefault))]
    public static partial void OutlinedTextField(string value, IFunction1 onValueChange,
                                                 IModifier? modifier, IComposer composer);

    // androidx.compose.material3.AndroidAlertDialog_androidKt.AlertDialog-Oix01E0
    [ComposeBridge(
        Class     = "androidx/compose/material3/AndroidAlertDialog_androidKt",
        JvmName   = "AlertDialog-Oix01E0",
        Signature = "(Lkotlin/jvm/functions/Function0;Lkotlin/jvm/functions/Function2;" +
                    "Landroidx/compose/ui/Modifier;" +
                    "Lkotlin/jvm/functions/Function2;Lkotlin/jvm/functions/Function2;" +
                    "Lkotlin/jvm/functions/Function2;Lkotlin/jvm/functions/Function2;" +
                    "Landroidx/compose/ui/graphics/Shape;JJJJF" +
                    "Landroidx/compose/ui/window/DialogProperties;" +
                    "Landroidx/compose/runtime/Composer;III)V",
        Defaults  = typeof(AlertDialogDefault))]
    public static partial void AlertDialog(
        IFunction0  onDismissRequest,
        IFunction2  confirmButton,
        IModifier?  modifier,
        IFunction2? dismissButton,
        IFunction2? icon,
        IFunction2? title,
        IFunction2? text,
        int         defaults,
        IComposer   composer);

    // androidx.compose.material3.ModalBottomSheet_androidKt.ModalBottomSheet-dYc4hso
    [ComposeBridge(
        Class     = "androidx/compose/material3/ModalBottomSheet_androidKt",
        JvmName   = "ModalBottomSheet-dYc4hso",
        Signature = "(Lkotlin/jvm/functions/Function0;Landroidx/compose/ui/Modifier;" +
                    "Landroidx/compose/material3/SheetState;FLandroidx/compose/ui/graphics/Shape;JJFJ" +
                    "Lkotlin/jvm/functions/Function2;Landroidx/compose/foundation/layout/WindowInsets;" +
                    "Landroidx/compose/material3/ModalBottomSheetProperties;" +
                    "Lkotlin/jvm/functions/Function3;Landroidx/compose/runtime/Composer;III)V",
        Defaults  = typeof(ModalBottomSheetDefault))]
    public static partial void ModalBottomSheet(
        IFunction0  onDismissRequest,
        IModifier?  modifier,
        IntPtr      sheetState,
        IFunction2? dragHandle,
        IFunction3  content,
        int         defaults,
        IComposer   composer);

    // androidx.compose.material3.BottomSheetScaffoldKt.BottomSheetScaffold-sdMYb0k
    [ComposeBridge(
        Class     = "androidx/compose/material3/BottomSheetScaffoldKt",
        JvmName   = "BottomSheetScaffold-sdMYb0k",
        Signature = "(Lkotlin/jvm/functions/Function3;Landroidx/compose/ui/Modifier;" +
                    "Landroidx/compose/material3/BottomSheetScaffoldState;FF" +
                    "Landroidx/compose/ui/graphics/Shape;JJFF" +
                    "Lkotlin/jvm/functions/Function2;Z" +
                    "Lkotlin/jvm/functions/Function2;Lkotlin/jvm/functions/Function3;JJ" +
                    "Lkotlin/jvm/functions/Function3;Landroidx/compose/runtime/Composer;III)V",
        Defaults  = typeof(BottomSheetScaffoldDefault))]
    public static partial void BottomSheetScaffold(
        IFunction3  sheetContent,
        IModifier?  modifier,
        IntPtr      scaffoldState,
        IFunction2? sheetDragHandle,
        IFunction2? topBar,
        IFunction3? snackbarHost,
        IFunction3  content,
        int         defaults,
        IComposer   composer);

    // androidx.compose.material3.ScaffoldKt.Scaffold-TvnljyQ
    [ComposeBridge(
        Class     = "androidx/compose/material3/ScaffoldKt",
        JvmName   = "Scaffold-TvnljyQ",
        Signature = "(Landroidx/compose/ui/Modifier;" +
                    "Lkotlin/jvm/functions/Function2;Lkotlin/jvm/functions/Function2;" +
                    "Lkotlin/jvm/functions/Function2;Lkotlin/jvm/functions/Function2;" +
                    "IJJ" +
                    "Landroidx/compose/foundation/layout/WindowInsets;" +
                    "Lkotlin/jvm/functions/Function3;" +
                    "Landroidx/compose/runtime/Composer;II)V",
        Defaults  = typeof(ScaffoldDefault))]
    public static partial void Scaffold(
        IModifier?  modifier,
        IFunction2? topBar,
        IFunction2? bottomBar,
        IFunction2? snackbarHost,
        IFunction2? floatingActionButton,
        IFunction3  content,
        int         defaults,
        IComposer   composer);

    // androidx.compose.material3.DatePickerDialog_androidKt.DatePickerDialog-GmEhDVc
    [ComposeBridge(
        Class     = "androidx/compose/material3/DatePickerDialog_androidKt",
        JvmName   = "DatePickerDialog-GmEhDVc",
        Signature = "(Lkotlin/jvm/functions/Function0;Lkotlin/jvm/functions/Function2;" +
                    "Landroidx/compose/ui/Modifier;Lkotlin/jvm/functions/Function2;" +
                    "Landroidx/compose/ui/graphics/Shape;F" +
                    "Landroidx/compose/material3/DatePickerColors;" +
                    "Landroidx/compose/ui/window/DialogProperties;" +
                    "Lkotlin/jvm/functions/Function3;Landroidx/compose/runtime/Composer;II)V",
        Defaults  = typeof(DatePickerDialogDefault))]
    public static partial void DatePickerDialog(
        IFunction0  onDismissRequest,
        IFunction2  confirmButton,
        IModifier?  modifier,
        IFunction2? dismissButton,
        IFunction3  content,
        int         defaults,
        IComposer   composer);

    // androidx.compose.material3.TimePickerKt.TimePicker-mT9BvqQ
    [ComposeBridge(
        Class     = "androidx/compose/material3/TimePickerKt",
        JvmName   = "TimePicker-mT9BvqQ",
        Signature = "(Landroidx/compose/material3/TimePickerState;Landroidx/compose/ui/Modifier;" +
                    "Landroidx/compose/material3/TimePickerColors;ILandroidx/compose/runtime/Composer;II)V",
        Defaults  = typeof(TimePickerDefault))]
    public static partial void TimePicker(IntPtr state, IModifier? modifier,
                                          int defaults, IComposer composer);

    // androidx.compose.material3.TimePickerDialogKt.TimePickerDialog-FItCLgY
    [ComposeBridge(
        Class     = "androidx/compose/material3/TimePickerDialogKt",
        JvmName   = "TimePickerDialog-FItCLgY",
        Signature = "(Lkotlin/jvm/functions/Function0;Lkotlin/jvm/functions/Function2;" +
                    "Lkotlin/jvm/functions/Function2;Landroidx/compose/ui/Modifier;" +
                    "Landroidx/compose/ui/window/DialogProperties;" +
                    "Lkotlin/jvm/functions/Function2;Lkotlin/jvm/functions/Function2;" +
                    "Landroidx/compose/ui/graphics/Shape;J" +
                    "Lkotlin/jvm/functions/Function3;Landroidx/compose/runtime/Composer;II)V",
        Defaults  = typeof(TimePickerDialogDefault))]
    public static partial void TimePickerDialog(
        IFunction0  onDismissRequest,
        IFunction2  confirmButton,
        IFunction2  dismissButton,
        IModifier?  modifier,
        IFunction2? title,
        IFunction2? modeToggleButton,
        IFunction3  content,
        int         defaults,
        IComposer   composer);

    // androidx.compose.material3.TooltipKt.TooltipBox (7-user-param overload)
    [ComposeBridge(
        Class     = "androidx/compose/material3/TooltipKt",
        JvmName   = "TooltipBox",
        Signature = "(Landroidx/compose/ui/window/PopupPositionProvider;" +
                    "Lkotlin/jvm/functions/Function3;" +
                    "Landroidx/compose/material3/TooltipState;" +
                    "Landroidx/compose/ui/Modifier;ZZ" +
                    "Lkotlin/jvm/functions/Function2;Landroidx/compose/runtime/Composer;II)V",
        Defaults  = typeof(TooltipBoxDefault))]
    public static partial void TooltipBox(
        IntPtr     positionProvider,
        IFunction3 tooltip,
        IntPtr     state,
        IModifier? modifier,
        IFunction2 content,
        int        defaults,
        IComposer  composer);

    // androidx.compose.material3.DatePickerKt.DatePicker
    [ComposeBridge(
        Class     = "androidx/compose/material3/DatePickerKt",
        JvmName   = "DatePicker",
        Signature = "(Landroidx/compose/material3/DatePickerState;Landroidx/compose/ui/Modifier;" +
                    "Landroidx/compose/material3/DatePickerFormatter;" +
                    "Landroidx/compose/material3/DatePickerColors;" +
                    "Lkotlin/jvm/functions/Function2;Lkotlin/jvm/functions/Function2;Z" +
                    "Landroidx/compose/ui/focus/FocusRequester;" +
                    "Landroidx/compose/runtime/Composer;II)V",
        Defaults  = typeof(DatePickerDefault))]
    public static partial void DatePicker(IntPtr state, IModifier? modifier,
                                          int defaults, IComposer composer);

    // androidx.compose.material3.DatePickerKt.rememberDatePickerState-EU0dCGE
    [ComposeBridge(
        Class     = "androidx/compose/material3/DatePickerKt",
        JvmName   = "rememberDatePickerState-EU0dCGE",
        Signature = "(Ljava/lang/Long;Ljava/lang/Long;Lkotlin/ranges/IntRange;I" +
                    "Landroidx/compose/material3/SelectableDates;" +
                    "Landroidx/compose/runtime/Composer;II)Landroidx/compose/material3/DatePickerState;",
        Defaults  = typeof(RememberDatePickerStateDefault))]
    public static partial IntPtr RememberDatePickerState(IComposer composer);

    // androidx.compose.material3.TimePickerKt.rememberTimePickerState
    [ComposeBridge(
        Class     = "androidx/compose/material3/TimePickerKt",
        JvmName   = "rememberTimePickerState",
        Signature = "(IIZLandroidx/compose/runtime/Composer;II)Landroidx/compose/material3/TimePickerState;",
        Defaults  = typeof(RememberTimePickerStateDefault))]
    public static partial IntPtr RememberTimePickerState(int initialHour, int initialMinute,
                                                         bool is24Hour, IComposer composer);

    // androidx.compose.material3.TooltipKt.rememberTooltipState
    [ComposeBridge(
        Class     = "androidx/compose/material3/TooltipKt",
        JvmName   = "rememberTooltipState",
        Signature = "(ZZLandroidx/compose/foundation/MutatorMutex;Landroidx/compose/runtime/Composer;II)Landroidx/compose/material3/TooltipState;",
        Defaults  = typeof(RememberTooltipStateDefault))]
    public static partial IntPtr RememberTooltipState(bool isPersistent, IComposer composer);

    // androidx.compose.material3.TooltipDefaults.INSTANCE.rememberPlainTooltipPositionProvider-kHDZbjc
    // Instance method on a Kotlin object singleton.
    [ComposeBridge(
        Class         = "androidx/compose/material3/TooltipDefaults",
        JvmName       = "rememberPlainTooltipPositionProvider-kHDZbjc",
        Signature     = "(FLandroidx/compose/runtime/Composer;II)Landroidx/compose/ui/window/PopupPositionProvider;",
        Defaults      = typeof(RememberPlainTooltipPositionProviderDefault),
        InstanceField = "INSTANCE")]
    public static partial IntPtr RememberPlainTooltipPositionProvider(IComposer composer);

    // androidx.compose.material3.CardKt.Card (non-clickable)
    const string CardSig =
        "(Landroidx/compose/ui/Modifier;Landroidx/compose/ui/graphics/Shape;" +
        "Landroidx/compose/material3/CardColors;Landroidx/compose/material3/CardElevation;" +
        "Landroidx/compose/foundation/BorderStroke;" +
        "Lkotlin/jvm/functions/Function3;Landroidx/compose/runtime/Composer;II)V";

    [ComposeBridge(
        Class     = "androidx/compose/material3/CardKt",
        JvmName   = "Card",
        Signature = CardSig,
        Defaults  = typeof(CardDefault))]
    public static partial void Card(IModifier? modifier, IFunction3 content, IComposer composer);

    // androidx.compose.material3.CardKt.OutlinedCard (same shape as Card)
    [ComposeBridge(
        Class     = "androidx/compose/material3/CardKt",
        JvmName   = "OutlinedCard",
        Signature = CardSig,
        Defaults  = typeof(CardDefault))]
    public static partial void OutlinedCard(IFunction3 content, IComposer composer);

    // androidx.compose.material3.CardKt.ElevatedCard (no border)
    [ComposeBridge(
        Class     = "androidx/compose/material3/CardKt",
        JvmName   = "ElevatedCard",
        Signature = "(Landroidx/compose/ui/Modifier;Landroidx/compose/ui/graphics/Shape;" +
                    "Landroidx/compose/material3/CardColors;Landroidx/compose/material3/CardElevation;" +
                    "Lkotlin/jvm/functions/Function3;Landroidx/compose/runtime/Composer;II)V",
        Defaults  = typeof(ElevatedCardDefault))]
    public static partial void ElevatedCard(IFunction3 content, IComposer composer);

    const string AssistChipSig =
        "(Lkotlin/jvm/functions/Function0;Lkotlin/jvm/functions/Function2;" +
        "Landroidx/compose/ui/Modifier;Z" +
        "Lkotlin/jvm/functions/Function2;Lkotlin/jvm/functions/Function2;" +
        "Landroidx/compose/ui/graphics/Shape;" +
        "Landroidx/compose/material3/ChipColors;Landroidx/compose/material3/ChipElevation;" +
        "Landroidx/compose/foundation/BorderStroke;" +
        "Landroidx/compose/foundation/interaction/MutableInteractionSource;" +
        "Landroidx/compose/runtime/Composer;III)V";

    [ComposeBridge(
        Class     = "androidx/compose/material3/ChipKt",
        JvmName   = "AssistChip",
        Signature = AssistChipSig,
        Defaults  = typeof(AssistChipDefault))]
    public static partial void AssistChip(
        IFunction0  onClick,
        IFunction2  label,
        IModifier?  modifier,
        IFunction2? leadingIcon,
        IFunction2? trailingIcon,
        int         defaults,
        IComposer   composer);

    [ComposeBridge(
        Class     = "androidx/compose/material3/ChipKt",
        JvmName   = "ElevatedAssistChip",
        Signature = AssistChipSig,
        Defaults  = typeof(AssistChipDefault))]
    public static partial void ElevatedAssistChip(
        IFunction0  onClick,
        IFunction2  label,
        IFunction2? leadingIcon,
        IFunction2? trailingIcon,
        int         defaults,
        IComposer   composer);

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

    [ComposeBridge(
        Class     = "androidx/compose/material3/ChipKt",
        JvmName   = "FilterChip",
        Signature = FilterChipSig,
        Defaults  = typeof(FilterChipDefault))]
    public static partial void FilterChip(
        bool        selected,
        IFunction0  onClick,
        IFunction2  label,
        IModifier?  modifier,
        IFunction2? leadingIcon,
        IFunction2? trailingIcon,
        int         defaults,
        IComposer   composer);

    [ComposeBridge(
        Class     = "androidx/compose/material3/ChipKt",
        JvmName   = "ElevatedFilterChip",
        Signature = FilterChipSig,
        Defaults  = typeof(FilterChipDefault))]
    public static partial void ElevatedFilterChip(
        bool        selected,
        IFunction0  onClick,
        IFunction2  label,
        IFunction2? leadingIcon,
        IFunction2? trailingIcon,
        int         defaults,
        IComposer   composer);

    // androidx.compose.material3.ChipKt.InputChip
    [ComposeBridge(
        Class     = "androidx/compose/material3/ChipKt",
        JvmName   = "InputChip",
        Signature = "(ZLkotlin/jvm/functions/Function0;Lkotlin/jvm/functions/Function2;" +
                    "Landroidx/compose/ui/Modifier;Z" +
                    "Lkotlin/jvm/functions/Function2;Lkotlin/jvm/functions/Function2;" +
                    "Lkotlin/jvm/functions/Function2;Landroidx/compose/ui/graphics/Shape;" +
                    "Landroidx/compose/material3/SelectableChipColors;" +
                    "Landroidx/compose/material3/SelectableChipElevation;" +
                    "Landroidx/compose/foundation/BorderStroke;" +
                    "Landroidx/compose/foundation/interaction/MutableInteractionSource;" +
                    "Landroidx/compose/runtime/Composer;III)V",
        Defaults  = typeof(InputChipDefault))]
    public static partial void InputChip(
        bool        selected,
        IFunction0  onClick,
        IFunction2  label,
        IModifier?  modifier,
        IFunction2? leadingIcon,
        IFunction2? avatar,
        IFunction2? trailingIcon,
        int         defaults,
        IComposer   composer);

    const string SuggestionChipSig =
        "(Lkotlin/jvm/functions/Function0;Lkotlin/jvm/functions/Function2;" +
        "Landroidx/compose/ui/Modifier;Z" +
        "Lkotlin/jvm/functions/Function2;Landroidx/compose/ui/graphics/Shape;" +
        "Landroidx/compose/material3/ChipColors;Landroidx/compose/material3/ChipElevation;" +
        "Landroidx/compose/foundation/BorderStroke;" +
        "Landroidx/compose/foundation/interaction/MutableInteractionSource;" +
        "Landroidx/compose/runtime/Composer;II)V";

    [ComposeBridge(
        Class     = "androidx/compose/material3/ChipKt",
        JvmName   = "SuggestionChip",
        Signature = SuggestionChipSig,
        Defaults  = typeof(SuggestionChipDefault))]
    public static partial void SuggestionChip(
        IFunction0  onClick,
        IFunction2  label,
        IModifier?  modifier,
        IFunction2? icon,
        int         defaults,
        IComposer   composer);

    [ComposeBridge(
        Class     = "androidx/compose/material3/ChipKt",
        JvmName   = "ElevatedSuggestionChip",
        Signature = SuggestionChipSig,
        Defaults  = typeof(SuggestionChipDefault))]
    public static partial void ElevatedSuggestionChip(
        IFunction0  onClick,
        IFunction2  label,
        IFunction2? icon,
        int         defaults,
        IComposer   composer);

    // androidx.compose.material3.NavigationBarKt.NavigationBar-HsRjFd4
    [ComposeBridge(
        Class     = "androidx/compose/material3/NavigationBarKt",
        JvmName   = "NavigationBar-HsRjFd4",
        Signature = "(Landroidx/compose/ui/Modifier;JJF" +
                    "Landroidx/compose/foundation/layout/WindowInsets;" +
                    "Lkotlin/jvm/functions/Function3;Landroidx/compose/runtime/Composer;II)V",
        Defaults  = typeof(NavigationBarDefault))]
    public static partial void NavigationBar(IModifier? modifier, IFunction3 content, IComposer composer);

    // androidx.compose.material3.NavigationBarKt.NavigationBarItem (RowScope receiver)
    [ComposeBridge(
        Class     = "androidx/compose/material3/NavigationBarKt",
        JvmName   = "NavigationBarItem",
        Signature = "(Landroidx/compose/foundation/layout/RowScope;Z" +
                    "Lkotlin/jvm/functions/Function0;Lkotlin/jvm/functions/Function2;" +
                    "Landroidx/compose/ui/Modifier;Z" +
                    "Lkotlin/jvm/functions/Function2;Z" +
                    "Landroidx/compose/material3/NavigationBarItemColors;" +
                    "Landroidx/compose/foundation/interaction/MutableInteractionSource;" +
                    "Landroidx/compose/runtime/Composer;II)V",
        Defaults  = typeof(NavigationBarItemDefault))]
    public static partial void NavigationBarItem(
        IntPtr      rowScope,
        bool        selected,
        IFunction0  onClick,
        IFunction2  icon,
        IModifier?  modifier,
        IFunction2? label,
        int         defaults,
        IComposer   composer);

    // androidx.compose.material3.NavigationRailKt.NavigationRail-qi6gXK8
    [ComposeBridge(
        Class     = "androidx/compose/material3/NavigationRailKt",
        JvmName   = "NavigationRail-qi6gXK8",
        Signature = "(Landroidx/compose/ui/Modifier;JJ" +
                    "Lkotlin/jvm/functions/Function3;" +
                    "Landroidx/compose/foundation/layout/WindowInsets;" +
                    "Lkotlin/jvm/functions/Function3;Landroidx/compose/runtime/Composer;II)V",
        Defaults  = typeof(NavigationRailDefault))]
    public static partial void NavigationRail(IModifier? modifier, IFunction3 content, IComposer composer);

    // androidx.compose.material3.NavigationRailKt.NavigationRailItem
    [ComposeBridge(
        Class     = "androidx/compose/material3/NavigationRailKt",
        JvmName   = "NavigationRailItem",
        Signature = "(ZLkotlin/jvm/functions/Function0;Lkotlin/jvm/functions/Function2;" +
                    "Landroidx/compose/ui/Modifier;Z" +
                    "Lkotlin/jvm/functions/Function2;Z" +
                    "Landroidx/compose/material3/NavigationRailItemColors;" +
                    "Landroidx/compose/foundation/interaction/MutableInteractionSource;" +
                    "Landroidx/compose/runtime/Composer;II)V",
        Defaults  = typeof(NavigationRailItemDefault))]
    public static partial void NavigationRailItem(
        bool        selected,
        IFunction0  onClick,
        IFunction2  icon,
        IModifier?  modifier,
        IFunction2? label,
        int         defaults,
        IComposer   composer);

    // 3 drawer-sheet variants share the same DrawerSheetDefault enum + signature.
    const string DrawerSheetSig =
        "(Landroidx/compose/ui/Modifier;Landroidx/compose/ui/graphics/Shape;JJF" +
        "Landroidx/compose/foundation/layout/WindowInsets;" +
        "Lkotlin/jvm/functions/Function3;Landroidx/compose/runtime/Composer;II)V";

    [ComposeBridge(
        Class     = "androidx/compose/material3/NavigationDrawerKt",
        JvmName   = "ModalDrawerSheet-afqeVBk",
        Signature = DrawerSheetSig,
        Defaults  = typeof(DrawerSheetDefault))]
    public static partial void ModalDrawerSheet(IFunction3 content, long drawerContainerColor, IComposer composer);

    [ComposeBridge(
        Class     = "androidx/compose/material3/NavigationDrawerKt",
        JvmName   = "DismissibleDrawerSheet-afqeVBk",
        Signature = DrawerSheetSig,
        Defaults  = typeof(DrawerSheetDefault))]
    public static partial void DismissibleDrawerSheet(IFunction3 content, long drawerContainerColor, IComposer composer);

    [ComposeBridge(
        Class     = "androidx/compose/material3/NavigationDrawerKt",
        JvmName   = "PermanentDrawerSheet-afqeVBk",
        Signature = DrawerSheetSig,
        Defaults  = typeof(DrawerSheetDefault))]
    public static partial void PermanentDrawerSheet(IFunction3 content, long drawerContainerColor, IComposer composer);
}
