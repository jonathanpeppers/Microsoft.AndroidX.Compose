using global::AndroidX.Compose.Material3;

namespace Microsoft.AndroidX.Compose;

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
