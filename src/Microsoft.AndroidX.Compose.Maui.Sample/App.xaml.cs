namespace Microsoft.AndroidX.Compose.Maui.Sample;

/// <summary>Root MAUI application — opens a single window on <see cref="MainPage"/>.</summary>
public partial class App : Application
{
    /// <summary>Construct the app and load merged resource dictionaries.</summary>
    public App()
    {
        InitializeComponent();
    }

    /// <inheritdoc/>
    protected override Window CreateWindow(IActivationState? activationState) =>
        new(new AppShell());
}
