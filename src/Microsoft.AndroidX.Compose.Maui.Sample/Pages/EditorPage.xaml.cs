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
}
