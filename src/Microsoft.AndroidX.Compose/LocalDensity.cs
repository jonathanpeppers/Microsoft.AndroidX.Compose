using Android.Runtime;
using AndroidX.Compose.Runtime;
using AndroidX.Compose.UI.Platform;

namespace AndroidX.Compose;

/// <summary>
/// The <see cref="UI.Unit.IDensity"/> that converts <see cref="Dp"/> and
/// <see cref="Sp"/> values to pixels at the current composition position.
/// Equivalent to Kotlin's <c>androidx.compose.ui.platform.LocalDensity</c>.
/// </summary>
/// <remarks>
/// <para>Provide a different density or font scale for a subtree with a
/// <see cref="CompositionLocalProvider"/>. For example, an in-app text-size
/// setting keeps the inherited density and scales the inherited font scale:</para>
/// <code>
/// var density = LocalDensity.Current(composer);
/// new CompositionLocalProvider {
///     LocalDensity.Provides(density.Density, density.FontScale * 1.25f),
///     new Text("Measured with the app text size"),
/// }
/// </code>
/// <para>Layouts, text, and components inside the provider measure with the
/// provided values. The provider does not change the Android
/// <see cref="Android.Content.Res.Configuration"/> or views outside this
/// composition.</para>
/// </remarks>
public static class LocalDensity
{
    /// <summary>
    /// Reads the density at the supplied composition position, equivalent
    /// to Kotlin's <c>LocalDensity.current</c>.
    /// </summary>
    public static UI.Unit.IDensity Current(IComposer composer)
    {
        ArgumentNullException.ThrowIfNull(composer);
        var value = CompositionLocalsKt.LocalDensity.GetCurrent(composer, 0)
            ?? throw new InvalidOperationException("LocalDensity was unavailable in this composition.");
        return value.JavaCast<UI.Unit.IDensity>();
    }

    /// <summary>
    /// Reads the density from the active implicit composition. Throws
    /// <see cref="InvalidOperationException"/> outside composable content.
    /// </summary>
    public static UI.Unit.IDensity Current() => Current(ComposableContext.Current);

    /// <summary>
    /// Pair this local with <paramref name="value"/> for installation by a
    /// <see cref="CompositionLocalProvider"/>.
    /// </summary>
    public static ProvidedValue Provides(UI.Unit.IDensity value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return new ProvidedValue(CompositionLocalsKt.LocalDensity.Provides((Java.Lang.Object)value));
    }

    /// <summary>
    /// Pair this local with a density created from
    /// <paramref name="density"/> and <paramref name="fontScale"/>, equivalent
    /// to Kotlin's <c>LocalDensity provides Density(density, fontScale)</c>.
    /// </summary>
    /// <param name="density">Pixels per <see cref="Dp"/>.</param>
    /// <param name="fontScale">
    /// Scale applied to <see cref="Sp"/> values. Pass
    /// <c>Current(composer).FontScale</c> to keep the inherited scale.
    /// </param>
    public static ProvidedValue Provides(float density, float fontScale)
    {
        using var value = UI.Unit.DensityKt.Density(density, fontScale);
        return Provides(value);
    }
}
