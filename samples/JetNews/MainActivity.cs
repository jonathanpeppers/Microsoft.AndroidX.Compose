using Android.OS;
using AndroidX.Compose.Runtime;
using ComposeNet;

namespace ComposeNet.Samples.JetNews;

/// <summary>
/// JetNews host activity. Subclasses <see cref="ComposeActivity"/>,
/// remembers app-wide state (nav controller, current route, bookmark
/// set, topic/people/publication selections, interests tab index),
/// then hands off to <see cref="JetnewsApp.Build"/>.
/// </summary>
[Activity(
    Label        = "@string/app_name",
    MainLauncher = true,
    Theme        = "@android:style/Theme.Material.Light.NoActionBar")]
public class MainActivity : ComposeActivity
{
    /// <summary>Build the root composition.</summary>
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        SetContent(() =>
        {
            var nav                  = Remember(() => new NavController());
            var currentRoute         = Remember(() => new MutableState<string>(Routes.Home));
            var bookmarks            = Remember(() => new MutableStateList<string>());
            var selectedTopics       = Remember(() => new MutableStateList<string>());
            var selectedPeople       = Remember(() => new MutableStateList<string>());
            var selectedPublications = Remember(() => new MutableStateList<string>());
            var interestsTab         = Remember(() => new MutableState<int>(0));
            return JetnewsApp.Build(
                nav,
                currentRoute,
                bookmarks,
                selectedTopics,
                selectedPeople,
                selectedPublications,
                interestsTab);
        });
    }
}
