using Android.Runtime;
using AndroidX.Compose.Material3;

namespace ComposeNet;

// ---- DatePickerState ----

/// <summary>
/// Caller-supplied state holder for <see cref="DatePicker"/> /
/// <see cref="DatePickerDialog"/>. The underlying JVM
/// <c>androidx.compose.material3.DatePickerState</c> is created lazily
/// the first time a <see cref="DatePicker"/> bound to this state is
/// rendered; reads/writes to <see cref="SelectedDateMillis"/> before
/// that point are no-ops/fallbacks.
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
/// </remarks>
public sealed class DatePickerState
{
    internal IDatePickerState? Jvm;

    /// <summary>
    /// The currently selected date as Unix epoch milliseconds (UTC), or
    /// <c>null</c> if no date is selected. Mirrors Kotlin's
    /// <c>DatePickerState.selectedDateMillis: Long?</c>. Returns
    /// <c>null</c> until the first <see cref="DatePicker"/> render binds
    /// this state to the JVM picker.
    /// </summary>
    public long? SelectedDateMillis
    {
        get => Jvm?.SelectedDateMillis?.LongValue();
        set
        {
            if (Jvm is not null)
                Jvm.SelectedDateMillis = value is long ms ? Java.Lang.Long.ValueOf(ms) : null;
        }
    }

    /// <summary>
    /// First-of-month milliseconds for the month currently shown by the
    /// picker. Mirrors Kotlin's <c>DatePickerState.displayedMonthMillis</c>.
    /// Returns <c>0</c> until the state is bound.
    /// </summary>
    public long DisplayedMonthMillis
    {
        get => Jvm?.DisplayedMonthMillis ?? 0L;
        set { if (Jvm is not null) Jvm.DisplayedMonthMillis = value; }
    }
}

// ---- TimePickerState ----

/// <summary>
/// Caller-supplied state holder for <see cref="TimePicker"/> /
/// <see cref="TimePickerDialog"/>. Mirrors Kotlin's
/// <c>TimePickerState</c>: <see cref="Hour"/>/<see cref="Minute"/>
/// expose the live values; <see cref="Is24Hour"/> reflects the format.
/// </summary>
/// <remarks>
/// Typical usage — <c>Remember</c> a state instance with the desired
/// initial values, pass it to a <see cref="TimePicker"/>, and read the
/// picked time from the dialog's <c>ConfirmButton.OnClick</c>:
/// <code>
/// var pickerState = Remember(() =&gt; new TimePickerState(initialHour: 9, initialMinute: 30));
///
/// new TimePickerDialog(onDismissRequest: ...)
/// {
///     ConfirmButton = new Button(onClick: () =&gt;
///     {
///         var h = pickerState.Hour;
///         var m = pickerState.Minute;
///     })
///     { new Text("OK") },
///     Body          = new TimePicker(pickerState),
/// }
/// </code>
/// </remarks>
public sealed class TimePickerState
{
    internal ITimePickerState? Jvm;

    internal int  InitialHour   { get; }
    internal int  InitialMinute { get; }
    internal bool InitialIs24Hour { get; }

    public TimePickerState(int initialHour = 12, int initialMinute = 0, bool is24Hour = true)
    {
        InitialHour     = initialHour;
        InitialMinute   = initialMinute;
        InitialIs24Hour = is24Hour;
    }

    /// <summary>Currently displayed hour (0–23). Falls back to the
    /// constructor's <c>initialHour</c> until bound.</summary>
    public int Hour
    {
        get => Jvm?.Hour ?? InitialHour;
        set { if (Jvm is not null) Jvm.Hour = value; }
    }

    /// <summary>Currently displayed minute (0–59). Falls back to the
    /// constructor's <c>initialMinute</c> until bound.</summary>
    public int Minute
    {
        get => Jvm?.Minute ?? InitialMinute;
        set { if (Jvm is not null) Jvm.Minute = value; }
    }

    /// <summary>Whether the picker is in 24-hour mode (vs. 12-hour with
    /// AM/PM). Falls back to the constructor's <c>is24Hour</c> until
    /// bound.</summary>
    public bool Is24Hour => Jvm?.Is24hour() ?? InitialIs24Hour;
}
