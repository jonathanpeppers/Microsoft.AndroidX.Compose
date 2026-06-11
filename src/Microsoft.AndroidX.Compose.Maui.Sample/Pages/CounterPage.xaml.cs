namespace Microsoft.AndroidX.Compose.Maui.Sample.Pages;

/// <summary>
/// Counter demo — Button + Label scenario from the stock
/// <c>dotnet new maui</c> template. Verifies <c>ButtonHandler</c>'s
/// Clicked event reaches the page and that a subsequent <c>Text</c>
/// write on a Label / Button propagates through the
/// Compose-backed handlers.
/// </summary>
public partial class CounterPage : ContentPage
{
    int _count;

    /// <summary>Build the page.</summary>
    public CounterPage()
    {
        InitializeComponent();
    }

    void OnCounterClicked(object? sender, EventArgs e)
    {
        _count++;

        CounterBtn.Text = _count == 1
            ? $"Clicked {_count} time"
            : $"Clicked {_count} times";

        CountLabel.Text = $"Tapped {_count} {(_count == 1 ? "time" : "times")}";

        SemanticScreenReader.Announce(CounterBtn.Text);
    }
}
