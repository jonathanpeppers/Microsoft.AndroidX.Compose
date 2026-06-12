namespace Microsoft.AndroidX.Compose.Maui.Sample.Pages;

/// <summary>
/// Indicator demo — exercises
/// <see cref="Microsoft.AndroidX.Compose.Maui.Handlers.IndicatorViewHandler"/>:
/// MAUI <see cref="IndicatorView"/> rendered as a Compose
/// <c>Row</c> of dot tiles. The Prev / Next buttons cycle
/// <see cref="IndicatorView.Position"/>; Toggle shape flips between
/// <see cref="IndicatorShape.Circle"/> and
/// <see cref="IndicatorShape.Square"/>.
/// </summary>
public partial class IndicatorPage : ContentPage
{
    static readonly string[] s_labels =
    {
        "Apple", "Banana", "Cherry", "Date", "Elderberry",
    };

    /// <summary>Build the page.</summary>
    public IndicatorPage()
    {
        InitializeComponent();
        UpdateLabel();
    }

    void OnPrev(object? sender, EventArgs e)
    {
        Indicator.Position = (Indicator.Position - 1 + s_labels.Length) % s_labels.Length;
        UpdateLabel();
    }

    void OnNext(object? sender, EventArgs e)
    {
        Indicator.Position = (Indicator.Position + 1) % s_labels.Length;
        UpdateLabel();
    }

    void OnToggleShape(object? sender, EventArgs e)
    {
        Indicator.IndicatorsShape = Indicator.IndicatorsShape == IndicatorShape.Circle
            ? IndicatorShape.Square
            : IndicatorShape.Circle;
    }

    void UpdateLabel() =>
        PositionLabel.Text = $"Position: {Indicator.Position} ({s_labels[Indicator.Position]})";
}
