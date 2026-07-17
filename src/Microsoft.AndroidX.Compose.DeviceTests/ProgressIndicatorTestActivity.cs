using Android.Runtime;
using AndroidX.Activity;
using AndroidX.Compose;

namespace Microsoft.AndroidX.Compose.DeviceTests;

/// <summary>Hosts determinate and indeterminate Material 3 progress indicators.</summary>
[Activity(Theme = "@android:style/Theme.Material.Light.NoActionBar")]
[Register("net/compose/devicetests/ProgressIndicatorTestActivity")]
public class ProgressIndicatorTestActivity : ComponentActivity
{
    static int s_completedRenderPasses;

    internal static ProgressIndicatorTestActivity? Current { get; private set; }
    internal static MutableState<float>? Progress { get; private set; }
    internal static LinearProgressIndicator? Linear { get; private set; }
    internal static CircularProgressIndicator? Circular { get; private set; }
    internal static int CompletedRenderPasses => Volatile.Read(ref s_completedRenderPasses);

    internal static void Reset()
    {
        Current = null;
        Progress = new MutableState<float>(0.25f);
        Linear = new LinearProgressIndicator
        {
            Modifier = Modifier.FillMaxWidth(),
        };
        Circular = new CircularProgressIndicator
        {
            StrokeWidthDp = 6,
        };
        Volatile.Write(ref s_completedRenderPasses, 0);
    }

    internal static void MarkRenderCompleted() =>
        Interlocked.Increment(ref s_completedRenderPasses);

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        this.SetContent(_ => new Composed(_ => BuildIndicators()));
        Current = this;
    }

    protected override void OnDestroy()
    {
        if (ReferenceEquals(Current, this))
            Current = null;
        Progress = null;
        Linear = null;
        Circular = null;
        base.OnDestroy();
    }

    static ComposableNode BuildIndicators()
    {
        var progress = Progress
            ?? throw new InvalidOperationException(
                "Progress state not set on ProgressIndicatorTestActivity.");
        var linear = Linear
            ?? throw new InvalidOperationException(
                "Linear indicator not set on ProgressIndicatorTestActivity.");
        var circular = Circular
            ?? throw new InvalidOperationException(
                "Circular indicator not set on ProgressIndicatorTestActivity.");

        linear.Progress = progress.Value;
        circular.Progress = progress.Value;
        return new ProgressIndicatorRenderMarker(new Column
        {
            linear,
            circular,
            new LinearProgressIndicator
            {
                Modifier = Modifier.FillMaxWidth(),
            },
            new CircularProgressIndicator(),
        });
    }
}
