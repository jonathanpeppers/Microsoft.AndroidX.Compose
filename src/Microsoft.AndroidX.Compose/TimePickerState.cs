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
/// <para>For conditional consumers, hoist <c>composer.RememberTimePickerState(pickerState)</c>
/// outside their condition to retain native save registration. After all native
/// ownership ends, the wrapper retains the selected time for a new owner.</para>
/// </remarks>
public sealed class TimePickerState
{
    readonly int _initialHour;
    readonly int _initialMinute;
    int _hour;
    int _minute;
    bool _is24Hour;
    bool _hasPendingHour;
    bool _hasPendingMinute;

    internal ITimePickerState? Jvm;

    internal int InitialHour => _initialHour;
    internal int InitialMinute => _initialMinute;
    internal bool InitialIs24Hour { get; }
    internal int RememberHour => _hour;
    internal int RememberMinute => _minute;

    /// <summary>Creates a time-picker state with constructor-only initial values.</summary>
    /// <param name="initialHour">Initial hour from 0 through 23.</param>
    /// <param name="initialMinute">Initial minute from 0 through 59.</param>
    /// <param name="is24Hour">Whether to use 24-hour presentation.</param>
    public TimePickerState(int initialHour = 12, int initialMinute = 0, bool is24Hour = true)
    {
        ValidateHour(initialHour, nameof(initialHour));
        ValidateMinute(initialMinute, nameof(initialMinute));
        _initialHour = initialHour;
        _initialMinute = initialMinute;
        _hour = initialHour;
        _minute = initialMinute;
        InitialIs24Hour = is24Hour;
        _is24Hour = is24Hour;
    }

    /// <summary>Currently displayed hour (0–23). Before binding, returns
    /// the constructor value, last owned value, or latest pending write.</summary>
    public int Hour
    {
        get => Jvm?.Hour ?? _hour;
        set
        {
            ValidateHour(value, nameof(value));
            if (Jvm is null)
            {
                _hour = value;
                _hasPendingHour = true;
            }
            else
                Jvm.Hour = value;
        }
    }

    /// <summary>Currently displayed minute (0–59). Before binding, returns
    /// the constructor value, last owned value, or latest pending write.</summary>
    public int Minute
    {
        get => Jvm?.Minute ?? _minute;
        set
        {
            ValidateMinute(value, nameof(value));
            if (Jvm is null)
            {
                _minute = value;
                _hasPendingMinute = true;
            }
            else
                Jvm.Minute = value;
        }
    }

    /// <summary>Whether the picker is in 24-hour mode (vs. 12-hour with
    /// AM/PM). While unbound, returns the last owned mode, or the
    /// constructor's <c>is24Hour</c> before first ownership.</summary>
    public bool Is24Hour => Jvm?.Is24hour() ?? _is24Hour;

    internal void BindJvm(ITimePickerState jvm)
    {
        Jvm = jvm;
        if (_hasPendingHour)
            Hour = _hour;
        if (_hasPendingMinute)
            Minute = _minute;
        _hasPendingHour = false;
        _hasPendingMinute = false;
    }

    internal void UnbindJvm()
    {
        if (Jvm is not { } jvm)
            return;
        _hour = jvm.Hour;
        _minute = jvm.Minute;
        _is24Hour = jvm.Is24hour();
        Jvm = null;
    }

    static void ValidateHour(int value, string paramName)
    {
        if ((uint)value > 23)
            throw new ArgumentOutOfRangeException(paramName, value, "Hour must be from 0 through 23.");
    }

    static void ValidateMinute(int value, string paramName)
    {
        if ((uint)value > 59)
            throw new ArgumentOutOfRangeException(paramName, value, "Minute must be from 0 through 59.");
    }
}
