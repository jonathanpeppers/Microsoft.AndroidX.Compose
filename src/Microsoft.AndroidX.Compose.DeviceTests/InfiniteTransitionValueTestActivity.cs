using Android.Runtime;
using AndroidX.Activity;
using AndroidX.Compose;
using AndroidX.Compose.Animation.Core;
using AndroidX.Compose.Runtime;
using Composable = AndroidX.Compose.ComposableAttribute;

namespace Microsoft.AndroidX.Compose.DeviceTests;

/// <summary>Records native infinite-transition timing and composition ownership.</summary>
[Activity(Theme = "@android:style/Theme.Material.Light.NoActionBar")]
[Register("net/compose/devicetests/InfiniteTransitionValueTestActivity")]
public class InfiniteTransitionValueTestActivity : ComponentActivity
{
    internal static TaskCompletionSource<InfiniteTransitionValueTestActivity> Ready { get; set; } =
        NewActivity();
    internal readonly MutableNumberState<int> Phase = new(0);
    internal TaskCompletionSource<InfiniteTransitionValueSnapshot> Committed = NewSnapshot();
    internal TaskCompletionSource<InfiniteTransitionValueSnapshot> Low = NewSnapshot();
    internal TaskCompletionSource<InfiniteTransitionValueSnapshot> Returned = NewSnapshot();
    internal TaskCompletionSource Removed = NewCompletion();
    internal TaskCompletionSource Destroyed = NewCompletion();
    internal readonly List<InfiniteTransitionValueSnapshot> Observations = [];
    internal float DurationScale { get; private set; }
    readonly System.Diagnostics.Stopwatch clock = new();
    bool sawLow;

    static TaskCompletionSource<InfiniteTransitionValueTestActivity> NewActivity() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);
    static TaskCompletionSource<InfiniteTransitionValueSnapshot> NewSnapshot() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);
    static TaskCompletionSource NewCompletion() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        DurationScale = global::Android.Provider.Settings.Global.GetFloat(
            ContentResolver, "animator_duration_scale", 1f);
        clock.Start();
        bool direct = Intent?.GetBooleanExtra("direct", false) ?? false;
        this.SetContent((IComposer c) => Content(c, this, direct));
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
    internal static void Content(
        IComposer composer,
        InfiniteTransitionValueTestActivity host,
        bool direct)
    {
        int phase = host.Phase.Value;
        if (phase == 1)
        {
            composer.SideEffect(() => host.Removed.TrySetResult());
            return;
        }

        var tween = composer.Remember(() => AnimationSpecs.Tween(
            2000, easing: EasingKt.LinearEasing));
        var spec = composer.Remember(() => AnimationSpecs.InfiniteRepeatable(
            tween, RepeatMode.Reverse));
        var transition = direct
            ? Composables.RememberInfiniteTransition("device-infinite")
            : composer.RememberInfiniteTransition("device-infinite");
        var animation = direct
            ? transition.AnimateFloat(1f, 0.2f, spec, "scale")
            : transition.AnimateFloat(composer, 1f, 0.2f, spec, "scale");
        var snapshot = new InfiniteTransitionValueSnapshot(
            phase,
            transition,
            (InfiniteTransitionAnimation)animation,
            animation.Value,
            host.clock.ElapsedMilliseconds);
        composer.SideEffect(() => host.Publish(snapshot));
        new Text($"Infinite transition: {snapshot.Value:F3}").Render(composer);
    }

    void Publish(InfiniteTransitionValueSnapshot snapshot)
    {
        Observations.Add(snapshot);
        Committed.TrySetResult(snapshot);
        if (snapshot.Value <= 0.25f)
        {
            sawLow = true;
            Low.TrySetResult(snapshot);
        }
        else if (sawLow && snapshot.Value >= 0.95f)
        {
            Returned.TrySetResult(snapshot);
        }
        global::Android.Util.Log.Info("InfiniteTransition",
            $"phase={snapshot.Phase} scale={DurationScale:R} value={snapshot.Value:R} " +
            $"elapsedMs={snapshot.ElapsedMilliseconds}");
        if (snapshot.Phase == 0)
            Ready.TrySetResult(this);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        Destroyed.TrySetResult();
    }
}
