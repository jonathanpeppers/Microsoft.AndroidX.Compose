using Android.Runtime;
using AndroidX.Activity;
using AndroidX.Compose;
using AndroidX.Compose.Runtime;
using AndroidX.Compose.Samples.Reply;
using AndroidX.Compose.UI.Platform;
using Snapshot = AndroidX.Compose.Runtime.Snapshots.Snapshot;

namespace Microsoft.AndroidX.Compose.DeviceTests;

/// <summary>Hosts the real Reply navigation and email UI, including native saved-state restoration.</summary>
[Activity(Theme = "@android:style/Theme.Material.Light.NoActionBar")]
[Register("net/compose/devicetests/ReplyNavigationTestActivity")]
public class ReplyNavigationTestActivity : ComponentActivity
{
    internal static TaskCompletionSource<ReplyNavigationTestActivity> Started { get; private set; } = NewStarted();
    internal TaskCompletionSource Destroyed { get; } = NewSignal();
    internal NavController Controller { get; } = new();
    internal ReplyState State => _state ?? throw new InvalidOperationException("Reply state is not initialized.");
    internal ComposeView View => _view ?? throw new InvalidOperationException("Reply ComposeView is not initialized.");
    readonly object _progressLock = new();
    TaskCompletionSource _progress = NewSignal();
    ReplyState? _state;
    ComposeView? _view;
    Snapshot.Companion? _snapshots;
    bool _resumed;
    bool _ending;
    bool _admitted;
    string? _admissionFailure;

    internal static void Prepare() => Started = NewStarted();

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        this.EnableEdgeToEdge();
        _state = new ReplyState(savedInstanceState);
        var view = new ComposeView(this) { Id = 0x34701 };
        _view = view;
        view.ViewAttachedToWindow += OnAttached;
        view.SetContent(() => ReplyApp.Content(Controller, State));
        SetContentView(view);
        Started.TrySetResult(this);
    }

    protected override void OnSaveInstanceState(Bundle outState)
    {
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
        if (!hasFocus && _admitted && !_ending)
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

    internal async Task AtNativeIdle()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        var runner = TestInstrumentation.Current
            ?? throw new InvalidOperationException("Reply test instrumentation is not running.");
        while (true)
        {
            Task progress;
            lock (_progressLock) progress = _progress.Task;
            await Task.Run(runner.WaitForIdleSync).WaitAsync(timeout.Token);
            bool idle = false;
            await OnUi(() =>
            {
                if (_admissionFailure is { } failure)
                    throw new InvalidOperationException(failure);
                if (_ending)
                    throw new InvalidOperationException("Reply test activity is ending.");
                if (View.GetChildAt(0) is not { } child)
                    return;
                var owner = child.JavaCast<IViewRootForTest>();
                if (WindowRecomposer_androidKt.FindViewTreeCompositionContext(View) is not Recomposer recomposer)
                    return;
                if (_snapshots is null)
                {
                    using var field = Java.Lang.Class.FromType(typeof(Snapshot)).GetField("Companion")
                        ?? throw new InvalidOperationException("Snapshot.Companion field is unavailable.");
                    _snapshots = field.Get(null)?.JavaCast<Snapshot.Companion>()
                        ?? throw new InvalidOperationException("Snapshot.Companion is unavailable.");
                }
                bool resumed = _resumed && View.HasWindowFocus && View.IsAttachedToWindow &&
                    View.IsLaidOut && owner.IsLifecycleInResumedState;
                if (resumed) _admitted = true;
                // A navigation entry is RESUMED only after its native transition completes.
                bool destinationResumed = Controller.Jvm?.CurrentBackStackEntry?.Lifecycle.CurrentState ==
                    global::AndroidX.Lifecycle.Lifecycle.State.Resumed;
                idle = resumed && destinationResumed && !owner.HasPendingMeasureOrLayout &&
                    !recomposer.HasPendingWork && !_snapshots.Current.HasPendingChanges &&
                    !_snapshots.IsApplyObserverNotificationPending;
            }).WaitAsync(timeout.Token);
            if (idle)
                return;
            await progress.WaitAsync(timeout.Token);
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
