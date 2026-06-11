namespace Microsoft.AndroidX.Compose.Maui.Sample.Pages;

/// <summary>
/// Image-sources demo — one row per <see cref="ImageSource"/> subtype
/// (<see cref="FileImageSource"/>, <see cref="UriImageSource"/>,
/// <see cref="StreamImageSource"/>, <see cref="FontImageSource"/>) so
/// every code path through
/// <see cref="Microsoft.AndroidX.Compose.Maui.Handlers.ImageHandler"/>
/// is exercised in one place.
/// </summary>
public partial class ImageSourcesPage : ContentPage
{
    /// <summary>Build the page.</summary>
    public ImageSourcesPage()
    {
        InitializeComponent();

        // StreamImageSource: open `dotnet_bot.png` from the app-package
        // raw assets folder. The factory is invoked by MAUI's
        // StreamImageSourceService each time the source is realised.
        StreamImage.Source = ImageSource.FromStream(
            ct => FileSystem.OpenAppPackageFileAsync("dotnet_bot.png"));
    }
}
