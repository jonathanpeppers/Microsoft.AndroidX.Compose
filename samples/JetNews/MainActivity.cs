using Android.OS;
using AndroidX.Compose.Material3;
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
            var drawerState          = Remember(() => new DrawerStateHolder(DrawerValue.Closed));
            // Acquire bookmarks from the activity's ViewModelStore so
            // the toggled set survives configuration change AND is
            // shared across nav destinations. Compose.ViewModel<T>
            // reads LocalViewModelStoreOwner from the active
            // composition; the root SetContent body sees the host
            // ComponentActivity, so this VM is activity-scoped.
            var bookmarks            = Compose.ViewModel(() => new BookmarksViewModel());
            var selectedTopics       = Remember(() => new MutableStateList<string>());
            var selectedPeople       = Remember(() => new MutableStateList<string>());
            var selectedPublications = Remember(() => new MutableStateList<string>());
            var interestsTab         = Remember(() => new MutableState<int>(0));
            return JetnewsApp.Build(
                nav,
                currentRoute,
                drawerState,
                bookmarks,
                selectedTopics,
                selectedPeople,
                selectedPublications,
                interestsTab);
        });
    }
}
