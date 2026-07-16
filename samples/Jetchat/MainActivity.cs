using Android.Views;
using AndroidX.Activity;
using AndroidX.Compose.Material3;
using static AndroidX.Compose.Composables;

namespace AndroidX.Compose.Samples.Jetchat;

[Activity(
    Label = "@string/app_name",
    MainLauncher = true,
    Theme = "@android:style/Theme.Material.Light.NoActionBar",
    WindowSoftInputMode = SoftInput.AdjustResize)]
[Android.Runtime.Register("net/compose/samples/jetchat/MainActivity")]
public class MainActivity : ComponentActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        this.EnableEdgeToEdge();
        this.SetContent(() =>
        {
            var ui               = Remember(() => new ConversationUiState("#composers", channelMembers: 42, FakeData.InitialMessages()));
            var input            = MutableStateOf(ComposeExtensions.NewTextFieldValue());
            var selectedMenu     = MutableStateOf("composers");
            var drawerScroll     = Remember(() => new ScrollState());
            var drawerState      = Remember(() => new DrawerStateHolder(DrawerValue.Closed));
            var selectedSelector = MutableStateOf(0);
            var popupOpen        = MutableStateOf(false);
            var messagesScroll   = RememberLazyListState();
            var isRecording      = MutableStateOf(false);
            var swipeOffset      = MutableStateOf(0f);
            var nav              = Remember(() => new NavController());
            var profileViewModel = Remember(() => new ProfileViewModel());
            JetchatApp.Content(
                nav:              nav,
                ui:               ui,
                input:            input,
                selectedMenu:     selectedMenu,
                drawerScroll:     drawerScroll,
                drawerState:      drawerState,
                selectedSelector: selectedSelector,
                popupOpen:        popupOpen,
                messagesScroll:   messagesScroll,
                isRecording:      isRecording,
                swipeOffset:      swipeOffset,
                profileViewModel: profileViewModel);
        });
    }
}
