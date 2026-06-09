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
    /// overrides. Each <see cref="AndroidX.Compose.UI.Text.TextStyle"/>
    /// is either a wrapper to use for that slot, or <c>null</c> to fall
    /// back to the M3 baseline default for that slot via Kotlin's
    /// synthetic <c>$default</c> mechanism.
    /// </summary>
    /// <param name="defaults">
    /// 15-bit mask: bit N set means "leave slot N at the Kotlin
    /// default" (the corresponding wrapper is ignored). Bit 0 is
    /// displayLarge, bit 14 is labelSmall.
    /// </param>
    internal static unsafe Typography BuildTypography(
        AndroidX.Compose.UI.Text.TextStyle? displayLarge,
        AndroidX.Compose.UI.Text.TextStyle? displayMedium,
        AndroidX.Compose.UI.Text.TextStyle? displaySmall,
        AndroidX.Compose.UI.Text.TextStyle? headlineLarge,
        AndroidX.Compose.UI.Text.TextStyle? headlineMedium,
        AndroidX.Compose.UI.Text.TextStyle? headlineSmall,
        AndroidX.Compose.UI.Text.TextStyle? titleLarge,
        AndroidX.Compose.UI.Text.TextStyle? titleMedium,
        AndroidX.Compose.UI.Text.TextStyle? titleSmall,
        AndroidX.Compose.UI.Text.TextStyle? bodyLarge,
        AndroidX.Compose.UI.Text.TextStyle? bodyMedium,
        AndroidX.Compose.UI.Text.TextStyle? bodySmall,
        AndroidX.Compose.UI.Text.TextStyle? labelLarge,
        AndroidX.Compose.UI.Text.TextStyle? labelMedium,
        AndroidX.Compose.UI.Text.TextStyle? labelSmall,
        int defaults)
    {
        if (s_typographyCtor_method == System.IntPtr.Zero)
        {
            s_typographyCtor_class = JNIEnv.FindClass("androidx/compose/material3/Typography");
            s_typographyCtor_method = JNIEnv.GetMethodID(s_typographyCtor_class, "<init>", TypographyDefaultCtorSig);
        }

        try
        {
            JValue* args = stackalloc JValue[17];
            args[0]  = new JValue(Handle(displayLarge));
            args[1]  = new JValue(Handle(displayMedium));
            args[2]  = new JValue(Handle(displaySmall));
            args[3]  = new JValue(Handle(headlineLarge));
            args[4]  = new JValue(Handle(headlineMedium));
            args[5]  = new JValue(Handle(headlineSmall));
            args[6]  = new JValue(Handle(titleLarge));
            args[7]  = new JValue(Handle(titleMedium));
            args[8]  = new JValue(Handle(titleSmall));
            args[9]  = new JValue(Handle(bodyLarge));
            args[10] = new JValue(Handle(bodyMedium));
            args[11] = new JValue(Handle(bodySmall));
            args[12] = new JValue(Handle(labelLarge));
            args[13] = new JValue(Handle(labelMedium));
            args[14] = new JValue(Handle(labelSmall));
            args[15] = new JValue(defaults);
            args[16] = new JValue(System.IntPtr.Zero);

            System.IntPtr handle = JNIEnv.NewObject(s_typographyCtor_class, s_typographyCtor_method, args);
            return Java.Lang.Object.GetObject<Typography>(handle, JniHandleOwnership.TransferLocalRef)!;
        }
        finally
        {
            // Keep every wrapper alive across the JNI call so its
            // backing global ref isn't released between the Handle()
            // reads above and JNIEnv.NewObject. The bridge owns this
            // responsibility — facade callers shouldn't have to.
            System.GC.KeepAlive(displayLarge);
            System.GC.KeepAlive(displayMedium);
            System.GC.KeepAlive(displaySmall);
            System.GC.KeepAlive(headlineLarge);
            System.GC.KeepAlive(headlineMedium);
            System.GC.KeepAlive(headlineSmall);
            System.GC.KeepAlive(titleLarge);
            System.GC.KeepAlive(titleMedium);
            System.GC.KeepAlive(titleSmall);
            System.GC.KeepAlive(bodyLarge);
            System.GC.KeepAlive(bodyMedium);
            System.GC.KeepAlive(bodySmall);
            System.GC.KeepAlive(labelLarge);
            System.GC.KeepAlive(labelMedium);
            System.GC.KeepAlive(labelSmall);
        }

        static System.IntPtr Handle(AndroidX.Compose.UI.Text.TextStyle? ts) =>
            ts is null ? System.IntPtr.Zero : ((Java.Lang.Object)ts).Handle;
    }
}
