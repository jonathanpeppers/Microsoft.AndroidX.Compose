using AndroidX.Compose.Runtime;

namespace ComposeNet;

/// <summary>
/// Material 3 <c>TimePickerDialog</c>. Both <c>ConfirmButton</c> and
/// <c>DismissButton</c> are required by Compose; <see cref="Title"/>
/// and <see cref="ModeToggleButton"/> are optional. Place a
/// <see cref="TimePicker"/> in the body.
/// </summary>
public sealed class TimePickerDialog : ComposableNode
{
    readonly System.Action _onDismissRequest;
    public TimePickerDialog(System.Action onDismissRequest) => _onDismissRequest = onDismissRequest;

    public ComposableNode? ConfirmButton    { get; set; }
    public ComposableNode? DismissButton    { get; set; }
    public ComposableNode? Title            { get; set; }
    public ComposableNode? ModeToggleButton { get; set; }
    /// <summary>Required: dialog body — typically a <see cref="TimePicker"/>.</summary>
    public ComposableNode? Body             { get; set; }

    internal override void Render(IComposer composer)
    {
        if (ConfirmButton is null || DismissButton is null)
            throw new System.InvalidOperationException(
                "TimePickerDialog.ConfirmButton and DismissButton are both required.");
        if (Body is null)
            throw new System.InvalidOperationException(
                "TimePickerDialog.Body is required (the dialog's content slot).");

        var onDismiss = new ComposableLambda0(_onDismissRequest);
        var confirm   = new ComposableLambda2(c => ConfirmButton.Render(c));
        var dismiss   = new ComposableLambda2(c => DismissButton.Render(c));
        var content   = new ComposableLambda3(c => Body.Render(c));
        ComposableLambda2? title = Title is null ? null
            : new ComposableLambda2(c => Title.Render(c));
        ComposableLambda2? toggle = ModeToggleButton is null ? null
            : new ComposableLambda2(c => ModeToggleButton.Render(c));

        int defaults = (int)TimePickerDialogDefault.All;
        if (title  is not null) defaults &= ~(int)TimePickerDialogDefault.Title;
        if (toggle is not null) defaults &= ~(int)TimePickerDialogDefault.ModeToggleButton;

        ComposeBridges.TimePickerDialog(
            onDismissRequest: onDismiss,
            confirmButton:    confirm,
            dismissButton:    dismiss,
            title:            title,
            modeToggleButton: toggle,
            content:          content,
            defaults:         defaults,
            composer:         composer);
    }
}
