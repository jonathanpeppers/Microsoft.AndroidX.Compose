using Android.Views;
using AndroidX.Activity;
using AndroidX.Activity.Result;
using AndroidX.Activity.Result.Contract;
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
    ActivityResultLauncher? _videoPicker;
    VideoActivityResultCallback? _videoPickerCallback;
    Action<VideoPickResult>? _pendingVideoPick;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        bool? darkThemeOverride = null;
#if DEBUG
        darkThemeOverride = Intent?.GetStringExtra("test-palette") switch
        {
            null => null,
            "light" => false,
            "dark" => true,
            var palette => throw new InvalidOperationException($"Unknown test palette '{palette}'."),
        };
#endif
        base.OnCreate(savedInstanceState);
        _videoPickerCallback = new VideoActivityResultCallback(OnVideoPicked);
        _videoPicker = RegisterForActivityResult(
            new ActivityResultContracts.GetContent(),
            _videoPickerCallback);
        VideoAttachmentStore.Prune(CacheDir);
        this.EnableEdgeToEdge();
        this.SetContent(() =>
        {
            var ui               = Remember(() => new ConversationUiState("#composers", channelMembers: 42, FakeData.InitialMessages()));
            var selectedMenu     = MutableStateOf("composers");
            var drawerScroll     = Remember(() => new ScrollState());
            var drawerState      = Remember(() => new DrawerStateHolder(DrawerValue.Closed));
            var popupOpen        = MutableStateOf(false);
            var messagesScroll   = RememberLazyListState();
            var isRecording      = MutableStateOf(false);
            var swipeOffset      = MutableStateOf(0f);
            var nav              = Remember(() => new NavController());
            var profileViewModel = Remember(() => new ProfileViewModel());
            JetchatApp.Content(
                nav:              nav,
                ui:               ui,
                selectedMenu:     selectedMenu,
                drawerScroll:     drawerScroll,
                drawerState:      drawerState,
                popupOpen:        popupOpen,
                messagesScroll:   messagesScroll,
                isRecording:      isRecording,
                swipeOffset:      swipeOffset,
                profileViewModel: profileViewModel,
                requestVideo:     PickVideo,
                darkThemeOverride: darkThemeOverride);
        });
    }

    protected override void OnPause()
    {
        VideoPlaybackCoordinator.PauseActive();
        base.OnPause();
    }

    protected override void OnStop()
    {
        VideoPlaybackCoordinator.PauseActive();
        base.OnStop();
    }

    protected override void OnDestroy()
    {
        _videoPicker?.Unregister();
        _videoPicker?.Dispose();
        _videoPicker = null;
        _videoPickerCallback?.Dispose();
        _videoPickerCallback = null;
        VideoPlaybackCoordinator.ReleaseActive();
        base.OnDestroy();
    }

    void PickVideo(Action<VideoPickResult> completed)
    {
        ArgumentNullException.ThrowIfNull(completed);
        if (_pendingVideoPick is not null)
        {
            completed(VideoPickResult.Failed("A video picker is already open."));
            return;
        }

        var picker = _videoPicker;
        if (picker is null)
        {
            completed(VideoPickResult.Failed("The video picker is unavailable."));
            return;
        }

        _pendingVideoPick = completed;
        using var mimeType = new Java.Lang.String("video/*");
        picker.Launch(mimeType);
    }

    void OnVideoPicked(Android.Net.Uri? source)
    {
        var completed = _pendingVideoPick;
        _pendingVideoPick = null;
        if (completed is null)
            return;
        if (source is null)
        {
            completed(VideoPickResult.Cancelled);
            return;
        }

        _ = ImportPickedVideoAsync(source, completed);
    }

    async Task ImportPickedVideoAsync(Android.Net.Uri source, Action<VideoPickResult> completed)
    {
        try
        {
            var uri = await VideoAttachmentStore.ImportAsync(this, source);
            completed(VideoPickResult.Selected(uri));
        }
        catch (Exception ex)
        {
            completed(VideoPickResult.Failed(ex.Message));
        }
    }
}
