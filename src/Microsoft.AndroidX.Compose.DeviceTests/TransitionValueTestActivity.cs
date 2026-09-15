using Android.Runtime;
using AndroidX.Activity;
using AndroidX.Compose;
using AndroidX.Compose.Animation.Core;
using AndroidX.Compose.Runtime;
using Color = AndroidX.Compose.Color;
using Composable = AndroidX.Compose.ComposableAttribute;

namespace Microsoft.AndroidX.Compose.DeviceTests;

/// <summary>Observes native transition completion without frame-idle or value-polling proxies.</summary>
[Activity(Theme = "@android:style/Theme.Material.Light.NoActionBar")]
[Register("net/compose/devicetests/TransitionValueTestActivity")]
public class TransitionValueTestActivity : ComponentActivity
{
    internal static TaskCompletionSource<TransitionValueTestActivity> Ready { get; set; } =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
    internal readonly MutableNumberState<int> Phase = new(0);
    internal readonly MutableNumberState<float> MappingScale = new(2f);
    internal readonly MutableNumberState<int> MappingColor = new(0);
    internal TaskCompletionSource<TransitionValueSnapshot> Committed = NewSnapshot();
    internal TaskCompletionSource<TransitionValueSnapshot> Started = NewSnapshot();
    internal TaskCompletionSource<TransitionValueSnapshot> Settled = NewSnapshot();
    internal TaskCompletionSource Removed = NewCompletion();
    internal TaskCompletionSource Destroyed = NewCompletion();
    internal TransitionValueSnapshot? Latest;
    internal readonly List<TransitionValueSnapshot> Observations = [];
    int requestedPhase;

    internal static TaskCompletionSource<TransitionValueSnapshot> NewSnapshot() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);
    internal static TaskCompletionSource NewCompletion() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        bool direct = Intent?.GetBooleanExtra("direct", false) ?? false;
        this.SetContent((IComposer c) => Content(c, this, direct));
    }

    internal void ChangePhase(int phase)
    {
        ResetSignals();
        requestedPhase = phase;
        Phase.Value = phase;
    }

    internal void ChangeMapping()
    {
        ResetSignals();
        MappingScale.Value = 4f;
        MappingColor.Value = 1;
    }

    void ResetSignals()
    {
        Committed = NewSnapshot();
        Started = NewSnapshot();
        Settled = NewSnapshot();
        Removed = NewCompletion();
    }

    [Composable]
    internal static void Content(IComposer composer, TransitionValueTestActivity host, bool direct)
    {
        int phase = host.Phase.Value;
        if (phase == 8)
        {
            composer.SideEffect(() => host.Removed.TrySetResult());
            return;
        }
        var target = phase switch
        {
            1 or 2 or 5 or 6 or 9 => TransitionTestState.Recording,
            3 => TransitionTestState.Cancelled,
            _ => TransitionTestState.Idle,
        };
        var spring = composer.Remember(() => AnimationSpecs.Spring(
            Spring.DampingRatioMediumBouncy, Spring.StiffnessLow));
        var tween = composer.Remember(() => AnimationSpecs.Tween(1000, easing: EasingKt.LinearEasing));
        var tint = composer.Remember(() => AnimationSpecs.Tween(200));
        IFiniteAnimationSpec? scaleSpec = phase == 4 ? null : phase == 5 ? tween : spring;
        IFiniteAnimationSpec? alphaSpec = phase == 4 ? null : tween;
        IFiniteAnimationSpec? colorSpec = phase == 4 ? null : tint;
        Func<TransitionTestState, float> scaleTarget = value => value switch
        {
            TransitionTestState.Recording => phase == 6 ? 3f : host.MappingScale.Value,
            TransitionTestState.Cancelled => 0.5f,
            _ => 1f,
        };
        Func<TransitionTestState, float> alphaTarget = value => value switch
        {
            TransitionTestState.Recording => 1f,
            TransitionTestState.Cancelled => 0.25f,
            _ => 0f,
        };
        Func<TransitionTestState, Color> colorTarget = value => value switch
        {
            TransitionTestState.Recording => phase == 6 ? Color.Green
                : host.MappingColor.Value == 0 ? Color.Red : Color.Cyan,
            TransitionTestState.Cancelled => Color.Blue,
            _ => Color.Black,
        };
        var transition = direct
            ? Composables.UpdateTransition(target, "test")
            : composer.UpdateTransition(target, "test");
        var scale = direct
            ? transition.AnimateFloat(scaleTarget, scaleSpec, "scale")
            : transition.AnimateFloat(composer, scaleTarget, scaleSpec, "scale");
        var alpha = direct
            ? transition.AnimateFloat(alphaTarget, alphaSpec)
            : transition.AnimateFloat(composer, alphaTarget, alphaSpec);
        var color = direct
            ? transition.AnimateColor(colorTarget, colorSpec)
            : transition.AnimateColor(composer, colorTarget, colorSpec);
        var tailIdentity = composer.Remember(static () => new object());
        var snapshot = new TransitionValueSnapshot(phase, transition,
            (TransitionAnimation<float>)scale, (TransitionAnimation<float>)alpha,
            (TransitionAnimation<Color>)color, transition.CurrentState, transition.TargetState,
            transition.IsRunning, transition.IsIdle, scale.Value, alpha.Value, color.Value, tailIdentity);
        composer.SideEffect(() => host.Publish(snapshot));
        new Text($"Transition phase {phase}: {snapshot.Current} -> {snapshot.Target}, running={snapshot.Running}")
            .Render(composer);
    }

    void Publish(TransitionValueSnapshot snapshot)
    {
        Latest = snapshot;
        Observations.Add(snapshot);
        global::Android.Util.Log.Info("TransitionValues",
            $"pid={(global::Android.OS.Process.MyPid())} phase={snapshot.Phase} " +
            $"{snapshot.Current}->{snapshot.Target} running={snapshot.Running} idle={snapshot.Idle} " +
            $"scale={snapshot.ScaleValue:R} alpha={snapshot.AlphaValue:R} color={snapshot.ColorValue.ToPacked():X16}");
        if (snapshot.Phase != requestedPhase) return;
        Committed.TrySetResult(snapshot);
        if (snapshot.Running) Started.TrySetResult(snapshot);
        // Native onTransitionEnd flips running/current only after every registered child finishes.
        // Require running for this request; initial idle and disposal cannot satisfy this signal.
        if (Started.Task.IsCompletedSuccessfully && snapshot.Idle)
            Settled.TrySetResult(snapshot);
        if (snapshot.Phase == 0) Ready.TrySetResult(this);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        Destroyed.TrySetResult();
    }
}
