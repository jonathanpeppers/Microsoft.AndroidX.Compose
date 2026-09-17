using Android.Views;
using Android.Content;
using AndroidX.Activity;
using AndroidX.Activity.Result;
using AndroidX.Activity.Result.Contract;
using AndroidX.Compose.Material3;
using AndroidX.Lifecycle;
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
    VideoPickerViewModel? _videoPickerState;
    internal VideoPickerViewModel? VideoPickerState => _videoPickerState;

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
        using (var provider = new ViewModelProvider(this))
        using (var modelClass = Java.Lang.Class.FromType(typeof(VideoPickerViewModel)))
        {
            _videoPickerState = provider.Get(modelClass) as VideoPickerViewModel
                ?? throw new InvalidOperationException("Video picker ViewModel could not be created.");
        }
        _videoPickerCallback = new VideoActivityResultCallback(OnVideoPicked);
        _videoPicker = RegisterForActivityResult(
            new ActivityResultContracts.GetContent(),
            _videoPickerCallback);
        VideoAttachmentStore.Prune(CacheDir);
        var seedVideoUri = VideoAttachmentStore.SeedVideoUri(this);
        this.EnableEdgeToEdge();
        this.SetContent(() =>
        {
            var ui               = Remember(() => new ConversationUiState("#composers", channelMembers: 42, FakeData.InitialMessages(seedVideoUri)));
            var selectedMenu     = MutableStateOf("composers");
            var drawerScroll     = Remember(() => new ScrollState());
            var drawerState      = Remember(() => new DrawerStateHolder(DrawerValue.Closed));
            var popupOpen        = MutableStateOf(false);
            var messagesScroll   = RememberLazyListState();
            var isRecording      = MutableStateOf(false);
            var swipeOffset      = MutableStateOf(0f);
            var nav              = Remember(() => new NavController());
            var profileViewModel = Remember(() => new ProfileViewModel());
            var videoPickerState = _videoPickerState
                ?? throw new InvalidOperationException("Video picker ViewModel is unavailable.");
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
                videoPickerState: videoPickerState,
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

    void PickVideo()
    {
        var state = _videoPickerState;
        if (state is null)
            return;
        if (!state.Begin())
            return;

        var picker = _videoPicker;
        if (picker is null)
        {
            state.Complete(VideoPickResult.Failed("The video picker is unavailable."));
            return;
        }

        using var mimeType = new Java.Lang.String("video/*");
        try
        {
            picker.Launch(mimeType);
        }
        catch (ActivityNotFoundException)
        {
            state.Complete(VideoPickResult.Failed(
                "No installed app can choose a video."));
        }
    }

    void OnVideoPicked(Android.Net.Uri? source)
    {
        var state = _videoPickerState;
        if (state is null)
            return;
        if (source is null)
        {
            state.Complete(VideoPickResult.Cancelled);
            return;
        }

        _ = ImportPickedVideoAsync(source, state);
    }

    async Task ImportPickedVideoAsync(Android.Net.Uri source, VideoPickerViewModel state)
    {
        try
        {
            var context = ApplicationContext
                ?? throw new InvalidOperationException("Jetchat application context is unavailable.");
            var uri = await VideoAttachmentStore.ImportAsync(context, source, state.Scope);
            state.Complete(VideoPickResult.Selected(uri));
        }
        catch (Exception ex)
        {
            state.Complete(VideoPickResult.Failed(ex.Message));
        }
    }
}
