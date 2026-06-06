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
    // Avatar drawable ids are resolved at JIT time so the field is
    // populated lazily inside OnCreate (Resource.* generation runs
    // during the Android build).
    static Message[] BuildSeedMessages() => new[]
    {
        new Message("Aubrey",  "Welcome to #composers!",                                                 "8 min ago", Resource.Drawable.avatar_aubrey),
        new Message("Taylor",  "Glad to be here. What are folks working on?",                            "6 min ago", Resource.Drawable.avatar_taylor),
        new Message("Aubrey",  "Just shipped a Material 3 update — finally have proper top app bars.",   "5 min ago", Resource.Drawable.avatar_aubrey),
        new Message("Jordan",  "Nice. I'm porting the Jetchat sample to .NET — it's coming together.",   "3 min ago", Resource.Drawable.avatar_jordan),
        new Message("Taylor",  "Ha, meta. Does Compose for .NET handle weight modifiers?",               "2 min ago", Resource.Drawable.avatar_taylor),
        new Message("Jordan",  "Yep, just landed Modifier.Weight() — that's how this input row works.",  "1 min ago", Resource.Drawable.avatar_jordan),
    };

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        SetContent(() =>
        {
            var ui          = Remember(() => new ConversationUiState(
                channelName: "composers",
                channelMembers: 42,
                initial: BuildSeedMessages()));
            var input       = Remember(() => new MutableState<string>(""));
            var drawerScroll = Remember(() => new ScrollState());
            return Conversation.Build(ui, input, drawerScroll);
        });
    }
}
