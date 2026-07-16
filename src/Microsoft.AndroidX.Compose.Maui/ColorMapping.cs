using MauiColor    = Microsoft.Maui.Graphics.Color;
using ComposeColor = AndroidX.Compose.Color;

namespace Microsoft.AndroidX.Compose.Maui;

/// <summary>
/// Conversion helpers between MAUI's normalised <c>0..1</c>
/// <see cref="MauiColor"/> and <see cref="ComposeColor"/>.
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
        c is null ? null : ToCompose(c).ToPacked();

    /// <summary>
    /// Convert a MAUI <see cref="MauiColor"/> to a packed 32-bit
    /// <c>0xAARRGGBB</c> integer suitable for
    /// <see cref="Android.Graphics.Paint.Color"/> /
    /// <c>Android.Graphics.Color</c>. Alpha defaults to opaque when
    /// the input is <see langword="null"/>.
    /// </summary>
    public static int ToArgb(MauiColor? c) =>
        c is null
            ? unchecked((int)0xFF000000)
            : (ToByte(c.Alpha) << 24) |
              (ToByte(c.Red)   << 16) |
              (ToByte(c.Green) <<  8) |
               ToByte(c.Blue);

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
