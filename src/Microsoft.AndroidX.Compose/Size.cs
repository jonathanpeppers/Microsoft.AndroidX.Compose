namespace AndroidX.Compose;

/// <summary>
/// Two-dimensional pixel size used by drawing APIs. Mirrors Compose's packed
/// <c>androidx.compose.ui.geometry.Size</c> value class.
/// </summary>
public readonly struct Size : IEquatable<Size>
{
    readonly long _packed;

    /// <summary>Creates a size from width and height in pixels.</summary>
    public Size(float width, float height)
    {
        _packed = ((long)BitConverter.SingleToInt32Bits(width) << 32)
            | (uint)BitConverter.SingleToInt32Bits(height);
    }

    Size(long packed) => _packed = packed;

    internal long Packed => _packed;

    internal static Size FromPacked(long packed) => new(packed);

    /// <summary>Width in pixels.</summary>
    public float Width => BitConverter.Int32BitsToSingle((int)((ulong)_packed >> 32));

    /// <summary>Height in pixels.</summary>
    public float Height => BitConverter.Int32BitsToSingle((int)_packed);

    /// <summary>A zero-width, zero-height size.</summary>
    public static Size Zero => new(0f, 0f);

    /// <inheritdoc/>
    public bool Equals(Size other) => _packed == other._packed;

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is Size other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => _packed.GetHashCode();

    /// <summary>Compares two sizes by their packed component values.</summary>
    public static bool operator ==(Size left, Size right) => left.Equals(right);

    /// <summary>Compares two sizes by their packed component values.</summary>
    public static bool operator !=(Size left, Size right) => !left.Equals(right);

    /// <inheritdoc/>
    public override string ToString() => $"Size({Width}, {Height})";
}
