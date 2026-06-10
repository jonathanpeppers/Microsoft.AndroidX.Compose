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
/// Mirrors <see cref="TextStyleCompanion"/>; intentionally exposed
/// <c>public</c> (vs. <see cref="TextStyleCompanion"/>'s
/// <c>internal</c>) so callers building their own
/// <see cref="AndroidX.Compose.Foundation.Text.KeyboardOptions"/>
/// (e.g. MAUI <c>EntryHandler</c>, gallery numeric-keyboard demo) can
/// reach <see cref="Default"/> without re-implementing the JNI dance
/// or pulling in an <c>InternalsVisibleTo</c> hook.
/// </summary>
public static class KeyboardOptionsCompanion
{
    static IntPtr s_companion_ref;
    static AndroidX.Compose.Foundation.Text.KeyboardOptions.Companion? s_companion;
    static AndroidX.Compose.Foundation.Text.KeyboardOptions? s_default;

    /// <summary>
    /// Resolve and cache the <c>KeyboardOptions.Companion</c> singleton.
    /// First call performs the JNI field lookup; subsequent calls
    /// return the cached wrapper.
    /// </summary>
    public static AndroidX.Compose.Foundation.Text.KeyboardOptions.Companion Get()
    {
        if (s_companion is not null) return s_companion;
        if (s_companion_ref == IntPtr.Zero)
        {
            IntPtr cls   = JNIEnv.FindClass("androidx/compose/foundation/text/KeyboardOptions");
            IntPtr fid   = JNIEnv.GetStaticFieldID(cls, "Companion", "Landroidx/compose/foundation/text/KeyboardOptions$Companion;");
            IntPtr local = JNIEnv.GetStaticObjectField(cls, fid);
            s_companion_ref = JNIEnv.NewGlobalRef(local);
            JNIEnv.DeleteLocalRef(local);
        }
        s_companion = Java.Lang.Object.GetObject<AndroidX.Compose.Foundation.Text.KeyboardOptions.Companion>(
            s_companion_ref, JniHandleOwnership.DoNotTransfer)!;
        return s_companion;
    }

    /// <summary>
    /// The default <c>KeyboardOptions</c> instance — equivalent to
    /// Kotlin's <c>KeyboardOptions.Default</c>. Use as the starting
    /// point for <c>.Copy(...)</c> overrides.
    /// </summary>
    public static AndroidX.Compose.Foundation.Text.KeyboardOptions Default =>
        s_default ??= Get().Default;
}
