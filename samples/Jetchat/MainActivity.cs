using global::Android.Views;
using global::AndroidX.Compose.Material3;
using global::AndroidX.Compose.UI.Text.Input;

namespace Microsoft.AndroidX.Compose.Samples.Jetchat;

[Activity(
    Label = "@string/app_name",
    MainLauncher = true,
    Theme = "@android:style/Theme.Material.Light.NoActionBar",
    WindowSoftInputMode = SoftInput.AdjustResize)]
public class MainActivity : ComposeActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        SetContent(() =>
        {
            var ui               = Remember(() => new ConversationUiState("#composers", channelMembers: 42, FakeData.InitialMessages()));
            var input            = Remember(() => new MutableState<TextFieldValue>(ComposeRuntime.NewTextFieldValue()));
            var selectedMenu     = Remember(() => new MutableState<string>("composers"));
            var drawerScroll     = Remember(() => new ScrollState());
            var drawerState      = Remember(() => new DrawerStateHolder(DrawerValue.Closed));
            var selectedSelector = Remember(() => new MutableState<int>(0));
            var popupOpen        = Remember(() => new MutableState<bool>(false));
            var messagesScroll   = ComposeRuntime.RememberLazyListState();
            var isRecording      = Remember(() => new MutableState<bool>(false));
            var swipeOffset      = Remember(() => new MutableNumberState<float>(0f));
            var nav              = Remember(() => new NavController());
            var profileViewModel = Remember(() => new ProfileViewModel());
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
