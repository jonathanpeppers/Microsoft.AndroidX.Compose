using Android.OS;
using AndroidX.Compose.Material3;
using ComposeNet;

namespace ComposeNet.Samples.Jetchat;

[Activity(
    Label = "@string/app_name",
    MainLauncher = true,
    Theme = "@android:style/Theme.Material.Light.NoActionBar")]
public class MainActivity : ComposeActivity
{
    // Two-channel seed. Authors / timestamps are written specifically
    // for this port (not lifted from upstream) so the conversation
    // exercises the bubble + avatar + streak logic without copying
    // any creative copy from the original sample.
    static ConversationUiState.ChannelState[] BuildSeedChannels() => new[]
    {
        new ConversationUiState.ChannelState("composers", 42, new[]
        {
            new Message("Aubrey", "Welcome to #composers!",                              "12 min ago", Resource.Drawable.avatar_aubrey),
            new Message("Taylor", "Glad to be here. What's everyone working on?",       "10 min ago", Resource.Drawable.avatar_taylor),
            new Message("Aubrey", "Just shipped a Material 3 update.",                   "9 min ago",  Resource.Drawable.avatar_aubrey),
            new Message("Jordan", "I'm porting the Jetchat sample to .NET.",             "7 min ago",  Resource.Drawable.avatar_jordan),
            new Message("Taylor", "Does Compose for .NET handle weight modifiers?",      "6 min ago",  Resource.Drawable.avatar_taylor),
            new Message("Jordan", "Yep — Modifier.Weight() landed; this row uses it.",   "5 min ago",  Resource.Drawable.avatar_jordan),
        }),
        new ConversationUiState.ChannelState("droidcon-nyc", 18, new[]
        {
            new Message("Jordan", "Anyone heading to droidcon NYC?",                     "2 h ago",    Resource.Drawable.avatar_jordan),
            new Message("Taylor", "I'll be there Thursday.",                             "1 h ago",    Resource.Drawable.avatar_taylor),
            new Message("Aubrey", "Same — let's grab coffee between sessions.",          "45 min ago", Resource.Drawable.avatar_aubrey),
        }),
    };

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        SetContent(() =>
        {
            var ui               = Remember(() => new ConversationUiState(BuildSeedChannels(), initialChannel: "composers"));
            var input            = Remember(() => new MutableState<string>(""));
            var drawerScroll     = Remember(() => new ScrollState());
            var drawerState      = Remember(() => new DrawerStateHolder(DrawerValue.Closed));
            var selectedSelector = Remember(() => new MutableState<int>(0));
            var popupOpen        = Remember(() => new MutableState<bool>(false));
            return Conversation.Build(ui, input, drawerScroll, drawerState, selectedSelector, popupOpen);
        });
    }
}
