namespace AndroidX.Compose;

/// <summary>Two-dimensional integer pixel offset used by bitmap drawing APIs.</summary>
public readonly struct IntOffset : IEquatable<IntOffset>
{
    readonly long _packed;

    /// <summary>Creates an integer pixel offset.</summary>
    public IntOffset(int x, int y) =>
        _packed = ((long)x << 32) | (uint)y;

    /// <summary>Horizontal coordinate.</summary>
    public int X => (int)(_packed >> 32);

    /// <summary>Vertical coordinate.</summary>
    public int Y => (int)_packed;

    /// <summary>The origin.</summary>
    public static IntOffset Zero => new(0, 0);

    internal long Packed => _packed;

    /// <inheritdoc/>
    public bool Equals(IntOffset other) => _packed == other._packed;

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is IntOffset other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => _packed.GetHashCode();

    /// <summary>Compares two offsets.</summary>
    public static bool operator ==(IntOffset left, IntOffset right) => left.Equals(right);

    /// <summary>Compares two offsets.</summary>
    public static bool operator !=(IntOffset left, IntOffset right) => !left.Equals(right);
}
