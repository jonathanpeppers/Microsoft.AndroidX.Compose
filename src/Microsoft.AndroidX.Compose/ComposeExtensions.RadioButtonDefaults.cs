using System.Runtime.CompilerServices;
using AndroidX.Compose.Material3;
using AndroidX.Compose.Runtime;

namespace AndroidX.Compose;

/// <summary>
/// Static factories mirroring Kotlin's
/// <c>androidx.compose.material3.RadioButtonDefaults</c> singleton —
/// returns a <see cref="RadioButtonColors"/> configured for a particular
/// override pattern. Surfaced as <see cref="IComposer"/> extensions so
/// call sites read <c>composer.RadioButtonColors(selectedColor: ...)</c>.
/// </summary>
public static partial class ComposeExtensions
{
    /// <summary>
    /// Mirrors Kotlin's
    /// <c>RadioButtonDefaults.colors(selectedColor = …, unselectedColor = …,
    /// disabledSelectedColor = …, disabledUnselectedColor = …)</c>.
    ///
    /// <para>The Kotlin <c>colors(...)</c> factory takes a
    /// <c>$default</c> bitmask, but the C# binder strips it because
    /// every parameter is the <c>@JvmInline value class Color</c>. We
    /// therefore call the parameterless <c>colors(composer, _changed)</c>
    /// to obtain the theme defaults and build the result via the bound
    /// <c>RadioButtonColors.copy(...)</c> overload — the only
    /// four-<c>long</c> entry point that survives the binder strip.</para>
    ///
    /// <para>Each non-<c>null</c> argument substitutes for the
    /// corresponding default; <c>null</c> falls back to the value from
    /// the current <c>MaterialTheme</c>.</para>
    /// </summary>
    public static RadioButtonColors RadioButtonColors(
        this IComposer composer,
        long? selectedColor = null,
        long? unselectedColor = null,
        long? disabledSelectedColor = null,
        long? disabledUnselectedColor = null,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = "")
    {
        ArgumentNullException.ThrowIfNull(composer);

        composer.StartReplaceableGroup(SourceLocationKey.Compute(line, file));
        try
        {
            var d = AndroidX.Compose.Material3.RadioButtonDefaults.Instance.Colors(composer, 0);
            if (selectedColor is null && unselectedColor is null
                && disabledSelectedColor is null && disabledUnselectedColor is null)
                return d;

            return d.Copy(
                selectedColor:           selectedColor           ?? d.SelectedColor,
                unselectedColor:         unselectedColor         ?? d.UnselectedColor,
                disabledSelectedColor:   disabledSelectedColor   ?? d.DisabledSelectedColor,
                disabledUnselectedColor: disabledUnselectedColor ?? d.DisabledUnselectedColor);
        }
        finally
        {
            composer.EndReplaceableGroup();
        }
    }
}
