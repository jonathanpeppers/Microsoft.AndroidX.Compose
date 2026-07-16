using AndroidX.Compose.Material3;

namespace AndroidX.Compose;

/// <summary>
/// Caller-supplied state holder for <see cref="DateRangePicker"/>. The
/// underlying JVM <c>androidx.compose.material3.DateRangePickerState</c>
/// is created lazily the first time a <see cref="DateRangePicker"/>
/// bound to this state is rendered. Before that point, selection and
/// displayed-month writes are retained and seed the JVM state.
/// </summary>
/// <remarks>
/// Typical usage — <c>Remember</c> a state instance, pass it to a
/// <see cref="DateRangePicker"/>, and read the picked range from the
/// dialog's <c>ConfirmButton.OnClick</c>:
/// <code>
/// var rangeState = Remember(() =&gt; new DateRangePickerState());
///
/// new DatePickerDialog(onDismissRequest: ...)
/// {
///     ConfirmButton = new Button(onClick: () =&gt;
///     {
///         var start = rangeState.SelectedStartDateMillis;
///         var end   = rangeState.SelectedEndDateMillis;
///         // start/end are Unix epoch milliseconds (UTC), nullable.
///     })
///     { new Text("OK") },
///     Body          = new DateRangePicker(rangeState),
/// }
/// </code>
/// </remarks>
public sealed class DateRangePickerState
{
    long? _initialSelectedStartDateMillis;
    long? _initialSelectedEndDateMillis;
    long? _initialDisplayedMonthMillis;

    internal IDateRangePickerState? Jvm;

    /// <summary>Creates a range-picker state with managed initial values.</summary>
    /// <param name="initialSelectedStartDateMillis">Initial range start as
    /// Unix epoch milliseconds (UTC).</param>
    /// <param name="initialSelectedEndDateMillis">Initial range end as
    /// Unix epoch milliseconds (UTC).</param>
    /// <param name="initialDisplayedMonthMillis">First-of-month milliseconds
    /// for the initially displayed month.</param>
    /// <param name="initialYearRange">Inclusive managed year range, or
    /// <c>null</c> for Kotlin's default.</param>
    /// <param name="initialDisplayMode">Initial packed Material 3 display
    /// mode, or <c>null</c> for calendar mode.</param>
    /// <param name="initialSelectableDates">Date-selection policy, or
    /// <c>null</c> to allow every date.</param>
    public DateRangePickerState(
        long?                initialSelectedStartDateMillis = null,
        long?                initialSelectedEndDateMillis   = null,
        long?                initialDisplayedMonthMillis    = null,
        DatePickerYearRange? initialYearRange               = null,
        int?                 initialDisplayMode              = null,
        ISelectableDates?    initialSelectableDates          = null)
    {
        ValidateSelection(initialSelectedStartDateMillis, initialSelectedEndDateMillis);
        _initialSelectedStartDateMillis = initialSelectedStartDateMillis;
        _initialSelectedEndDateMillis = initialSelectedEndDateMillis;
        _initialDisplayedMonthMillis = initialDisplayedMonthMillis;
        InitialYearRange = initialYearRange;
        InitialDisplayMode = initialDisplayMode;
        InitialSelectableDates = initialSelectableDates;
    }

    /// <summary>Initial selected range start as Unix epoch milliseconds.</summary>
    public long? InitialSelectedStartDateMillis
    {
        get => _initialSelectedStartDateMillis;
    }

    /// <summary>Initial selected range end as Unix epoch milliseconds.</summary>
    public long? InitialSelectedEndDateMillis
    {
        get => _initialSelectedEndDateMillis;
    }

    /// <summary>Initial displayed month as first-of-month epoch milliseconds.</summary>
    public long? InitialDisplayedMonthMillis
    {
        get => _initialDisplayedMonthMillis;
    }

    /// <summary>Inclusive managed year range for the year-grid view.</summary>
    public DatePickerYearRange? InitialYearRange { get; }

    /// <summary>Initial packed Material 3 display mode.</summary>
    public int? InitialDisplayMode { get; }

    /// <summary>Initial date-selection policy.</summary>
    public ISelectableDates? InitialSelectableDates { get; }

    /// <summary>
    /// The start of the currently selected range as Unix epoch
    /// milliseconds (UTC), or <c>null</c> if no start date is selected.
    /// Mirrors Kotlin's <c>DateRangePickerState.selectedStartDateMillis: Long?</c>.
    /// Before binding, reads and writes use the pending range that will
    /// seed the JVM state. Setting either end of
    /// the range goes through the JVM <c>setSelection</c> helper to keep
    /// the start/end pair consistent.
    /// </summary>
    public long? SelectedStartDateMillis
    {
        get => Jvm is { } jvm
            ? jvm.SelectedStartDateMillis?.LongValue()
            : _initialSelectedStartDateMillis;
        set => SetSelection(value, SelectedEndDateMillis);
    }

    /// <summary>
    /// The end of the currently selected range as Unix epoch
    /// milliseconds (UTC), or <c>null</c> if no end date is selected.
    /// Mirrors Kotlin's <c>DateRangePickerState.selectedEndDateMillis: Long?</c>.
    /// Before binding, reads and writes use the pending range.
    /// </summary>
    public long? SelectedEndDateMillis
    {
        get => Jvm is { } jvm
            ? jvm.SelectedEndDateMillis?.LongValue()
            : _initialSelectedEndDateMillis;
        set => SetSelection(SelectedStartDateMillis, value);
    }

    /// <summary>
    /// First-of-month milliseconds for the month currently shown by the
    /// picker. Mirrors Kotlin's <c>DateRangePickerState.displayedMonthMillis</c>.
    /// Before binding, reads and writes use the pending initial month.
    /// Returns <c>0</c> when no initial month was supplied.
    /// </summary>
    public long DisplayedMonthMillis
    {
        get => Jvm?.DisplayedMonthMillis ?? _initialDisplayedMonthMillis ?? 0L;
        set
        {
            if (Jvm is null)
                _initialDisplayedMonthMillis = value;
            else
                Jvm.DisplayedMonthMillis = value;
        }
    }

    /// <summary>
    /// Sets both ends of the selected range in a single JVM round-trip.
    /// Use this when both endpoints change at once to avoid the
    /// intermediate "start without end" / "end without start" state the
    /// individual property setters would otherwise produce.
    /// </summary>
    public void SetSelection(long? startDateMillis, long? endDateMillis)
    {
        ValidateSelection(startDateMillis, endDateMillis);
        if (Jvm is null)
        {
            _initialSelectedStartDateMillis = startDateMillis;
            _initialSelectedEndDateMillis = endDateMillis;
            return;
        }

        var start = startDateMillis is long s ? Java.Lang.Long.ValueOf(s) : null;
        var end   = endDateMillis   is long e ? Java.Lang.Long.ValueOf(e) : null;
        try
        {
            Jvm.SetSelection(start, end);
        }
        finally
        {
            start?.Dispose();
            end?.Dispose();
        }
    }

    static void ValidateSelection(long? startDateMillis, long? endDateMillis)
    {
        if (endDateMillis is not long end)
            return;
        if (startDateMillis is not long start)
            throw new ArgumentException("An end date requires a start date.", nameof(endDateMillis));
        if (end < start)
            throw new ArgumentOutOfRangeException(nameof(endDateMillis), end, "End date must be greater than or equal to start date.");
    }
}
