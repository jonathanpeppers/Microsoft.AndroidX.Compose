using Android.Runtime;
using AndroidX.Activity;
using AndroidX.Compose;
using AndroidX.Compose.UI.Layout;
using AndroidX.Compose.UI.Platform;
using AndroidX.Compose.UI.Text;
using Bitmap = Android.Graphics.Bitmap;
using ComposePath = AndroidX.Compose.Path;

namespace Microsoft.AndroidX.Compose.DeviceTests;

/// <summary>Renders deterministic advanced DrawScope operations for native pixel checks.</summary>
[Activity(Theme = "@android:style/Theme.Material.Light.NoActionBar")]
[Register("net/compose/devicetests/AdvancedDrawingTestActivity")]
public class AdvancedDrawingTestActivity : ComponentActivity
{
    static readonly Exception s_expected = new("Expected DrawScope restoration probe.");
    ComposeView? _view;

    internal static TaskCompletionSource<AdvancedDrawingTestActivity> Started { get; private set; } = NewStarted();
    internal TaskCompletionSource Destroyed { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    internal (float X, float Y) CanvasOrigin;
    internal bool ExceptionRestored;
    internal bool TransformExpired;
    internal Size InsetSize;
    internal int DrawIntoCanvasCalls;
    internal int TextDrawCalls;
    internal bool Resumed;

    internal static void Prepare() => Started = NewStarted();

    static TaskCompletionSource<AdvancedDrawingTestActivity> NewStarted() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        var view = new ComposeView(this);
        _view = view;
        view.SetContent(composer => new Composed(c =>
        {
            var textLayout = c.Remember(
                () => new MutableState<TextLayoutResult?>(initial: null));
            return new Column
            {
                new BasicTextField("DrawScope text", _ => { }, readOnly: true)
                {
                    OnTextLayout = layout => textLayout.Value = layout,
                },
                BuildCanvas(textLayout),
            };
        }));
        SetContentView(view);
        Started.TrySetResult(this);
    }

    Canvas BuildCanvas(MutableState<TextLayoutResult?> textLayout)
    {
        var positioned = new ComposableLambda1(value =>
        {
            var coordinates = value?.JavaCast<ILayoutCoordinates>()
                ?? throw new InvalidOperationException("Advanced drawing coordinates missing.");
            var position = Offset.FromPacked(LayoutCoordinatesKt.PositionInRoot(coordinates));
            CanvasOrigin = (position.X, position.Y);
        });
        var canvas = new Canvas(scope => Draw(scope, textLayout.Value))
        {
            Modifier = Modifier.Companion.AppendBound(
                current => OnGloballyPositionedModifierKt.OnGloballyPositioned(current, positioned),
                ModifierOpKey.Opaque).Size(240, 180),
        };
        return canvas;
    }

    void Draw(DrawScope scope, TextLayoutResult? textLayout)
    {
        var background = new Color(0xFF, 0x12, 0x24, 0x44);
        scope.DrawRect(background);
        var originalSize = scope.Size;
        try
        {
            scope.Inset(12f, inner =>
            {
                InsetSize = inner.Size;
                throw s_expected;
            });
        }
        catch (Exception error) when (ReferenceEquals(error, s_expected))
        {
            ExceptionRestored = scope.Size == originalSize;
        }

        scope.DrawRect(Color.Blue, size: new Size(20f, 20f));
        scope.ClipRect(new Rect(30f, 0f, 70f, 40f), clipped =>
            clipped.DrawRect(Color.Red));
        scope.Translate(new Offset(80f, 0f), translated =>
            translated.DrawRect(Color.Green, size: new Size(20f, 20f)));
        scope.WithTransform(
            transform => transform.Translate(new Offset(110f, 0f)),
            transformed => transformed.DrawRect(Color.Magenta, size: new Size(20f, 20f)));
        DrawTransform? escaped = null;
        scope.WithTransform(transform => escaped = transform, _ => { });
        var expired = escaped
            ?? throw new InvalidOperationException("DrawTransform escape probe was not assigned.");
        try
        {
            expired.Translate(Offset.Zero);
        }
        catch (InvalidOperationException)
        {
            TransformExpired = true;
        }
        scope.ClipRect(new Rect(0f, 50f, 100f, 90f), clipped =>
            clipped.Translate(new Offset(10f, 50f), translated =>
                translated.DrawRect(Color.Yellow, size: new Size(30f, 30f))));

        using var dash = PathEffect.Dash([8f, 4f]);
        Offset[] points =
        [
            new Offset(10f, 105f),
            new Offset(50f, 105f),
            new Offset(90f, 105f),
        ];
        scope.DrawPoints(
            points, PointMode.Polygon, Color.Cyan,
            strokeWidth: 5f, cap: StrokeCap.Round, pathEffect: dash);

        using var native = Bitmap.CreateBitmap(
                20, 20,
                Bitmap.Config.Argb8888
                    ?? throw new InvalidOperationException("ARGB bitmap config unavailable."))
            ?? throw new InvalidOperationException("Android bitmap factory returned null.");
        native.EraseColor(global::Android.Graphics.Color.Yellow);
        using var image = new ImageBitmap(native);
        scope.DrawImage(
            image,
            IntOffset.Zero,
            new IntSize(20, 20),
            new IntOffset(150, 0),
            new IntSize(40, 40));

        using var outline = new ComposePath()
            .MoveTo(130f, 60f)
            .LineTo(150f, 45f)
            .LineTo(170f, 60f)
            .LineTo(150f, 75f)
            .Close();
        scope.DrawOutline(outline, Color.White);

        if (textLayout is not null)
        {
            scope.DrawText(textLayout, Color.White, new Offset(10f, 125f));
            Interlocked.Increment(ref TextDrawCalls);
        }

        scope.DrawIntoCanvas(_ => Interlocked.Increment(ref DrawIntoCanvasCalls));
    }

    protected override void OnResume()
    {
        base.OnResume();
        Resumed = true;
    }

    internal Task OnUi(Action action)
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        RunOnUiThread(() =>
        {
            try
            {
                action();
                completion.SetResult();
            }
            catch (Exception error)
            {
                completion.SetException(error);
            }
        });
        return completion.Task.WaitAsync(TimeSpan.FromSeconds(10));
    }

    internal async Task<Bitmap> Capture()
    {
        SurfaceFrameCommit? frame = null;
        await OnUi(() =>
        {
            var view = _view ?? throw new InvalidOperationException("Advanced drawing view missing.");
            frame = new SurfaceFrameCommit(view.ViewTreeObserver
                ?? throw new InvalidOperationException("Advanced drawing frame observer missing."));
            view.Invalidate();
        });
        using var committed = frame
            ?? throw new InvalidOperationException("Advanced drawing frame not armed.");
        await committed.Committed.WaitAsync(TimeSpan.FromSeconds(10));
        return await SurfacePixelCopy.Capture(this, OnUi);
    }

    internal (int X, int Y) Pixel(int x, int y)
    {
        int[] location = [0, 0];
        var view = _view ?? throw new InvalidOperationException("Advanced drawing view missing.");
        view.GetLocationInWindow(location);
        return ((int)(location[0] + CanvasOrigin.X + x), (int)(location[1] + CanvasOrigin.Y + y));
    }

    protected override void OnDestroy()
    {
        _view?.DisposeComposition();
        _view = null;
        base.OnDestroy();
        Destroyed.TrySetResult();
    }
}
