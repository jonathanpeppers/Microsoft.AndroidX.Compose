using Android.Runtime;
using AndroidX.Compose.Material3;

namespace ComposeNet;

// Hand-written JNI bridge for the Material 3 `Typography` synthetic
// default constructor. Used by MaterialTheme.BuildTypography(...) so
// callers can override individual text-style slots while leaving the
// rest at the M3 baseline tokens.
//
// Same story as ShapesBridges: only the parameterless ctor is bound.
// The full primary constructor takes 15 TextStyle params plus
// Kotlin's synthetic `$default` int + DefaultConstructorMarker —
// hand-rolled JNI is the simplest path until the binder fix lands.
internal static partial class ComposeBridges
{
    const string TypographyDefaultCtorSig =
        "(Landroidx/compose/ui/text/TextStyle;" + // displayLarge
        "Landroidx/compose/ui/text/TextStyle;"  + // displayMedium
        "Landroidx/compose/ui/text/TextStyle;"  + // displaySmall
        "Landroidx/compose/ui/text/TextStyle;"  + // headlineLarge
        "Landroidx/compose/ui/text/TextStyle;"  + // headlineMedium
        "Landroidx/compose/ui/text/TextStyle;"  + // headlineSmall
        "Landroidx/compose/ui/text/TextStyle;"  + // titleLarge
        "Landroidx/compose/ui/text/TextStyle;"  + // titleMedium
        "Landroidx/compose/ui/text/TextStyle;"  + // titleSmall
        "Landroidx/compose/ui/text/TextStyle;"  + // bodyLarge
        "Landroidx/compose/ui/text/TextStyle;"  + // bodyMedium
        "Landroidx/compose/ui/text/TextStyle;"  + // bodySmall
        "Landroidx/compose/ui/text/TextStyle;"  + // labelLarge
        "Landroidx/compose/ui/text/TextStyle;"  + // labelMedium
        "Landroidx/compose/ui/text/TextStyle;"  + // labelSmall
        "ILkotlin/jvm/internal/DefaultConstructorMarker;)V";

    static System.IntPtr s_typographyCtor_class;
    static System.IntPtr s_typographyCtor_method;

    /// <summary>
    /// Build a Material 3 <see cref="Typography"/> with per-slot
    /// overrides. Each <see cref="System.IntPtr"/> is either a
    /// <c>TextStyle</c> JNI handle to use for that slot, or
    /// <see cref="System.IntPtr.Zero"/> to fall back to the M3
    /// baseline default for that slot via Kotlin's synthetic
    /// <c>$default</c> mechanism.
    /// </summary>
    /// <param name="defaults">
    /// 15-bit mask: bit N set means "leave slot N at the Kotlin
    /// default" (the corresponding handle is ignored). Bit 0 is
    /// displayLarge, bit 14 is labelSmall.
    /// </param>
    internal static unsafe Typography BuildTypography(
        System.IntPtr displayLarge,
        System.IntPtr displayMedium,
        System.IntPtr displaySmall,
        System.IntPtr headlineLarge,
        System.IntPtr headlineMedium,
        System.IntPtr headlineSmall,
        System.IntPtr titleLarge,
        System.IntPtr titleMedium,
        System.IntPtr titleSmall,
        System.IntPtr bodyLarge,
        System.IntPtr bodyMedium,
        System.IntPtr bodySmall,
        System.IntPtr labelLarge,
        System.IntPtr labelMedium,
        System.IntPtr labelSmall,
        int defaults)
    {
        if (s_typographyCtor_method == System.IntPtr.Zero)
        {
            s_typographyCtor_class = JNIEnv.FindClass("androidx/compose/material3/Typography");
            s_typographyCtor_method = JNIEnv.GetMethodID(s_typographyCtor_class, "<init>", TypographyDefaultCtorSig);
        }

        JValue* args = stackalloc JValue[17];
        args[0]  = new JValue(displayLarge);
        args[1]  = new JValue(displayMedium);
        args[2]  = new JValue(displaySmall);
        args[3]  = new JValue(headlineLarge);
        args[4]  = new JValue(headlineMedium);
        args[5]  = new JValue(headlineSmall);
        args[6]  = new JValue(titleLarge);
        args[7]  = new JValue(titleMedium);
        args[8]  = new JValue(titleSmall);
        args[9]  = new JValue(bodyLarge);
        args[10] = new JValue(bodyMedium);
        args[11] = new JValue(bodySmall);
        args[12] = new JValue(labelLarge);
        args[13] = new JValue(labelMedium);
        args[14] = new JValue(labelSmall);
        args[15] = new JValue(defaults);
        args[16] = new JValue(System.IntPtr.Zero);

        System.IntPtr handle = JNIEnv.NewObject(s_typographyCtor_class, s_typographyCtor_method, args);
        return Java.Lang.Object.GetObject<Typography>(handle, JniHandleOwnership.TransferLocalRef)!;
    }
}
