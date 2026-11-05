using Android.Runtime;
using Android.Views;
using AndroidX.Activity;
using AndroidX.Compose;
using AndroidX.Compose.Runtime;
using AndroidX.Compose.UI.Layout;
using Kotlin.Jvm.Functions;
using Modifier = AndroidX.Compose.Modifier;

namespace Microsoft.AndroidX.Compose.DeviceTests;

/// <summary>
/// Material 3 <see cref="Slider"/> and <see cref="RangeSlider"/> with
/// <c>OnValueChangeFinished</c>, plus a slider without it, for real touch input.
/// </summary>
[Activity(Theme = "@android:style/Theme.Material.Light.NoActionBar")]
[Register("net/compose/devicetests/SliderValueChangeFinishedTestActivity")]
public class SliderValueChangeFinishedTestActivity : ComponentActivity
{
    internal static TaskCompletionSource<SliderValueChangeFinishedTestActivity> Created = NewSource();
    internal readonly TaskCompletionSource Focused = new(TaskCreationOptions.RunContinuationsAsynchronously);
    internal readonly TaskCompletionSource Destroyed = new(TaskCreationOptions.RunContinuationsAsynchronously);
    internal readonly MutableNumberState<float> Value = new(0f);
    internal readonly MutableNumberState<int> Version = new(1);
    internal readonly MutableState<FloatRange> Range = new(new FloatRange(0.2f, 0.8f));
    internal readonly MutableNumberState<float> PlainValue = new(0f);
    internal int ChangeCount, FinishedCount, FinishedVersion;
    internal int RangeChangeCount, RangeFinishedCount, PlainChangeCount;
    internal float FinishedValue = float.NaN;
    internal FloatRange? RangeFinishedValue;
    internal SliderTouchBounds? SliderBounds, RangeBounds, PlainBounds;

    internal static TaskCompletionSource<SliderValueChangeFinishedTestActivity> NewSource() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        Window?.AddFlags(WindowManagerFlags.KeepScreenOn);
        this.SetContent(_ => new Composed(Build));
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
        int version = Version.Value;
        var sliderPositioned = composer.RememberAction(peer => SliderBounds = Measure(peer));
        var rangePositioned = composer.RememberAction(peer => RangeBounds = Measure(peer));
        var plainPositioned = composer.RememberAction(peer => PlainBounds = Measure(peer));
        return new MaterialTheme
        {
            new Column
            {
                Modifier.FillMaxSize().SafeDrawingPadding().Padding(24),
                new Slider(value: Value.Value, onValueChange: v =>
                {
                    ChangeCount++;
                    Value.Value = v;
                })
                {
                    Modifier = Track(sliderPositioned),
                    // Captures this composition's version: the release must call the latest callback.
                    OnValueChangeFinished = () =>
                    {
                        FinishedCount++;
                        FinishedValue = Value.Value;
                        FinishedVersion = version;
                    },
                },
                new RangeSlider(value: Range.Value, onValueChange: r =>
                {
                    RangeChangeCount++;
                    Range.Value = r;
                })
                {
                    Modifier = Track(rangePositioned),
                    OnValueChangeFinished = () =>
                    {
                        RangeFinishedCount++;
                        RangeFinishedValue = Range.Value;
                    },
                },
                new Slider(value: PlainValue.Value, onValueChange: v =>
                {
                    PlainChangeCount++;
                    PlainValue.Value = v;
                })
                {
                    Modifier = Track(plainPositioned),
                },
            },
        };
    }

    static Modifier Track(IFunction1 positioned) =>
        Modifier.FillMaxWidth().Padding(vertical: 24)
            .AppendBound(m => OnGloballyPositionedModifierKt.OnGloballyPositioned(m, positioned),
                ModifierOpKey.Opaque);

    static SliderTouchBounds Measure(Java.Lang.Object? peer)
    {
        var coordinates = peer?.JavaCast<ILayoutCoordinates>()
            ?? throw new InvalidOperationException("Slider layout coordinates missing.");
        var position = Offset.FromPacked(LayoutCoordinatesKt.PositionOnScreen(coordinates));
        return new SliderTouchBounds(position.X, position.Y,
            (int)(coordinates.Size >> 32), (int)coordinates.Size);
    }
}
