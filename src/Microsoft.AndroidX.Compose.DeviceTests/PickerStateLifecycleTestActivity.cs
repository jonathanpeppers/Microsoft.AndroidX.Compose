using Android.Runtime;
using AndroidX.Activity;
using AndroidX.Compose;
using ComposeDatePicker = AndroidX.Compose.DatePicker;
using ComposeTimePicker = AndroidX.Compose.TimePicker;

namespace Microsoft.AndroidX.Compose.DeviceTests;

/// <summary>
/// Hosts picker state holders in a real composition so instrumentation tests
/// can verify first binding and removal/re-entry lifecycle behavior.
/// </summary>
[Activity(Theme = "@android:style/Theme.Material.Light.NoActionBar")]
[Register("net/compose/devicetests/PickerStateLifecycleTestActivity")]
public class PickerStateLifecycleTestActivity : ComponentActivity
{
    internal enum PickerKind
    {
        Date,
        DateRange,
        Time,
    }

    static int s_completedRenderPasses;

    internal static PickerStateLifecycleTestActivity? Current { get; private set; }
    internal static PickerKind Kind { get; private set; }
    internal static MutableState<bool>? Visible { get; private set; }
    internal static MutableState<bool>? ShowDefault { get; private set; }
    internal static DatePickerState? DateState { get; private set; }
    internal static DatePickerState? DefaultDateState { get; private set; }
    internal static DateRangePickerState? DateRangeState { get; private set; }
    internal static DateRangePickerState? DefaultDateRangeState { get; private set; }
    internal static TimePickerState? TimeState { get; private set; }
    internal static int CompletedRenderPasses => Volatile.Read(ref s_completedRenderPasses);

    internal static void MarkRenderCompleted() =>
        Interlocked.Increment(ref s_completedRenderPasses);

    internal static void Reset(PickerKind kind)
    {
        Current = null;
        Kind = kind;
        Visible = null;
        ShowDefault = null;
        DateState = null;
        DefaultDateState = null;
        DateRangeState = null;
        DefaultDateRangeState = null;
        TimeState = null;
        Volatile.Write(ref s_completedRenderPasses, 0);

        switch (kind)
        {
            case PickerKind.Date:
                DateState = new DatePickerState(
                    initialSelectedDateMillis: 1_704_067_200_000L,
                    initialDisplayedMonthMillis: 1_704_067_200_000L,
                    initialYearRange: new DatePickerYearRange(2020, 2030));
                DateState.SelectedDateMillis = 1_706_745_600_000L;
                DateState.DisplayedMonthMillis = 1_709_251_200_000L;
                DefaultDateState = new DatePickerState();
                break;
            case PickerKind.DateRange:
                DateRangeState = new DateRangePickerState(
                    initialSelectedStartDateMillis: 1_735_689_600_000L,
                    initialSelectedEndDateMillis: 1_736_035_200_000L,
                    initialDisplayedMonthMillis: 1_735_689_600_000L,
                    initialYearRange: new DatePickerYearRange(2020, 2030));
                DateRangeState.SetSelection(1_746_057_600_000L, 1_746_403_200_000L);
                DateRangeState.DisplayedMonthMillis = 1_746_057_600_000L;
                DefaultDateRangeState = new DateRangePickerState();
                break;
            case PickerKind.Time:
                TimeState = new TimePickerState(initialHour: 7, initialMinute: 15, is24Hour: false)
                {
                    Hour = 8,
                    Minute = 46,
                };
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unsupported picker kind.");
        }
    }

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        Visible = new MutableState<bool>(true);
        ShowDefault = new MutableState<bool>(false);
        this.SetContent(_ => new Composed(_ =>
        {
            var visible = Visible
                ?? throw new InvalidOperationException("Visibility state not set on PickerStateLifecycleTestActivity.");
            if (!visible.Value)
                return new LifecycleRenderMarker(null);

            var picker = Kind switch
            {
                PickerKind.Date => BuildDatePickers(),
                PickerKind.DateRange => BuildDateRangePickers(),
                PickerKind.Time => BuildTimePickers(),
                _ => throw new InvalidOperationException("Picker kind not set on PickerStateLifecycleTestActivity."),
            };
            return new LifecycleRenderMarker(picker);
        }));
        Current = this;
    }

    protected override void OnDestroy()
    {
        if (ReferenceEquals(Current, this))
            Current = null;
        Visible = null;
        ShowDefault = null;
        base.OnDestroy();
    }

    static ComposableNode BuildDatePickers()
    {
        var state = DateState
            ?? throw new InvalidOperationException("Date state not set on PickerStateLifecycleTestActivity.");
        var defaultState = DefaultDateState
            ?? throw new InvalidOperationException("Default date state not set on PickerStateLifecycleTestActivity.");
        var showDefault = ShowDefault
            ?? throw new InvalidOperationException("Picker variant not set on PickerStateLifecycleTestActivity.");
        return showDefault.Value
            ? new Box { new ComposeDatePicker(defaultState) }
            : new Column { new ComposeDatePicker(state) };
    }

    static ComposableNode BuildDateRangePickers()
    {
        var state = DateRangeState
            ?? throw new InvalidOperationException("Date-range state not set on PickerStateLifecycleTestActivity.");
        var defaultState = DefaultDateRangeState
            ?? throw new InvalidOperationException("Default date-range state not set on PickerStateLifecycleTestActivity.");
        var showDefault = ShowDefault
            ?? throw new InvalidOperationException("Picker variant not set on PickerStateLifecycleTestActivity.");
        return showDefault.Value
            ? new Box { new DateRangeStateBinder(defaultState) }
            : new Column { new DateRangeStateBinder(state) };
    }

    static ComposableNode BuildTimePickers()
    {
        var state = TimeState
            ?? throw new InvalidOperationException("Time state not set on PickerStateLifecycleTestActivity.");
        return new Box
        {
            new ComposeTimePicker(state),
            new TimeInput(state),
        };
    }
}
