namespace Microsoft.AndroidX.Compose.Maui.Sample;

/// <summary>Root MAUI application — opens a single window on <see cref="MainPage"/>.</summary>
public class App : Application
{
    /// <inheritdoc/>
    protected override Window CreateWindow(IActivationState? activationState) =>
        new(new NavigationPage(new MainPage()));
}
