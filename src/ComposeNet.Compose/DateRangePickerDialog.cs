using AndroidX.Compose.Runtime;

namespace ComposeNet;

/// <summary>
/// Material 3 dialog host for a <see cref="DateRangePicker"/>. The
/// underlying Kotlin composable is the same
/// <c>DatePickerDialog_androidKt.DatePickerDialog</c> used by the
/// single-date <see cref="DatePickerDialog"/>; only the body changes.
/// Place a <see cref="DateRangePicker"/> (or any composable) inside
/// <see cref="Body"/>.
/// </summary>
public sealed class DateRangePickerDialog : ComposableNode
{
    readonly System.Action _onDismissRequest;
    public DateRangePickerDialog(System.Action onDismissRequest) => _onDismissRequest = onDismissRequest;

    /// <summary>Required: the affirmative button (no Kotlin default).</summary>
    public ComposableNode? ConfirmButton { get; set; }

    /// <summary>Optional: secondary button.</summary>
    public ComposableNode? DismissButton { get; set; }

    /// <summary>Required: dialog body — typically a <see cref="DateRangePicker"/>.</summary>
    public ComposableNode? Body { get; set; }

    internal override void Render(IComposer composer)
    {
        if (ConfirmButton is null)
            throw new System.InvalidOperationException(
                "DateRangePickerDialog.ConfirmButton is required.");
        if (Body is null)
            throw new System.InvalidOperationException(
                "DateRangePickerDialog.Body is required (the dialog's content slot).");

        var onDismiss = new ComposableLambda0(_onDismissRequest);
        var confirm   = ComposableLambdas.Wrap2(composer, c => ConfirmButton.Render(c));
        var content   = ComposableLambdas.Wrap3(composer, c => Body.Render(c));
        var dismiss = DismissButton is null ? null
            : ComposableLambdas.Wrap2(composer, c => DismissButton.Render(c));

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
