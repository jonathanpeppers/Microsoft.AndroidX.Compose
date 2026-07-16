using AndroidX.Compose.Material3;

namespace AndroidX.Compose;

/// <summary>
/// Caller-supplied state holder for <see cref="DatePicker"/> /
/// <see cref="DatePickerDialog"/>. The underlying JVM
/// <c>androidx.compose.material3.DatePickerState</c> is created lazily
/// the first time a <see cref="DatePicker"/> bound to this state is
/// rendered. Live-value writes made before that point are retained and
/// seed the JVM state when it is created.
/// </summary>
/// <remarks>
/// Typical usage — <c>Remember</c> a state instance, pass it to a
/// <see cref="DatePicker"/>, and read the picked value from the
/// dialog's <c>ConfirmButton.OnClick</c>:
/// <code>
/// var pickerState = Remember(() =&gt; new DatePickerState());
///
/// new DatePickerDialog(onDismissRequest: ...)
/// {
///     ConfirmButton = new Button(onClick: () =&gt;
///     {
///         var ms = pickerState.SelectedDateMillis;
///         // ms is the picked date as Unix epoch milliseconds (UTC).
///     })
///     { new Text("OK") },
///     Body          = new DatePicker(pickerState),
/// }
/// </code>
///
/// The constructor's optional parameters seed the underlying state on
/// first composition. After binding, live properties read and write the
/// JVM state directly.
/// </remarks>
public sealed class DatePickerState
{
    long? _initialSelectedDateMillis;
    long? _initialDisplayedMonthMillis;

    internal IDatePickerState? Jvm;

    /// <summary>
    /// Constructs a state holder seeded with the supplied initial values.
    /// All parameters are optional — passing <c>null</c> keeps Kotlin's
    /// default for that slot (auto-default-mask leaves the matching
    /// <c>$default</c> bit set).
    /// </summary>
    /// <param name="initialSelectedDateMillis">Initial selection as Unix
    /// epoch milliseconds (UTC), or <c>null</c> for "no initial selection".</param>
    /// <param name="initialDisplayedMonthMillis">First-of-month milliseconds
    /// for the initially displayed month, or <c>null</c> to let Kotlin
    /// derive it from the selected date.</param>
    /// <param name="initialYearRange">Inclusive managed range of selectable years
    /// shown in the year-grid. <c>null</c> keeps Kotlin's default
    /// (1900–2100).</param>
    /// <param name="initialDisplayMode">Initial packed Material 3 display
    /// mode, or <c>null</c> for calendar mode.</param>
    /// <param name="initialSelectableDates">Per-day enable/disable
    /// adapter. <c>null</c> keeps Kotlin's default (every date
    /// selectable).</param>
    public DatePickerState(
        long?                initialSelectedDateMillis   = null,
        long?                initialDisplayedMonthMillis = null,
        DatePickerYearRange? initialYearRange            = null,
        int?                 initialDisplayMode           = null,
        ISelectableDates?    initialSelectableDates       = null)
    {
        _initialSelectedDateMillis = initialSelectedDateMillis;
        _initialDisplayedMonthMillis = initialDisplayedMonthMillis;
        InitialYearRange = initialYearRange;
        InitialDisplayMode = initialDisplayMode;
        InitialSelectableDates = initialSelectableDates;
    }

    /// <summary>
    /// Initial selection as Unix epoch milliseconds (UTC). This value can
    /// only be configured while constructing the holder.
    /// </summary>
    public long? InitialSelectedDateMillis
    {
        get => _initialSelectedDateMillis;
    }

    /// <summary>
    /// First-of-month milliseconds for the initial month shown by the
    /// picker. Kotlin's <c>initialDisplayedMonthMillis</c> slot —
    /// usually left <c>null</c> so Kotlin defaults to the month
    /// containing <see cref="InitialSelectedDateMillis"/>.
    /// </summary>
    public long? InitialDisplayedMonthMillis
    {
        get => _initialDisplayedMonthMillis;
    }

    /// <summary>
    /// Inclusive managed year range for the year-grid view.
    /// </summary>
    public DatePickerYearRange? InitialYearRange { get; }

    /// <summary>
    /// Initial display mode (<c>DatePicker</c> or <c>Input</c>). Maps
    /// to Kotlin's <c>DisplayMode</c> packed-int enum. <c>null</c> uses
    /// Kotlin's default (calendar mode).
    /// </summary>
    public int? InitialDisplayMode { get; }

    /// <summary>
    /// Per-day / per-year enable/disable adapter. Read once by the
    /// Phase 4b <c>RememberDatePickerState</c> bridge on first
    /// composition. The adapter itself can mutate state — Kotlin
    /// re-invokes <c>isSelectableDate</c> / <c>isSelectableYear</c> on
    /// every grid render — but the adapter <i>instance</i> reference
    /// participates in <c>remember</c>'s key, so callers must hold a
    /// stable reference (e.g. allocate one adapter per host instance
    /// as a <c>readonly</c> field, mirroring the Phase 10
    /// <c>ConfirmStateChange</c> pattern).
    /// </summary>
    public ISelectableDates? InitialSelectableDates { get; }

    /// <summary>
    /// The currently selected date as Unix epoch milliseconds (UTC), or
    /// <c>null</c> if no date is selected. Mirrors Kotlin's
    /// <c>DatePickerState.selectedDateMillis: Long?</c>. Before binding,
    /// reads and writes use the pending value that will seed the JVM state.
    /// </summary>
    public long? SelectedDateMillis
    {
        get => Jvm is { } jvm
            ? jvm.SelectedDateMillis?.LongValue()
            : _initialSelectedDateMillis;
        set
        {
            if (Jvm is null)
            {
                _initialSelectedDateMillis = value;
                return;
            }

            using var boxed = value is long milliseconds
                ? Java.Lang.Long.ValueOf(milliseconds)
                : null;
            Jvm.SelectedDateMillis = boxed;
        }
    }

    /// <summary>
    /// First-of-month milliseconds for the month currently shown by the
    /// picker. Mirrors Kotlin's <c>DatePickerState.displayedMonthMillis</c>.
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
}
