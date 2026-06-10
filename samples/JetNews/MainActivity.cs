using Android.Content;
using AndroidX.Activity;
using AndroidX.Compose.Material3;

namespace AndroidX.Compose.Samples.JetNews;

/// <summary>
/// JetNews host activity. Subclasses <see cref="ComponentActivity"/>,
/// remembers app-wide state (nav controller, current route, bookmark
/// set, topic/people/publication selections, interests tab index),
/// then hands off to <see cref="JetnewsApp.Build"/>.
/// </summary>
[Activity(
    Label        = "@string/app_name",
    MainLauncher = true,
    Theme        = "@android:style/Theme.Material.Light.NoActionBar")]
[Android.Runtime.Register("net/compose/samples/jetnews/MainActivity")]
public class MainActivity : ComponentActivity
{
    /// <summary>Build the root composition.</summary>
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        this.EnableEdgeToEdge();
        this.SetContent(c =>
        {
            var nav                  = c.Remember(() => new NavController());
            var currentRoute         = c.Remember(() => new MutableState<string>(Routes.Home));
            var drawerState          = c.Remember(() => new DrawerStateHolder(DrawerValue.Closed));
            // Acquire bookmarks from the activity's ViewModelStore so
            // the toggled set survives configuration change AND is
            // shared across nav destinations. c.ViewModel<T>
            // reads LocalViewModelStoreOwner from the active
            // composition; the root SetContent body sees the host
            // ComponentActivity, so this VM is activity-scoped.
            var bookmarks            = c.ViewModel(() => new BookmarksViewModel());
            var selectedTopics       = c.Remember(() => new MutableStateList<string>());
            var selectedPeople       = c.Remember(() => new MutableStateList<string>());
            var selectedPublications = c.Remember(() => new MutableStateList<string>());
            var interestsTab         = c.Remember(() => new MutableState<int>(0));
            var snackbars            = c.Remember(() => new SnackbarController());
            return JetnewsApp.Build(
                nav,
                currentRoute,
                drawerState,
                bookmarks,
                selectedTopics,
                selectedPeople,
                selectedPublications,
                interestsTab,
                snackbars,
                onShare: post => SharePost(post, snackbars));
        });
    }

    void SharePost(Post post, SnackbarController snackbars)
    {
        // Build a plain text/plain intent — JetNews has no real article
        // URL today, so the synthetic developer.android.com path stub
        // matches the deep-link format upstream JetNews uses for its
        // app-link demo (see #159's Navigation 3 / DeepLinkPattern bullet).
        try
        {
            var send = new Intent(Intent.ActionSend);
            send.SetType("text/plain");
            send.PutExtra(Intent.ExtraSubject, post.Title);
            send.PutExtra(
                Intent.ExtraText,
                $"{post.Title}\nhttps://developer.android.com/jetnews/post/{post.Id}");

            // Wrap in a chooser so the user always sees the system
            // picker rather than a possibly-stale default share target.
            var chooser = Intent.CreateChooser(send, "Share article");
            StartActivity(chooser);
        }
        catch (ActivityNotFoundException)
        {
            // No share target installed (e.g. minimal emulator image)
            // — surface a snackbar instead of crashing.
            snackbars.Show("No app available to share with");
        }
    }
}
