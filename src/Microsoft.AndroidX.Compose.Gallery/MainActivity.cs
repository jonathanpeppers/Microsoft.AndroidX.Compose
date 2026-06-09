using global::Android.Content;
using global::Android.OS;

namespace Microsoft.AndroidX.Compose.Gallery;

/// <summary>
/// The gallery's only activity. Hosts the entire UI under a
/// <see cref="ComposeActivity.SetContent(Func{ComposableNode})"/>
/// call into <see cref="GalleryApp.Build"/> — there's no MVVM, no
/// fragment graph, no XML layout pipeline.
/// </summary>
[Activity(Label = "@string/app_name", MainLauncher = true, LaunchMode = global::Android.Content.PM.LaunchMode.SingleTop, Theme = "@android:style/Theme.Material.Light.NoActionBar")]
public class MainActivity : ComposeActivity
{
    /// <summary>
    /// The active root <see cref="NavController"/>, captured by
    /// <see cref="GalleryApp.Build"/>. Test/automation hook only —
    /// driven via <c>adb am start --es route &lt;route&gt;</c>.
    /// </summary>
    internal static NavController? Nav;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        SetContent(GalleryApp.Build);
        HandleRoute(Intent);
    }

    protected override void OnNewIntent(Intent? intent)
    {
        base.OnNewIntent(intent);
        Intent = intent;
        HandleRoute(intent);
    }

    static void HandleRoute(Intent? intent)
    {
        var route = intent?.GetStringExtra("route");
        if (string.IsNullOrEmpty(route))
            return;
        new Handler(Looper.MainLooper!).PostDelayed(() =>
        {
            try { Nav?.Navigate(route); }
            catch { /* automation hook only */ }
        }, 400);
    }
}
