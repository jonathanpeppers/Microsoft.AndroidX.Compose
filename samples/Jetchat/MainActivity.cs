using Android.OS;
using ComposeNet;

namespace ComposeNet.Samples.Jetchat;

[Activity(
    Label = "@string/app_name",
    MainLauncher = true,
    Theme = "@android:style/Theme.Material.Light.NoActionBar")]
public class MainActivity : ComposeActivity
{
    // Seed data lifted from the upstream Jetchat sample, condensed.
    static readonly Message[] SeedMessages =
    {
        new("Aubrey",  "Welcome to #composers!",                                                "8 min ago", "👩"),
        new("Taylor",  "Glad to be here. What are folks working on?",                           "6 min ago", "🧑"),
        new("Aubrey",  "Just shipped a Material 3 update — finally have proper top app bars.",  "5 min ago", "👩"),
        new("Jordan",  "Nice. I'm porting the Jetchat sample to .NET — it's coming together.", "3 min ago", "🧔"),
        new("Taylor",  "Ha, meta. Does Compose for .NET handle weight modifiers?",              "2 min ago", "🧑"),
        new("Jordan",  "Yep, just landed Modifier.Weight() — that's how this input row works.", "1 min ago", "🧔"),
    };

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        SetContent(() =>
        {
            var ui    = Remember(() => new ConversationUiState(
                channelName: "composers",
                channelMembers: 42,
                initial: SeedMessages));
            var input = Remember(() => new MutableState<string>(""));
            return Conversation.Build(ui, input);
        });
    }
}
