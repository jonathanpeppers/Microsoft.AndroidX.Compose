namespace ComposeNet;

/// <summary>
/// C# mirror of Kotlin's <c>androidx.compose.ui.unit.TextUnit</c> in
/// its <c>Sp</c> (scaled pixels) variant. Compose's <c>TextUnit</c> is
/// a <c>@JvmInline value class</c> over a <c>Long</c>; the upper 32
/// bits encode a unit-type discriminator (<c>0x1</c> for Sp,
/// <c>0x2</c> for Em), the lower 32 bits encode the float value's raw
/// bits. The bridge generator recognizes <see cref="Sp"/>? and lowers
/// it to the underlying <c>long</c> JNI slot.
///
/// Construct via the static factory or the extension methods on
/// <see cref="SpExtensions"/>: <c>16.Sp()</c>, <c>14.5f.Sp()</c>.
/// </summary>
public readonly record struct Sp(float Value)
{
    /// <summary>Kotlin's <c>TextUnit.Unspecified</c> sentinel (NaN-encoded type 0xF).</summary>
    public static Sp Unspecified => new(float.NaN);

    /// <summary>Construct from an integer point size.</summary>
    public static Sp From(int sp) => new(sp);

    /// <summary>Construct from a floating-point point size.</summary>
    public static Sp From(float sp) => new(sp);

    /// <summary>
    /// Pack a nullable <see cref="Sp"/> into the packed <c>TextUnit</c>
    /// long. <c>null</c> → <c>0L</c>; the auto-mask leaves the
    /// <c>$default</c> bit set so Kotlin's real default applies.
    /// </summary>
    public static long Pack(Sp? value)
    {
        if (value is null) return 0L;
        var raw = System.BitConverter.SingleToUInt32Bits(value.Value.Value);
        return (1L << 32) | (long)raw;
    }
}

/// <summary>
/// Convenience constructors for the <see cref="ComposeNet.Sp"/>
/// struct: <c>16.Sp()</c>, <c>14.5f.Sp()</c>.
/// </summary>
public static class SpExtensions
{
    /// <summary>Wrap an integer point size in an <c>Sp</c>.</summary>
    public static Sp Sp(this int value) => new(value);

    /// <summary>Wrap a float point size in an <c>Sp</c>.</summary>
    public static Sp Sp(this float value) => new(value);
}
