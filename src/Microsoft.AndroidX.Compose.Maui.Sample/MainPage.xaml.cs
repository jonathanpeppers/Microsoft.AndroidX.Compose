namespace Microsoft.AndroidX.Compose.Maui.Sample;

/// <summary>
/// One-page smoke test for the Compose-backed MAUI handlers — an
/// <see cref="Image"/>, three <see cref="Label"/>s, three
/// <see cref="Entry"/>s (plain + password + numeric), and a counter
/// <see cref="Button"/> in MAUI Primary. Tapping the button updates
/// its text through the standard MAUI property pipeline; typing in
/// the plain <c>Entry</c> echoes back through the <c>GreetingLabel</c>.
/// </summary>
public partial class MainPage : ContentPage
{
    int _count;

    /// <summary>Build the page.</summary>
    public MainPage()
    {
        InitializeComponent();
    }

    void OnCounterClicked(object? sender, EventArgs e)
    {
        _count++;

        CounterBtn.Text = _count == 1
            ? $"Clicked {_count} time"
            : $"Clicked {_count} times";

        SemanticScreenReader.Announce(CounterBtn.Text);
    }

    void OnNameTextChanged(object? sender, TextChangedEventArgs e)
    {
        GreetingLabel.Text = string.IsNullOrWhiteSpace(e.NewTextValue)
            ? "Greeting will appear here."
            : $"Hello, {e.NewTextValue}!";
    }
}
