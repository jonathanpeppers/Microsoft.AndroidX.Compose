namespace AndroidX.Compose;

/// <summary>
/// Elliptical corner radius used by rounded drawing primitives. Mirrors
/// Compose's packed <c>androidx.compose.ui.geometry.CornerRadius</c>.
/// </summary>
public readonly struct CornerRadius : IEquatable<CornerRadius>
{
    readonly long _packed;

    /// <summary>Creates an elliptical radius in pixels.</summary>
    public CornerRadius(float x, float y)
    {
        _packed = ((long)BitConverter.SingleToInt32Bits(x) << 32)
            | (uint)BitConverter.SingleToInt32Bits(y);
    }

    /// <summary>Creates a circular radius in pixels.</summary>
    public CornerRadius(float radius) : this(radius, radius) { }

    internal long Packed => _packed;

    /// <summary>Horizontal radius in pixels.</summary>
    public float X => BitConverter.Int32BitsToSingle((int)((ulong)_packed >> 32));

    /// <summary>Vertical radius in pixels.</summary>
    public float Y => BitConverter.Int32BitsToSingle((int)_packed);

    /// <summary>A square corner.</summary>
    public static CornerRadius Zero => new(0f);

    /// <inheritdoc/>
    public bool Equals(CornerRadius other) => _packed == other._packed;

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is CornerRadius other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => _packed.GetHashCode();

    /// <summary>Compares two corner radii by their packed component values.</summary>
    public static bool operator ==(CornerRadius left, CornerRadius right) => left.Equals(right);

    /// <summary>Compares two corner radii by their packed component values.</summary>
    public static bool operator !=(CornerRadius left, CornerRadius right) => !left.Equals(right);

    /// <inheritdoc/>
    public override string ToString() => $"CornerRadius({X}, {Y})";
}
