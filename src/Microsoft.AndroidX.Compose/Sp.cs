namespace AndroidX.Compose;

/// <summary>
/// Scale-independent pixel — C# mirror of Kotlin's
/// <c>androidx.compose.ui.unit.TextUnit</c> in <c>Sp</c> form
/// (<c>@JvmInline value class TextUnit(val packedValue: Long)</c>).
/// </summary>
/// <remarks>
/// <para>
/// Compose's <c>TextUnit</c> is an inline value class around a <c>Long</c>;
/// the high bits carry the type tag (Sp/Em/Unspecified), and the low 32 bits
/// carry the raw IEEE 754 float value, including its sign.
/// This struct preserves that wire shape — <see cref="PackedValue"/> is the
/// exact long that <c>TextUnitKt.GetSp</c> returns — so it crosses
/// JNI unchanged.
/// </para>
/// <para>
/// Use <see cref="Sp(int)"/>, <see cref="Sp(float)"/>, or the implicit
/// conversion from <see cref="int"/> to construct a typed Sp value via
/// the bound <c>TextUnitKt.GetSp</c> factory. Fractional amounts can also
/// be written as <c>0.5f.Sp()</c>. There is no implicit float conversion,
/// so a packed <see cref="long"/> cannot silently become a pixel count
/// through a widening numeric conversion.
/// </para>
/// </remarks>
public readonly struct Sp : IEquatable<Sp>, IComparable<Sp>
{
    /// <summary>
    /// The raw packed <c>TextUnit.packedValue</c>. Pass this directly to
    /// any JNI bridge that expects a <c>long TextUnit</c> argument.
    /// </summary>
    public long PackedValue { get; }

    /// <summary>
    /// Construct an Sp from the raw packed <c>TextUnit</c> long. Use the
    /// other overloads (which take an <see cref="int"/> or <see cref="float"/>) for ergonomic
    /// construction; this one is for round-tripping packed values from
    /// existing bound APIs.
    /// </summary>
    public Sp(long packedValue)
    {
        PackedValue = packedValue;
    }

    /// <summary>
    /// Construct an Sp from a scale-independent pixel count by calling
    /// the bound <c>TextUnitKt.GetSp(int)</c> factory.
    /// </summary>
    public Sp(int sp)
        : this(AndroidX.Compose.UI.Unit.TextUnitKt.GetSp(sp))
    {
    }

    /// <summary>
    /// Construct an Sp from a fractional scale-independent pixel count by
    /// calling the bound <c>TextUnitKt.GetSp(float)</c> factory without rounding.
    /// </summary>
    /// <remarks>
    /// Preserves Kotlin's handling of negative values, signed zero, infinities,
    /// and NaN; no additional validation or normalization is performed.
    /// </remarks>
    public Sp(float sp)
        : this(AndroidX.Compose.UI.Unit.TextUnitKt.GetSp(sp))
    {
    }

    /// <summary>The zero-sp constant (equivalent to <c>0.sp</c> in Kotlin).</summary>
    public static Sp Zero => new(0);

    /// <summary>Implicit conversion from <see cref="int"/> for ergonomic call sites.</summary>
    public static implicit operator Sp(int value) => new Sp(value);

    /// <inheritdoc/>
    public bool Equals(Sp other) => PackedValue == other.PackedValue;

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is Sp sp && Equals(sp);

    /// <inheritdoc/>
    public override int GetHashCode() => PackedValue.GetHashCode();

    /// <summary>Equality operator.</summary>
    public static bool operator ==(Sp left, Sp right) => left.Equals(right);

    /// <summary>Inequality operator.</summary>
    public static bool operator !=(Sp left, Sp right) => !left.Equals(right);

    /// <inheritdoc/>
    /// <remarks>
    /// Delegates to Kotlin's <c>TextUnit.compareTo</c>. Defined for same-tag
    /// values. Integer and float constructors produce Sp-tagged values;
    /// the raw-long constructor and <see langword="default"/> can carry
    /// other tags. Kotlin rejects unspecified or mismatched tags.
    /// </remarks>
    public int CompareTo(Sp other) =>
        AndroidX.Compose.UI.Unit.TextUnit.CompareTo(PackedValue, other.PackedValue);

    /// <summary>
    /// Scale an <see cref="Sp"/> value by a scalar factor. Mirrors Kotlin's
    /// <c>TextUnit.times(Float)</c>.
    /// </summary>
    public static Sp operator *(Sp a, float scalar) =>
        new(AndroidX.Compose.UI.Unit.TextUnit.Times(a.PackedValue, scalar));

    /// <summary>Scale an <see cref="Sp"/> value by a scalar factor.</summary>
    public static Sp operator *(float scalar, Sp a) =>
        new(AndroidX.Compose.UI.Unit.TextUnit.Times(a.PackedValue, scalar));

    /// <summary>
    /// Divide an <see cref="Sp"/> value by a scalar factor. Mirrors Kotlin's
    /// <c>TextUnit.div(Float)</c>.
    /// </summary>
    public static Sp operator /(Sp a, float scalar) =>
        new(AndroidX.Compose.UI.Unit.TextUnit.Div(a.PackedValue, scalar));

    /// <summary>
    /// Negate an <see cref="Sp"/> value. Mirrors Kotlin's
    /// <c>TextUnit.unaryMinus()</c>.
    /// </summary>
    public static Sp operator -(Sp a) =>
        new(AndroidX.Compose.UI.Unit.TextUnit.UnaryMinus(a.PackedValue));

    /// <summary>Less-than comparison.</summary>
    public static bool operator <(Sp a, Sp b) => a.CompareTo(b) < 0;

    /// <summary>Greater-than comparison.</summary>
    public static bool operator >(Sp a, Sp b) => a.CompareTo(b) > 0;

    /// <summary>Less-than-or-equal comparison.</summary>
    public static bool operator <=(Sp a, Sp b) => a.CompareTo(b) <= 0;

    /// <summary>Greater-than-or-equal comparison.</summary>
    public static bool operator >=(Sp a, Sp b) => a.CompareTo(b) >= 0;

    /// <inheritdoc/>
    public override string ToString()
    {
        var type = unchecked((uint)(PackedValue >> 32));
        if (type == 0)
            return "Unspecified";
        if (type != 1)
            return $"InvalidSp(0x{PackedValue:X16})";

        var value = BitConverter.Int32BitsToSingle(unchecked((int)PackedValue));
        return $"{value}.sp";
    }

    internal static long Pack(Sp? value) => value?.PackedValue ?? 0L;
}
