using Android.Runtime;
using AndroidX.Compose.Runtime;
using AndroidX.Compose.UI.Focus;
using AndroidX.Compose.UI.Platform;

namespace AndroidX.Compose;

/// <summary>
/// The focus manager for the current Compose owner, equivalent to Kotlin's
/// <c>LocalFocusManager.current</c>. Capture it during composition, then use
/// it from UI event handlers; do not perform a local lookup in those handlers.
/// </summary>
public static class LocalFocusManager
{
    /// <summary>Reads the focus manager at the supplied composition position.</summary>
    public static IFocusManager Current(IComposer composer)
    {
        ArgumentNullException.ThrowIfNull(composer);
        var value = CompositionLocalsKt.LocalFocusManager.GetCurrent(composer, 0)
            ?? throw new InvalidOperationException("LocalFocusManager was unavailable in this composition.");
        return value.JavaCast<IFocusManager>();
    }

    /// <summary>
    /// Reads the focus manager from the active implicit composition. Throws
    /// <see cref="InvalidOperationException"/> outside composable content.
    /// </summary>
    public static IFocusManager Current() => Current(ComposableContext.Current);

    /// <summary>Overrides the focus manager for a <see cref="CompositionLocalProvider"/>.</summary>
    public static ProvidedValue Provides(IFocusManager value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return new ProvidedValue(CompositionLocalsKt.LocalFocusManager.Provides((Java.Lang.Object)value));
    }
}
