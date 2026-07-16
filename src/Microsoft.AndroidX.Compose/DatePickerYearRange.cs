namespace AndroidX.Compose;

/// <summary>
/// Inclusive range of years displayed by a <see cref="DatePicker"/> or
/// <see cref="DateRangePicker"/>.
/// </summary>
/// <remarks>
/// The default value represents Kotlin's default picker range,
/// <c>1900..2100</c>.
/// </remarks>
public readonly struct DatePickerYearRange : IEquatable<DatePickerYearRange>
{
    const int DefaultStartYear = 1900;
    const int DefaultEndYear = 2100;

    readonly int _encodedStartYear;
    readonly int _encodedEndYear;

    /// <summary>Creates an inclusive year range.</summary>
    /// <param name="startYear">First selectable year.</param>
    /// <param name="endYear">Last selectable year.</param>
    public DatePickerYearRange(int startYear, int endYear)
    {
        if (endYear < startYear)
            throw new ArgumentOutOfRangeException(nameof(endYear), endYear, "End year must be greater than or equal to start year.");

        _encodedStartYear = startYear ^ DefaultStartYear;
        _encodedEndYear = endYear ^ DefaultEndYear;
    }

    /// <summary>First selectable year. Defaults to <c>1900</c>.</summary>
    public int StartYear => _encodedStartYear ^ DefaultStartYear;

    /// <summary>Last selectable year. Defaults to <c>2100</c>.</summary>
    public int EndYear => _encodedEndYear ^ DefaultEndYear;

    /// <inheritdoc/>
    public bool Equals(DatePickerYearRange other) =>
        _encodedStartYear == other._encodedStartYear &&
        _encodedEndYear == other._encodedEndYear;

    /// <inheritdoc/>
    public override bool Equals(object? obj) =>
        obj is DatePickerYearRange other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() =>
        HashCode.Combine(_encodedStartYear, _encodedEndYear);

    /// <summary>Compares two year ranges by their inclusive endpoints.</summary>
    public static bool operator ==(DatePickerYearRange left, DatePickerYearRange right) =>
        left.Equals(right);

    /// <summary>Compares two year ranges by their inclusive endpoints.</summary>
    public static bool operator !=(DatePickerYearRange left, DatePickerYearRange right) =>
        !left.Equals(right);

    /// <inheritdoc/>
    public override string ToString() => $"DatePickerYearRange({StartYear}..{EndYear})";
}
