using Android.OS;
using Android.Runtime;
using Android.Views;
using AndroidX.Activity;
using AndroidX.Compose;
using AndroidX.Compose.Foundation.Interaction;
using AndroidX.Compose.Material3;
using AndroidX.Compose.Runtime;
using AndroidX.Compose.UI.Layout;
using AndroidX.Compose.UI.Platform;
using Snapshot = AndroidX.Compose.Runtime.Snapshots.Snapshot;
using Color = AndroidX.Compose.Color;
using MaterialTheme = AndroidX.Compose.MaterialTheme;

namespace Microsoft.AndroidX.Compose.DeviceTests;

/// <summary>Hosts one FAB for native styling, interaction, and composition-identity checks.</summary>
[Activity(Theme = "@android:style/Theme.Material.Light.NoActionBar")]
[Register("net/compose/devicetests/FabStylingTestActivity")]
public class FabStylingTestActivity : ComponentActivity
{
    internal static TaskCompletionSource<FabStylingTestActivity> Ready { get; set; } = NewReady();
    internal static TaskCompletionSource<FabStylingTestActivity> NewReady() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);
    internal readonly MutableState<int> Phase = new(0);
    internal TaskCompletionSource Committed = NewCompletion();
    internal TaskCompletionSource PressChanged = NewCompletion();
    internal TaskCompletionSource Destroyed = NewCompletion();
    internal object? ContentIdentity;
    internal long ContentColor;
    internal float TonalElevation;
    internal bool Pressed;
    internal int Clicks;
    ILayoutCoordinates? coordinates;
    ILayoutCoordinates? iconCoordinates;
    ILayoutCoordinates? labelCoordinates;
    ViewTreeObserver? frameObserver;
    Java.Lang.IRunnable? frameCallback;
    TaskCompletionSource? frameCompletion;
    TaskCompletionSource? frameNativeCompletion;
    int committedPhase = -1;
    bool resumed;
    int variant;
    bool direct;
    FabContentProbe? probe;
    ComposeView? view;
    IViewRootForTest? owner;
    Recomposer? recomposer;
    Snapshot.Companion? snapshots;
    global::Android.Views.ViewTreeObserver? drawObserver;
    readonly object progressLock = new();
    TaskCompletionSource progress = NewCompletion();

    internal static TaskCompletionSource NewCompletion() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        variant = Intent?.GetIntExtra("variant", 0) ?? 0;
        direct = Intent?.GetBooleanExtra("direct", false) ?? false;
        probe = new FabContentProbe(this);
        view = new ComposeView(this);
        drawObserver = view.ViewTreeObserver
            ?? throw new InvalidOperationException("FAB ComposeView has no draw observer.");
        drawObserver.Draw += OnDraw;
        view.SetContent((IComposer c) =>
        {
            int phase = Phase.Value;
            bool dark = phase is 4 or 6;
            var light = c.Remember(() => MaterialTheme.LightColorScheme(
                primaryContainer: Color.FromHex("#E8D0FF"), onPrimaryContainer: Color.FromHex("#201030"),
                tertiaryContainer: Color.FromHex("#D0F8E0"), onTertiaryContainer: Color.FromHex("#103020")));
            var darkScheme = c.Remember(() => MaterialTheme.DarkColorScheme(
                primaryContainer: Color.FromHex("#402050"), onPrimaryContainer: Color.FromHex("#F0D0FF"),
                tertiaryContainer: Color.FromHex("#104030"), onTertiaryContainer: Color.FromHex("#C0FFE0")));
            var theme = new MaterialTheme { ColorScheme = dark ? darkScheme : light };
            theme.Add(new Column
            {
                Modifier.FillMaxSize().Background(Color.White).Padding(24.Dp()),
                new Text($"FAB {variant}, {(direct ? "direct" : "tree")}, phase {phase}") { Color = Color.Black },
                new Composed(inner =>
                {
                    RenderFab(inner, phase);
                    inner.SideEffect(() =>
                    {
                        committedPhase = phase;
                        Committed.TrySetResult();
                        SignalProgress();
                    });
                    return null;
                }),
            });
            theme.Render(c);
        });
        SetContentView(view);
    }

    void RenderFab(IComposer c, int phase)
    {
        var content = probe ?? throw new InvalidOperationException("FAB content probe missing.");
        var sourceA = c.Remember(InteractionSourceKt.MutableInteractionSource);
        var sourceB = c.Remember(InteractionSourceKt.MutableInteractionSource);
        var flat = c.Remember(() => FloatingActionButtonDefaults.Instance.BottomAppBarFabElevation(0, 12, 8, 16));
        var raised = c.Remember(() => FloatingActionButtonDefaults.Instance.BottomAppBarFabElevation(6, 16, 12, 20));
        bool supplied = phase is 1 or 2 or 7 or 8 or 9;
        Color? container = phase switch
        {
            1 => Color.FromHex("#FFE082"),
            2 or 8 or 9 => Color.Blue,
            5 or 6 => Color.FromPacked(c.ColorScheme().TertiaryContainer),
            7 => Color.Transparent,
            10 => Color.FromHex("#FFE082"),
            11 => Color.FromPacked(c.ColorScheme().PrimaryContainer),
            _ => null,
        };
        Color? foreground = phase is 1 or 7 ? Color.Black : phase is 2 or 8 or 9 ? Color.White : null;
        var elevation = supplied ? (phase is 1 or 7 ? flat : raised) : null;
        var source = supplied ? (phase is 1 or 7 ? sourceA : sourceB) : null;
        var pressed = PressInteractionKt.CollectIsPressedAsState(source ?? sourceA, c, 0);
        bool isPressed = pressed.Value is Java.Lang.Boolean value && value.BooleanValue();
        c.SideEffect(() =>
        {
            if (Pressed != isPressed)
            {
                Pressed = isPressed;
                PressChanged.TrySetResult();
            }
        });
        var modifier = c.Remember(() =>
        {
            var placed = new ComposableLambda1(value =>
            {
                coordinates = value?.JavaCast<ILayoutCoordinates>()
                    ?? throw new InvalidOperationException("FAB placement did not provide native coordinates.");
                SignalProgress();
            });
            return Modifier.TestTag("fab").AppendBound(
                bound => OnGloballyPositionedModifierKt.OnGloballyPositioned(bound, placed),
                ModifierOpKey.Opaque);
        });
        Modifier? suppliedModifier = phase == 14 ? null : modifier;
        Shape? shape = phase == 12 ? Shape.Rectangle : null;
        var logical = FloatingActionButtonDefault.All;
        if (suppliedModifier is not null) logical &= ~FloatingActionButtonDefault.Modifier;
        if (shape is not null) logical &= ~FloatingActionButtonDefault.Shape;
        if (container is not null) logical &= ~FloatingActionButtonDefault.ContainerColor;
        if (foreground is not null) logical &= ~FloatingActionButtonDefault.ContentColor;
        if (elevation is not null) logical &= ~FloatingActionButtonDefault.Elevation;
        if (source is not null) logical &= ~FloatingActionButtonDefault.InteractionSource;
        int logicalMask = variant == 3
            ? (int)Enum.Parse<ExtendedFloatingActionButtonDefault>(logical.ToString())
            : (int)logical;
        global::Android.Util.Log.Info("FabStyling",
            $"Before FAB variant={variant}, direct={direct}, phase={phase}, logicalDefault={logicalMask}, " +
            $"nativeDefault=0, nativeChanged=0, source={(source is null ? "native-owned" : ReferenceEquals(source, sourceA) ? "A" : "B")}");
        Action click = () => Clicks++;
        if (variant == 4)
        {
            // Independent bound control: every native default bit is fixed, with no facade resolver.
            global::AndroidX.Compose.UI.Graphics.IShape nativeShape;
            long resolvedContainer, nativeContent;
            FloatingActionButtonElevation nativeElevation;
            c.StartReplaceableGroup(CompositionGroupKey.Compute(0, typeof(FabStylingTestActivity)));
            try
            {
                nativeShape = FloatingActionButtonDefaults.Instance.GetShape(c, 0);
                long nativeContainer = FloatingActionButtonDefaults.Instance.GetContainerColor(c, 0);
                resolvedContainer = container?.ToPacked() ?? nativeContainer;
                nativeContent = ColorSchemeKt.ContentColorFor(resolvedContainer, c, 0);
                nativeElevation = FloatingActionButtonDefaults.Instance.Elevation(0, 0, 0, 0, c,
                    p5: 0, _changed: (int)FloatingActionButtonElevationDefault.All);
            }
            finally
            {
                c.EndReplaceableGroup();
            }
            var body = ComposableLambdas.Wrap2(c, inner => content.Render(inner));
            FloatingActionButtonKt.FloatingActionButton(c.RememberAction(click),
                suppliedModifier?.Build() ?? Modifier.BuildEmpty(),
                shape?.JavaCast<global::AndroidX.Compose.UI.Graphics.IShape>() ?? nativeShape,
                resolvedContainer, foreground?.ToPacked() ?? nativeContent, elevation ?? nativeElevation, source,
                body, c, p9: 0, _changed: (int)FloatingActionButtonDefault.None);
            return;
        }
        if (direct)
        {
            RenderDirect(c, this, click, suppliedModifier, shape, container, foreground, elevation, source, phase != 8);
            return;
        }
        if (variant == 3)
        {
            new ExtendedFloatingActionButton(click, phase != 8)
            {
                Modifier = suppliedModifier, Shape = shape, Icon = content, Text = CreateLabel(),
                ContainerColor = container, ContentColor = foreground, Elevation = elevation, InteractionSource = source,
            }.Render(c);
            return;
        }
        var node = c.Remember<ComposableContainer>(() =>
        {
            ComposableContainer created = variant switch
            {
                0 => new FloatingActionButton(click),
                1 => new SmallFloatingActionButton(click),
                2 => new LargeFloatingActionButton(click),
                _ => throw new InvalidOperationException("Unknown FAB variant."),
            };
            created.Add(content);
            return created;
        });
        node.Modifier = suppliedModifier;
        switch (node)
        {
            case FloatingActionButton fab:
                fab.Shape = shape;
                fab.ContainerColor = container; fab.ContentColor = foreground; fab.Elevation = elevation; fab.InteractionSource = source;
                break;
            case SmallFloatingActionButton fab:
                fab.Shape = shape;
                fab.ContainerColor = container; fab.ContentColor = foreground; fab.Elevation = elevation; fab.InteractionSource = source;
                break;
            case LargeFloatingActionButton fab:
                fab.Shape = shape;
                fab.ContainerColor = container; fab.ContentColor = foreground; fab.Elevation = elevation; fab.InteractionSource = source;
                break;
        }
        node.Render(c);
    }

    [global::AndroidX.Compose.Composable]
    internal static void RenderDirect(IComposer c, FabStylingTestActivity activity, Action click,
        Modifier? modifier, Shape? shape, Color? container, Color? foreground, FloatingActionButtonElevation? elevation,
        IMutableInteractionSource? source, bool expanded)
    {
        var probe = activity.probe ?? throw new InvalidOperationException("Direct FAB content probe missing.");
        // These are surfaced-argument omission bits, not Kotlin default masks.
        // One lexical helper site per variant exercises live omission changes.
        ulong omitted = shape is null ? 1UL << 3 : 0;
        if (modifier is null) omitted |= 1UL << 2;
        if (container is null) omitted |= 1UL << 4;
        if (foreground is null) omitted |= 1UL << 5;
        if (elevation is null) omitted |= 1UL << 6;
        if (source is null) omitted |= 1UL << 7;
        switch (activity.variant)
        {
            case 0:
                Composables.FloatingActionButton_PrimaryResource_Implicit_WithAddedSlots(c, click, () => probe.Render(),
                    modifier, shape, container, foreground, elevation, source, omitted);
                break;
            case 1:
                Composables.SmallFloatingActionButton_PrimaryResource_Implicit_WithAddedSlots(c, click, () => probe.Render(),
                    modifier, shape, container, foreground, elevation, source, omitted);
                break;
            case 2:
                Composables.LargeFloatingActionButton_PrimaryResource_Implicit_WithAddedSlots(c, click, () => probe.Render(),
                    modifier, shape, container, foreground, elevation, source, omitted);
                break;
            case 3:
                Composables.ExtendedFloatingActionButton_PrimaryResource_Implicit_WithAddedSlots(c, click, expanded,
                    () => activity.CreateLabel().Render(), () => probe.Render(),
                    modifier, shape, container, foreground, elevation, source, omitted << 2);
                break;
        }
    }

    ComposableNode CreateLabel() => new Composed(c =>
        new Text("Expanded label") { Modifier = MeasureContent(c, label: true) });

    internal Modifier MeasureContent(IComposer composer, bool label) => composer.Remember(() =>
    {
        var placed = new ComposableLambda1(value =>
        {
            var current = value?.JavaCast<ILayoutCoordinates>()
                ?? throw new InvalidOperationException("FAB content placement did not provide native coordinates.");
            if (label) labelCoordinates = current;
            else iconCoordinates = current;
            SignalProgress();
        });
        return Modifier.Companion.AppendBound(
            bound => OnGloballyPositionedModifierKt.OnGloballyPositioned(bound, placed),
            ModifierOpKey.Opaque);
    });

    protected override void OnResume()
    {
        base.OnResume();
        resumed = true;
        if (HasWindowFocus) Ready.TrySetResult(this);
        SignalProgress();
    }

    public override void OnWindowFocusChanged(bool hasFocus)
    {
        base.OnWindowFocusChanged(hasFocus);
        if (hasFocus && resumed) Ready.TrySetResult(this);
        SignalProgress();
    }

    protected override void OnPause()
    {
        resumed = false;
        SignalProgress();
        base.OnPause();
    }

    protected override void OnDestroy()
    {
        RemoveFrameCallback();
        frameCompletion?.TrySetException(new ObjectDisposedException(nameof(FabStylingTestActivity)));
        frameCompletion = null;
        if (drawObserver is { IsAlive: true }) drawObserver.Draw -= OnDraw;
        drawObserver = null;
        owner = null;
        recomposer = null;
        snapshots = null;
        coordinates = null;
        iconCoordinates = null;
        labelCoordinates = null;
        view = null;
        base.OnDestroy();
        Destroyed.TrySetResult();
    }

    void OnDraw(object? sender, EventArgs e) => SignalProgress();

    void SignalProgress()
    {
        lock (progressLock)
        {
            progress.TrySetResult();
            progress = NewCompletion();
        }
    }

    internal async Task<(int Left, int Top, int Width, int Height, int WindowLeft, int WindowTop,
        (float Left, float Top, float Right, float Bottom)[] Content)> WaitForNativeIdleAsync()
    {
        var instrumentation = TestInstrumentation.Current
            ?? throw new InvalidOperationException("FAB instrumentation is not running.");
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        while (true)
        {
            Task changed;
            lock (progressLock) changed = progress.Task;
            await Task.Run(instrumentation.WaitForIdleSync).WaitAsync(timeout.Token);
            bool idle = false, pending = false;
            (int Left, int Top, int Width, int Height, int WindowLeft, int WindowTop,
                (float Left, float Top, float Right, float Bottom)[] Content) bounds = default;
            Exception? failure = null;
            instrumentation.RunOnMainSync(() =>
            {
                try
                {
                    var composeView = view ?? throw new InvalidOperationException("FAB ComposeView missing.");
                    owner ??= (composeView.GetChildAt(0)
                        ?? throw new InvalidOperationException("FAB native owner missing.")).JavaCast<IViewRootForTest>();
                    recomposer ??= WindowRecomposer_androidKt.FindViewTreeCompositionContext(composeView) as Recomposer
                        ?? throw new InvalidOperationException("FAB window Recomposer missing.");
                    if (snapshots is null)
                    {
                        using var field = Java.Lang.Class.FromType(typeof(Snapshot)).GetField("Companion")
                            ?? throw new InvalidOperationException("Snapshot.Companion field missing.");
                        var singleton = field.Get(null)
                            ?? throw new InvalidOperationException("Snapshot.Companion singleton missing.");
                        snapshots = singleton.JavaCast<Snapshot.Companion>();
                    }
                    pending = owner.HasPendingMeasureOrLayout || recomposer.HasPendingWork
                        || snapshots.Current.HasPendingChanges || snapshots.IsApplyObserverNotificationPending;
                    if (coordinates is { IsAttached: true } current && iconCoordinates is { IsAttached: true }
                        && composeView.IsAttachedToWindow
                        && composeView.HasWindowFocus && HasWindowFocus && resumed && owner.IsLifecycleInResumedState
                        && committedPhase == Phase.Value && !pending)
                    {
                        var position = Offset.FromPacked(LayoutCoordinatesKt.PositionOnScreen(current));
                        var windowPosition = Offset.FromPacked(LayoutCoordinatesKt.PositionInWindow(current));
                        List<(float Left, float Top, float Right, float Bottom)> content = [];
                        ILayoutCoordinates?[] items = [iconCoordinates, labelCoordinates];
                        foreach (var item in items)
                        {
                            if (item is not { IsAttached: true }) continue;
                            var origin = Offset.FromPacked(LayoutCoordinatesKt.PositionInWindow(item));
                            content.Add((origin.X, origin.Y,
                                origin.X + (int)((ulong)item.Size >> 32), origin.Y + (int)(item.Size & uint.MaxValue)));
                        }
                        bounds = ((int)MathF.Round(position.X), (int)MathF.Round(position.Y),
                            (int)((ulong)current.Size >> 32), (int)(current.Size & uint.MaxValue),
                            (int)MathF.Round(windowPosition.X), (int)MathF.Round(windowPosition.Y), [.. content]);
                        idle = true;
                    }
                }
                catch (Exception error)
                {
                    failure = error;
                }
            });
            if (failure is not null) throw new InvalidOperationException("Native FAB readiness probe failed.", failure);
            if (idle) return bounds;
            if (!pending) await changed.WaitAsync(timeout.Token);
        }
    }

    internal async Task<global::Android.Graphics.Bitmap> CaptureFabAsync(global::Android.Graphics.Rect bounds)
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(29))
            throw new PlatformNotSupportedException("FAB pixel assertions require native frame-commit callbacks.");
        var instrumentation = TestInstrumentation.Current
            ?? throw new InvalidOperationException("FAB instrumentation is not running.");
        var committed = NewCompletion();
        var nativeFrameFinished = NewCompletion();
        var frame = new Java.Lang.Runnable(() =>
        {
            committed.TrySetResult();
            nativeFrameFinished.TrySetResult();
        });
        bool registered = false;
        try
        {
            instrumentation.RunOnMainSync(() =>
            {
                try
                {
                    var composeView = view ?? throw new InvalidOperationException("FAB ComposeView missing.");
                    frameObserver = composeView.ViewTreeObserver
                        ?? throw new InvalidOperationException("FAB draw observer missing.");
                    frameCallback = frame;
                    frameCompletion = committed;
                    frameNativeCompletion = nativeFrameFinished;
                    if (OperatingSystem.IsAndroidVersionAtLeast(29))
                        frameObserver.RegisterFrameCommitCallback(frame);
                    registered = true;
                    composeView.Invalidate();
                }
                catch (Exception error)
                {
                    committed.TrySetException(error);
                }
            });
            await committed.Task.WaitAsync(TimeSpan.FromSeconds(15));
        }
        finally
        {
            instrumentation.RunOnMainSync(() =>
            {
                RemoveFrameCallback();
                frameCompletion = null;
            });
            if (!registered || nativeFrameFinished.Task.IsCompleted)
                frame.Dispose();
            else
                RetireAfterNativeCompletion(nativeFrameFinished.Task, frame.Dispose, "frame commit");
        }

        var config = global::Android.Graphics.Bitmap.Config.Argb8888
            ?? throw new InvalidOperationException("Native ARGB bitmap format unavailable.");
        var bitmap = global::Android.Graphics.Bitmap.CreateBitmap(bounds.Width(), bounds.Height(), config)
            ?? throw new InvalidOperationException("Native FAB bitmap allocation failed.");
        var listener = new FabPixelCopyListener();
        var handler = new Handler(Looper.MainLooper
            ?? throw new InvalidOperationException("Native main looper unavailable."));
        bool requested = false;
        bool transferred = false;
        try
        {
            // Copy the committed window buffer, not an activity-entry animation composited by SurfaceFlinger.
            instrumentation.RunOnMainSync(() =>
            {
                try
                {
                    if (OperatingSystem.IsAndroidVersionAtLeast(26))
                    {
                        PixelCopy.Request(Window ?? throw new InvalidOperationException("FAB window unavailable."),
                            bounds, bitmap, listener, handler);
                        requested = true;
                    }
                }
                catch (Exception error)
                {
                    listener.Completion.TrySetException(error);
                }
            });
            await listener.Completion.Task.WaitAsync(TimeSpan.FromSeconds(15));
            transferred = true;
            return bitmap;
        }
        finally
        {
            if (!requested || listener.Completion.Task.IsCompleted)
            {
                if (!transferred) bitmap.Dispose();
                listener.Dispose();
                handler.Dispose();
            }
            else
            {
                RetireAfterNativeCompletion(listener.Completion.Task, () =>
                {
                    bitmap.Dispose();
                    listener.Dispose();
                    handler.Dispose();
                }, "PixelCopy");
            }
        }
    }

    void RemoveFrameCallback()
    {
        if (frameNativeCompletion?.Task.IsCompleted != true
            && OperatingSystem.IsAndroidVersionAtLeast(29)
            && frameObserver is { IsAlive: true } observer && frameCallback is { } callback)
        {
            if (observer.UnregisterFrameCommitCallback(callback))
                frameNativeCompletion?.TrySetResult();
        }
        frameObserver = null;
        frameCallback = null;
        frameNativeCompletion = null;
    }

    static void RetireAfterNativeCompletion(Task completion, Action release, string operation)
    {
        _ = completion.ContinueWith(finished =>
        {
            global::Android.Util.Log.Warn("FabStyling",
                $"Late {operation} completion after the managed waiter ended: {finished.Exception?.ToString() ?? "success"}.");
            try
            {
                release();
            }
            catch (Exception error)
            {
                global::Android.Util.Log.Error("FabStyling", $"Late {operation} cleanup failed: {error}");
            }
        }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
    }
}
