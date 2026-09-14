namespace AndroidX.Compose;

/// <summary>
/// Foundation text editor without Material chrome. The TextFieldValue overload
/// exposes selection and IME composition; the string overload leaves that editing
/// state with Foundation while reporting text changes. Supply a decoration to wrap the native
/// inner editor, rendering it exactly once. Unset styling and keyboard properties
/// use Foundation defaults, not Material theme defaults.
/// </summary>
public sealed partial class BasicTextField;
