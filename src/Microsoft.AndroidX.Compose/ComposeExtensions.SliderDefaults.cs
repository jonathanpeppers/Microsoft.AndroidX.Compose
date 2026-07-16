using System.Runtime.CompilerServices;
using AndroidX.Compose.Material3;
using AndroidX.Compose.Runtime;

namespace AndroidX.Compose;

/// <summary>
/// Static factories mirroring Kotlin's
/// <c>androidx.compose.material3.SliderDefaults</c> singleton — builds
/// a <see cref="SliderColors"/> with one or more slot overrides. Surfaced
/// as an <see cref="IComposer"/> extension so call sites read
/// <c>composer.SliderColors(thumbColor: ...)</c>.
///
/// Useful for overriding a subset of the ten color slots (<c>thumbColor</c>,
/// <c>activeTrackColor</c>, <c>activeTickColor</c>, <c>inactiveTrackColor</c>,
/// <c>inactiveTickColor</c>, plus the four disabled siblings) without
/// hand-rolling a full <see cref="SliderColors"/>. Any slot left
/// <c>null</c> falls back to the value
/// <c>SliderDefaults.colors()</c> would return at the current
/// <c>MaterialTheme</c>.
/// </summary>
public static partial class ComposeExtensions
{
    /// <summary>
    /// Mirrors Kotlin's
    /// <c>SliderDefaults.colors(thumbColor = …, activeTrackColor = …, …)</c>.
    ///
    /// Each non-<c>null</c> argument clears the corresponding Kotlin
    /// <c>$default</c> bit and is packed only when calling the generated
    /// binding. <c>null</c> leaves the bit set so the theme default wins.
    /// </summary>
    public static SliderColors SliderColors(
        this IComposer composer,
        Color? thumbColor = null,
        Color? activeTrackColor = null,
        Color? activeTickColor = null,
        Color? inactiveTrackColor = null,
        Color? inactiveTickColor = null,
        Color? disabledThumbColor = null,
        Color? disabledActiveTrackColor = null,
        Color? disabledActiveTickColor = null,
        Color? disabledInactiveTrackColor = null,
        Color? disabledInactiveTickColor = null,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = "")
    {
        ArgumentNullException.ThrowIfNull(composer);

        // Default mask: every bit set = "use Kotlin's default for that
        // slot". Clear a bit when the caller passes a value. The enum
        // is generated from the declarative `[assembly: ComposeDefaults]`
        // entry; member ordering matches the slot order on the bound
        // `SliderDefaults.colors(...)` overload.
        var defaults = SliderColorsDefault.All;
        if (thumbColor                  is not null) defaults &= ~SliderColorsDefault.ThumbColor;
        if (activeTrackColor            is not null) defaults &= ~SliderColorsDefault.ActiveTrackColor;
        if (activeTickColor             is not null) defaults &= ~SliderColorsDefault.ActiveTickColor;
        if (inactiveTrackColor          is not null) defaults &= ~SliderColorsDefault.InactiveTrackColor;
        if (inactiveTickColor           is not null) defaults &= ~SliderColorsDefault.InactiveTickColor;
        if (disabledThumbColor          is not null) defaults &= ~SliderColorsDefault.DisabledThumbColor;
        if (disabledActiveTrackColor    is not null) defaults &= ~SliderColorsDefault.DisabledActiveTrackColor;
        if (disabledActiveTickColor     is not null) defaults &= ~SliderColorsDefault.DisabledActiveTickColor;
        if (disabledInactiveTrackColor  is not null) defaults &= ~SliderColorsDefault.DisabledInactiveTrackColor;
        if (disabledInactiveTickColor   is not null) defaults &= ~SliderColorsDefault.DisabledInactiveTickColor;

        composer.StartReplaceableGroup(SourceLocationKey.Compute(line, file));
        try
        {
            // Kotlin signature: colors-q0g_0yA(JJJJJJJJJJ;Composer;III) →
            // ten longs, $changed, $changed1, $default. Last trailing
            // int (`_changed1`) is $default per the SliderKt.Slider /
            // ProgressIndicatorKt convention. (Even if the mask is
            // wrong, Color.Unspecified packs to 0L and Compose's color
            // resolver falls back to theme — Kotlin's belt-and-braces
            // default semantics — so unset slots still pick up the
            // current MaterialTheme color.)
            return AndroidX.Compose.Material3.SliderDefaults.Instance.Colors(
                thumbColor?.ToPacked()                 ?? 0L,
                activeTrackColor?.ToPacked()           ?? 0L,
                activeTickColor?.ToPacked()            ?? 0L,
                inactiveTrackColor?.ToPacked()         ?? 0L,
                inactiveTickColor?.ToPacked()          ?? 0L,
                disabledThumbColor?.ToPacked()         ?? 0L,
                disabledActiveTrackColor?.ToPacked()   ?? 0L,
                disabledActiveTickColor?.ToPacked()    ?? 0L,
                disabledInactiveTrackColor?.ToPacked() ?? 0L,
                disabledInactiveTickColor?.ToPacked()  ?? 0L,
                composer,
                p11:       0,
                _changed:  0,
                _changed1: (int)defaults);
        }
        finally
        {
            composer.EndReplaceableGroup();
        }
    }
}
