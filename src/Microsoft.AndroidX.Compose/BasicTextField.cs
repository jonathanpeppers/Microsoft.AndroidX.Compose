namespace AndroidX.Compose;

/// <summary>
/// Foundation text editor without Material chrome. The TextFieldValue overload
/// exposes selection and IME composition; the string overload leaves that editing
/// state with Foundation while reporting text changes. Supply a decoration to wrap the native
/// inner editor, rendering it exactly once. Unset styling and keyboard properties
/// use Foundation defaults, not Material theme defaults.
/// </summary>
/// <remarks>
/// Line limits constrain the visible editor height, not the stored text or the
/// full paragraph layout. Single-line mode disables soft wrapping and overrides
/// both line limits to one; it does not remove hard newlines from the value.
/// As in upstream's legacy string overload, <c>KeyboardOptions.ShowKeyboardOnFocus</c>
/// is not supported by that overload.
/// </remarks>
public sealed partial class BasicTextField;
