using Android.Runtime;
using Android.Views;
using AndroidX.Activity;
using AndroidX.Compose;
using AndroidX.Compose.Runtime;
using AndroidX.Compose.UI;
using AndroidX.Compose.UI.Layout;
using Modifier = AndroidX.Compose.Modifier;

namespace Microsoft.AndroidX.Compose.DeviceTests;

/// <summary>Real native input owner with live callbacks and removable pointer modifiers.</summary>
[Activity(Theme = "@android:style/Theme.Material.Light.NoActionBar")]
[Register("net/compose/devicetests/LongPressDragTestActivity")]
public class LongPressDragTestActivity : ComponentActivity
{
    internal static TaskCompletionSource<LongPressDragTestActivity> Created = NewSource<LongPressDragTestActivity>();
    internal readonly TaskCompletionSource Focused = new(TaskCreationOptions.RunContinuationsAsynchronously);
    internal readonly TaskCompletionSource Destroyed = new(TaskCreationOptions.RunContinuationsAsynchronously);
    internal TaskCompletionSource Started = new(TaskCreationOptions.RunContinuationsAsynchronously);
    internal TaskCompletionSource Ended = new(TaskCreationOptions.RunContinuationsAsynchronously);
    internal TaskCompletionSource Cancelled = new(TaskCreationOptions.RunContinuationsAsynchronously);
    internal readonly MutableState<int> Version = new(0);
    internal readonly MutableState<int> Key = new(0);
    internal readonly MutableState<bool> Visible = new(true);
    internal readonly MutableState<float> X = new(0);
    internal readonly MutableState<float> Y = new(0);
    internal readonly MutableState<bool> Recording = new(false);
    internal readonly MutableNumberState<float> Swipe = new(0);
    internal readonly List<string> Events = [];
    internal LongPressDragGestureBlock? Handler;
    internal LongPressDragGestureBlock? LowLevelHandler;
    internal View? Owner;
    internal float CenterX, CenterY;
    internal int Passes, StartCount, EndCount, CancelCount, MoveCount, LastVersion;
    bool _recordingMode;
    bool _lastRecording;

    internal static TaskCompletionSource<T> NewSource<T>() => new(TaskCreationOptions.RunContinuationsAsynchronously);

    internal void Arm()
    {
        Started = new(TaskCreationOptions.RunContinuationsAsynchronously);
        Ended = new(TaskCreationOptions.RunContinuationsAsynchronously);
        Cancelled = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        Window?.AddFlags(WindowManagerFlags.KeepScreenOn);
        _recordingMode = Intent?.GetBooleanExtra("recording", false) == true;
        this.SetContent(c => new Composed(Build));
        Created.TrySetResult(this);
    }

    /// <summary>Signals actual input-window readiness.</summary>
    public override void OnWindowFocusChanged(bool hasFocus)
    {
        base.OnWindowFocusChanged(hasFocus);
        if (hasFocus) Focused.TrySetResult();
    }

    protected override void OnDestroy()
    {
        Window?.ClearFlags(WindowManagerFlags.KeepScreenOn);
        base.OnDestroy();
        Destroyed.TrySetResult();
    }

    ComposableNode Build(IComposer composer)
    {
        Owner = LocalView.Current(composer);
        if (_recordingMode) return BuildRecording(composer);
        int version = Version.Value;
        int key = Key.Value;
        bool visible = Visible.Value;
        var callbacks = new LongPressDragCallbacks(
            delta =>
            {
                MoveCount++;
                LastVersion = version;
                X.Value += delta.X;
                Y.Value += delta.Y;
                Record($"move:{version}:{delta.X:F1},{delta.Y:F1}");
            },
            position =>
            {
                StartCount++;
                LastVersion = version;
                Record($"start:{version}:{position.X:F1},{position.Y:F1}");
                Started.TrySetResult();
            },
            () =>
            {
                EndCount++;
                LastVersion = version;
                Record($"end:{version}");
                Ended.TrySetResult();
            },
            () =>
            {
                CancelCount++;
                LastVersion = version;
                Record($"cancel:{version}");
                Cancelled.TrySetResult();
            });
        var highLevel = Modifier.DetectDragGesturesAfterLongPress(
            callbacks.OnDrag, callbacks.OnDragStart, callbacks.OnDragEnd, callbacks.OnDragCancel, key);
        var native = ComposedModifierKt.MaterializeModifier(composer,
            highLevel.Build() ?? throw new InvalidOperationException("Missing pointer modifier."));
        using (var probe = new PointerInputElementProbe())
        {
            native.FoldIn(Kotlin.Unit.Instance, probe);
            Handler = probe.Handler ?? throw new InvalidOperationException("No native pointer-input element.");
        }
        var lowLevel = composer.Remember(() => new LongPressDragGestureBlock(callbacks));
        LowLevelHandler = lowLevel;
        composer.SideEffect(() => lowLevel.Callbacks = callbacks);
        var bounds = new ComposableLambda1(arg =>
        {
            var coordinates = arg?.JavaCast<ILayoutCoordinates>()
                ?? throw new InvalidOperationException("Missing pointer target coordinates.");
            var position = Offset.FromPacked(LayoutCoordinatesKt.PositionOnScreen(coordinates));
            CenterX = position.X + (int)(coordinates.Size >> 32) / 2f;
            CenterY = position.Y + (int)coordinates.Size / 2f;
        });
        composer.SideEffect(() => Passes++);
        return new MaterialTheme
        {
            new Column
            {
                Modifier.FillMaxSize().SafeDrawingPadding(),
                new Text($"Generation {version}: {X.Value:F1}, {Y.Value:F1}"),
                new Tooltip
                {
                    EnableUserInput = false,
                    Tip = new Text("Must not intercept the recording gesture"),
                    Anchor = new Box
                    {
                        Modifier.FillMaxWidth().Height(180)
                            .Background(Color.Blue)
                            .AppendBound(current => OnGloballyPositionedModifierKt.OnGloballyPositioned(current, bounds),
                                ModifierOpKey.Opaque)
                            .Then(visible ? Modifier.Companion.AppendBound(current => current.Then(native), ModifierOpKey.Opaque)
                                : Modifier.Companion),
                        new Text("Long press target") { Color = Color.White },
                    },
                },
                new Box
                {
                    Modifier.FillMaxWidth().Height(100).PointerInput(lowLevel, key),
                    new Text("Bound PointerInput entry point"),
                },
            },
        };
    }

    ComposableNode BuildRecording(IComposer composer)
    {
        var scheme = global::AndroidX.Compose.Material3.MaterialTheme.Instance.GetColorScheme(composer, 0);
        bool recording = Recording.Value;
        float swipe = Swipe.Value;
        composer.SideEffect(() =>
        {
            Passes++;
            if (recording && !_lastRecording)
            {
                StartCount++;
                Record("recording:start");
                Started.TrySetResult();
            }
            _lastRecording = recording;
        });
        var bounds = new ComposableLambda1(arg =>
        {
            var coordinates = arg?.JavaCast<ILayoutCoordinates>()
                ?? throw new InvalidOperationException("Missing recording target coordinates.");
            var position = Offset.FromPacked(LayoutCoordinatesKt.PositionOnScreen(coordinates));
            CenterX = position.X + (int)(coordinates.Size >> 32) / 2f;
            CenterY = position.Y + (int)coordinates.Size / 2f;
        });
        return new MaterialTheme
        {
            new Column
            {
                Modifier.FillMaxSize().SafeDrawingPadding(),
                new Text($"Recording={recording}; swipe={swipe:F1}"),
                new Row
                {
                    Modifier.Padding(start: 280, top: 80),
                    new Row
                    {
                        Modifier.Size(56).AppendBound(current =>
                            OnGloballyPositionedModifierKt.OnGloballyPositioned(current, bounds), ModifierOpKey.Opaque),
                        new Tooltip
                        {
                            EnableUserInput = false,
                            Tip = new Text("Touch and hold to record"),
                            Anchor = global::AndroidX.Compose.Samples.Jetchat.RecordButton.BuildButton(
                                Recording, Swipe,
                                onCommit: () =>
                                {
                                    EndCount++;
                                    Recording.Value = false;
                                    Swipe.Value = 0;
                                    Record("recording:commit");
                                    Ended.TrySetResult();
                                },
                                onCancel: () =>
                                {
                                    CancelCount++;
                                    Recording.Value = false;
                                    Swipe.Value = 0;
                                    Record("recording:cancel");
                                    Cancelled.TrySetResult();
                                }, scheme),
                        },
                    },
                },
            },
        };
    }

    void Record(string message)
    {
        Events.Add(message);
        global::Android.Util.Log.Info("Pointer337", $"pid={(global::Android.OS.Process.MyPid())} {message}");
    }
}
