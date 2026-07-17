using AndroidX.Compose.Runtime;
using Android.Runtime;

namespace AndroidX.Compose;

/// <summary>
/// The <see cref="AndroidX.Lifecycle.ILifecycleOwner"/> that hosts the
/// current composition (typically the host activity or fragment). Use
/// this to scope coroutines / effects to the same lifecycle as the UI
/// tree. Equivalent to Kotlin's
/// <c>androidx.lifecycle.compose.LocalLifecycleOwner</c>.
/// </summary>
/// <remarks>
/// The lifecycle-runtime-compose binding exposes the JVM
/// <c>LocalLifecycleOwnerKt</c> class but strips its static getter, so the
/// composition-local peer is resolved through a generated JNI bridge.
/// </remarks>
public static class LocalLifecycleOwner
{
    static readonly CompositionLocal<AndroidX.Lifecycle.ILifecycleOwner> s_instance =
        new(Java.Lang.Object.GetObject<ProvidableCompositionLocal>(
            ComposeBridges.LocalLifecycleOwner(),
            JniHandleOwnership.TransferLocalRef)
            ?? throw new InvalidOperationException(
                "androidx.lifecycle.compose.LocalLifecycleOwner was unavailable."));

    /// <summary>
    /// Read the current value, equivalent to Kotlin's
    /// <c>LocalLifecycleOwner.current</c>.
    /// </summary>
    public static AndroidX.Lifecycle.ILifecycleOwner Current(IComposer composer) =>
        s_instance.Current(composer);

    /// <summary>Read the current value from the implicit composition.</summary>
    public static AndroidX.Lifecycle.ILifecycleOwner Current() => s_instance.Current();

    /// <summary>
    /// Pair this local with <paramref name="value"/> for installation
    /// by a <see cref="CompositionLocalProvider"/>.
    /// </summary>
    public static ProvidedValue Provides(AndroidX.Lifecycle.ILifecycleOwner value) =>
        s_instance.Provides(value);
}
