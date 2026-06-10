using MauiColor    = Microsoft.Maui.Graphics.Color;
using ComposeColor = AndroidX.Compose.Color;

namespace Microsoft.AndroidX.Compose.Maui;

/// <summary>
/// Conversion helpers between MAUI's normalised <c>0..1</c>
/// <see cref="MauiColor"/> and Compose's packed
/// <see cref="ComposeColor"/> (an <c>@JvmInline value class</c> over
/// a <c>ulong</c>, surfaced in C# via the implicit <c>long</c>
/// conversion accepted by the generated bindings).
/// </summary>
/// <remarks>
/// Shared across every handler that needs to push a colour into a
/// Compose slot (text colour, background tint, border, shadow, etc.).
/// Centralising here avoids duplicating the float→byte conversion in
/// every <c>MapXxxColor</c> mapper and keeps the rounding policy in
/// one place.
/// </remarks>
internal static class ColorMapping
{
    /// <summary>
    /// Build a Compose <see cref="ComposeColor"/> from a MAUI
    /// <see cref="MauiColor"/>, clamping channels to <c>0..1</c> and
    /// rounding to nearest 8-bit value.
    /// </summary>
    public static ComposeColor ToCompose(MauiColor c) =>
        new(ToByte(c.Alpha), ToByte(c.Red), ToByte(c.Green), ToByte(c.Blue));

    /// <summary>
    /// Convert a MAUI <see cref="MauiColor"/> to the packed
    /// <see cref="long"/> Compose's generated bindings expect for
    /// <c>Color</c>-typed slots, returning <see langword="null"/> for
    /// a null input so a mapper can clear the slot.
    /// </summary>
    public static long? ToPackedLong(MauiColor? c) =>
        c is null ? null : (long)ToCompose(c);

    /// <summary>
    /// Convert a normalised <c>0..1</c> channel to an 8-bit value with
    /// round-to-nearest semantics. <c>(byte)(x * 255f)</c> truncates and
    /// drops one possible level (e.g. <c>0.5f</c> becomes <c>127</c>
    /// instead of <c>128</c>); adding <c>0.5f</c> before the cast
    /// restores symmetric rounding.
    /// </summary>
    static byte ToByte(float channel) =>
        (byte)(Math.Clamp(channel, 0f, 1f) * 255f + 0.5f);
}
