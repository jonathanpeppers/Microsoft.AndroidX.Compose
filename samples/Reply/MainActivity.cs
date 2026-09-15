using AndroidX.Activity;
using static AndroidX.Compose.Composables;

namespace AndroidX.Compose.Samples.Reply;

/// <summary>
/// Reply host activity. Subclasses <see cref="ComponentActivity"/>,
/// remembers the nav controller and restores email context, then hands off to
/// <see cref="ReplyApp.Content"/>.
/// </summary>
[Activity(
    Label        = "@string/app_name",
    MainLauncher = true,
    Theme        = "@android:style/Theme.Material.Light.NoActionBar")]
[Android.Runtime.Register("net/compose/samples/reply/MainActivity")]
public class MainActivity : ComponentActivity
{
    ReplyState? _state;

    /// <summary>Build the root composition.</summary>
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        this.EnableEdgeToEdge();
        var state = new ReplyState(savedInstanceState);
        _state = state;
        this.SetContent(() =>
        {
            var nav = Remember(() => new NavController());
            ReplyApp.Content(nav, state);
        });
    }

    /// <summary>Preserves email context without duplicating the navigator's saved back stack.</summary>
    protected override void OnSaveInstanceState(Bundle outState)
    {
        var state = _state ?? throw new InvalidOperationException("Reply state was not initialized.");
        state.Save(outState);
        base.OnSaveInstanceState(outState);
    }
}
