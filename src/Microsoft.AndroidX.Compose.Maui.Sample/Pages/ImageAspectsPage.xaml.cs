namespace Microsoft.AndroidX.Compose.Maui.Sample.Pages;

/// <summary>
/// Image-aspects demo — same image rendered with every
/// <see cref="Aspect"/> enum value so the cropping / letterbox /
/// stretch difference between
/// <see cref="Aspect.AspectFit"/>, <see cref="Aspect.AspectFill"/>,
/// <see cref="Aspect.Fill"/>, and <see cref="Aspect.Center"/>
/// is visible side-by-side.
/// </summary>
public partial class ImageAspectsPage : ContentPage
{
    /// <summary>Build the page.</summary>
    public ImageAspectsPage()
    {
        InitializeComponent();
    }
}
