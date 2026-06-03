using AndroidX.Compose.Runtime;

namespace ComposeNet;

/// <summary>
/// Material 3 <c>DatePickerDialog</c>. The <c>DatePickerState</c>
/// builder (<c>rememberDatePickerState-EU0dCGE</c>) is mangled and
/// stripped from the binding, so we resolve it through JNI inside the
/// <see cref="DatePicker"/> facade. Place a <see cref="DatePicker"/>
/// (or any composable) inside <see cref="Body"/>.
/// </summary>
public sealed class DatePickerDialog : ComposableNode
{
    readonly System.Action _onDismissRequest;
    public DatePickerDialog(System.Action onDismissRequest) => _onDismissRequest = onDismissRequest;

    /// <summary>Required: the affirmative button (no Kotlin default).</summary>
    public ComposableNode? ConfirmButton { get; set; }

    /// <summary>Optional: secondary button.</summary>
    public ComposableNode? DismissButton { get; set; }

    /// <summary>Required: dialog body — typically a <see cref="DatePicker"/>.</summary>
    public ComposableNode? Body { get; set; }

    internal override void Render(IComposer composer)
    {
        if (ConfirmButton is null)
            throw new System.InvalidOperationException(
                "DatePickerDialog.ConfirmButton is required.");
        if (Body is null)
            throw new System.InvalidOperationException(
                "DatePickerDialog.Body is required (the dialog's content slot).");

        var onDismiss = new ComposableLambda0(_onDismissRequest);
        var confirm   = new ComposableLambda2(c => ConfirmButton.Render(c));
        var content   = new ComposableLambda3(c => Body.Render(c));
        ComposableLambda2? dismiss = DismissButton is null ? null
            : new ComposableLambda2(c => DismissButton.Render(c));

        int defaults = (int)DatePickerDialogDefault.All;
        var modifier = BuildModifier();
        if (modifier is not null) defaults &= ~(int)DatePickerDialogDefault.Modifier;
        if (dismiss  is not null) defaults &= ~(int)DatePickerDialogDefault.DismissButton;

        ComposeBridges.DatePickerDialog(
            onDismissRequest: onDismiss,
            confirmButton:    confirm,
            modifier:         modifier,
            dismissButton:    dismiss,
            content:          content,
            defaults:         defaults,
            composer:         composer);
    }
}
