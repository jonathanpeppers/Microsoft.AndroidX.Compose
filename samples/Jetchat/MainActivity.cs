using Android.Views;
using AndroidX.Activity;
using AndroidX.Compose.Material3;

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
        this.SetContent(c =>
        {
            var ui               = c.Remember(() => new ConversationUiState("#composers", channelMembers: 42, FakeData.InitialMessages()));
            var input            = c.MutableStateOf(c.NewTextFieldValue());
            var selectedMenu     = c.MutableStateOf("composers");
            var drawerScroll     = c.Remember(() => new ScrollState());
            var drawerState      = c.Remember(() => new DrawerStateHolder(DrawerValue.Closed));
            var selectedSelector = c.MutableStateOf(0);
            var popupOpen        = c.MutableStateOf(false);
            var messagesScroll   = c.RememberLazyListState();
            var isRecording      = c.MutableStateOf(false);
            var swipeOffset      = c.MutableStateOf(0f);
            var nav              = c.Remember(() => new NavController());
            var profileViewModel = c.Remember(() => new ProfileViewModel());
            return JetchatApp.Build(
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
