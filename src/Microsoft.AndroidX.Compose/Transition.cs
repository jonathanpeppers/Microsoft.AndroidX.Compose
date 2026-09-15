using System.Runtime.CompilerServices;
using AndroidX.Compose.Animation.Core;
using AndroidX.Compose.Runtime;
using CoreTransition = AndroidX.Compose.Animation.Core.Transition;
using CoreTransitionKt = AndroidX.Compose.Animation.Core.TransitionKt;
using ColorTransitionKt = AndroidX.Compose.Animation.TransitionKt;

namespace AndroidX.Compose;

/// <summary>A composition-owned transition coordinating animated values from one typed target state.</summary>
/// <typeparam name="T">A non-null immutable target, such as a bool, enum, string, or record.</typeparam>
/// <remarks>
/// Obtain this wrapper from <c>composer.UpdateTransition(target)</c> or
/// <c>Composables.UpdateTransition(target)</c> on every composition pass. Targets use managed
/// <see cref="object.Equals(object)"/> equality. Keep each animation call at its own stable
/// composition location. Native Compose owns registration, interruption, frame scheduling,
/// and removal; retaining this wrapper does not keep an animation in composition.
/// </remarks>
public sealed class Transition<T> where T : notnull
{
    internal CoreTransition Jvm { get; }

    internal Transition(CoreTransition jvm) => Jvm = jvm;

    /// <summary>The native transition's current state, updated as a segment finishes or is interrupted.</summary>
    public T CurrentState => Unbox(Jvm.CurrentState);

    /// <summary>The target state most recently supplied in composition.</summary>
    public T TargetState => Unbox(Jvm.TargetState);

    /// <summary>Whether the native transition is running its animation frame loop.</summary>
    public bool IsRunning => Jvm.IsRunning;

    /// <summary>Whether the transition is not running and its current state equals its target.</summary>
    /// <remarks>
    /// Reads are snapshot-observable. A newly supplied target may be pending before the first
    /// frame; do not treat <see cref="IsRunning"/> alone as completion. For same-target mapping
    /// changes, observe running before waiting for idle. Removal from composition also ends
    /// the native transition and is not proof that its values reached their targets.
    /// </remarks>
    public bool IsIdle => !IsRunning && Equals(CurrentState, TargetState);

    /// <summary>
    /// Adds or updates a float animation in the explicit composition.
    /// Null spec and label use Compose's spring and FloatAnimation defaults.
    /// </summary>
    /// <remarks>
    /// The mapping runs synchronously in composition with an ambient composer. It can read
    /// snapshot state or composition locals. Replacing the mapping or spec keeps the animation's
    /// native peer and read-only state identity; native Compose retargets the existing animation.
    /// </remarks>
    public IState<float> AnimateFloat(
        IComposer composer,
        [ComposableContent] Func<T, float> targetValueByState,
        IFiniteAnimationSpec? animationSpec = null,
        string? label = null,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = "")
    {
        ArgumentNullException.ThrowIfNull(targetValueByState);
        return Animate(composer,
            value => Java.Lang.Float.ValueOf(targetValueByState(Unbox(value))),
            static value => value is Java.Lang.Float number
                ? number.FloatValue()
                : throw new InvalidCastException("Transition float animation did not return java.lang.Float."),
            CoreTransitionKt.AnimateFloat, animationSpec, label, line, file);
    }

    /// <summary>Adds or updates a float animation at the current implicit-composer call site.</summary>
    public IState<float> AnimateFloat(
        [ComposableContent] Func<T, float> targetValueByState,
        IFiniteAnimationSpec? animationSpec = null,
        string? label = null,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = "") =>
        AnimateFloat(ComposableContext.Current, targetValueByState, animationSpec, label, line, file);

    /// <summary>
    /// Adds or updates a color animation in the explicit composition.
    /// Null spec and label use Compose's spring and ColorAnimation defaults.
    /// </summary>
    /// <remarks>Native Compose uses the target color's color-space converter, not packed-integer interpolation.</remarks>
    public IState<Color> AnimateColor(
        IComposer composer,
        [ComposableContent] Func<T, Color> targetValueByState,
        IFiniteAnimationSpec? animationSpec = null,
        string? label = null,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = "")
    {
        ArgumentNullException.ThrowIfNull(targetValueByState);
        return Animate(composer,
            value => ComposeBridges.BoxColor(targetValueByState(Unbox(value)).ToPacked()),
            static value => value is UI.Graphics.Color color
                ? Color.FromPacked(unchecked((long)color.Value))
                : throw new InvalidCastException("Transition color animation did not return a Compose Color."),
            ColorTransitionKt.AnimateColor, animationSpec, label, line, file);
    }

    /// <summary>Adds or updates a color animation at the current implicit-composer call site.</summary>
    public IState<Color> AnimateColor(
        [ComposableContent] Func<T, Color> targetValueByState,
        IFiniteAnimationSpec? animationSpec = null,
        string? label = null,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = "") =>
        AnimateColor(ComposableContext.Current, targetValueByState, animationSpec, label, line, file);

    IState<TValue> Animate<TValue>(
        IComposer composer,
        Func<Java.Lang.Object?, Java.Lang.Object?> target,
        Func<Java.Lang.Object?, TValue> unbox,
        Func<CoreTransition, Kotlin.Jvm.Functions.IFunction3?, string?,
            Kotlin.Jvm.Functions.IFunction3, IComposer?, int, int, IState> animate,
        IFiniteAnimationSpec? spec, string? label, int line, string file)
    {
        ArgumentNullException.ThrowIfNull(composer);
        composer.StartReplaceableGroup(CompositionGroupKey.Compute(
            SourceLocationKey.Compute(line, file), typeof(TransitionAnimation<TValue>)));
        try
        {
            var targetCallback = ComposableLambdas.Wrap3Result(composer, (value, _) => target(value));
            // Keep both callback slots present when a caller changes between supplied and default specs.
            var specCallback = ComposableLambdas.Wrap3Result(composer, (_, _) =>
                (Java.Lang.Object)(spec ?? throw new InvalidOperationException("Transition spec callback invoked without a spec.")));
            var defaults = TransitionAnimationDefault.All;
            if (spec is not null) defaults &= ~TransitionAnimationDefault.TransitionSpec;
            if (label is not null) defaults &= ~TransitionAnimationDefault.Label;
            var state = animate(Jvm, spec is null ? null : specCallback, label,
                targetCallback, composer, 0, (int)defaults);
            return composer.Remember(
                () => new TransitionAnimation<TValue>(state, unbox, targetCallback, specCallback),
                state, targetCallback, specCallback);
        }
        finally
        {
            composer.EndReplaceableGroup();
        }
    }

    internal static T Unbox(Java.Lang.Object? state) =>
        state is ManagedBox { Value: T value } ? value
            : throw new InvalidCastException($"Transition<{typeof(T).Name}> received an incompatible native target.");
}
