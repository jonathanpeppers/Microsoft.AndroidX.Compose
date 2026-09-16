using Android.Runtime;
using AndroidX.Activity;
using AndroidX.Compose;
using AndroidX.Compose.UI.Layout;
using AndroidX.Compose.UI.Platform;
using System.Collections.Concurrent;
using LayoutDirection = AndroidX.Compose.UI.Unit.LayoutDirection;

namespace Microsoft.AndroidX.Compose.DeviceTests;

/// <summary>Renders stable shape peers through native resizing, RTL changes, and recomposition.</summary>
[Activity(Theme = "@android:style/Theme.Material.Light.NoActionBar")]
[Register("net/compose/devicetests/ShapeRenderingTestActivity")]
public class ShapeRenderingTestActivity : ComponentActivity
{
    internal static TaskCompletionSource<ShapeRenderingTestActivity> Started { get; private set; } = NewStarted();
    internal ShapeCallbackProbe Probe { get; } = new();
    internal GenericShape? Generic;
    internal MutableState<int> Cycle { get; } = new(0);
    internal ConcurrentDictionary<string, (float X, float Y, int Width, int Height)> Tiles { get; } = new();
    internal int RenderedCycle = -1;
    internal bool Resumed;
    internal bool Ending;
    internal string? ForegroundFailure;
    internal TaskCompletionSource Destroyed { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    ComposeView? _view;

    internal static void Prepare() => Started = NewStarted();
    static TaskCompletionSource<ShapeRenderingTestActivity> NewStarted() => new(TaskCreationOptions.RunContinuationsAsynchronously);

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        var view = new ComposeView(this);
        _view = view;
        view.SetContent(c => new Composed(composer =>
        {
            int cycle = Cycle.Value;
            Generic = composer.Remember(() => new GenericShape(Probe.Build));
            var cut = composer.Remember(() => new CutCornerShape(40.Dp(), 0.Dp(), 0.Dp(), 0.Dp()));
            var absoluteCut = composer.Remember(() => new AbsoluteCutCornerShape(40.Dp(), 0.Dp(), 0.Dp(), 0.Dp()));
            var absoluteRound = composer.Remember(() => new AbsoluteRoundedCornerShape(40.Dp(), 0.Dp(), 0.Dp(), 0.Dp()));
            int width = cycle % 2 == 0 ? 120 : 160;
            var direction = (cycle % 2 == 0 ? LayoutDirection.Ltr : LayoutDirection.Rtl)
                ?? throw new InvalidOperationException("Shape test layout direction missing.");
            RenderedCycle = cycle;
            return new CompositionLocalProvider
            {
                new global::AndroidX.Compose.ProvidedValue(CompositionLocalsKt.LocalLayoutDirection.Provides(direction)),
                new Column
                {
                    Modifier.FillMaxSize().Background(Color.White).Padding(24.Dp()),
                    Tile("generic", Generic, width),
                    Spacer.Height(12),
                    Tile("cut", cut, width),
                    Spacer.Height(12),
                    Tile("absolute-cut", absoluteCut, width),
                    Spacer.Height(12),
                    Tile("absolute-round", absoluteRound, width),
                },
            };
        }));
        SetContentView(view);
        Started.TrySetResult(this);
    }

    Box Tile(string id, Shape shape, int width)
    {
        var callback = new ComposableLambda1(value =>
        {
            var coordinates = value?.JavaCast<ILayoutCoordinates>()
                ?? throw new InvalidOperationException("Shape layout coordinates missing.");
            var position = Offset.FromPacked(LayoutCoordinatesKt.PositionInRoot(coordinates));
            var size = coordinates.Size;
            Tiles[id] = (position.X, position.Y, (int)(size >> 32), (int)size);
        });
        return new Box
        {
            Modifier.Companion.AppendBound(
                current => OnGloballyPositionedModifierKt.OnGloballyPositioned(current, callback),
                ModifierOpKey.Opaque).Size(width, 80).Clip(shape).Background(Color.Red),
        };
    }

    protected override void OnResume() { base.OnResume(); Resumed = true; }

    protected override void OnPause()
    {
        Resumed = false;
        if (!Ending)
            ForegroundFailure = "Shape activity paused during native acceptance.";
        base.OnPause();
    }

    /// <inheritdoc/>
    public override void OnWindowFocusChanged(bool hasFocus)
    {
        base.OnWindowFocusChanged(hasFocus);
        if (!hasFocus && RenderedCycle >= 0 && !Ending)
            ForegroundFailure = "Shape activity lost native window focus.";
    }

    internal Task OnUi(Action action)
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        RunOnUiThread(() =>
        {
            try { action(); completion.SetResult(); }
            catch (Exception error) { completion.SetException(error); }
        });
        return completion.Task.WaitAsync(TimeSpan.FromSeconds(10));
    }

    internal async Task CommitFrame()
    {
        SurfaceFrameCommit? frame = null;
        await OnUi(() =>
        {
            var view = _view ?? throw new InvalidOperationException("Shape view missing.");
            if (!Resumed || !HasWindowFocus || !view.IsAttachedToWindow || !view.IsHardwareAccelerated)
                throw new InvalidOperationException("Shape capture requires a resumed, focused, attached hardware window.");
            if (ForegroundFailure is { } failure)
                throw new InvalidOperationException(failure);
            frame = new SurfaceFrameCommit(view.ViewTreeObserver
                ?? throw new InvalidOperationException("Shape frame observer missing."));
            view.Invalidate();
        });
        using var committed = frame ?? throw new InvalidOperationException("Shape frame not armed.");
        await committed.Committed.WaitAsync(TimeSpan.FromSeconds(10));
    }

    internal (int X, int Y) Pixel(string id, float fractionX, float fractionY, bool screen = false)
    {
        var tile = Tiles[id];
        int[] location = new int[2];
        var view = _view ?? throw new InvalidOperationException("Shape view missing.");
        if (screen)
            view.GetLocationOnScreen(location);
        else
            view.GetLocationInWindow(location);
        return ((int)(location[0] + tile.X + tile.Width * fractionX),
            (int)(location[1] + tile.Y + tile.Height * fractionY));
    }

    protected override void OnDestroy()
    {
        _view?.DisposeComposition();
        _view = null;
        Generic = null;
        base.OnDestroy();
        Destroyed.TrySetResult();
    }
}
