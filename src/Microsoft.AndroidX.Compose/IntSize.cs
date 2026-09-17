namespace AndroidX.Compose;

/// <summary>Two-dimensional integer pixel size used by bitmap drawing APIs.</summary>
public readonly struct IntSize : IEquatable<IntSize>
{
    readonly long _packed;

    /// <summary>Creates an integer pixel size.</summary>
    public IntSize(int width, int height) =>
        _packed = ((long)width << 32) | (uint)height;

    /// <summary>Width in pixels.</summary>
    public int Width => (int)(_packed >> 32);

    /// <summary>Height in pixels.</summary>
    public int Height => (int)_packed;

    /// <summary>A zero-width, zero-height size.</summary>
    public static IntSize Zero => new(0, 0);

    internal long Packed => _packed;

    /// <inheritdoc/>
    public bool Equals(IntSize other) => _packed == other._packed;

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is IntSize other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => _packed.GetHashCode();

    /// <summary>Compares two sizes.</summary>
    public static bool operator ==(IntSize left, IntSize right) => left.Equals(right);

    /// <summary>Compares two sizes.</summary>
    public static bool operator !=(IntSize left, IntSize right) => !left.Equals(right);
}
