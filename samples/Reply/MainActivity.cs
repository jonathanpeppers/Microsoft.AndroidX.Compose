using AndroidX.Activity;
using static AndroidX.Compose.Composables;

namespace AndroidX.Compose.Samples.Reply;

/// <summary>
/// Reply host activity. Subclasses <see cref="ComponentActivity"/>,
/// remembers app-wide state (nav controller, current route, opened
/// email id, multi-select set), then hands off to
/// <see cref="ReplyApp.Content"/>.
/// </summary>
[Activity(
    Label        = "@string/app_name",
    MainLauncher = true,
    Theme        = "@android:style/Theme.Material.Light.NoActionBar")]
[Android.Runtime.Register("net/compose/samples/reply/MainActivity")]
public class MainActivity : ComponentActivity
{
    /// <summary>Build the root composition.</summary>
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        this.EnableEdgeToEdge();
        this.SetContent(() =>
        {
            var nav              = Remember(() => new NavController());
            var currentRoute     = MutableStateOf(Route.Inbox);
            var openedEmailId    = MutableStateOf(0L);
            var selectedEmailIds = Remember(() => new MutableStateList<long>());
            ReplyApp.Content(nav, currentRoute, openedEmailId, selectedEmailIds);
        });
    }
}
