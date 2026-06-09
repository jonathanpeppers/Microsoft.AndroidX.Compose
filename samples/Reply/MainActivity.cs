namespace Microsoft.AndroidX.Compose.Samples.Reply;

/// <summary>
/// Reply host activity. Subclasses <see cref="ComposeActivity"/>,
/// remembers app-wide state (nav controller, current route, opened
/// email id, multi-select set), then hands off to
/// <see cref="ReplyApp.Build"/>.
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
            var nav              = Remember(() => new NavController());
            var currentRoute     = Remember(() => new MutableState<string>(Route.Inbox));
            var openedEmailId    = Remember(() => new MutableState<long>(0L));
            var selectedEmailIds = Remember(() => new MutableStateList<long>());
            return ReplyApp.Build(nav, currentRoute, openedEmailId, selectedEmailIds);
        });
    }
}
