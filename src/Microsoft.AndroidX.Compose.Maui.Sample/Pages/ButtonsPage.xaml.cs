namespace Microsoft.AndroidX.Compose.Maui.Sample.Pages;

/// <summary>
/// Buttons demo — exercises every mapper on
/// <see cref="Microsoft.AndroidX.Compose.Maui.Handlers.ButtonHandler"/>:
/// Text, TextColor, Background (container), and HorizontalLayoutAlignment.
/// All buttons share a single Clicked handler that updates a label, so
/// the page also confirms multiple Compose-backed buttons can coexist
/// in one VerticalStackLayout.
/// </summary>
public partial class ButtonsPage : ContentPage
{
    int _clicks;

    /// <summary>Build the page.</summary>
    public ButtonsPage()
    {
        InitializeComponent();
    }

    void OnButtonClicked(object? sender, EventArgs e)
    {
        _clicks++;
        var label = (sender as Button)?.Text ?? "(no text)";
        ClickCountLabel.Text =
            $"Last click: \"{label}\" ({_clicks} total)";
    }
}
