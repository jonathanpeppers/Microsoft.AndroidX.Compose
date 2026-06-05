namespace ComposeNet;

/// <summary>
/// Material 3 <c>AlertDialog</c>. The canonical multi-slot facade —
/// <see cref="ConfirmButton"/> is required, and any of
/// <see cref="DismissButton"/>, <see cref="Icon"/>, <see cref="Title"/>,
/// and <see cref="Text"/> may be supplied via C# object-initializer
/// syntax. Slots left <c>null</c> are reported as defaulted through the
/// <c>$default</c> bitmask so Compose substitutes the Kotlin defaults.
///
/// <code>
/// new AlertDialog(onDismissRequest: () => show.Value = false)
/// {
///     Title         = new Text("Confirm?"),
///     Text          = new Text("This cannot be undone."),
///     ConfirmButton = new Button(onClick: ...) { new Text("OK") },
///     DismissButton = new Button(onClick: ...) { new Text("Cancel") },
/// }
/// </code>
/// </summary>
public sealed partial class AlertDialog;
