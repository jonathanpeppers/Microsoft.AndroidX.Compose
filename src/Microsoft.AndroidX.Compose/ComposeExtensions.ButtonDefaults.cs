using System.Runtime.CompilerServices;
using AndroidX.Compose.Material3;
using AndroidX.Compose.Runtime;

namespace AndroidX.Compose;

/// <summary>
/// Static factories mirroring Kotlin's
/// <c>androidx.compose.material3.ButtonDefaults</c> singleton — each
/// returns a <see cref="ButtonColors"/> configured for a particular
/// override pattern. Surfaced as <see cref="IComposer"/> extensions so
/// call sites read <c>composer.ButtonColors(containerColor: ...)</c>.
///
/// Useful when you want to override a subset of the four color slots
/// (<c>containerColor</c>, <c>contentColor</c>,
/// <c>disabledContainerColor</c>, <c>disabledContentColor</c>) without
/// hand-rolling a full <see cref="ButtonColors"/>. Any slot left
/// <c>null</c> falls back to the value
/// <c>ButtonDefaults.buttonColors()</c> would return at the current
/// <c>MaterialTheme</c> — exactly what Kotlin's
/// <c>buttonColors.copy(...)</c> ergonomic does.
/// </summary>
public static partial class ComposeExtensions
{
    /// <summary>
    /// Mirrors Kotlin's
    /// <c>ButtonDefaults.buttonColors(containerColor = …, contentColor = …,
    /// disabledContainerColor = …, disabledContentColor = …)</c>.
    ///
    /// Each non-<c>null</c> argument clears the corresponding Kotlin
    /// <c>$default</c> bit and is packed only when calling the generated
    /// binding. <c>null</c> leaves the bit set so the theme default wins.
    /// </summary>
    public static ButtonColors ButtonColors(
        this IComposer composer,
        Color? containerColor = null,
        Color? contentColor = null,
        Color? disabledContainerColor = null,
        Color? disabledContentColor = null,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = "")
    {
        ArgumentNullException.ThrowIfNull(composer);

        // Default mask: every bit set = "use Kotlin's default for that
        // slot". Clear a bit when the caller passes a value.
        int defaults = 0b1111;
        if (containerColor        is not null) defaults &= ~0b0001;
        if (contentColor          is not null) defaults &= ~0b0010;
        if (disabledContainerColor is not null) defaults &= ~0b0100;
        if (disabledContentColor   is not null) defaults &= ~0b1000;

        composer.StartReplaceableGroup(SourceLocationKey.Compute(line, file));
        try
        {
            return AndroidX.Compose.Material3.ButtonDefaults.Instance.ButtonColors(
                containerColor?.ToPacked()         ?? 0L,
                contentColor?.ToPacked()           ?? 0L,
                disabledContainerColor?.ToPacked() ?? 0L,
                disabledContentColor?.ToPacked()   ?? 0L,
                composer,
                defaults,
                0);
        }
        finally
        {
            composer.EndReplaceableGroup();
        }
    }
}
