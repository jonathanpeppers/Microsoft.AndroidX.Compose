namespace ComposeNet;

/// <summary>
/// Material 3 <c>TimePicker</c>. The generator wires the
/// <c>rememberTimePickerState(initialHour, initialMinute, is24Hour, composer)</c>
/// round-trip and auto-creates a <see cref="TimePickerState"/> wrapper
/// when the caller leaves the <c>state</c> ctor argument null, so reading
/// <see cref="TimePickerState.Hour"/> / <see cref="TimePickerState.Minute"/>
/// from a button callback Just Works without manual <c>remember</c> plumbing.
/// </summary>
public sealed partial class TimePicker;
