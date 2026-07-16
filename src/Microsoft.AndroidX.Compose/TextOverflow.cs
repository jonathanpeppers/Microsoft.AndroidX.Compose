namespace AndroidX.Compose;

/// <summary>
/// Controls how text that exceeds its available space is rendered.
/// </summary>
public readonly struct TextOverflow : IEquatable<TextOverflow>
{
    readonly int _value;

    TextOverflow(int value)
    {
        _value = value;
    }

    /// <summary>Truncate the text at the edge of the container.</summary>
    public static TextOverflow Clip => new(0);

    /// <summary>Replace the overflowing text with an ellipsis (default for single-line).</summary>
    public static TextOverflow Ellipsis => new(1);

    /// <summary>Render the text outside the container bounds (no clipping).</summary>
    public static TextOverflow Visible => new(2);

    /// <summary>Place the ellipsis at the start of the text.</summary>
    public static TextOverflow StartEllipsis => new(3);

    /// <summary>Place the ellipsis in the middle of the text.</summary>
    public static TextOverflow MiddleEllipsis => new(4);

    internal static int Pack(TextOverflow? value) =>
        value is { } overflow ? overflow._value + 1 : 0;

    /// <inheritdoc/>
    public bool Equals(TextOverflow other) => _value == other._value;

    /// <inheritdoc/>
    public override bool Equals(object? obj) =>
        obj is TextOverflow overflow && Equals(overflow);

    /// <inheritdoc/>
    public override int GetHashCode() => _value;

    /// <summary>Equality operator.</summary>
    public static bool operator ==(TextOverflow left, TextOverflow right) =>
        left.Equals(right);

    /// <summary>Inequality operator.</summary>
    public static bool operator !=(TextOverflow left, TextOverflow right) =>
        !left.Equals(right);

    /// <inheritdoc/>
    public override string ToString() => _value switch
    {
        0 => nameof(Clip),
        1 => nameof(Ellipsis),
        2 => nameof(Visible),
        3 => nameof(StartEllipsis),
        4 => nameof(MiddleEllipsis),
        _ => "Invalid",
    };
}
