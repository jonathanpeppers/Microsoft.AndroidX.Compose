namespace Microsoft.AndroidX.Compose.Maui.Sample.Pages;

/// <summary>
/// Multi-line editor demo — exercises every mapper on
/// <see cref="Microsoft.AndroidX.Compose.Maui.Handlers.EditorHandler"/>.
/// Keeps a live word + character count label in sync via
/// <see cref="Editor.TextChanged"/>.
/// </summary>
public partial class EditorPage : ContentPage
{
    /// <summary>Build the page.</summary>
    public EditorPage()
    {
        InitializeComponent();
    }

    void OnNoteTextChanged(object? sender, TextChangedEventArgs e)
    {
        var text  = e.NewTextValue ?? string.Empty;
        var words = text.Split(
            new[] { ' ', '\t', '\r', '\n' },
            StringSplitOptions.RemoveEmptyEntries).Length;
        WordCountLabel.Text = $"Words: {words}   Characters: {text.Length}";
    }

    void OnEditorCaretStart(object? sender, EventArgs e)
    {
        CursorEditor.CursorPosition  = 0;
        CursorEditor.SelectionLength = 0;
        UpdateEditorCursorEcho();
    }

    void OnEditorCaretEnd(object? sender, EventArgs e)
    {
        var len = CursorEditor.Text?.Length ?? 0;
        CursorEditor.CursorPosition  = len;
        CursorEditor.SelectionLength = 0;
        UpdateEditorCursorEcho();
    }

    void OnEditorSelectQuick(object? sender, EventArgs e)
    {
        var text = CursorEditor.Text ?? string.Empty;
        var idx = text.IndexOf("quick", StringComparison.Ordinal);
        if (idx < 0) return;
        CursorEditor.CursorPosition  = idx;
        CursorEditor.SelectionLength = "quick".Length;
        UpdateEditorCursorEcho();
    }

    void UpdateEditorCursorEcho() =>
        EditorCursorEcho.Text =
            $"Caret: {CursorEditor.CursorPosition}  Selection length: {CursorEditor.SelectionLength}";
}
