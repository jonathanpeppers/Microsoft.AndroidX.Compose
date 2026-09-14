using Android.Runtime;
using AndroidX.Activity;
using AndroidX.Compose;
using AndroidX.Compose.Runtime;
using AndroidX.Compose.UI.Platform;
using Composable = AndroidX.Compose.ComposableAttribute;
using Snapshot = AndroidX.Compose.Runtime.Snapshots.Snapshot;
using WindowInsets = AndroidX.Compose.WindowInsets;

namespace Microsoft.AndroidX.Compose.DeviceTests;

/// <summary>Measures real edge-to-edge Material 3 Scaffold layouts for issue #339.</summary>
[Activity(Theme = "@android:style/Theme.Material.Light.NoActionBar")]
[Register("net/compose/devicetests/ScaffoldInsetsTestActivity")]
public class ScaffoldInsetsTestActivity : ComponentActivity
{
    internal const int Omitted = 0;
    internal const int ExplicitNull = 1;
    internal const int Zero = 2;
    internal const int Fixed = 3;
    internal const int Default = 4;
    internal const int Excluded = 5;
    internal const int CompoundZero = 6;

    static readonly object s_activityLock = new();
    static TaskCompletionSource s_activityChanged = NewSignal();
    static ScaffoldInsetsTestActivity? s_current;
    static int s_style;
    static bool s_bars;
    static int s_initialMode;

    readonly object _snapshotLock = new();
    readonly TaskCompletionSource _destroyed = NewSignal();
    TaskCompletionSource _snapshotChanged = NewSignal();
    MutableManagedState<(int Generation, int Mode)>? _request;
    ScaffoldInsetsFrame? _frame;
    ScaffoldInsetsSnapshot? _snapshot;
    ComposeView? _view;
    IViewRootForTest? _rootForTest;
    global::Android.Views.ViewTreeObserver? _drawObserver;
    ScaffoldInsetsRootView? _insetsRoot;
    Recomposer? _recomposer;
    Snapshot.Companion? _snapshotCompanion;
    bool _resumed;
    bool _focused;
    bool _admitted;
    bool _ending;
    int _insetsDispatches;
    string _lastDraw = "No native draw.";
    string? _foregroundFailure;
    string _phase = "initial";
    int _bodyObservationVersion;
    bool _nativeWorkPending;
    bool? _firstRecomposerPending;
    int _idleProbes;
#if DEBUG
    ScaffoldInsetsLambdaObserver? _lambdaObserver;
#endif
    int _style;
    bool _bars;

    internal static ScaffoldInsetsTestActivity? Current
    {
        get { lock (s_activityLock) return s_current; }
    }

    internal Task Destroyed => _destroyed.Task;

    internal static void Configure(int style, bool bars, int initialMode = ExplicitNull)
    {
        lock (s_activityLock)
        {
            if (s_current is not null)
                throw new InvalidOperationException("A Scaffold insets test activity is still running.");
            s_style = style;
            s_bars = bars;
            s_initialMode = initialMode;
        }
    }

    internal static async Task<ScaffoldInsetsTestActivity> WaitForActivityAsync(
        ScaffoldInsetsTestActivity? previous = null)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        while (true)
        {
            Task changed;
            lock (s_activityLock)
            {
                if (s_current is { } current && !ReferenceEquals(previous, current))
                    return current;
                changed = s_activityChanged.Task;
            }
            await changed.WaitAsync(timeout.Token);
        }
    }

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        _phase = savedInstanceState is null ? "initial" : "restored";
        try
        {
            this.EnableEdgeToEdge();
            var window = Window
                ?? throw new InvalidOperationException("Window not set on ScaffoldInsetsTestActivity.");
            window.AddFlags(global::Android.Views.WindowManagerFlags.KeepScreenOn);
            var decor = window.DecorView
                ?? throw new InvalidOperationException("DecorView not set on ScaffoldInsetsTestActivity.");
            decor.LayoutDirection = global::Android.Views.LayoutDirection.Ltr;
            lock (s_activityLock)
            {
                _style = s_style;
                _bars = s_bars;
                _request = new((0, s_initialMode));
            }
#if DEBUG
            _lambdaObserver = new ScaffoldInsetsLambdaObserver(this);
#endif
            // Keep the Android saved-view-state key stable across Recreate.
            var view = new ComposeView(this) { Id = 0x33901 };
            _view = view;
            _insetsRoot = new ScaffoldInsetsRootView(this)
            {
                InsetsApplied = () =>
                {
                    _insetsDispatches++;
                    SignalProgress();
                },
            };
            _insetsRoot.AddView(view, new global::Android.Widget.FrameLayout.LayoutParams(
                global::Android.Views.ViewGroup.LayoutParams.MatchParent,
                global::Android.Views.ViewGroup.LayoutParams.MatchParent));
            view.SetContent(c => RenderRoot(c, this));
            SetContentView(_insetsRoot);
            lock (s_activityLock)
            {
                s_current = this;
                s_activityChanged.TrySetResult();
                s_activityChanged = NewSignal();
            }
        }
        catch (Exception creationError)
        {
            try
            {
                ReleaseObservation();
            }
            catch (Exception cleanupError)
            {
                throw new AggregateException(
                    "Scaffold activity creation and observation cleanup both failed.",
                    creationError, cleanupError);
            }
            throw;
        }
    }

    protected override void OnDestroy()
    {
        ReleaseObservation();
        base.OnDestroy();
        _destroyed.TrySetResult();
    }

    protected override void OnResume()
    {
        base.OnResume();
        _resumed = true;
        SignalProgress();
    }

    protected override void OnPause()
    {
        _resumed = false;
        RecordForegroundLoss("Activity paused");
        base.OnPause();
    }

    /// <summary>Tracks the actual test window's focus rather than assuming OnCreate is foreground admission.</summary>
    public override void OnWindowFocusChanged(bool hasFocus)
    {
        base.OnWindowFocusChanged(hasFocus);
        _focused = hasFocus;
        SignalProgress();
        if (!hasFocus && _view?.HasWindowFocus != true)
            RecordForegroundLoss("Window focus lost");
    }

    internal void ExpectLifecycleEnd() => _ending = true;

    void RecordForegroundLoss(string reason)
    {
        if (!_admitted || _ending)
            return;
        lock (_snapshotLock)
        {
            _foregroundFailure = $"{reason} during a Scaffold test; {_lastDraw}";
            _snapshotChanged.TrySetResult();
            _snapshotChanged = NewSignal();
        }
    }

    void ReleaseObservation()
    {
        try
        {
            if (_drawObserver is { IsAlive: true } observer)
                observer.Draw -= OnDraw;
            _drawObserver = null;
            if (_insetsRoot is { } root)
                root.InsetsApplied = null;
            _insetsRoot = null;
#if DEBUG
            _lambdaObserver?.Dispose();
#endif
        }
        finally
        {
            _rootForTest = null;
            _view = null;
            _frame = null;
            _recomposer = null;
            _snapshotCompanion = null;
#if DEBUG
            _lambdaObserver = null;
#endif
            lock (s_activityLock)
            {
                if (ReferenceEquals(s_current, this))
                    s_current = null;
                s_activityChanged.TrySetResult();
                s_activityChanged = NewSignal();
            }
        }
    }

    internal bool HasPendingMeasureOrLayout
    {
        get
        {
            var root = _rootForTest
                ?? throw new InvalidOperationException("The native Compose owner has not been observed.");
            return root.HasPendingMeasureOrLayout;
        }
    }

    internal bool IsCurrentFrame(ScaffoldInsetsFrame frame) => ReferenceEquals(_frame, frame);

    internal void ObserveDraw()
    {
        if (_drawObserver is not null)
            return;
        var view = _view
            ?? throw new InvalidOperationException("ComposeView is not set on ScaffoldInsetsTestActivity.");
        var child = view.GetChildAt(0)
            ?? throw new InvalidOperationException("ComposeView has no native owner after placement.");
        _rootForTest = child.JavaCast<IViewRootForTest>();
        _recomposer = WindowRecomposer_androidKt.FindViewTreeCompositionContext(view) as Recomposer
            ?? throw new InvalidOperationException("The test window has no bound Recomposer.");
        // The binding exposes the Companion type and readers, but not its static singleton field.
        var snapshotClass = Java.Lang.Class.FromType(typeof(Snapshot));
        using var field = snapshotClass.GetField("Companion")
            ?? throw new InvalidOperationException("Snapshot.Companion field is unavailable.");
        var singleton = field.Get(null)
            ?? throw new InvalidOperationException("Snapshot.Companion singleton is unavailable.");
        _snapshotCompanion = singleton.JavaCast<Snapshot.Companion>();
        _drawObserver = view.ViewTreeObserver
            ?? throw new InvalidOperationException("ComposeView has no ViewTreeObserver after placement.");
        _drawObserver.Draw += OnDraw;
    }

    internal void SignalProgress()
    {
        lock (_snapshotLock)
        {
            _snapshotChanged.TrySetResult();
            _snapshotChanged = NewSignal();
        }
    }

    internal int BodyObservationVersion
    {
        get { lock (_snapshotLock) return _bodyObservationVersion; }
    }

    internal int RecordBodyObservation()
    {
        lock (_snapshotLock) return ++_bodyObservationVersion;
    }

    void OnDraw(object? sender, EventArgs e)
    {
        TryPublishIdleSnapshot("native-draw");
        SignalProgress();
    }

    void TryPublishIdleSnapshot(string trigger)
    {
        var root = _rootForTest ?? throw new InvalidOperationException("Native Compose root is unavailable.");
        var view = _view ?? throw new InvalidOperationException("ComposeView is unavailable during draw.");
        var recomposer = _recomposer ?? throw new InvalidOperationException("Recomposer is unavailable during draw.");
        var snapshots = _snapshotCompanion ?? throw new InvalidOperationException("Snapshot observer is unavailable.");
        bool focused = view.HasWindowFocus;
        bool resumed = _resumed && root.IsLifecycleInResumedState;
        bool pendingLayout = root.HasPendingMeasureOrLayout;
        bool pendingComposition = recomposer.HasPendingWork;
        bool pendingSnapshot = snapshots.Current.HasPendingChanges || snapshots.IsApplyObserverNotificationPending;
        _nativeWorkPending = pendingLayout || pendingComposition || pendingSnapshot;
        _firstRecomposerPending ??= pendingComposition;
        _idleProbes++;
        string admission = $"phase={_phase} trigger={trigger} activity={Handle:x} window={view.WindowToken} focused={focused} focusCallback={_focused} resumed={resumed} " +
            $"insetsDispatches={_insetsDispatches} pendingLayout={pendingLayout} " +
            $"pendingComposition={pendingComposition} pendingSnapshot={pendingSnapshot} " +
            $"recomposer={recomposer.Handle:x}/{recomposer.CurrentState.Value} " +
            $"firstRecomposerPending={_firstRecomposerPending} idleProbes={_idleProbes}";
        lock (_snapshotLock) _lastDraw = admission;
        if (!focused || !resumed || _insetsDispatches == 0 || _ending)
            return;
        _admitted = true;
        _frame?.CompleteIdleLayout(pendingLayout || pendingComposition || pendingSnapshot, admission);
    }

    internal int ChangeInsets(int mode)
    {
        var request = _request
            ?? throw new InvalidOperationException("Insets state not set on ScaffoldInsetsTestActivity.");
        var next = (request.Value.Generation + 1, mode);
        request.Value = next;
        return next.Item1;
    }

    internal async Task<ScaffoldInsetsSnapshot> WaitForSnapshotAsync(
        int generation, int afterBodyObservation = -1)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        while (true)
        {
            try
            {
                Task changed;
                lock (_snapshotLock) changed = _snapshotChanged.Task;
                var instrumentation = TestInstrumentation.Current
                    ?? throw new InvalidOperationException("Test instrumentation is not running.");
                await Task.Run(instrumentation.WaitForIdleSync).WaitAsync(timeout.Token);
                var probe = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                RunOnUiThread(() =>
                {
                    try
                    {
                        if (_frame is not null && _rootForTest is not null)
                            TryPublishIdleSnapshot("instrumentation-idle-after-placement");
                        probe.TrySetResult(_nativeWorkPending);
                    }
                    catch (Exception error)
                    {
                        probe.TrySetException(error);
                    }
                });
                bool nativeWorkPending = await probe.Task.WaitAsync(timeout.Token);
                lock (_snapshotLock)
                {
                    if (_foregroundFailure is { } failure)
                        throw new InvalidOperationException(failure);
                    if (_snapshot is { } snapshot && snapshot.Generation == generation
                        && snapshot.BodyObservationVersion > afterBodyObservation)
                        return snapshot;
                }
                if (nativeWorkPending)
                    continue;
                await changed.WaitAsync(timeout.Token);
            }
            catch (OperationCanceledException ex)
            {
                ScaffoldInsetsSnapshot? last;
                lock (_snapshotLock) last = _snapshot;
                throw new TimeoutException(
                    $"Scaffold did not acknowledge completed layout for generation {generation}; " +
                    $"last native idle probe: {_lastDraw}; last snapshot: {last}", ex);
            }
        }
    }

    internal void Publish(ScaffoldInsetsFrame frame, ScaffoldInsetsSnapshot snapshot)
    {
        if (!ReferenceEquals(_frame, frame))
            return;
        global::Android.Util.Log.Debug("ScaffoldInsets", snapshot.Trace);
        lock (_snapshotLock)
        {
            _snapshot = snapshot;
            _snapshotChanged.TrySetResult();
            _snapshotChanged = NewSignal();
        }
    }

    internal void RecordNativeArguments(
        IntPtr ownedGlobalReference, global::AndroidX.Compose.Foundation.Layout.IWindowInsets insets)
    {
        var frame = _frame
            ?? throw new InvalidOperationException("Scaffold native content was observed before its frame.");
        frame.RecordNativeArguments(ownedGlobalReference, insets);
    }

    [Composable]
    internal static void RenderRoot(IComposer composer, ScaffoldInsetsTestActivity activity)
    {
        var request = activity._request
            ?? throw new InvalidOperationException("Insets state not set on ScaffoldInsetsTestActivity.");
        var (generation, mode) = request.Value;
        var frame = new ScaffoldInsetsFrame(activity, composer, generation, mode, activity._bars,
            Composables.ScaffoldContentWindowInsets().AsPaddingValues());
        activity._frame = frame;
        // Keep composition-aware readers unconditional while changing only the supplied object.
        var defaults = activity._style == 3
            ? Composables.ScaffoldContentWindowInsets()
            : composer.ScaffoldContentWindowInsets();
        var insets = SelectInsets(mode, defaults, composer.NavigationBarsInsets(), composer.ImeInsets());
        var modifier = Modifier.FillMaxSize().Then(frame.Measure(ScaffoldInsetsFrame.Root));
        bool bars = activity._bars;
        if (activity._style < 2)
        {
            var scaffold = new Scaffold
            {
                Modifier = modifier,
                TopBar = bars ? BuildTopBar(frame) : null,
                BottomBar = bars ? BuildBottomBar(frame) : null,
            };
            if (mode != Omitted)
                scaffold.ContentWindowInsets = insets;
            if (activity._style == 0)
                scaffold.Body = new ScaffoldInsetsBody(frame) { Modifier = Modifier.FillMaxSize() };
            else
                scaffold.BodyContent = padding => BuildBody(frame, padding);
            scaffold.Render(composer);
        }
        else if (activity._style == 2)
        {
            if (mode == Omitted)
                Composables.Scaffold(
                    composer, (padding, c) => BuildBody(frame, padding).Render(c),
                    modifier: modifier,
                    topBar: bars ? c => BuildTopBar(frame).Render(c) : null,
                    bottomBar: bars ? c => BuildBottomBar(frame).Render(c) : null);
            else
                // Null -> zero -> custom -> null stays on this same native call site.
                Composables.Scaffold(
                    composer, (padding, c) => BuildBody(frame, padding).Render(c),
                    modifier: modifier,
                    topBar: bars ? c => BuildTopBar(frame).Render(c) : null,
                    bottomBar: bars ? c => BuildBottomBar(frame).Render(c) : null,
                    contentWindowInsets: insets);
        }
        else
        {
            RenderImplicit(frame, mode, bars, modifier, insets);
        }
    }

    [Composable]
    internal static void RenderImplicit(
        ScaffoldInsetsFrame frame, int mode, bool bars, Modifier modifier, WindowInsets? insets)
    {
        if (mode == Omitted)
            Composables.Scaffold(
                padding => BuildBody(frame, padding).Render(),
                modifier: modifier,
                topBar: bars ? () => BuildTopBar(frame).Render() : null,
                bottomBar: bars ? () => BuildBottomBar(frame).Render() : null);
        else
            Composables.Scaffold(
                padding => BuildBody(frame, padding).Render(),
                modifier: modifier,
                topBar: bars ? () => BuildTopBar(frame).Render() : null,
                bottomBar: bars ? () => BuildBottomBar(frame).Render() : null,
                contentWindowInsets: insets);
    }

    static WindowInsets? SelectInsets(
        int mode, WindowInsets defaults, WindowInsets navigation, WindowInsets ime)
    {
        return mode switch
        {
            Omitted or ExplicitNull => null,
            Zero => new WindowInsets(),
            Fixed => new WindowInsets(left: 13, top: 23, right: 31, bottom: 41),
            Default => defaults,
            Excluded => defaults.Exclude(navigation).Exclude(ime),
            CompoundZero => new WindowInsets(left: 13, top: 23, right: 31, bottom: 41)
                .Only(WindowInsetsSides.Vertical)
                .Exclude(new WindowInsets(top: 23, bottom: 41)),
            _ => throw new InvalidOperationException("Unknown Scaffold insets scenario."),
        };
    }

    static ComposableNode BuildBody(ScaffoldInsetsFrame frame, PaddingValues padding)
    {
        frame.RecordPadding(padding);
        return new Box
        {
            Modifier.FillMaxSize().Padding(padding),
            new ScaffoldInsetsStateProbe(frame)
            {
                Modifier = Modifier.FillMaxSize(),
            },
        };
    }

    static ComposableNode BuildTopBar(ScaffoldInsetsFrame frame) => new Box
    {
        Modifier.FillMaxWidth().Then(frame.Measure(ScaffoldInsetsFrame.TopBar)),
        new TopAppBar { Title = new Text("Scaffold insets") },
    };

    static ComposableNode BuildBottomBar(ScaffoldInsetsFrame frame) => new Box
    {
        Modifier.FillMaxWidth().Then(frame.Measure(ScaffoldInsetsFrame.BottomBar)),
        new BottomAppBar { new Text("Bottom app bar") },
    };

    static TaskCompletionSource NewSignal() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);
}
