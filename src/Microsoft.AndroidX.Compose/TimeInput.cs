namespace AndroidX.Compose;

/// <summary>
/// Material 3 <c>TimeInput</c> — the keyboard-entry counterpart to
/// <see cref="TimePicker"/>. Share a single <see cref="TimePickerState"/>
/// across both facades to edit the same native peer. Hoist
/// <c>composer.RememberTimePickerState()</c> above conditional clock/keyboard
/// layouts so their owner and its native save registration survive toggles.
/// </summary>
/// <remarks>
/// As with <see cref="TimePicker"/>, leaving the <c>state</c> ctor
/// argument null auto-creates a fresh <see cref="TimePickerState"/>
/// wrapper. Reading <see cref="TimePickerState.Hour"/> /
/// <see cref="TimePickerState.Minute"/> from a button callback Just
/// Works without manual <c>remember</c> plumbing.
/// </remarks>
public sealed partial class TimeInput;
