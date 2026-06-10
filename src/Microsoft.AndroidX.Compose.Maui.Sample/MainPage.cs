namespace Microsoft.AndroidX.Compose.Maui.Sample;

/// <summary>
/// One-page smoke test: a <see cref="Label"/> and a counter <see cref="Button"/>
/// both rendered by Compose-backed handlers. Tapping the button updates the
/// label text through the standard MAUI property pipeline.
/// </summary>
public class MainPage : ContentPage
{
    int _count;
    readonly Label _countLabel;

    /// <summary>Build the page.</summary>
    public MainPage()
    {
        Title = "AndroidX.Compose backend";

        _countLabel = new Label
        {
            Text       = "Tapped 0 times",
            TextColor  = Colors.Black,
            FontSize   = 20,
        };

        var button = new Button { Text = "Tap me" };
        button.Clicked += (_, _) =>
        {
            _count++;
            _countLabel.Text = $"Tapped {_count} times";
        };

        Content = new VerticalStackLayout
        {
            Padding = 24,
            Spacing = 16,
            Children =
            {
                new Label
                {
                    Text     = "MAUI Label + Button via Jetpack Compose",
                    FontSize = 16,
                },
                _countLabel,
                button,
            },
        };
    }
}
