using Android.OS;
using Android.Views;
using AndroidX.Compose.Material3;
using AndroidX.Compose.UI.Text.Input;
using ComposeNet;

namespace ComposeNet.Samples.Jetchat;

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
            var input            = Remember(() => new MutableState<TextFieldValue>(Compose.NewTextFieldValue()));
            var selectedMenu     = Remember(() => new MutableState<string>("composers"));
            var drawerScroll     = Remember(() => new ScrollState());
            var drawerState      = Remember(() => new DrawerStateHolder(DrawerValue.Closed));
            var selectedSelector = Remember(() => new MutableState<int>(0));
            var popupOpen        = Remember(() => new MutableState<bool>(false));
            var messagesScroll   = Compose.RememberLazyListState();
            return Conversation.Build(ui, input, selectedMenu, drawerScroll, drawerState, selectedSelector, popupOpen, messagesScroll);
        });
    }
}
