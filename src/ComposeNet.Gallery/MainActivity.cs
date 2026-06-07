using Android.OS;

namespace ComposeNet.Gallery;

/// <summary>
/// The gallery's only activity. Hosts the entire UI under a
/// <see cref="ComposeActivity.SetContent(System.Func{ComposableNode})"/>
/// call into <see cref="GalleryApp.Build"/> — there's no MVVM, no
/// fragment graph, no XML layout pipeline.
/// </summary>
[Activity(Label = "@string/app_name", MainLauncher = true, Theme = "@android:style/Theme.Material.Light.NoActionBar")]
public class MainActivity : ComposeActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        SetContent(GalleryApp.Build);
    }
}
