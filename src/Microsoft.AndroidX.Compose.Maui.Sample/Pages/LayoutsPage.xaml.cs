namespace Microsoft.AndroidX.Compose.Maui.Sample.Pages;

/// <summary>
/// Layouts demo — hosts a <c>Grid</c>, <c>AbsoluteLayout</c>, and
/// <c>FlexLayout</c> inside a Compose-backed page. None of those three
/// layout types are registered as <c>IComposeHandler</c>, so the demo
/// verifies the <see cref="AndroidX.Compose.AndroidView"/> fallback in
/// <see cref="Microsoft.AndroidX.Compose.Maui.ComposeWalker"/> round-trips
/// the stock <c>LayoutHandler</c> + <c>LayoutViewGroup</c>.
/// </summary>
public partial class LayoutsPage : ContentPage
{
    /// <summary>Build the page.</summary>
    public LayoutsPage()
    {
        InitializeComponent();
    }
}
