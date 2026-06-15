namespace Microsoft.AndroidX.Compose.Maui.Sample.Pages;

/// <summary>
/// Entries demo — exercises every mapper on
/// <see cref="Microsoft.AndroidX.Compose.Maui.Handlers.EntryHandler"/>:
/// Text (with TextChanged echo), Placeholder, IsPassword, Keyboard
/// (Numeric / Email / Url / Telephone), TextColor + Font, IsReadOnly,
/// and HorizontalLayoutAlignment.
/// </summary>
public partial class EntriesPage : ContentPage
{
    /// <summary>Build the page.</summary>
    public EntriesPage()
    {
        InitializeComponent();
    }

    void OnNameTextChanged(object? sender, TextChangedEventArgs e)
    {
        GreetingLabel.Text = string.IsNullOrWhiteSpace(e.NewTextValue)
            ? "Greeting will appear here."
            : $"Hello, {e.NewTextValue}!";
    }

    void OnMaxLengthChanged(object? sender, TextChangedEventArgs e)
    {
        var text = e.NewTextValue ?? string.Empty;
        MaxLengthLabel.Text = text.Length >= 8
            ? $"At cap ({text.Length}/8) — extra typing rejected."
            : $"Length: {text.Length}/8";
    }

    void OnCaretToStart(object? sender, EventArgs e)
    {
        CursorEntry.CursorPosition  = 0;
        CursorEntry.SelectionLength = 0;
        UpdateCursorEcho();
    }

    void OnCaretToEnd(object? sender, EventArgs e)
    {
        var len = CursorEntry.Text?.Length ?? 0;
        CursorEntry.CursorPosition  = len;
        CursorEntry.SelectionLength = 0;
        UpdateCursorEcho();
    }

    void OnSelectQuick(object? sender, EventArgs e)
    {
        var text = CursorEntry.Text ?? string.Empty;
        var idx = text.IndexOf("quick", StringComparison.Ordinal);
        if (idx < 0) return;
        CursorEntry.CursorPosition  = idx;
        CursorEntry.SelectionLength = "quick".Length;
        UpdateCursorEcho();
    }

    void UpdateCursorEcho()
    {
        var len = CursorEntry.Text?.Length ?? 0;
        CursorEcho.Text =
            $"Caret: {CursorEntry.CursorPosition} / {len}  Selection length: {CursorEntry.SelectionLength}";
    }
}
