using Android.Runtime;
using AndroidX.Compose.Material3;
using AndroidX.Compose.Runtime;

namespace ComposeNet;

/// <summary>
/// Material 3 <c>DatePicker</c>. The Kotlin <c>DatePickerKt.DatePicker</c>
/// composable IS exposed by the binding, but its <c>$default</c> bitmask
/// param isn't user-visible (the binding drops the trailing
/// <c>$default</c> parameter on @Composable functions), so we go through
/// raw JNI to set it. Place inside <see cref="DatePickerDialog"/>'s body.
/// Pass an explicit <see cref="ComposeNet.DatePickerState"/> to read the
/// selection from a button callback; if none is supplied a fresh state
/// is created internally and the selection is unobservable.
/// </summary>
public sealed class DatePicker : ComposableNode
{
    readonly DatePickerState? _state;
    public DatePicker(DatePickerState? state = null) => _state = state;

    internal override void Render(IComposer composer)
    {
        var stateHandle = ComposeBridges.RememberDatePickerState(composer);
        if (_state is not null && _state.Jvm is null)
            _state.Jvm = Java.Lang.Object.GetObject<IDatePickerState>(stateHandle, JniHandleOwnership.DoNotTransfer)!;
        ComposeBridges.DatePicker(stateHandle, (int)DatePickerDefault.All, composer);
    }
}
