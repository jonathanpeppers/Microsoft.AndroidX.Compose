using Android.Runtime;
using AndroidX.Compose.Material3;

namespace ComposeNet;

// Hand-written JNI bridges for Kotlin synthetic `$default` overloads
// that yield a fully-defaulted Material 3 ColorScheme. Used by
// MaterialTheme.LightColorScheme() / MaterialTheme.DarkColorScheme() so
// callers can take the Material 3 baseline palette without having to
// supply all 47 Color values themselves.
//
// `ColorSchemeKt.lightColorScheme(J×47)` and `darkColorScheme(J×47)`
// ARE bound directly — `Color` is `@JvmInline value class Color(val value: ULong)`
// which the binder represents as plain `long`, so the JVM names aren't
// stripped. What ISN'T bound is the synthetic `$default` sibling
// (Kotlin emits it for any function whose parameters have default
// values), so a managed caller currently has to pass all 47 colors
// explicitly. These bridges invoke the synthetic with every bit of the
// `$default` mask set, telling Kotlin to substitute its real per-slot
// defaults from the M3 tonal palette tokens.
//
// Why raw JNI instead of `[ComposeBridge]`: the generator's
// non-`@Composable` shape requires at least one user parameter (it
// maps each C# param positionally to a JNI slot and computes the
// auto-mask from nullable / IntPtr.Zero passes). Here every parameter
// is defaulted, so the easiest path is to hand-roll the JNI call and
// pre-bake mask0 = mask1 = -1.
internal static partial class ComposeBridges
{
    // 47 J slots (one per Color param) + II (two $default mask ints,
    // since 47 > 32 bits) + L (synthetic-overload Object marker).
    const string ColorSchemeDefaultSig =
        "(JJJJJJJJJJJJJJJJJJJJJJJJJJJJJJJJJJJJJJJJJJJJJJJ" +
        "IILjava/lang/Object;)" +
        "Landroidx/compose/material3/ColorScheme;";

    static System.IntPtr s_lightColorSchemeDefault_class;
    static System.IntPtr s_lightColorSchemeDefault_method;
    static System.IntPtr s_darkColorSchemeDefault_class;
    static System.IntPtr s_darkColorSchemeDefault_method;

    /// <summary>
    /// Build a Material 3 light <see cref="ColorScheme"/> using every
    /// per-slot default from the M3 tonal palette tokens.
    /// </summary>
    internal static ColorScheme DefaultLightColorScheme() => InvokeColorSchemeDefault(
        ref s_lightColorSchemeDefault_class,
        ref s_lightColorSchemeDefault_method,
        "lightColorScheme$default");

    /// <summary>
    /// Build a Material 3 dark <see cref="ColorScheme"/> using every
    /// per-slot default from the M3 tonal palette tokens.
    /// </summary>
    internal static ColorScheme DefaultDarkColorScheme() => InvokeColorSchemeDefault(
        ref s_darkColorSchemeDefault_class,
        ref s_darkColorSchemeDefault_method,
        "darkColorScheme$default");

    static unsafe ColorScheme InvokeColorSchemeDefault(
        ref System.IntPtr classCache,
        ref System.IntPtr methodCache,
        string jvmName)
    {
        if (methodCache == System.IntPtr.Zero)
        {
            classCache = JNIEnv.FindClass("androidx/compose/material3/ColorSchemeKt");
            methodCache = JNIEnv.GetStaticMethodID(classCache, jvmName, ColorSchemeDefaultSig);
        }

        // 47 colors + 2 mask ints + 1 marker = 50 JValue slots.
        JValue* args = stackalloc JValue[50];
        for (int i = 0; i < 47; i++)
            args[i] = new JValue(0L);
        // Both masks all-bits-set: bit N == 1 tells Kotlin "use the
        // per-slot default". Bits past position 46 (the last real
        // parameter) are never tested by the generated $default body,
        // so -1 for both is safe.
        args[47] = new JValue(-1);
        args[48] = new JValue(-1);
        args[49] = new JValue(System.IntPtr.Zero);

        System.IntPtr handle = JNIEnv.CallStaticObjectMethod(classCache, methodCache, args);
        return Java.Lang.Object.GetObject<ColorScheme>(handle, JniHandleOwnership.TransferLocalRef)!;
    }
}
