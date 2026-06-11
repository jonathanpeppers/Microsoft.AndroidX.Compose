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
}
