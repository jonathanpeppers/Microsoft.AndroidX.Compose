using AndroidX.Compose.Runtime;

namespace ComposeNet;

/// <summary>
/// Material 3 <c>AlertDialog</c>. The first composable in the facade to
/// use <em>named slot properties</em> instead of a single collection
/// initializer: <see cref="ConfirmButton"/> is required, and any of
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
///
/// This shape is the template that <c>ModalBottomSheet</c>,
/// <c>DatePickerDialog</c>, <c>TimePickerDialog</c>, and the tooltip
/// composables will follow once their <c>*State</c> bridges land.
/// </summary>
public sealed class AlertDialog : ComposableNode
{
    readonly System.Action _onDismissRequest;
    public AlertDialog(System.Action onDismissRequest) => _onDismissRequest = onDismissRequest;

    /// <summary>Required: the affirmative-action button (typically <see cref="ComposeNet.Button"/>).</summary>
    public ComposableNode? ConfirmButton { get; set; }

    /// <summary>Optional: secondary button rendered alongside <see cref="ConfirmButton"/>.</summary>
    public ComposableNode? DismissButton { get; set; }

    /// <summary>Optional: leading icon shown above the title.</summary>
    public ComposableNode? Icon { get; set; }

    /// <summary>Optional: dialog title.</summary>
    public ComposableNode? Title { get; set; }

    /// <summary>Optional: dialog body / supporting text.</summary>
    public ComposableNode? Text { get; set; }

    internal override void Render(IComposer composer)
    {
        if (ConfirmButton is null)
            throw new System.InvalidOperationException(
                "AlertDialog.ConfirmButton is required (the Kotlin parameter has no default).");

        var onDismiss = new ComposableLambda0(_onDismissRequest);
        var confirm   = new ComposableLambda2(c => ConfirmButton.Render(c));

        ComposableLambda2? dismissBtn = DismissButton is null ? null
            : new ComposableLambda2(c => DismissButton.Render(c));
        ComposableLambda2? icon = Icon is null ? null
            : new ComposableLambda2(c => Icon.Render(c));
        ComposableLambda2? title = Title is null ? null
            : new ComposableLambda2(c => Title.Render(c));
        ComposableLambda2? text = Text is null ? null
            : new ComposableLambda2(c => Text.Render(c));

        // Start from "default everything" and clear the bit for each
        // optional slot the user actually supplied.
        int defaults = (int)AlertDialogDefault.All;
        var modifier = BuildModifier();
        if (modifier   is not null) defaults &= ~(int)AlertDialogDefault.Modifier;
        if (dismissBtn is not null) defaults &= ~(int)AlertDialogDefault.DismissButton;
        if (icon       is not null) defaults &= ~(int)AlertDialogDefault.Icon;
        if (title      is not null) defaults &= ~(int)AlertDialogDefault.Title;
        if (text       is not null) defaults &= ~(int)AlertDialogDefault.Text;

        ComposeBridges.AlertDialog(
            onDismissRequest: onDismiss,
            confirmButton:    confirm,
            modifier:         modifier,
            dismissButton:    dismissBtn,
            icon:             icon,
            title:            title,
            text:             text,
            defaults:         defaults,
            composer:         composer);
    }
}
