using System.Globalization;

namespace Microsoft.AndroidX.Compose.Maui.Sample.Pages;

/// <summary>
/// Sliders &amp; steppers demo — exercises
/// <see cref="Microsoft.AndroidX.Compose.Maui.Handlers.SliderHandler"/>
/// and <see cref="Microsoft.AndroidX.Compose.Maui.Handlers.StepperHandler"/>.
/// Two sliders (default + colored, custom bounds) drive bound labels;
/// a stepper drives a third label and a reset button restores the
/// configured starting state. The page deliberately mixes
/// Compose-backed leaves (Slider / Stepper / Button / Label) with the
/// stock MAUI <c>HorizontalStackLayout</c> + <c>VerticalStackLayout</c>
/// + <c>ScrollView</c> handlers (themselves Compose-backed) so the
/// composition is a single nested tree under the page's <c>ComposeView</c>.
/// </summary>
public partial class SlidersPage : ContentPage
{
    /// <summary>Build the page.</summary>
    public SlidersPage()
    {
        InitializeComponent();
    }

    void OnDefaultSliderChanged(object? sender, ValueChangedEventArgs e) =>
        DefaultValueLabel.Text =
            $"value = {e.NewValue.ToString("0.00", CultureInfo.InvariantCulture)}";

    void OnColoredSliderChanged(object? sender, ValueChangedEventArgs e) =>
        ColoredValueLabel.Text =
            $"value = {e.NewValue.ToString("0.0", CultureInfo.InvariantCulture)}";

    void OnThumbImageSliderChanged(object? sender, ValueChangedEventArgs e) =>
        ThumbImageValueLabel.Text =
            $"value = {e.NewValue.ToString("0", CultureInfo.InvariantCulture)}";

    void OnStepperChanged(object? sender, ValueChangedEventArgs e) =>
        StepperValueLabel.Text =
            e.NewValue.ToString("0", CultureInfo.InvariantCulture);

    void OnResetClicked(object? sender, EventArgs e)
    {
        DefaultSlider.Value     = 0.25;
        ColoredSlider.Value     = 0;
        ThumbImageSlider.Value  = 50;
        DemoStepper.Value       = 6;
    }
}
