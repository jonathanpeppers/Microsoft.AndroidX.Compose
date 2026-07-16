using AndroidX.Compose.Material3;

namespace AndroidX.Compose;

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
    int _initialHour;
    int _initialMinute;

    internal ITimePickerState? Jvm;

    internal int InitialHour => _initialHour;
    internal int InitialMinute => _initialMinute;
    internal bool InitialIs24Hour { get; }

    /// <summary>Creates a time-picker state with constructor-only initial values.</summary>
    /// <param name="initialHour">Initial hour from 0 through 23.</param>
    /// <param name="initialMinute">Initial minute from 0 through 59.</param>
    /// <param name="is24Hour">Whether to use 24-hour presentation.</param>
    public TimePickerState(int initialHour = 12, int initialMinute = 0, bool is24Hour = true)
    {
        ValidateHour(initialHour);
        ValidateMinute(initialMinute);
        _initialHour = initialHour;
        _initialMinute = initialMinute;
        InitialIs24Hour = is24Hour;
    }

    /// <summary>Currently displayed hour (0–23). Falls back to the
    /// constructor's <c>initialHour</c> until bound.</summary>
    public int Hour
    {
        get => Jvm?.Hour ?? _initialHour;
        set
        {
            ValidateHour(value);
            if (Jvm is null)
                _initialHour = value;
            else
                Jvm.Hour = value;
        }
    }

    /// <summary>Currently displayed minute (0–59). Falls back to the
    /// constructor's <c>initialMinute</c> until bound.</summary>
    public int Minute
    {
        get => Jvm?.Minute ?? _initialMinute;
        set
        {
            ValidateMinute(value);
            if (Jvm is null)
                _initialMinute = value;
            else
                Jvm.Minute = value;
        }
    }

    /// <summary>Whether the picker is in 24-hour mode (vs. 12-hour with
    /// AM/PM). Falls back to the constructor's <c>is24Hour</c> until
    /// bound.</summary>
    public bool Is24Hour => Jvm?.Is24hour() ?? InitialIs24Hour;

    static void ValidateHour(int value)
    {
        if ((uint)value > 23)
            throw new ArgumentOutOfRangeException(nameof(value), value, "Hour must be from 0 through 23.");
    }

    static void ValidateMinute(int value)
    {
        if ((uint)value > 59)
            throw new ArgumentOutOfRangeException(nameof(value), value, "Minute must be from 0 through 59.");
    }
}
