namespace AndroidX.Compose;

/// <summary>Axis-aligned pixel rectangle used by path geometry operations.</summary>
public readonly struct Rect : IEquatable<Rect>
{
    /// <summary>Creates a rectangle from its four edges.</summary>
    public Rect(float left, float top, float right, float bottom)
    {
        Left = left;
        Top = top;
        Right = right;
        Bottom = bottom;
    }

    /// <summary>Left edge in pixels.</summary>
    public float Left { get; }

    /// <summary>Top edge in pixels.</summary>
    public float Top { get; }

    /// <summary>Right edge in pixels.</summary>
    public float Right { get; }

    /// <summary>Bottom edge in pixels.</summary>
    public float Bottom { get; }

    /// <summary>Rectangle width in pixels.</summary>
    public float Width => Right - Left;

    /// <summary>Rectangle height in pixels.</summary>
    public float Height => Bottom - Top;

    /// <inheritdoc/>
    public bool Equals(Rect other) =>
        Left.Equals(other.Left) &&
        Top.Equals(other.Top) &&
        Right.Equals(other.Right) &&
        Bottom.Equals(other.Bottom);

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is Rect other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(Left, Top, Right, Bottom);

    /// <summary>Compares two rectangles by their edges.</summary>
    public static bool operator ==(Rect left, Rect right) => left.Equals(right);

    /// <summary>Compares two rectangles by their edges.</summary>
    public static bool operator !=(Rect left, Rect right) => !left.Equals(right);

    /// <inheritdoc/>
    public override string ToString() => $"Rect({Left}, {Top}, {Right}, {Bottom})";
}
