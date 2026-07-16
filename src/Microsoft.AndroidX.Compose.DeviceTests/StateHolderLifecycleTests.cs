namespace Microsoft.AndroidX.Compose.DeviceTests;

/// <summary>
/// Verifies picker state-holder pending values, live JVM state, Kotlin
/// defaults, and peer reuse across composition removal and re-entry.
/// </summary>
[TestClass]
[DoNotParallelize]
public class StateHolderLifecycleTests
{
    [TestMethod]
    public async Task DatePickerState_PreservesPendingValuesAndPeer()
    {
        var activity = await StartActivity(PickerStateLifecycleTestActivity.PickerKind.Date);
        try
        {
            var state = PickerStateLifecycleTestActivity.DateState
                ?? throw new InvalidOperationException("Date state was not initialized.");
            var jvm = await WaitFor(
                () => state.Jvm,
                static value => value is not null,
                "DatePickerState did not bind to a JVM peer.")
                ?? throw new InvalidOperationException("DatePickerState JVM peer was unavailable.");

            Assert.AreEqual(1_706_745_600_000L, jvm.SelectedDateMillis?.LongValue());
            Assert.AreEqual(1_709_251_200_000L, jvm.DisplayedMonthMillis);
            AssertYearRange(jvm.YearRange, 2020, 2030);

            await HidePicker(activity);
            state.SelectedDateMillis = null;
            Assert.IsNull(state.SelectedDateMillis);
            Assert.IsNull(jvm.SelectedDateMillis);
            state.SelectedDateMillis = 1_712_275_200_000L;
            state.DisplayedMonthMillis = 1_711_929_600_000L;
            Assert.AreEqual(1_712_275_200_000L, jvm.SelectedDateMillis?.LongValue());
            Assert.AreEqual(1_711_929_600_000L, jvm.DisplayedMonthMillis);

            await ShowPickerAndAssertPeerReused(activity, () => state.Jvm);

            var defaultState = PickerStateLifecycleTestActivity.DefaultDateState
                ?? throw new InvalidOperationException("Default date state was not initialized.");
            await ShowDefaultPicker(activity);
            var defaultJvm = await WaitFor(
                () => defaultState.Jvm,
                static value => value is not null,
                "Default DatePickerState did not bind to a JVM peer.")
                ?? throw new InvalidOperationException("Default DatePickerState JVM peer was unavailable.");
            Assert.IsNull(defaultJvm.SelectedDateMillis);
            AssertYearRange(defaultJvm.YearRange, 1900, 2100);
        }
        finally
        {
            await FinishActivity(activity);
        }
    }

    [TestMethod]
    public async Task DateRangePickerState_PreservesPendingValuesAndPeer()
    {
        var activity = await StartActivity(PickerStateLifecycleTestActivity.PickerKind.DateRange);
        try
        {
            var state = PickerStateLifecycleTestActivity.DateRangeState
                ?? throw new InvalidOperationException("Date-range state was not initialized.");
            var jvm = await WaitFor(
                () => state.Jvm,
                static value => value is not null,
                "DateRangePickerState did not bind to a JVM peer.")
                ?? throw new InvalidOperationException("DateRangePickerState JVM peer was unavailable.");

            Assert.AreEqual(1_746_057_600_000L, jvm.SelectedStartDateMillis?.LongValue());
            Assert.AreEqual(1_746_403_200_000L, jvm.SelectedEndDateMillis?.LongValue());
            Assert.AreEqual(1_746_057_600_000L, jvm.DisplayedMonthMillis);
            AssertYearRange(jvm.YearRange, 2020, 2030);

            await HidePicker(activity);
            state.SetSelection(null, null);
            Assert.IsNull(state.SelectedStartDateMillis);
            Assert.IsNull(state.SelectedEndDateMillis);
            state.SetSelection(1_748_736_000_000L, 1_749_081_600_000L);
            state.DisplayedMonthMillis = 1_748_736_000_000L;
            Assert.AreEqual(1_748_736_000_000L, jvm.SelectedStartDateMillis?.LongValue());
            Assert.AreEqual(1_749_081_600_000L, jvm.SelectedEndDateMillis?.LongValue());
            Assert.AreEqual(1_748_736_000_000L, jvm.DisplayedMonthMillis);

            await ShowPickerAndAssertPeerReused(activity, () => state.Jvm);

            var defaultState = PickerStateLifecycleTestActivity.DefaultDateRangeState
                ?? throw new InvalidOperationException("Default date-range state was not initialized.");
            await ShowDefaultPicker(activity);
            var defaultJvm = await WaitFor(
                () => defaultState.Jvm,
                static value => value is not null,
                "Default DateRangePickerState did not bind to a JVM peer.")
                ?? throw new InvalidOperationException("Default DateRangePickerState JVM peer was unavailable.");
            Assert.IsNull(defaultJvm.SelectedStartDateMillis);
            Assert.IsNull(defaultJvm.SelectedEndDateMillis);
            AssertYearRange(defaultJvm.YearRange, 1900, 2100);
        }
        finally
        {
            await FinishActivity(activity);
        }
    }

    [TestMethod]
    public async Task TimePickerState_PreservesPendingValuesAndPeer()
    {
        var activity = await StartActivity(PickerStateLifecycleTestActivity.PickerKind.Time);
        try
        {
            var state = PickerStateLifecycleTestActivity.TimeState
                ?? throw new InvalidOperationException("Time state was not initialized.");
            var jvm = await WaitFor(
                () => state.Jvm,
                static value => value is not null,
                "TimePickerState did not bind to a JVM peer.")
                ?? throw new InvalidOperationException("TimePickerState JVM peer was unavailable.");

            Assert.AreEqual(8, jvm.Hour);
            Assert.AreEqual(46, jvm.Minute);
            Assert.IsFalse(jvm.Is24hour());

            await HidePicker(activity);
            state.Hour = 19;
            state.Minute = 27;
            Assert.AreEqual(19, jvm.Hour);
            Assert.AreEqual(27, jvm.Minute);

            await ShowPickerAndAssertPeerReused(activity, () => state.Jvm);
        }
        finally
        {
            await FinishActivity(activity);
        }
    }

    static async Task FinishActivity(PickerStateLifecycleTestActivity activity)
    {
        activity.RunOnUiThread(activity.Finish);
        await WaitFor(
            static () => PickerStateLifecycleTestActivity.Current,
            current => !ReferenceEquals(current, activity),
            "Picker lifecycle test activity did not finish.");
    }

    static async Task<PickerStateLifecycleTestActivity> StartActivity(
        PickerStateLifecycleTestActivity.PickerKind kind)
    {
        var context = global::Android.App.Application.Context
            ?? throw new InvalidOperationException(
                "Application.Context not set for picker lifecycle tests.");
        PickerStateLifecycleTestActivity.Reset(kind);
        using var intent = new global::Android.Content.Intent(
            context,
            typeof(PickerStateLifecycleTestActivity));
        intent.AddFlags(global::Android.Content.ActivityFlags.NewTask);
        context.StartActivity(intent);

        return await WaitFor(
            static () => PickerStateLifecycleTestActivity.Current,
            static value => value is not null,
            "Picker lifecycle test activity did not start.")
            ?? throw new InvalidOperationException(
                "Picker lifecycle test activity was unavailable.");
    }

    static async Task HidePicker(PickerStateLifecycleTestActivity activity)
    {
        int hiddenPass = PickerStateLifecycleTestActivity.CompletedRenderPasses;
        activity.RunOnUiThread(() =>
        {
            var visible = PickerStateLifecycleTestActivity.Visible
                ?? throw new InvalidOperationException(
                    "Visibility state not set on PickerStateLifecycleTestActivity.");
            visible.Value = false;
        });
        await WaitFor(
            static () => PickerStateLifecycleTestActivity.CompletedRenderPasses,
            value => value > hiddenPass,
            "Picker did not leave composition.");
    }

    static async Task ShowPickerAndAssertPeerReused<TJvm>(
        PickerStateLifecycleTestActivity activity,
        Func<TJvm?> readJvm)
        where TJvm : class, global::Android.Runtime.IJavaObject
    {
        var before = readJvm()
            ?? throw new InvalidOperationException("Picker state JVM peer was unavailable before removal.");
        var handle = before.Handle;

        int shownPass = PickerStateLifecycleTestActivity.CompletedRenderPasses;
        activity.RunOnUiThread(() =>
        {
            var visible = PickerStateLifecycleTestActivity.Visible
                ?? throw new InvalidOperationException(
                    "Visibility state not set on PickerStateLifecycleTestActivity.");
            visible.Value = true;
        });
        await WaitFor(
            static () => PickerStateLifecycleTestActivity.CompletedRenderPasses,
            value => value > shownPass,
            "Picker did not re-enter composition.");

        var after = readJvm()
            ?? throw new InvalidOperationException("Picker state JVM peer was unavailable after re-entry.");
        Assert.AreEqual(handle, after.Handle);
    }

    static async Task ShowDefaultPicker(PickerStateLifecycleTestActivity activity)
    {
        int priorPass = PickerStateLifecycleTestActivity.CompletedRenderPasses;
        activity.RunOnUiThread(() =>
        {
            var showDefault = PickerStateLifecycleTestActivity.ShowDefault
                ?? throw new InvalidOperationException(
                    "Picker variant not set on PickerStateLifecycleTestActivity.");
            showDefault.Value = true;
        });
        await WaitFor(
            static () => PickerStateLifecycleTestActivity.CompletedRenderPasses,
            value => value > priorPass,
            "Default picker did not enter composition.");
    }

    static void AssertYearRange(Kotlin.Ranges.IntRange range, int startYear, int endYear)
    {
        var start = range.Start
            ?? throw new InvalidOperationException("Kotlin year range had no start.");
        var end = range.EndInclusive
            ?? throw new InvalidOperationException("Kotlin year range had no inclusive end.");
        Assert.AreEqual(startYear, start.IntValue());
        Assert.AreEqual(endYear, end.IntValue());
    }

    static async Task<T> WaitFor<T>(
        Func<T> read,
        Func<T, bool> predicate,
        string message)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(10);
        T value;
        do
        {
            value = read();
            if (predicate(value))
                return value;
            await Task.Delay(20);
        }
        while (DateTime.UtcNow < deadline);

        Assert.Fail(message);
        return value;
    }
}
