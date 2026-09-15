namespace AndroidX.Compose;

/// <summary>
/// Material 3 <c>TimePicker</c>. The generator wires the
/// <c>rememberTimePickerState(initialHour, initialMinute, is24Hour, composer)</c>
/// round-trip and auto-creates a <see cref="TimePickerState"/> wrapper
/// when the caller leaves the <c>state</c> ctor argument null, so reading
/// <see cref="TimePickerState.Hour"/> / <see cref="TimePickerState.Minute"/>
/// from a button callback Just Works without manual <c>remember</c> plumbing.
/// </summary>
/// <remarks>
/// A caller-supplied <see cref="TimePickerState"/> can be shared with
/// <see cref="TimeInput"/>. The first composition location owns the native
/// remember call and continues executing it on recomposition; siblings
/// consume the same peer. For conditional clock/keyboard layouts, call
/// <c>composer.RememberTimePickerState()</c> at their common ancestor and
/// pass its result to both. Keeping that owner outside the condition
/// preserves the peer and native save registration when either picker leaves.
/// </remarks>
public sealed partial class TimePicker;
