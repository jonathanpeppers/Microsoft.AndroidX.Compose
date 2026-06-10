using Android.App;
using Android.Content.PM;

namespace Microsoft.AndroidX.Compose.Maui.Sample;

/// <summary>Android entry-point activity for the MAUI sample.</summary>
[Activity(
    Theme              = "@style/Maui.SplashTheme",
    MainLauncher       = true,
    LaunchMode         = LaunchMode.SingleTop,
    ConfigurationChanges =
        ConfigChanges.ScreenSize | ConfigChanges.Orientation |
        ConfigChanges.UiMode     | ConfigChanges.ScreenLayout |
        ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
}
