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
    /// The Compose lowering takes a packed <c>Color</c> (a <c>long</c>)
    /// per slot plus a <c>$default</c> bitmask flagging which slots the
    /// caller actually supplied. Each non-<c>null</c> argument here
    /// clears the corresponding bit so Compose adopts your color;
    /// <c>null</c> leaves the bit set so Kotlin's default (the theme's
    /// color-scheme slot) wins.
    /// </summary>
    public static SliderColors SliderColors(
        this IComposer composer,
        long? thumbColor = null,
        long? activeTrackColor = null,
        long? activeTickColor = null,
        long? inactiveTrackColor = null,
        long? inactiveTickColor = null,
        long? disabledThumbColor = null,
        long? disabledActiveTrackColor = null,
        long? disabledActiveTickColor = null,
        long? disabledInactiveTrackColor = null,
        long? disabledInactiveTickColor = null,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = "")
    {
        ArgumentNullException.ThrowIfNull(composer);

        // Default mask: every bit set = "use Kotlin's default for that
        // slot". Clear a bit when the caller passes a value. Mirrors
        // ComposeExtensions.ButtonDefaults.cs: pass mask as the first
        // trailing int (the Kotlin-lowered $default slot for
        // @ReadOnlyComposable factories) and 0L for unset color slots
        // (Color.Unspecified packs to 0L → theme fallback regardless).
        // 10 slots → bits 0..9.
        int defaults = 0b11_1111_1111;
        if (thumbColor                  is not null) defaults &= ~(1 << 0);
        if (activeTrackColor            is not null) defaults &= ~(1 << 1);
        if (activeTickColor             is not null) defaults &= ~(1 << 2);
        if (inactiveTrackColor          is not null) defaults &= ~(1 << 3);
        if (inactiveTickColor           is not null) defaults &= ~(1 << 4);
        if (disabledThumbColor          is not null) defaults &= ~(1 << 5);
        if (disabledActiveTrackColor    is not null) defaults &= ~(1 << 6);
        if (disabledActiveTickColor     is not null) defaults &= ~(1 << 7);
        if (disabledInactiveTrackColor  is not null) defaults &= ~(1 << 8);
        if (disabledInactiveTickColor   is not null) defaults &= ~(1 << 9);

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
                thumbColor                 ?? 0L,
                activeTrackColor           ?? 0L,
                activeTickColor            ?? 0L,
                inactiveTrackColor         ?? 0L,
                inactiveTickColor          ?? 0L,
                disabledThumbColor         ?? 0L,
                disabledActiveTrackColor   ?? 0L,
                disabledActiveTickColor    ?? 0L,
                disabledInactiveTrackColor ?? 0L,
                disabledInactiveTickColor  ?? 0L,
                composer,
                p11:       0,
                _changed:  0,
                _changed1: defaults);
        }
        finally
        {
            composer.EndReplaceableGroup();
        }
    }
}
