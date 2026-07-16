using Android.Runtime;
using AndroidX.Compose;
using AndroidX.Compose.Material3;
using AndroidX.Compose.Runtime;

namespace Microsoft.AndroidX.Compose.DeviceTests;

sealed class DateRangeStateBinder(DateRangePickerState state) : ComposableNode
{
    public override void Render(IComposer composer)
    {
        if (state.Jvm is not null)
            return;

        var handle = ComposeBridges.RememberDateRangePickerState(
            state.RememberSelectedStartDateMillis,
            state.RememberSelectedEndDateMillis,
            state.RememberDisplayedMonthMillis,
            state.InitialYearRange,
            state.InitialDisplayMode,
            state.InitialSelectableDates,
            composer);
        var jvm = Java.Lang.Object.GetObject<IDateRangePickerState>(
            handle,
            JniHandleOwnership.DoNotTransfer)
            ?? throw new InvalidOperationException(
                "DateRangePickerState Remember bridge returned no state peer.");
        state.BindJvm(jvm);
    }
}
