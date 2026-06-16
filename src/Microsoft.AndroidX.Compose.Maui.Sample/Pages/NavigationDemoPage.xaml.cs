namespace Microsoft.AndroidX.Compose.Maui.Sample.Pages;

/// <summary>
/// Phase 4 Slice 1 launcher. Pushes a modal
/// <see cref="NavigationPage"/> hosting <see cref="NavStackPage"/> so
/// the Compose-backed <see cref="Handlers.NavigationPageHandler"/>
/// renders the chrome instead of stock <c>NavigationViewHandler</c>.
/// </summary>
public partial class NavigationDemoPage : ContentPage
{
    /// <summary>Build the page.</summary>
    public NavigationDemoPage()
    {
        InitializeComponent();
    }

    async void OnLaunchClicked(object? sender, EventArgs e)
    {
        var nav = new NavigationPage(new NavStackPage(depth: 1));
        await Navigation.PushModalAsync(nav);
    }
}
