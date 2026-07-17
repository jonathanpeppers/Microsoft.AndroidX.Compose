namespace AndroidX.Compose;

/// <summary>Inclusive finite floating-point range used by slider APIs.</summary>
/// <remarks>
/// The default value is <c>0f..1f</c>. Endpoints must be finite, and the
/// end must be greater than or equal to the start.
/// </remarks>
public readonly struct FloatRange : IEquatable<FloatRange>
{
    const int DefaultEndBits = 0x3F800000;

    readonly float _start;
    readonly int _encodedEndBits;

    /// <summary>Creates an inclusive floating-point range.</summary>
    /// <param name="start">Inclusive lower endpoint.</param>
    /// <param name="end">Inclusive upper endpoint.</param>
    public FloatRange(float start, float end)
    {
        if (!float.IsFinite(start))
            throw new ArgumentOutOfRangeException(nameof(start), start, "Start must be finite.");
        if (!float.IsFinite(end))
            throw new ArgumentOutOfRangeException(nameof(end), end, "End must be finite.");
        if (end < start)
            throw new ArgumentOutOfRangeException(nameof(end), end, "End must be greater than or equal to start.");

        _start = start;
        _encodedEndBits = BitConverter.SingleToInt32Bits(end) ^ DefaultEndBits;
    }

    /// <summary>Inclusive lower endpoint. Defaults to <c>0f</c>.</summary>
    public float Start => _start;

    /// <summary>Inclusive upper endpoint. Defaults to <c>1f</c>.</summary>
    public float End => BitConverter.Int32BitsToSingle(_encodedEndBits ^ DefaultEndBits);

    /// <inheritdoc/>
    public bool Equals(FloatRange other) =>
        Start.Equals(other.Start) && End.Equals(other.End);

    /// <inheritdoc/>
    public override bool Equals(object? obj) =>
        obj is FloatRange other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(Start, End);

    /// <summary>Compares two ranges by their inclusive endpoints.</summary>
    public static bool operator ==(FloatRange left, FloatRange right) =>
        left.Equals(right);

    /// <summary>Compares two ranges by their inclusive endpoints.</summary>
    public static bool operator !=(FloatRange left, FloatRange right) =>
        !left.Equals(right);

    /// <inheritdoc/>
    public override string ToString() =>
        FormattableString.Invariant($"FloatRange({Start:R}..{End:R})");
}
