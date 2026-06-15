namespace Microsoft.AndroidX.Compose.Maui.Sample.Pages;

/// <summary>
/// Shapes demo — drops every concrete <c>Microsoft.Maui.Controls.Shapes</c>
/// subclass into a Compose-backed page to verify the
/// <see cref="AndroidX.Compose.AndroidView"/> fallback path round-trips
/// MAUI's stock <c>ShapeViewHandler</c> + <c>MauiShapeView</c>. No
/// code-behind — the demo intentionally stays declarative so the visuals
/// stand or fall on the fallback alone.
/// </summary>
public partial class ShapesPage : ContentPage
{
    /// <summary>Build the page.</summary>
    public ShapesPage()
    {
        InitializeComponent();
    }
}
