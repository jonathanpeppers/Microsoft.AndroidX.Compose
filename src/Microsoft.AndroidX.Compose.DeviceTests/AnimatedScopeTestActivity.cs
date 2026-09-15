using Android.Runtime;
using AndroidX.Activity;
using AndroidX.Compose;
using AndroidX.Compose.Animation;
using AndroidX.Compose.Animation.Core;
using AndroidX.Compose.Runtime;
using AndroidX.Compose.UI.Layout;
using Composable = AndroidX.Compose.ComposableAttribute;

namespace Microsoft.AndroidX.Compose.DeviceTests;

/// <summary>Hosts native animated scopes with independently recomposed, measured children.</summary>
[Activity(Theme = "@android:style/Theme.Material.Light.NoActionBar")]
[Register("net/compose/devicetests/AnimatedScopeTestActivity")]
public class AnimatedScopeTestActivity : ComponentActivity
{
    internal static TaskCompletionSource<AnimatedScopeTestActivity> Ready = NewReady();
    internal static TaskCompletionSource<AnimatedScopeTestActivity> NewReady() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);
    internal static TaskCompletionSource NewSignal() => new(TaskCreationOptions.RunContinuationsAsynchronously);

    internal MutableState<bool> Visible { get; } = new(true);
    internal MutableState<int> Step { get; } = new(0);
    internal MutableState<int> Tick { get; } = new(0);
    internal Dictionary<string, IAnimatedVisibilityScope> Scopes { get; } = [];
    internal Dictionary<string, object> Identities { get; } = [];
    internal Dictionary<string, int> CommittedTicks { get; } = [];
    internal Dictionary<string, TaskCompletionSource> Disposals { get; } = [];
    internal HashSet<string> Placed { get; } = [];
    internal TaskCompletionSource Measured = NewSignal();
    internal TaskCompletionSource Committed = NewSignal();
    internal TaskCompletionSource Entered = NewSignal();
    internal TaskCompletionSource ExitStarted = NewSignal();
    internal TaskCompletionSource Destroyed = NewSignal();
    internal long ExitDurationNanos;
    internal int RootPasses;
    internal bool OutsideScopeRestored;
    internal bool ExitWasLive;
    internal bool Direct;

    internal EnterTransition ParentEnter { get; } =
        Transitions.FadeIn(animationSpec: AnimationSpecKt.Tween(100, 0, EasingKt.LinearEasing));
    internal ExitTransition ParentExit { get; } =
        Transitions.FadeOut(animationSpec: AnimationSpecKt.Tween(100, 0, EasingKt.LinearEasing));
    internal EnterTransition ChildEnter { get; } =
        Transitions.ScaleIn(0.2f, AnimationSpecKt.Tween(800, 0, EasingKt.LinearEasing));
    internal ExitTransition ChildExit { get; } =
        Transitions.ScaleOut(0.2f, AnimationSpecKt.Tween(800, 0, EasingKt.LinearEasing));

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        Direct = Intent?.GetBooleanExtra("direct", false) ?? false;
        this.SetContent(c => Root(c, this));
        Ready.TrySetResult(this);
    }

    [Composable]
    internal static void Root(IComposer composer, AnimatedScopeTestActivity activity)
    {
        activity.RootPasses++;
        bool visible = activity.Visible.Value;
        int step = activity.Step.Value;
        if (activity.Direct)
        {
            Composables.AnimatedVisibility(visible, () =>
            {
                Composables.Column(() =>
                {
                    Leaf(activity, "outer-before");
                    Composables.AnimatedContent(step, value =>
                    {
                        Composables.Column(() =>
                        {
                            Leaf(activity, $"content-{value}");
                            Composables.AnimatedVisibility(true, () => Leaf(activity, $"nested-{value}"));
                        });
                    });
                    Leaf(activity, "outer-after");
                });
            }, enter: activity.ParentEnter, exit: activity.ParentExit);
        }
        else
        {
            new AnimatedVisibility(visible, activity.ParentEnter, activity.ParentExit)
            {
                new Column
                {
                    new Composed(_ => { Leaf(activity, "outer-before"); return null; }),
                    new AnimatedContent<int>(step, value => new Column
                    {
                        new Composed(_ => { Leaf(activity, $"content-{value}"); return null; }),
                        new AnimatedVisibility(true)
                        {
                            new Composed(_ => { Leaf(activity, $"nested-{value}"); return null; }),
                        },
                    }),
                    new Composed(_ => { Leaf(activity, "outer-after"); return null; }),
                },
            }.Render(composer);
        }
        bool restored = RenderContext.CurrentAnimatedVisibilityScope is null;
        composer.SideEffect(() => activity.OutsideScopeRestored = restored);
    }

    [Composable]
    internal static void Leaf(AnimatedScopeTestActivity activity, string id)
    {
        var composer = ComposableContext.Current;
        var scope = RenderContext.RequireAnimatedVisibilityScope();
        int tick = activity.Tick.Value;
        object identity = composer.Remember(() => new object());
        var transition = scope.Transition;
        bool entered = Equals(transition.CurrentState, EnterExitState.Visible) &&
            Equals(transition.TargetState, EnterExitState.Visible) && !transition.IsRunning;
        bool exiting = Equals(transition.TargetState, EnterExitState.PostExit) && transition.IsRunning;
        long duration = transition.TotalDurationNanos;
        composer.DisposableEffect(id, () =>
        {
            var disposed = NewSignal();
            activity.Disposals[id] = disposed;
            return () => disposed.TrySetResult();
        });
        composer.SideEffect(() =>
        {
            activity.Scopes[id] = scope;
            activity.Identities[id] = identity;
            activity.CommittedTicks[id] = tick;
            if (activity.CommittedTicks.Count >= 4 && activity.CommittedTicks.Values.All(v => v == tick))
                activity.Committed.TrySetResult();
            if (id == "outer-after")
            {
                if (entered) activity.Entered.TrySetResult();
                if (exiting && duration >= 800_000_000)
                {
                    activity.ExitDurationNanos = duration;
                    activity.ExitWasLive = !activity.Disposals[id].Task.IsCompleted;
                    activity.ExitStarted.TrySetResult();
                }
            }
        });
        var modifier = Modifier.AnimateEnterExit(activity.ChildEnter, activity.ChildExit, id).Padding(new Dp(8));
        modifier = modifier.AppendBound(current => OnGloballyPositionedModifierKt.OnGloballyPositioned(
            current, new ComposableLambda1(boxed =>
            {
                var coordinates = boxed?.JavaCast<ILayoutCoordinates>()
                    ?? throw new InvalidOperationException("Animated child layout coordinates missing.");
                long size = coordinates.Size;
                if ((int)(size >> 32) <= 0 || (int)size <= 0)
                    throw new InvalidOperationException("Animated child was placed with empty bounds.");
                activity.Placed.Add(id);
                if (activity.Placed.Count >= 4) activity.Measured.TrySetResult();
            })), ModifierOpKey.Opaque);
        Composables.Text($"{id}: {tick}", modifier: modifier);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        Destroyed.TrySetResult();
    }
}
