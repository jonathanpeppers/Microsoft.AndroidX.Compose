using Android.Runtime;
using Android.Views;
using AndroidX.Compose;
using AndroidX.Compose.Runtime;
using AndroidX.Compose.Samples.Jetchat;
using AndroidX.Compose.UI.Platform;
using AndroidX.Compose.UI.Semantics;

namespace Microsoft.AndroidX.Compose.DeviceTests;

/// <summary>Observes lifecycle and native readiness around the unchanged Jetchat activity content.</summary>
[Activity(Theme = "@android:style/Theme.Material.Light.NoActionBar", WindowSoftInputMode = SoftInput.AdjustResize)]
[Register("net/compose/devicetests/JetchatRestorationTestActivity")]
public class JetchatRestorationTestActivity : MainActivity
{
    internal static TaskCompletionSource<JetchatRestorationTestActivity> Started { get; private set; } = NewStarted();
    internal TaskCompletionSource Destroyed { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    internal Guid InstanceId { get; } = Guid.NewGuid();
    internal bool Restored { get; private set; }
    internal bool Resumed { get; private set; }
    internal MutableManagedState<ConversationUiState>? Owners { get; private set; }
    internal ComposeView ComposeRoot => FindComposeView(Window?.DecorView
        ?? throw new InvalidOperationException("Jetchat decor is unavailable."))
        ?? throw new InvalidOperationException("Jetchat ComposeView is unavailable.");

    internal static void Prepare() => Started = NewStarted();

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        Restored = savedInstanceState is not null;
        base.OnCreate(savedInstanceState);
        if (Intent?.GetBooleanExtra("test-owner-switch", false) == true)
        {
            // The sample has one channel; this mode exercises replacement in the same real conversation slot.
            var owners = new MutableManagedState<ConversationUiState>(new("#first", 1, []));
            Owners = owners;
            this.SetContent(c =>
            {
                var ui = owners.Value;
                var menu = c.MutableStateOf("first");
                var popup = c.MutableStateOf(false);
                var scroll = c.RememberLazyListState();
                var recording = c.MutableStateOf(false);
                var swipe = c.MutableStateOf(0f);
                return new MaterialTheme
                {
                    Conversation.Build(
                        ui, menu, popup, scroll, recording, swipe,
                        completed => completed(VideoPickResult.Cancelled),
                        () => { }, _ => { }),
                };
            });
        }
        Started.TrySetResult(this);
    }

    protected override void OnResume()
    {
        base.OnResume();
        Resumed = true;
    }

    protected override void OnPause()
    {
        Resumed = false;
        base.OnPause();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        Destroyed.TrySetResult();
    }

    internal async Task AtNativeIdle()
    {
        var runner = TestInstrumentation.Current
            ?? throw new InvalidOperationException("Jetchat test instrumentation is not running.");
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        string status = "No Compose owner.";
        try
        {
            while (true)
            {
                await Task.Run(runner.WaitForIdleSync).WaitAsync(timeout.Token);
                bool idle = false;
                await OnUi(() =>
                {
                    if (IsDestroyed || IsFinishing)
                        throw new InvalidOperationException("Jetchat activity ended during observation.");
                    var decor = Window?.DecorView
                        ?? throw new InvalidOperationException("Jetchat decor is unavailable.");
                    if (FindComposeView(decor) is not { } view || view.GetChildAt(0) is not { } child)
                        return;
                    var owner = child.JavaCast<IViewRootForTest>();
                    var recomposer = WindowRecomposer_androidKt.FindViewTreeCompositionContext(view) as Recomposer;
                    idle = Resumed && HasWindowFocus && decor.IsLaidOut && !decor.IsLayoutRequested &&
                        owner.IsLifecycleInResumedState && !owner.HasPendingMeasureOrLayout &&
                        recomposer is { HasPendingWork: false };
                    status = $"instance={InstanceId}, resumed={Resumed}, focus={HasWindowFocus}, " +
                        $"ownerResumed={owner.IsLifecycleInResumedState}, layout={owner.HasPendingMeasureOrLayout}, " +
                        $"pending={recomposer?.HasPendingWork}, restored={Restored}";
                });
                if (idle)
                    return;
                await Task.Delay(20, timeout.Token);
            }
        }
        catch (OperationCanceledException error) when (timeout.IsCancellationRequested)
        {
            throw new TimeoutException($"Jetchat native readiness timed out: {status}", error);
        }
    }

    internal Task OnUi(Action action)
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        RunOnUiThread(() =>
        {
            try { action(); completion.TrySetResult(); }
            catch (Exception error) { completion.TrySetException(error); }
        });
        return completion.Task.WaitAsync(TimeSpan.FromSeconds(15));
    }

    internal (string Text, int Start, int End, bool Focused) ReadNativeEditor()
    {
        if (!Resumed || !HasWindowFocus || IsDestroyed || IsFinishing)
            throw new InvalidOperationException("The native editor's activity is no longer resumed and focused.");
        var child = ComposeRoot.GetChildAt(0)
            ?? throw new InvalidOperationException("Jetchat Compose owner is unavailable.");
        var owner = child.JavaCast<IViewRootForTest>();
        var properties = SemanticsProperties.Instance;
        List<SemanticsNode> editors = [];
        CollectEditors(owner.SemanticsOwner.UnmergedRootSemanticsNode);
        if (editors.Count != 1)
            throw new InvalidOperationException($"Expected one placed native Jetchat editor, found {editors.Count}.");
        var config = editors[0].Config;
        var text = config.Get(properties.EditableText)?.JavaCast<global::AndroidX.Compose.UI.Text.AnnotatedString>()
            ?? throw new InvalidOperationException("Native editor has no EditableText.");
        var range = config.Get(properties.TextSelectionRange)
            ?? throw new InvalidOperationException("Native editor has no TextSelectionRange.");
        long selection = TextFieldValueTestBridges.UnboxRange(range);
        var focused = config.Get(properties.Focused) as Java.Lang.Boolean
            ?? throw new InvalidOperationException("Native editor has no Focused semantics.");
        return (text.Text, (int)(selection >> 32), (int)selection, focused.BooleanValue());

        void CollectEditors(SemanticsNode node)
        {
            if (!node.LayoutInfo.IsAttached || !node.LayoutInfo.IsPlaced)
                return;
            if (node.Config.Contains(properties.EditableText))
                editors.Add(node);
            foreach (var descendant in node.Children)
                CollectEditors(descendant);
        }
    }

    static ComposeView? FindComposeView(View view)
    {
        if (view is ComposeView compose)
            return compose;
        if (view is ViewGroup group)
            for (int i = 0; i < group.ChildCount; i++)
                if (group.GetChildAt(i) is { } child && FindComposeView(child) is { } found)
                    return found;
        return null;
    }

    static TaskCompletionSource<JetchatRestorationTestActivity> NewStarted() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);
}
