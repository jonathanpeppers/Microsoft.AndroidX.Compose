namespace AndroidX.Compose;

/// <summary>
/// Inclusive range of years displayed by a <see cref="DatePicker"/> or
/// <see cref="DateRangePicker"/>.
/// </summary>
public readonly struct DatePickerYearRange
{
    /// <summary>Creates an inclusive year range.</summary>
    /// <param name="startYear">First selectable year.</param>
    /// <param name="endYear">Last selectable year.</param>
    public DatePickerYearRange(int startYear, int endYear)
    {
        if (endYear < startYear)
            throw new ArgumentOutOfRangeException(nameof(endYear), endYear, "End year must be greater than or equal to start year.");

        StartYear = startYear;
        EndYear = endYear;
    }

    /// <summary>First selectable year.</summary>
    public int StartYear { get; }

    /// <summary>Last selectable year.</summary>
    public int EndYear { get; }
}
