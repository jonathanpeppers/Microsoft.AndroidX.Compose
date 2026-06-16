using AndroidX.Compose;
using MauiTextAlignment = Microsoft.Maui.TextAlignment;

namespace Microsoft.AndroidX.Compose.Maui.Platform;

/// <summary>
/// Maps <see cref="Microsoft.Maui.ITextAlignment.VerticalTextAlignment"/>
/// (a <see cref="MauiTextAlignment"/>) onto Compose's
/// <see cref="Alignment.Vertical"/> and the matching
/// <see cref="ModifierExtensions.WrapContentHeight(Modifier, Alignment.Vertical, bool)"/>
/// modifier. Used by every Compose-backed text-input handler so the
/// cross-platform property lines up consistently regardless of which
/// underlying composable the handler renders.
/// </summary>
/// <remarks>
/// MAUI exposes four <see cref="MauiTextAlignment"/> values
/// (<c>Start</c> / <c>Center</c> / <c>End</c> / <c>Justify</c>) but
/// Compose has no vertical equivalent of <c>Justify</c>; we collapse it
/// to <see cref="Alignment.Vertical.Top"/>, matching how the MAUI
/// Android backend treats vertical justify.
/// </remarks>
internal static class VerticalAlignmentMapping
{
    /// <summary>Translates a MAUI <see cref="MauiTextAlignment"/> into a Compose
    /// <see cref="Alignment.Vertical"/>.</summary>
    public static Alignment.Vertical ToVertical(MauiTextAlignment alignment) => alignment switch
    {
        MauiTextAlignment.Center => Alignment.Vertical.CenterVertically,
        MauiTextAlignment.End    => Alignment.Vertical.Bottom,
        _                        => Alignment.Vertical.Top,
    };

    /// <summary>Wraps <paramref name="modifier"/> in a
    /// <c>wrapContentHeight(align)</c> with the converted alignment so
    /// the wrapped composable top/center/bottom-aligns inside the
    /// parent's allocated height.</summary>
    public static Modifier ApplyVerticalTextAlignment(this Modifier modifier, MauiTextAlignment alignment) =>
        modifier.WrapContentHeight(ToVertical(alignment));
}
