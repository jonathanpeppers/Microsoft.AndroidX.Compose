using Android.OS;
using Android.Views;
using AndroidX.Activity;
using AndroidX.Compose;

namespace MyApplication;

/// <summary>The application's Compose host activity.</summary>
[Activity(
    Label = "@string/app_name",
    MainLauncher = true,
    Exported = true,
    Theme = "@style/Theme.MyApplication",
    WindowSoftInputMode = SoftInput.AdjustResize)]
public class MainActivity : ComponentActivity
{
    /// <inheritdoc/>
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        this.EnableEdgeToEdge();
        this.SetContent(App.Build);
    }
}
