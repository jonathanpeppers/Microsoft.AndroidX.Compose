using Android.Runtime;

namespace AndroidX.Compose;

// Hand-written raw-JNI bridges for `androidx.compose.ui.graphics.Brush`
// and its `Companion` factories. Almost every entry point is mangled
// because the gradient factories take `List<Color>` (and Color is a
// `@JvmInline value class`), so the binder strips the C# overloads —
// even though the type itself (`AndroidX.Compose.UI.Graphics.Brush`)
// is bound.
//
// Why not `[ComposeBridge]`?
//   The bridge generator doesn't model "build a `java.util.ArrayList<Color>`
//   from a `long[]` arg by calling `Color.box-impl(J)` per element"
//   — that's the JNI dance below. Once that's hand-rolled, the rest of
//   each factory (FindClass → GetStaticMethodID → CallStaticObjectMethod
//   on the Companion) is small enough that staying hand-written is the
//   pragmatic shape. Constructed Brush instances are returned as raw
//   `IntPtr` so the public `Brush` wrapper can pick them up via
//   `Java.Lang.Object.GetObject<Brush>(handle, TransferLocalRef)`.
//
// Also hand-rolls:
//   - The `RectangleShape` singleton accessor used by `Shape.Rectangle`.
//   - The `SolidColor(Color)` ctor (mangled because Color is a value
//     class — the binding exposes the class but not the ctor).
internal static partial class ComposeBridges
{
    // Cached `androidx.compose.ui.graphics.Color` class ref (stable
    // global from Mono.Android — see KotlinResult.cs for the rationale).
    static IntPtr s_colorClass;
    static IntPtr s_color_boxImpl;

    // Cached `androidx.compose.ui.graphics.Brush$Companion` singleton —
    // returned by every gradient factory. Held as a global ref so the
    // GC can't move it out from under repeated FindClass invocations.
    static IntPtr s_brushCompanion_class;
    static IntPtr s_brushCompanion_handle;

    static IntPtr s_brush_linearGradient_method;
    static IntPtr s_brush_horizontalGradient_method;
    static IntPtr s_brush_verticalGradient_method;
    static IntPtr s_brush_radialGradient_method;
    static IntPtr s_brush_sweepGradient_method;

    static IntPtr s_solidColor_class;
    static IntPtr s_solidColor_ctor;

    static IntPtr s_arrayList_class;
    static IntPtr s_arrayList_ctor;
    static IntPtr s_arrayList_add;

    // Cached `RectangleShapeKt.getRectangleShape()` singleton — Compose's
    // canonical "no clipping, just a rectangle" Shape. Used by
    // `Shape.Rectangle` and by the default branch of every
    // `Modifier.Background(Brush, ...)` overload that doesn't take an
    // explicit shape.
    static IntPtr s_rectangleShape_handle;

    static void EnsureColorBox()
    {
        if (s_color_boxImpl != IntPtr.Zero) return;
        s_colorClass = JNIEnv.FindClass("androidx/compose/ui/graphics/Color");
        s_color_boxImpl = JNIEnv.GetStaticMethodID(
            s_colorClass, "box-impl", "(J)Landroidx/compose/ui/graphics/Color;");
    }

    static void EnsureArrayList()
    {
        if (s_arrayList_add != IntPtr.Zero) return;
        s_arrayList_class = JNIEnv.FindClass("java/util/ArrayList");
        s_arrayList_ctor  = JNIEnv.GetMethodID(s_arrayList_class, "<init>", "(I)V");
        s_arrayList_add   = JNIEnv.GetMethodID(s_arrayList_class, "add",    "(Ljava/lang/Object;)Z");
    }

    // Build a `java.util.ArrayList<androidx.compose.ui.graphics.Color>`
    // from a packed-long array. Each long is boxed into a
    // `Color` peer via the Kotlin-synthetic `Color.box-impl(J)`
    // static method — that's the only way to materialise a `Color`
    // peer from a packed value, because the type's instance ctor
    // is private at the bytecode level. Caller owns the returned
    // local ref.
    internal static unsafe IntPtr BoxColorList(long[] colors)
    {
        ArgumentNullException.ThrowIfNull(colors);
        if (colors.Length == 0)
            throw new ArgumentException("Gradient must have at least one color stop.", nameof(colors));

        EnsureColorBox();
        EnsureArrayList();

        JValue* ctorArgs = stackalloc JValue[1];
        ctorArgs[0] = new JValue(colors.Length);
        IntPtr list = JNIEnv.NewObject(s_arrayList_class, s_arrayList_ctor, ctorArgs);
        try
        {
            JValue* boxArgs = stackalloc JValue[1];
            JValue* addArgs = stackalloc JValue[1];
            for (int i = 0; i < colors.Length; i++)
            {
                boxArgs[0] = new JValue(colors[i]);
                IntPtr boxed = JNIEnv.CallStaticObjectMethod(
                    s_colorClass, s_color_boxImpl, boxArgs);
                try
                {
                    addArgs[0] = new JValue(boxed);
                    JNIEnv.CallBooleanMethod(list, s_arrayList_add, addArgs);
                }
                finally
                {
                    if (boxed != IntPtr.Zero)
                        JNIEnv.DeleteLocalRef(boxed);
                }
            }
            return list;
        }
        catch
        {
            if (list != IntPtr.Zero)
                JNIEnv.DeleteLocalRef(list);
            throw;
        }
    }

    // androidx.compose.ui.graphics.Brush$Companion — singleton companion
    // object that exposes every gradient factory. Lazily loaded on first
    // use, then cached as a global ref so we never re-walk
    // FindClass/GetStaticFieldID once warmed up.
    static IntPtr BrushCompanion()
    {
        if (s_brushCompanion_handle != IntPtr.Zero)
            return s_brushCompanion_handle;

        s_brushCompanion_class = JNIEnv.FindClass(
            "androidx/compose/ui/graphics/Brush$Companion");

        IntPtr brushClass = JNIEnv.FindClass("androidx/compose/ui/graphics/Brush");
        IntPtr companionField = JNIEnv.GetStaticFieldID(
            brushClass, "Companion", "Landroidx/compose/ui/graphics/Brush$Companion;");
        IntPtr local = JNIEnv.GetStaticObjectField(brushClass, companionField);
        try
        {
            s_brushCompanion_handle = JNIEnv.NewGlobalRef(local);
        }
        finally
        {
            if (local != IntPtr.Zero)
                JNIEnv.DeleteLocalRef(local);
        }
        return s_brushCompanion_handle;
    }

    // Brush.Companion.linearGradient-mHitzGk(List<Color>, long start,
    // long end, int tileMode) -> Brush.
    internal static unsafe IntPtr BrushLinearGradient(
        long[] colors, long start, long end, int tileMode)
    {
        IntPtr list = BoxColorList(colors);
        try
        {
            if (s_brush_linearGradient_method == IntPtr.Zero)
            {
                IntPtr companion = BrushCompanion();
                _ = companion;
                s_brush_linearGradient_method = JNIEnv.GetMethodID(
                    s_brushCompanion_class,
                    "linearGradient-mHitzGk",
                    "(Ljava/util/List;JJI)Landroidx/compose/ui/graphics/Brush;");
            }

            JValue* args = stackalloc JValue[4];
            args[0] = new JValue(list);
            args[1] = new JValue(start);
            args[2] = new JValue(end);
            args[3] = new JValue(tileMode);
            return JNIEnv.CallObjectMethod(
                BrushCompanion(), s_brush_linearGradient_method, args);
        }
        finally
        {
            if (list != IntPtr.Zero)
                JNIEnv.DeleteLocalRef(list);
        }
    }

    // Brush.Companion.horizontalGradient-8A-3gB4(List<Color>, float
    // startX, float endX, int tileMode) -> Brush.
    internal static unsafe IntPtr BrushHorizontalGradient(
        long[] colors, float startX, float endX, int tileMode)
    {
        IntPtr list = BoxColorList(colors);
        try
        {
            if (s_brush_horizontalGradient_method == IntPtr.Zero)
            {
                _ = BrushCompanion();
                s_brush_horizontalGradient_method = JNIEnv.GetMethodID(
                    s_brushCompanion_class,
                    "horizontalGradient-8A-3gB4",
                    "(Ljava/util/List;FFI)Landroidx/compose/ui/graphics/Brush;");
            }

            JValue* args = stackalloc JValue[4];
            args[0] = new JValue(list);
            args[1] = new JValue(startX);
            args[2] = new JValue(endX);
            args[3] = new JValue(tileMode);
            return JNIEnv.CallObjectMethod(
                BrushCompanion(), s_brush_horizontalGradient_method, args);
        }
        finally
        {
            if (list != IntPtr.Zero)
                JNIEnv.DeleteLocalRef(list);
        }
    }

    // Brush.Companion.verticalGradient-8A-3gB4(List<Color>, float startY,
    // float endY, int tileMode) -> Brush.
    internal static unsafe IntPtr BrushVerticalGradient(
        long[] colors, float startY, float endY, int tileMode)
    {
        IntPtr list = BoxColorList(colors);
        try
        {
            if (s_brush_verticalGradient_method == IntPtr.Zero)
            {
                _ = BrushCompanion();
                s_brush_verticalGradient_method = JNIEnv.GetMethodID(
                    s_brushCompanion_class,
                    "verticalGradient-8A-3gB4",
                    "(Ljava/util/List;FFI)Landroidx/compose/ui/graphics/Brush;");
            }

            JValue* args = stackalloc JValue[4];
            args[0] = new JValue(list);
            args[1] = new JValue(startY);
            args[2] = new JValue(endY);
            args[3] = new JValue(tileMode);
            return JNIEnv.CallObjectMethod(
                BrushCompanion(), s_brush_verticalGradient_method, args);
        }
        finally
        {
            if (list != IntPtr.Zero)
                JNIEnv.DeleteLocalRef(list);
        }
    }

    // Brush.Companion.radialGradient-P_Vx-Ks(List<Color>, long center,
    // float radius, int tileMode) -> Brush.
    internal static unsafe IntPtr BrushRadialGradient(
        long[] colors, long center, float radius, int tileMode)
    {
        IntPtr list = BoxColorList(colors);
        try
        {
            if (s_brush_radialGradient_method == IntPtr.Zero)
            {
                _ = BrushCompanion();
                s_brush_radialGradient_method = JNIEnv.GetMethodID(
                    s_brushCompanion_class,
                    "radialGradient-P_Vx-Ks",
                    "(Ljava/util/List;JFI)Landroidx/compose/ui/graphics/Brush;");
            }

            JValue* args = stackalloc JValue[4];
            args[0] = new JValue(list);
            args[1] = new JValue(center);
            args[2] = new JValue(radius);
            args[3] = new JValue(tileMode);
            return JNIEnv.CallObjectMethod(
                BrushCompanion(), s_brush_radialGradient_method, args);
        }
        finally
        {
            if (list != IntPtr.Zero)
                JNIEnv.DeleteLocalRef(list);
        }
    }

    // Brush.Companion.sweepGradient-Uv8p0NA(List<Color>, long center) ->
    // Brush. No tileMode arg — sweep wraps around 360° naturally.
    internal static unsafe IntPtr BrushSweepGradient(long[] colors, long center)
    {
        IntPtr list = BoxColorList(colors);
        try
        {
            if (s_brush_sweepGradient_method == IntPtr.Zero)
            {
                _ = BrushCompanion();
                s_brush_sweepGradient_method = JNIEnv.GetMethodID(
                    s_brushCompanion_class,
                    "sweepGradient-Uv8p0NA",
                    "(Ljava/util/List;J)Landroidx/compose/ui/graphics/Brush;");
            }

            JValue* args = stackalloc JValue[2];
            args[0] = new JValue(list);
            args[1] = new JValue(center);
            return JNIEnv.CallObjectMethod(
                BrushCompanion(), s_brush_sweepGradient_method, args);
        }
        finally
        {
            if (list != IntPtr.Zero)
                JNIEnv.DeleteLocalRef(list);
        }
    }

    // new androidx.compose.ui.graphics.SolidColor(Color) — the data
    // class for "single solid color as a Brush". Its ctor isn't bound
    // because Color is a value-class param, so we go via raw JNI.
    internal static unsafe IntPtr BrushSolidColor(long color)
    {
        if (s_solidColor_ctor == IntPtr.Zero)
        {
            s_solidColor_class = JNIEnv.FindClass("androidx/compose/ui/graphics/SolidColor");
            s_solidColor_ctor  = JNIEnv.GetMethodID(s_solidColor_class, "<init>", "(J)V");
        }

        JValue* args = stackalloc JValue[1];
        args[0] = new JValue(color);
        return JNIEnv.NewObject(s_solidColor_class, s_solidColor_ctor, args);
    }

    // androidx.compose.ui.graphics.RectangleShapeKt.getRectangleShape() —
    // the canonical "no clipping, just a rectangle" Shape singleton.
    // Cached as a global ref so the per-call cost is one CallStaticObjectMethod
    // -free local ref handout.
    internal static IntPtr RectangleShape()
    {
        if (s_rectangleShape_handle != IntPtr.Zero)
            return s_rectangleShape_handle;

        IntPtr cls = JNIEnv.FindClass("androidx/compose/ui/graphics/RectangleShapeKt");
        IntPtr mid = JNIEnv.GetStaticMethodID(
            cls, "getRectangleShape", "()Landroidx/compose/ui/graphics/Shape;");
        IntPtr local = JNIEnv.CallStaticObjectMethod(cls, mid);
        try
        {
            s_rectangleShape_handle = JNIEnv.NewGlobalRef(local);
        }
        finally
        {
            if (local != IntPtr.Zero)
                JNIEnv.DeleteLocalRef(local);
        }
        return s_rectangleShape_handle;
    }
}
