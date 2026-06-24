namespace Microsoft.AndroidX.Compose.Maui.Sample.Pages;

/// <summary>
/// Demo page exercising
/// <see cref="Microsoft.Maui.Controls.ScrollView.HorizontalScrollBarVisibility"/>
/// and
/// <see cref="Microsoft.Maui.Controls.ScrollView.VerticalScrollBarVisibility"/>.
/// Three vertical <see cref="Microsoft.Maui.Controls.ScrollView"/>s
/// side-by-side cycle through Default / Always / Never; one horizontal
/// strip exercises the bottom-edge overlay.
/// </summary>
/// <remarks>
/// See <see cref="Microsoft.AndroidX.Compose.Maui.Platform.ScrollbarOverlayDrawCallback"/>
/// for the drawn-overlay limitations
/// (Default ≡ Always, no auto-hide, thumb-only).
/// </remarks>
public partial class ScrollBarVisibilityPage : ContentPage
{
    /// <summary>Inflate the XAML.</summary>
    public ScrollBarVisibilityPage() => InitializeComponent();
}
