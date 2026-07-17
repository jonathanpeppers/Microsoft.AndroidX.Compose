using Android.Runtime;

namespace AndroidX.Compose;

/// <summary>
/// Cached accessor for <c>androidx.compose.foundation.text.KeyboardOptions.Companion</c> —
/// the singleton that exposes <c>Default</c> (the immutable
/// <c>KeyboardOptions</c> instance shared by every Compose text field
/// when callers haven't overridden the IME slot).
///
/// The Mono binding for <c>KeyboardOptions</c> generates the
/// <c>Companion</c> nested class but does NOT surface a static field
/// accessor on the outer type (Kotlin <c>object</c> companions usually
/// land as a <c>Companion</c> getter — the binder skips it here). The
/// outer class also has no public ctor (the Kotlin one takes
/// <c>@JvmInline value class</c> params and is stripped) and no
/// <c>KeyboardOptionsKt</c> static helper — JNI lookup of the
/// <c>Companion</c> field is the only path to bootstrap an instance.
/// After that, everything flows through bound APIs
/// (<c>Companion.Default</c>, <c>Default.Copy(int keyboardType, …)</c>,
/// etc.).
///
/// Mirrors <see cref="TextStyleCompanion"/>. Callers building their own
/// <see cref="AndroidX.Compose.Foundation.Text.KeyboardOptions"/>
/// (e.g. MAUI <c>EntryHandler</c>, gallery numeric-keyboard demo) can
/// reach <see cref="Default"/> without re-implementing the JNI dance
/// while the JNI bootstrap accessor for the bound <c>Companion</c>
/// singleton remains internal.
/// </summary>
public static class KeyboardOptionsCompanion
{
    static AndroidX.Compose.Foundation.Text.KeyboardOptions.Companion? s_companion;
    static AndroidX.Compose.Foundation.Text.KeyboardOptions? s_default;

    // Why raw JNI: Kotlin exposes Companion as a static field lookup,
    // while ComposeBridge models method and constructor invocation.
    internal static AndroidX.Compose.Foundation.Text.KeyboardOptions.Companion Get()
    {
        if (s_companion is not null) return s_companion;
        IntPtr local = IntPtr.Zero;
        try
        {
            IntPtr cls = JNIEnv.FindClass("androidx/compose/foundation/text/KeyboardOptions");
            IntPtr fid = JNIEnv.GetStaticFieldID(cls, "Companion",
                "Landroidx/compose/foundation/text/KeyboardOptions$Companion;");
            local = JNIEnv.GetStaticObjectField(cls, fid);
            return s_companion = Java.Lang.Object.GetObject<AndroidX.Compose.Foundation.Text.KeyboardOptions.Companion>(
                local, JniHandleOwnership.TransferLocalRef)
                ?? throw new InvalidOperationException(
                    "KeyboardOptions.Companion could not be resolved.");
        }
        finally
        {
            // GetObject(.., TransferLocalRef) consumes `local` on success;
            // the explicit DeleteLocalRef only runs if the wrapper threw
            // before taking ownership.
            if (local != IntPtr.Zero && s_companion is null)
                JNIEnv.DeleteLocalRef(local);
        }
    }

    /// <summary>
    /// The default <c>KeyboardOptions</c> instance — equivalent to
    /// Kotlin's <c>KeyboardOptions.Default</c>. Use as the starting
    /// point for <c>.Copy(...)</c> overrides.
    /// </summary>
    public static AndroidX.Compose.Foundation.Text.KeyboardOptions Default =>
        s_default ??= Get().Default;
}
