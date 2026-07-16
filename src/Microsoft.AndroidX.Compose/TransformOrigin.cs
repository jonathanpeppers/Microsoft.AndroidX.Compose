namespace AndroidX.Compose;

/// <summary>
/// Pivot point used by transform-aware modifiers such as
/// <see cref="Modifier.GraphicsLayer"/> for scale and rotation.
/// </summary>
/// <remarks>
/// Fractions normally range from <c>0</c> to <c>1</c>: <c>0</c> is the
/// left or top edge, <c>0.5</c> is the center, and <c>1</c> is the right
/// or bottom edge. Values outside that range are allowed.
/// </remarks>
public readonly struct TransformOrigin : IEquatable<TransformOrigin>
{
    /// <summary>
    /// Creates a transform origin from horizontal and vertical pivot
    /// fractions.
    /// </summary>
    /// <param name="pivotFractionX">Horizontal pivot fraction.</param>
    /// <param name="pivotFractionY">Vertical pivot fraction.</param>
    public TransformOrigin(float pivotFractionX, float pivotFractionY)
    {
        PivotFractionX = pivotFractionX;
        PivotFractionY = pivotFractionY;
    }

    /// <summary>The horizontal pivot fraction.</summary>
    public float PivotFractionX { get; }

    /// <summary>The vertical pivot fraction.</summary>
    public float PivotFractionY { get; }

    /// <summary>The geometric center of the composable.</summary>
    public static TransformOrigin Center => new(0.5f, 0.5f);

    internal long PackedValue
    {
        get
        {
            long x = unchecked((uint)BitConverter.SingleToInt32Bits(PivotFractionX));
            long y = unchecked((uint)BitConverter.SingleToInt32Bits(PivotFractionY));
            return (x << 32) | y;
        }
    }

    internal static long Pack(TransformOrigin? value) => value?.PackedValue ?? 0L;

    /// <inheritdoc/>
    public bool Equals(TransformOrigin other) =>
        PivotFractionX.Equals(other.PivotFractionX) &&
        PivotFractionY.Equals(other.PivotFractionY);

    /// <inheritdoc/>
    public override bool Equals(object? obj) =>
        obj is TransformOrigin origin && Equals(origin);

    /// <inheritdoc/>
    public override int GetHashCode() =>
        HashCode.Combine(PivotFractionX, PivotFractionY);

    /// <summary>Equality operator.</summary>
    public static bool operator ==(TransformOrigin left, TransformOrigin right) =>
        left.Equals(right);

    /// <summary>Inequality operator.</summary>
    public static bool operator !=(TransformOrigin left, TransformOrigin right) =>
        !left.Equals(right);

    /// <inheritdoc/>
    public override string ToString() =>
        $"TransformOrigin({PivotFractionX}, {PivotFractionY})";
}
