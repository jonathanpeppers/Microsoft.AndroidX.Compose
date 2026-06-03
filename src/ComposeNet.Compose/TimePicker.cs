using Android.Runtime;
using AndroidX.Compose.Material3;
using AndroidX.Compose.Runtime;

namespace ComposeNet;

/// <summary>
/// Material 3 <c>TimePicker</c>. Resolves <c>TimePickerState</c> via
/// raw JNI (<c>rememberTimePickerState</c> takes a <see cref="IComposer"/>
/// so it requires the composer-aware bridge). Pass an explicit
/// <see cref="ComposeNet.TimePickerState"/> to read the picked
/// hour/minute from a button callback.
/// </summary>
public sealed class TimePicker : ComposableNode
{
    readonly TimePickerState _state;
    public TimePicker(TimePickerState? state = null) => _state = state ?? new TimePickerState();

    internal override void Render(IComposer composer)
    {
        var stateHandle = ComposeBridges.RememberTimePickerState(_state.InitialHour, _state.InitialMinute, _state.InitialIs24Hour, composer);
        if (_state.Jvm is null)
            _state.Jvm = Java.Lang.Object.GetObject<ITimePickerState>(stateHandle, JniHandleOwnership.DoNotTransfer)!;
        var modifier = BuildModifier();
        int defaults = (int)TimePickerDefault.All;
        if (modifier is not null) defaults &= ~(int)TimePickerDefault.Modifier;
        ComposeBridges.TimePicker(stateHandle, modifier, defaults, composer);
    }
}
