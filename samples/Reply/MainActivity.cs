using AndroidX.Activity;

namespace AndroidX.Compose.Samples.Reply;

/// <summary>
/// Reply host activity. Subclasses <see cref="ComponentActivity"/>,
/// remembers app-wide state (nav controller, current route, opened
/// email id, multi-select set), then hands off to
/// <see cref="ReplyApp.Build"/>.
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
        this.SetContent(c =>
        {
            var nav              = c.Remember(() => new NavController());
            var currentRoute     = c.MutableStateOf(Route.Inbox);
            var openedEmailId    = c.MutableStateOf(0L);
            var selectedEmailIds = c.Remember(() => new MutableStateList<long>());
            return ReplyApp.Build(nav, currentRoute, openedEmailId, selectedEmailIds);
        });
    }
}
