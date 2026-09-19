using Android.Content;
using AndroidX.Activity;
using AndroidX.Compose.Material3;
using static AndroidX.Compose.Composables;

namespace AndroidX.Compose.Samples.JetNews;

/// <summary>
/// JetNews host activity. Subclasses <see cref="ComponentActivity"/>,
/// remembers app-wide state (nav controller, current route, bookmark
/// set, topic/people/publication selections, interests tab index),
/// then hands off to <see cref="JetnewsApp.Content"/>.
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
        this.SetContent(() =>
        {
            var nav                  = Remember(() => new NavController());
            var currentRoute         = MutableStateOf(Routes.Home);
            var drawerState          = Remember(() => new DrawerStateHolder(DrawerValue.Closed));
            // Acquire bookmarks from the activity's ViewModelStore so
            // the toggled set survives configuration change AND is
            // shared across nav destinations. ViewModel<T> reads
            // LocalViewModelStoreOwner from the active
            // composition; the root SetContent body sees the host
            // ComponentActivity, so this VM is activity-scoped.
            var bookmarks            = ViewModel(() => new BookmarksViewModel());
            var selectedTopics       = Remember(() => new MutableStateList<string>());
            var selectedPeople       = Remember(() => new MutableStateList<string>());
            var selectedPublications = Remember(() => new MutableStateList<string>());
            var interestsTab         = MutableStateOf(0);
            var snackbars            = Remember(() => new SnackbarController());
            JetnewsApp.Content(
                nav,
                currentRoute,
                drawerState,
                bookmarks,
                selectedTopics,
                selectedPeople,
                selectedPublications,
                interestsTab,
                snackbars,
                onOpenLink: url => OpenLink(url, snackbars),
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
            using var send = new Intent(Intent.ActionSend);
            send.SetType("text/plain");
            send.PutExtra(Intent.ExtraSubject, post.Title);
            send.PutExtra(
                Intent.ExtraText,
                $"{post.Title}\nhttps://developer.android.com/jetnews/post/{post.Id}");

            // Wrap in a chooser so the user always sees the system
            // picker rather than a possibly-stale default share target.
            using var chooser = Intent.CreateChooser(
                send,
                GetString(Resource.String.post_share_article));
            StartActivity(chooser);
        }
        catch (ActivityNotFoundException)
        {
            snackbars.Show(GetString(Resource.String.post_share_no_handler));
        }
    }

    void OpenLink(string url, SnackbarController snackbars)
    {
        if (!System.Uri.TryCreate(url, UriKind.Absolute, out _))
        {
            snackbars.Show(GetString(Resource.String.post_link_invalid));
            return;
        }

        using var uri = Android.Net.Uri.Parse(url)
            ?? throw new InvalidOperationException($"Android could not parse article URL '{url}'.");
        using var intent = new Intent(Intent.ActionView, uri);
        try
        {
            StartActivity(intent);
        }
        catch (ActivityNotFoundException)
        {
            snackbars.Show(GetString(Resource.String.post_link_no_handler));
        }
    }
}
