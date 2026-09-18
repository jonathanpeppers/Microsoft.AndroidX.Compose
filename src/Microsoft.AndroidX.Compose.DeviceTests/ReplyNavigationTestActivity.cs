using Android.Runtime;
using AndroidX.Activity;
using AndroidX.Compose;
using AndroidX.Compose.Material3.Adaptive.Layout;
using AndroidX.Compose.Runtime;
using AndroidX.Compose.Samples.Reply;
using AndroidX.Compose.Material3.Adaptive;
using AndroidX.Compose.UI.Platform;
using AndroidX.Window.Layout;
using ComposeButton = AndroidX.Compose.Button;
using Snapshot = AndroidX.Compose.Runtime.Snapshots.Snapshot;

namespace Microsoft.AndroidX.Compose.DeviceTests;

/// <summary>Hosts the real Reply navigation and email UI, including native saved-state restoration.</summary>
[Activity(Theme = "@android:style/Theme.Material.Light.NoActionBar")]
[Register("net/compose/devicetests/ReplyNavigationTestActivity")]
public class ReplyNavigationTestActivity : ComponentActivity
{
    internal static TaskCompletionSource<ReplyNavigationTestActivity> Started { get; private set; } = NewStarted();
    internal static Func<ReplyNavigationTestActivity, WindowAdaptiveInfo?>?
        AdaptiveInfoOverrideFactory { get; private set; }
    internal static bool NavigableScaffoldMode { get; private set; }
    internal TaskCompletionSource Destroyed { get; } = NewSignal();
    internal NavController Controller { get; } = new();
    internal ReplyState State => _state ?? throw new InvalidOperationException("Reply state is not initialized.");
    internal ComposeView View => _view ?? throw new InvalidOperationException("Reply ComposeView is not initialized.");
    internal ListDetailPaneScaffoldNavigator<long> PaneNavigator =>
        _paneNavigator ?? throw new InvalidOperationException(
            "Navigable list-detail pane navigator is not initialized.");
    internal TaskCompletionSource<NavigationSuiteType> NavigationTypeObserved { get; } =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
    internal TaskCompletionSource<int> PanePartitionsObserved { get; } =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
    internal TaskCompletionSource<WindowLayoutInfo> WindowLayoutInfoObserved
        { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    internal MutableManagedState<WindowAdaptiveInfo?> AdaptiveInfoOverride { get; } =
        new(null);
    internal int PanePartitions { get; private set; }
    internal int VerticalPartitions { get; private set; }
    internal int ExcludedBoundsCount { get; private set; }
    internal bool OwnedPopupExpected => _ownedPopupExpected;
    readonly object _progressLock = new();
    TaskCompletionSource _progress = NewSignal();
    ReplyState? _state;
    ComposeView? _view;
    ListDetailPaneScaffoldNavigator<long>? _paneNavigator;
    Action<long>? _navigateToPane;
    Snapshot.Companion? _snapshots;
    bool _resumed;
    bool _ending;
    bool _admitted;
    bool _ownedPopupExpected;
    string? _admissionFailure;

    internal static void Prepare(
        Func<ReplyNavigationTestActivity, WindowAdaptiveInfo?>?
            adaptiveInfoOverrideFactory = null)
    {
        Started = NewStarted();
        AdaptiveInfoOverrideFactory = adaptiveInfoOverrideFactory;
        NavigableScaffoldMode = false;
    }

    internal static void PrepareNavigableScaffold()
    {
        Started = NewStarted();
        AdaptiveInfoOverrideFactory = null;
        NavigableScaffoldMode = true;
    }

    internal static void PrepareForRecreation() => Started = NewStarted();

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        this.EnableEdgeToEdge();
        _state = new ReplyState(savedInstanceState);
        var view = new ComposeView(this) { Id = 0x34701 };
        _view = view;
        view.ViewAttachedToWindow += OnAttached;
        AdaptiveInfoOverride.Value = AdaptiveInfoOverrideFactory?.Invoke(this);
        view.SetContent(() =>
        {
            var composer = ComposableContext.Current;
            if (NavigableScaffoldMode)
            {
                RenderNavigableScaffold(composer);
                return;
            }
            var windowLayoutInfo =
                Composables.CollectWindowLayoutInfo(this).Value;
            if (windowLayoutInfo is not null)
            {
                composer.SideEffect(
                    () => WindowLayoutInfoObserved.TrySetResult(
                        windowLayoutInfo));
            }
            ReplyApp.Content(
                Controller,
                State,
                type => NavigationTypeObserved.TrySetResult(type),
                AdaptiveInfoOverride.Value,
                ObservePaneDirective);
        });
        SetContentView(view);
        Started.TrySetResult(this);
    }

    void RenderNavigableScaffold(IComposer composer)
    {
        var navigator =
            composer.RememberListDetailPaneScaffoldNavigator<long>();
        _paneNavigator = navigator;
        var scope = composer.RememberCoroutineScope();
        void Navigate(long key) => Run(
            scope,
            ct => navigator.NavigateToAsync(
                AdaptivePaneRole.Detail,
                key,
                ct));
        _navigateToPane = Navigate;
        var list = new ComposeButton(
            () => Navigate(1L))
        {
            new Text("Open detail"),
        };
        var detail = new Column
        {
            new Text("Navigable detail"),
            new ComposeButton(() => Navigate(2L))
            {
                new Text("Open next detail"),
            },
        };
        new NavigableListDetailPaneScaffold<long>(navigator)
        {
            Modifier = Modifier.FillMaxSize(),
            ListPane = list,
            DetailPane = detail,
            DefaultBackBehavior = PaneBackNavigationBehavior.PopLatest,
        }.Render(composer);
        composer.SideEffect(Signal);
    }

    internal void NavigateToPane(long contentKey)
    {
        var navigate = _navigateToPane
            ?? throw new InvalidOperationException(
                "Navigable list-detail pane callback is not initialized.");
        navigate(contentKey);
    }

    protected override void OnSaveInstanceState(Bundle outState)
    {
        if (!NavigableScaffoldMode)
            State.Save(outState);
        base.OnSaveInstanceState(outState);
    }

    protected override void OnResume()
    {
        base.OnResume();
        _resumed = true;
        Signal();
    }

    protected override void OnPause()
    {
        _resumed = false;
        if (_admitted && !_ending)
            _admissionFailure = "Reply test activity paused during observation.";
        Signal();
        base.OnPause();
    }

    /// <summary>Rejects observations after an unexpected foreground-window change.</summary>
    public override void OnWindowFocusChanged(bool hasFocus)
    {
        base.OnWindowFocusChanged(hasFocus);
        if (!hasFocus && _admitted && !_ending &&
            !_ownedPopupExpected)
            _admissionFailure = "Reply test activity lost window focus.";
        Signal();
    }

    protected override void OnDestroy()
    {
        if (_view?.ViewTreeObserver is { IsAlive: true } observer)
            observer.Draw -= OnDraw;
        if (_view is { } view)
            view.ViewAttachedToWindow -= OnAttached;
        base.OnDestroy();
        Destroyed.TrySetResult();
        Signal();
    }

    internal void ExpectLifecycleEnd() => _ending = true;

    internal void ExpectOwnedPopup() => _ownedPopupExpected = true;

    internal void ExpectHostWindow()
    {
        _ownedPopupExpected = false;
        _admissionFailure = null;
    }

    internal void SetAdaptiveInfo(WindowAdaptiveInfo adaptiveInfo)
    {
        ArgumentNullException.ThrowIfNull(adaptiveInfo);
        AdaptiveInfoOverride.Value = adaptiveInfo;
    }

    void ObservePaneDirective(PaneScaffoldDirective directive)
    {
        PanePartitions = directive.MaxHorizontalPartitions;
        VerticalPartitions = directive.MaxVerticalPartitions;
        ExcludedBoundsCount =
            AdaptiveTestWindowInfo.ExcludedBoundsCount(directive);
        PanePartitionsObserved.TrySetResult(PanePartitions);
        Signal();
    }

    internal async Task AtNativeIdle()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        var runner = TestInstrumentation.Current
            ?? throw new InvalidOperationException("Reply test instrumentation is not running.");
        using var frame = new Java.Lang.Runnable(Signal);
        string status = "No native readiness observation.";
        int observations = 0;
        try
        {
            while (true)
            {
                Task progress;
                lock (_progressLock) progress = _progress.Task;
                await Task.Run(runner.WaitForIdleSync).WaitAsync(timeout.Token);
                bool idle = false;
                await OnUi(() =>
                {
                    observations++;
                    if (_admissionFailure is { } failure)
                        throw new InvalidOperationException(failure);
                    if (_ending)
                        throw new InvalidOperationException("Reply test activity is ending.");
                    status = "Compose owner child unavailable.";
                    if (View.GetChildAt(0) is not { } child)
                        return;
                    var owner = child.JavaCast<IViewRootForTest>();
                    status = "Window recomposer unavailable.";
                    if (WindowRecomposer_androidKt.FindViewTreeCompositionContext(View) is not Recomposer recomposer)
                        return;
                    if (_snapshots is null)
                    {
                        using var field = Java.Lang.Class.FromType(typeof(Snapshot)).GetField("Companion")
                            ?? throw new InvalidOperationException("Snapshot.Companion field is unavailable.");
                        _snapshots = field.Get(null)?.JavaCast<Snapshot.Companion>()
                            ?? throw new InvalidOperationException("Snapshot.Companion is unavailable.");
                    }
                    bool focusReady =
                        View.HasWindowFocus || _ownedPopupExpected;
                    bool resumed = _resumed && focusReady && View.IsAttachedToWindow &&
                        View.IsLaidOut && owner.IsLifecycleInResumedState;
                    if (resumed) _admitted = true;
                    var entry = Controller.Jvm?.CurrentBackStackEntry;
                    var lifecycle = entry?.Lifecycle.CurrentState;
                    // A navigation entry is RESUMED only after its native transition completes.
                    bool destinationResumed =
                        NavigableScaffoldMode ||
                        lifecycle ==
                            global::AndroidX.Lifecycle.Lifecycle.State.Resumed;
                    bool measurePending = owner.HasPendingMeasureOrLayout;
                    bool compositionPending = recomposer.HasPendingWork;
                    bool snapshotPending = _snapshots.Current.HasPendingChanges;
                    bool notificationPending = _snapshots.IsApplyObserverNotificationPending;
                    status = $"observations={observations}, activityResumed={_resumed}, focus={View.HasWindowFocus}, " +
                        $"attached={View.IsAttachedToWindow}, laidOut={View.IsLaidOut}, " +
                        $"ownerResumed={owner.IsLifecycleInResumedState}, route={entry?.Destination.Route}, " +
                        $"entryLifecycle={lifecycle}, measurePending={measurePending}, " +
                        $"compositionPending={compositionPending}, snapshotPending={snapshotPending}, " +
                        $"notificationPending={notificationPending}";
                    bool compositionReady =
                        !compositionPending || _ownedPopupExpected;
                    idle = resumed && destinationResumed && !measurePending && compositionReady &&
                        !snapshotPending && !notificationPending;
                }).WaitAsync(timeout.Token);
                if (idle)
                    return;
                // Transition lifecycle changes need not draw. Recheck on a real platform frame
                // as well as draw/window events, without changing any readiness condition.
                await OnUi(() =>
                {
                    View.RemoveCallbacks(frame);
                    View.PostOnAnimation(frame);
                }).WaitAsync(timeout.Token);
                await progress.WaitAsync(timeout.Token);
            }
        }
        catch (OperationCanceledException error) when (timeout.IsCancellationRequested)
        {
            throw new TimeoutException($"Reply native readiness timed out: {status}", error);
        }
        finally
        {
            await OnUi(() => View.RemoveCallbacks(frame));
        }
    }

    internal Task OnUi(Action action)
    {
        var completion = NewSignal();
        RunOnUiThread(() =>
        {
            try { action(); completion.TrySetResult(); }
            catch (Exception error) { completion.TrySetException(error); }
        });
        return completion.Task.WaitAsync(TimeSpan.FromSeconds(15));
    }

    static async void Run(
        CoroutineScope scope,
        Func<CancellationToken, Task> action)
    {
        try
        {
            await scope.Launch(action);
        }
        catch (OperationCanceledException)
        {
            // Leaving composition cancels this await; Kotlin may finish.
        }
    }

    void OnAttached(object? sender, EventArgs args)
    {
        var observer = View.ViewTreeObserver
            ?? throw new InvalidOperationException("Reply draw observer is unavailable.");
        observer.Draw += OnDraw;
        Signal();
    }

    void OnDraw(object? sender, EventArgs args) => Signal();

    void Signal()
    {
        lock (_progressLock)
        {
            _progress.TrySetResult();
            _progress = NewSignal();
        }
    }

    static TaskCompletionSource NewSignal() => new(TaskCreationOptions.RunContinuationsAsynchronously);
    static TaskCompletionSource<ReplyNavigationTestActivity> NewStarted() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);
}
