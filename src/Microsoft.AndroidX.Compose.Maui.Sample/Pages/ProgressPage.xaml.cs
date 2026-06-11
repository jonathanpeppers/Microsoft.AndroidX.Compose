using System.Globalization;

namespace Microsoft.AndroidX.Compose.Maui.Sample.Pages;

/// <summary>
/// Progress &amp; activity demo — exercises
/// <see cref="Microsoft.AndroidX.Compose.Maui.Handlers.ProgressBarHandler"/>
/// and
/// <see cref="Microsoft.AndroidX.Compose.Maui.Handlers.ActivityIndicatorHandler"/>.
/// A driver slider feeds <c>Progress</c> on two ProgressBars (default
/// theme + a tinted one) so the bar tracks the slider in real time;
/// a Button toggles <c>IsRunning</c> on the ActivityIndicator. When
/// <c>IsRunning</c> is <c>false</c> the handler emits an empty
/// Compose <c>Box</c> instead of a <c>CircularProgressIndicator</c>,
/// so the spinner has zero animation cost while paused.
/// </summary>
public partial class ProgressPage : ContentPage
{
    /// <summary>Build the page.</summary>
    public ProgressPage()
    {
        InitializeComponent();
    }

    void OnDriverChanged(object? sender, ValueChangedEventArgs e)
    {
        var fraction = e.NewValue;
        // ProgressBar.Progress is double on MAUI's interface; the
        // Compose handler clamps + downcasts to float.
        DefaultProgress.Progress = fraction;
        ColoredProgress.Progress = fraction;
        ProgressLabel.Text =
            $"progress = {fraction.ToString("0.00", CultureInfo.InvariantCulture)}";
    }

    void OnToggleClicked(object? sender, EventArgs e)
    {
        DemoIndicator.IsRunning = !DemoIndicator.IsRunning;
        ToggleButton.Text = DemoIndicator.IsRunning ? "Stop" : "Start";
        IndicatorStatusLabel.Text = DemoIndicator.IsRunning
            ? "Running (Compose CircularProgressIndicator emitted)"
            : "Stopped (no Compose CircularProgressIndicator in tree)";
    }
}
