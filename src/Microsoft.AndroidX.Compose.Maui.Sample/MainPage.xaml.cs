namespace Microsoft.AndroidX.Compose.Maui.Sample;

/// <summary>
/// One-page smoke test: a <see cref="Label"/> and a counter <see cref="Button"/>
/// both rendered by Compose-backed handlers. Tapping the button updates the
/// button text through the standard MAUI property pipeline.
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
}
