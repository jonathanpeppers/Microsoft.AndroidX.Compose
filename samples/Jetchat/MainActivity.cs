using global::Android.Views;
using global::AndroidX.Activity;
using global::AndroidX.Compose.Material3;
using global::AndroidX.Compose.UI.Text.Input;

namespace Microsoft.AndroidX.Compose.Samples.Jetchat;

[Activity(
    Label = "@string/app_name",
    MainLauncher = true,
    Theme = "@android:style/Theme.Material.Light.NoActionBar",
    WindowSoftInputMode = SoftInput.AdjustResize)]
[global::Android.Runtime.Register("net/compose/samples/jetchat/MainActivity")]
public class MainActivity : ComponentActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        this.EnableEdgeToEdge();
        this.SetContent(c =>
        {
            var ui               = c.Remember(() => new ConversationUiState("#composers", channelMembers: 42, FakeData.InitialMessages()));
            var input            = c.Remember(() => new MutableState<TextFieldValue>(ComposeExtensions.NewTextFieldValue()));
            var selectedMenu     = c.Remember(() => new MutableState<string>("composers"));
            var drawerScroll     = c.Remember(() => new ScrollState());
            var drawerState      = c.Remember(() => new DrawerStateHolder(DrawerValue.Closed));
            var selectedSelector = c.Remember(() => new MutableState<int>(0));
            var popupOpen        = c.Remember(() => new MutableState<bool>(false));
            var messagesScroll   = c.RememberLazyListState();
            var isRecording      = c.Remember(() => new MutableState<bool>(false));
            var swipeOffset      = c.Remember(() => new MutableNumberState<float>(0f));
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
