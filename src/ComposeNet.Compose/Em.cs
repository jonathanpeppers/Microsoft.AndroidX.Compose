namespace ComposeNet;

/// <summary>
/// C# mirror of Kotlin's <c>androidx.compose.ui.unit.TextUnit</c> in
/// its <c>Em</c> (relative-to-font-size) variant. See <see cref="Sp"/>
/// for the encoding — <see cref="Em"/> uses the <c>0x2</c> type
/// discriminator in the upper 32 bits.
/// </summary>
public readonly record struct Em(float Value)
{
    /// <summary>Construct from a floating-point em multiplier.</summary>
    public static Em From(float em) => new(em);

    /// <summary>
    /// Pack a nullable <see cref="Em"/> into the packed <c>TextUnit</c>
    /// long. <c>null</c> → <c>0L</c>; the auto-mask leaves the
    /// <c>$default</c> bit set so Kotlin's real default applies.
    /// </summary>
    public static long Pack(Em? value)
    {
        if (value is null) return 0L;
        var raw = System.BitConverter.SingleToUInt32Bits(value.Value.Value);
        return (2L << 32) | (long)raw;
    }
}
