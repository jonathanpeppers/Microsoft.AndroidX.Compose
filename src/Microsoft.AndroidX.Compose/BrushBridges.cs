using Android.Runtime;
using BoundBrush = AndroidX.Compose.UI.Graphics.Brush;
using BoundColor = AndroidX.Compose.UI.Graphics.Color;

namespace AndroidX.Compose;

// Interop for the Compose-graphics boxing factory:
//
//   - `androidx.compose.ui.graphics.Color.box-impl(J)Color` — the
//     Kotlin-synthetic boxing factory that turns a packed `long` into
//     a `Color` object. The `Color` class itself is bound, but its
//     ctor and `box-impl` static are skipped (value-class lowering).
//     The invocation is source-generated; BoxColor owns the returned local.
//
// Everything else (the bound SolidColor constructor, Brush.Companion's gradient factories,
// `RectangleShapeKt.RectangleShape`) is bound and called directly
// from `Brush` / `Shape`.
internal static partial class ComposeBridges
{
    static BoundBrush.Companion? s_brushCompanion;

    // Lazy access to the `androidx.compose.ui.graphics.Brush$Companion`
    // singleton — the binder exposes the type but not a public C# accessor
    // for the Kotlin `Companion` static field, so we fetch it via JNI
    // once and cache the bound peer. Subsequent gradient calls go
    // straight through the bound `LinearGradient_mHitzGk`/etc. methods.
    internal static BoundBrush.Companion BrushCompanion()
    {
        if (s_brushCompanion is not null) return s_brushCompanion;
        IntPtr local = IntPtr.Zero;
        try
        {
            IntPtr brushClass = Java.Lang.Class.FromType(typeof(BoundBrush)).Handle;
            IntPtr fid = JNIEnv.GetStaticFieldID(
                brushClass, "Companion",
                "Landroidx/compose/ui/graphics/Brush$Companion;");
            local = JNIEnv.GetStaticObjectField(brushClass, fid);
            return s_brushCompanion = Java.Lang.Object.GetObject<BoundBrush.Companion>(
                local, JniHandleOwnership.TransferLocalRef)!;
        }
        finally
        {
            if (local != IntPtr.Zero && s_brushCompanion is null)
                JNIEnv.DeleteLocalRef(local);
        }
    }

    // Box a packed-long Color into a bound `BoundColor` peer via the
    // Kotlin-synthetic `Color.box-impl(J)` static. Required because
    // `BoundBrush.Companion.LinearGradient_mHitzGk(IList<Color>, ...)`
    // (and every other gradient factory) takes a List of *boxed* Color
    // objects — packed longs alone aren't acceptable.
    [ComposeBridge(Class = "androidx/compose/ui/graphics/Color",
                   JvmName = "box-impl",
                   Signature = "(J)Landroidx/compose/ui/graphics/Color;")]
    internal static partial IntPtr BoxColorCore(long packed);

    // Why raw JNI: BoxColorCore returns a local reference that DoNotTransfer does not consume.
    // Release it in finally even if creating the managed Color peer throws.
    internal static BoundColor BoxColor(long packed)
    {
        IntPtr handle = BoxColorCore(packed);
        try
        {
            return Java.Lang.Object.GetObject<BoundColor>(handle, JniHandleOwnership.DoNotTransfer)
                ?? throw new InvalidOperationException("Color.box-impl did not return a Color.");
        }
        finally
        {
            JNIEnv.DeleteLocalRef(handle);
        }
    }

    // Build an `IList<BoundColor>` from a managed `Color[]` for the
    // gradient factories. `JavaList<T>` is the Mono.Android wrapper
    // over `java.util.ArrayList` — no further JNI required for `add`.
    internal static IList<BoundColor> ToColorList(Color[] colors)
    {
        ArgumentNullException.ThrowIfNull(colors);
        if (colors.Length == 0)
            throw new ArgumentException(
                "Gradient must have at least one color stop.", nameof(colors));
        var list = new JavaList<BoundColor>();
        foreach (var c in colors)
            list.Add(BoxColor(c.ToPacked()));
        return list;
    }
}
