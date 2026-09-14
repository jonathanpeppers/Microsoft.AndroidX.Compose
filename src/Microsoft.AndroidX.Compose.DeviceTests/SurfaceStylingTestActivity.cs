using Android.Runtime;
using AndroidX.Activity;
using AndroidX.Compose;
using AndroidX.Compose.Foundation;
using AndroidX.Compose.Material3;
using AndroidX.Compose.Runtime;
using AndroidX.Compose.UI.Platform;
using Kotlin.Jvm.Functions;
using Color = AndroidX.Compose.Color;
using MaterialTheme = AndroidX.Compose.MaterialTheme;
using Snapshot = AndroidX.Compose.Runtime.Snapshots.Snapshot;
using Surface = AndroidX.Compose.Surface;

namespace Microsoft.AndroidX.Compose.DeviceTests;

/// <summary>Exercises one stable Surface call site through live styling and recreation.</summary>
[Activity(Theme = "@android:style/Theme.Material.Light.NoActionBar")]
[Register("net/compose/devicetests/SurfaceStylingTestActivity")]
public class SurfaceStylingTestActivity : ComponentActivity
{
    internal static readonly Color CustomColor = Color.FromRgb(0x51, 0x2B, 0xD4);
    internal static readonly Color BorderColor = Color.FromRgb(0xE6, 0xAF, 0x2E);
    internal static readonly Color Backdrop = Color.FromRgb(0xB2, 0xDF, 0xDB);
    internal static TaskCompletionSource<SurfaceStylingTestActivity> Started { get; private set; } = NewStarted();
    internal static int Style;
    internal static bool Dark;
    internal static bool LiteralDefaults;

    readonly object _progressLock = new();
    TaskCompletionSource _progress = NewSignal();
    readonly MutableManagedState<(int Generation, int Mode)> _request = new((0, 0));
    readonly MutableManagedState<bool> _dark = new(false);
    ComposeView? _view;
    bool _resumed;
    bool _ending;
    bool _admitted;
    string? _foregroundFailure;
    Snapshot.Companion? _snapshots;
    IFunction2? _content;
    int _defaults;
    int _nativeDefaults;
    long _nativeColor;
    long _nativeContentColor;
    int _changed;
    object? _tail;
    MutableNumberState<int>? _outsideCounter;
    SurfaceStylingSnapshot? _snapshot;
    internal TaskCompletionSource Destroyed { get; } = NewSignal();
    internal ColorScheme? Scheme { get; private set; }

    internal static void Prepare(int style, bool dark, bool literalDefaults = false)
    {
        Style = style;
        Dark = dark;
        LiteralDefaults = literalDefaults;
        Started = NewStarted();
    }

    internal static void PrepareRecreation() => Started = NewStarted();

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        Window?.AddFlags(global::Android.Views.WindowManagerFlags.KeepScreenOn);
        _request.Value = (0, savedInstanceState?.GetInt("surface-mode", 0) ?? 0);
        _dark.Value = savedInstanceState?.GetBoolean("surface-dark", Dark) ?? Dark;
        if (Surface.ContentObserver is not null)
            throw new InvalidOperationException("A Surface observer is already installed.");
        Surface.ContentObserver = ObserveContent;
        var view = new ComposeView(this) { Id = 0x34301 };
        _view = view;
        var observer = view.ViewTreeObserver
            ?? throw new InvalidOperationException("Surface test ViewTreeObserver unavailable.");
        observer.Draw += OnDraw;
        view.SetContent(c =>
        {
            bool dark = _dark.Value;
            Scheme = c.Remember(() => dark ? MaterialTheme.DarkColorScheme() : MaterialTheme.LightColorScheme(), dark);
            var theme = new MaterialTheme { ColorScheme = Scheme };
            var parent = new Surface { TonalElevation = 2, ContentColor = Color.FromPacked(Scheme.Secondary) };
            parent.Add(new Column
            {
                Modifier.FillMaxSize().Background(Backdrop).Padding(24.Dp()),
                new Composed(BuildSurface),
            });
            theme.Add(parent);
            return theme;
        });
        SetContentView(view);
        Started.TrySetResult(this);
    }

    protected override void OnSaveInstanceState(Bundle outState)
    {
        outState.PutInt("surface-mode", _request.Value.Mode);
        outState.PutBoolean("surface-dark", _dark.Value);
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
            _foregroundFailure = "Surface test activity paused during measurement.";
        Signal();
        base.OnPause();
    }

    /// <summary>Signals live native focus changes; cached callbacks never substitute for HasWindowFocus.</summary>
    public override void OnWindowFocusChanged(bool hasFocus)
    {
        base.OnWindowFocusChanged(hasFocus);
        if (!hasFocus && _admitted && !_ending && _view?.HasWindowFocus != true)
            _foregroundFailure = "Surface test window lost focus during measurement.";
        Signal();
    }

    protected override void OnDestroy()
    {
        if (_view?.ViewTreeObserver is { IsAlive: true } observer)
            observer.Draw -= OnDraw;
        Surface.ContentObserver = null;
        _content = null;
        _snapshot = null;
        _snapshots = null;
        _view = null;
        base.OnDestroy();
        Destroyed.TrySetResult();
    }

    internal void ExpectEnd() => _ending = true;
    internal int Change(int mode)
    {
        var next = (_request.Value.Generation + 1, mode);
        _request.Value = next;
        return next.Item1;
    }

    internal int ChangePalette(bool dark)
    {
        _dark.Value = dark;
        return Change(_request.Value.Mode);
    }

    void ObserveContent(IFunction2 content, int defaults, int nativeDefaults,
        long nativeColor, long nativeContentColor, int changed)
    {
        _content = content;
        _defaults = defaults;
        _nativeDefaults = nativeDefaults;
        _nativeColor = nativeColor;
        _nativeContentColor = nativeContentColor;
        _changed = changed;
    }

    ComposableNode BuildSurface(IComposer composer)
    {
        var (generation, mode) = _request.Value;
        _outsideCounter = composer.RememberSaveable(() => new MutableNumberState<int>(0));
        var border = composer.Remember(() => BorderStrokeKt.BorderStroke(2, BorderColor.ToPacked()));
        bool styled = mode is 1 or 4 or 5;
        var modifier = styled ? Modifier.Alpha(1f) : null;
        Color? color = mode is 1 or 4 or 7 ? CustomColor : mode == 2 ? Color.Transparent : null;
        Color? contentColor = mode is 1 or 6 ? Color.White : mode == 2 ? Color.Transparent : null;
        global::AndroidX.Compose.Dp? elevation = mode is 1 or 5 ? 8 : mode == 2 ? 0 : null;
        var suppliedBorder = mode == 1 ? border : null;

        if (Style == 4)
        {
            var scheme = Scheme ?? throw new InvalidOperationException("Native control theme unavailable.");
            bool explicitZero = mode is 2 or 3;
            long resolvedColor = mode is 1 or 4 or 7 ? CustomColor.ToPacked()
                : explicitZero ? 0L : scheme.Surface;
            // A fixed Kotlin call shape: contentColorFor is present even when
            // the control supplies an explicit content color.
            long defaultContent = ColorSchemeKt.ContentColorFor(resolvedColor, composer, 0);
            long resolvedContent = mode is 1 or 6 ? Color.White.ToPacked()
                : explicitZero ? 0L : defaultContent;
            var content = ComposableLambdas.Wrap2(composer, c => Probe(c, generation, mode).Render(c));
            const int nativeDefaults = (int)(SurfaceDefault.Modifier | SurfaceDefault.Shape);
            ObserveContent(content, nativeDefaults, nativeDefaults, resolvedColor, resolvedContent, 0);
            SurfaceKt.Surface(null, null, resolvedColor, resolvedContent,
                elevation?.Value ?? 0, elevation?.Value ?? 0, suppliedBorder, content,
                composer, 0, nativeDefaults);
        }
        else if (Style == 0)
        {
            var surface = new Surface
            {
                Modifier = modifier, Color = color, ContentColor = contentColor,
                TonalElevation = elevation, ShadowElevation = elevation, Border = suppliedBorder,
            };
            surface.Add(new Composed(c => Probe(c, generation, mode)));
            surface.Render(composer);
        }
        else if (LiteralDefaults)
        {
            if (Style == 1)
                Composables.Surface(composer, c => Probe(c, generation, mode).Render(c));
            else if (Style == 3)
                Composables.Surface_PrimaryResource_Implicit(
                    composer, () => Probe(ComposableContext.Current, generation, mode).Render(),
                    __omittedArguments: 0x6);
            else
                RenderLiteralImplicit(this, generation, mode);
        }
        else
        {
            // Surface's public helper bitmap uses content, modifier, shape, then the five new slots.
            ulong omitted = mode switch
            {
                1 => 0x4,
                2 or 3 => 0x6,
                4 => 0xF4,
                5 => 0x9C,
                6 => 0xEE,
                7 => 0xF6,
                _ => 0xFE,
            };
            if (Style == 1)
                Composables.Surface_PrimaryResource_Explicit_WithAddedSlots(
                    composer, c => Probe(c, generation, mode).Render(c),
                    modifier: modifier, color: color, contentColor: contentColor,
                    tonalElevation: elevation, shadowElevation: elevation, border: suppliedBorder,
                    __omittedArguments: omitted);
            else
                Composables.Surface_PrimaryResource_Implicit_WithAddedSlots(
                    composer, () => Probe(ComposableContext.Current, generation, mode).Render(),
                    modifier: modifier, color: color, contentColor: contentColor,
                    tonalElevation: elevation, shadowElevation: elevation, border: suppliedBorder,
                    __omittedArguments: omitted);
        }
        _tail = composer.Remember(static () => new object());
        return new Spacer();
    }

    [global::AndroidX.Compose.Composable]
    internal static void RenderLiteralImplicit(SurfaceStylingTestActivity activity, int generation, int mode) =>
        Composables.Surface(() => activity.Probe(ComposableContext.Current, generation, mode).Render());

    ComposableNode Probe(IComposer composer, int generation, int mode)
    {
        var sentinel = composer.Remember(static () => new object());
        var counter = composer.RememberSaveable(() => new MutableNumberState<int>(0));
        var content = ContentColorKt.LocalContentColor.GetCurrent(composer, 0)
            ?.JavaCast<global::AndroidX.Compose.UI.Graphics.Color>()
            ?? throw new InvalidOperationException("Surface LocalContentColor unavailable.");
        var tonal = SurfaceKt.LocalAbsoluteTonalElevation.GetCurrent(composer, 0)
            ?.JavaCast<global::AndroidX.Compose.UI.Unit.Dp>()
            ?? throw new InvalidOperationException("Surface absolute tonal elevation unavailable.");
        long packed = unchecked((long)content.Value);
        float elevation = tonal.Value;
        int value = counter.Value;
        var counterPeer = ((IMutableStateWrapper)counter).State;
        var outside = _outsideCounter ?? throw new InvalidOperationException("Surface outside counter unavailable.");
        int outsideValue = outside.Value;
        var lambda = _content ?? throw new InvalidOperationException("Surface lambda not observed.");
        int defaults = _defaults, nativeDefaults = _nativeDefaults, changed = _changed;
        long nativeColor = _nativeColor, nativeContentColor = _nativeContentColor;
        composer.SideEffect(() =>
        {
            _snapshot = new(generation, mode, packed, elevation, sentinel, counter, counterPeer, value,
                outside, outsideValue,
                _tail ?? throw new InvalidOperationException("Surface trailing remember not observed."),
                lambda, defaults, nativeDefaults, nativeColor, nativeContentColor, changed);
            Signal();
        });
        return new Box
        {
            Modifier.Size(200.Dp(), 100.Dp()),
            new Text("Surface content") { Modifier = Modifier.Padding(16.Dp()) },
        };
    }

    internal async Task<SurfaceStylingSnapshot> ReadAtIdle()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var instrumentation = TestInstrumentation.Current
            ?? throw new InvalidOperationException("Surface test instrumentation unavailable.");
        while (true)
        {
            Task progress;
            lock (_progressLock) progress = _progress.Task;
            await Task.Run(instrumentation.WaitForIdleSync).WaitAsync(timeout.Token);
            bool pending = false, admitted = false;
            await OnUi(() =>
            {
                if (_foregroundFailure is { } failure)
                    throw new InvalidOperationException(failure);
                var view = _view ?? throw new InvalidOperationException("Surface ComposeView unavailable.");
                if (view.GetChildAt(0) is not { } child)
                    return;
                var owner = child.JavaCast<IViewRootForTest>();
                var recomposer = WindowRecomposer_androidKt.FindViewTreeCompositionContext(view) as Recomposer
                    ?? throw new InvalidOperationException("Surface window recomposer unavailable.");
                if (_snapshots is null)
                {
                    using var field = Java.Lang.Class.FromType(typeof(Snapshot)).GetField("Companion")
                        ?? throw new InvalidOperationException("Snapshot.Companion field unavailable.");
                    _snapshots = field.Get(null)?.JavaCast<Snapshot.Companion>()
                        ?? throw new InvalidOperationException("Snapshot.Companion unavailable.");
                }
                pending = owner.HasPendingMeasureOrLayout || recomposer.HasPendingWork
                    || _snapshots.Current.HasPendingChanges || _snapshots.IsApplyObserverNotificationPending;
                admitted = _resumed && view.HasWindowFocus && owner.IsLifecycleInResumedState && !_ending;
                if (admitted) _admitted = true;
            }).WaitAsync(timeout.Token);
            if (admitted && !pending)
                return _snapshot ?? throw new InvalidOperationException("Native-idle Surface has no committed probe.");
            if (pending) continue;
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
        return completion.Task.WaitAsync(TimeSpan.FromSeconds(10));
    }

    internal (int X, int Y) Pixel(float x, float y)
    {
        var view = _view ?? throw new InvalidOperationException("Surface view unavailable for capture.");
        int[] location = new int[2];
        view.GetLocationOnScreen(location);
        float density = Resources?.DisplayMetrics?.Density
            ?? throw new InvalidOperationException("Surface display density unavailable.");
        return (location[0] + (int)((24 + x) * density), location[1] + (int)((24 + y) * density));
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
    static TaskCompletionSource<SurfaceStylingTestActivity> NewStarted() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);
}
