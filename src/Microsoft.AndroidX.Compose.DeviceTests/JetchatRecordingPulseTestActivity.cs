using Android.Runtime;
using AndroidX.Activity;
using AndroidX.Compose;
using AndroidX.Compose.Runtime;
using Composable = AndroidX.Compose.ComposableAttribute;

namespace Microsoft.AndroidX.Compose.DeviceTests;

/// <summary>Renders and observes the actual Jetchat recording-indicator pulse path.</summary>
[Activity(Theme = "@android:style/Theme.Material.Light.NoActionBar")]
[Register("net/compose/devicetests/JetchatRecordingPulseTestActivity")]
public class JetchatRecordingPulseTestActivity : ComponentActivity
{
    internal static TaskCompletionSource<JetchatRecordingPulseTestActivity> Ready { get; set; } =
        NewActivity();
    internal readonly MutableNumberState<int> Phase = new(0);
    internal readonly MutableNumberState<float> SwipeOffset = new(0);
    internal TaskCompletionSource<JetchatRecordingPulseSnapshot> Committed = NewSnapshot();
    internal TaskCompletionSource<JetchatRecordingPulseSnapshot> Low = NewSnapshot();
    internal TaskCompletionSource<JetchatRecordingPulseSnapshot> Returned = NewSnapshot();
    internal TaskCompletionSource Removed = NewCompletion();
    internal TaskCompletionSource Destroyed = NewCompletion();
    internal readonly List<JetchatRecordingPulseSnapshot> Observations = [];
    internal float DurationScale { get; private set; }
    readonly System.Diagnostics.Stopwatch clock = new();
    bool sawLow;

    static TaskCompletionSource<JetchatRecordingPulseTestActivity> NewActivity() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);
    static TaskCompletionSource<JetchatRecordingPulseSnapshot> NewSnapshot() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);
    static TaskCompletionSource NewCompletion() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        DurationScale = global::Android.Provider.Settings.Global.GetFloat(
            ContentResolver, "animator_duration_scale", 1f);
        clock.Start();
        this.SetContent((IComposer c) => Content(c, this));
    }

    internal void ChangePhase(int phase)
    {
        Committed = NewSnapshot();
        Low = NewSnapshot();
        Returned = NewSnapshot();
        Removed = NewCompletion();
        sawLow = false;
        Phase.Value = phase;
    }

    [Composable]
    internal static void Content(IComposer composer, JetchatRecordingPulseTestActivity host)
    {
        int phase = host.Phase.Value;
        if (phase == 1)
        {
            composer.SideEffect(() => host.Removed.TrySetResult());
            return;
        }

        var scheme = global::AndroidX.Compose.Material3.MaterialTheme.Instance.GetColorScheme(composer, 0);
        var indicator = global::AndroidX.Compose.Samples.Jetchat.RecordButton.BuildRecordingIndicator(
            host.SwipeOffset, scheme, value => host.Publish(phase, value));
        new global::AndroidX.Compose.MaterialTheme
        {
            indicator,
        }.Render(composer);
    }

    void Publish(int phase, float value)
    {
        var snapshot = new JetchatRecordingPulseSnapshot(phase, value, clock.ElapsedMilliseconds);
        Observations.Add(snapshot);
        Committed.TrySetResult(snapshot);
        if (value <= 0.205f)
        {
            sawLow = true;
            Low.TrySetResult(snapshot);
        }
        else if (sawLow && value >= 0.995f)
        {
            Returned.TrySetResult(snapshot);
        }
        global::Android.Util.Log.Info("JetchatRecordingPulse",
            $"phase={phase} scale={DurationScale:R} value={value:R} " +
            $"elapsedMs={snapshot.ElapsedMilliseconds}");
        if (phase == 0)
            Ready.TrySetResult(this);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        Destroyed.TrySetResult();
    }
}
