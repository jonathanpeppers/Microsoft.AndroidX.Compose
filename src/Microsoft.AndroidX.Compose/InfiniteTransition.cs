using System.Runtime.CompilerServices;
using AndroidX.Compose.Animation.Core;
using AndroidX.Compose.Runtime;
using CoreInfiniteTransition = AndroidX.Compose.Animation.Core.InfiniteTransition;
using CoreInfiniteTransitionKt = AndroidX.Compose.Animation.Core.InfiniteTransitionKt;

namespace AndroidX.Compose;

/// <summary>A composition-owned transition that repeats its registered animations indefinitely.</summary>
/// <remarks>
/// Obtain this wrapper from <c>composer.RememberInfiniteTransition()</c> or
/// <c>Composables.RememberInfiniteTransition()</c> on every composition pass.
/// Native Compose owns frame scheduling, duration-scale policy, registration, and removal.
/// Retaining this wrapper does not keep an animation in composition.
/// </remarks>
public sealed class InfiniteTransition
{
    internal CoreInfiniteTransition Jvm { get; }

    internal InfiniteTransition(CoreInfiniteTransition jvm) => Jvm = jvm;

    /// <summary>Adds or updates a repeating float animation in the explicit composition.</summary>
    /// <remarks>
    /// Keep each call at a stable composition location. Native Compose retains the animation
    /// state across recomposition and removes it when this call leaves composition.
    /// </remarks>
    public IState<float> AnimateFloat(
        IComposer composer,
        float initialValue,
        float targetValue,
        InfiniteRepeatableSpec animationSpec,
        string? label = null,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = "")
    {
        ArgumentNullException.ThrowIfNull(composer);
        ArgumentNullException.ThrowIfNull(animationSpec);
        if (!float.IsFinite(initialValue))
            throw new ArgumentOutOfRangeException(nameof(initialValue), "Initial value must be finite.");
        if (!float.IsFinite(targetValue))
            throw new ArgumentOutOfRangeException(nameof(targetValue), "Target value must be finite.");

        composer.StartReplaceableGroup(CompositionGroupKey.Compute(
            SourceLocationKey.Compute(line, file), typeof(InfiniteTransitionAnimation)));
        try
        {
            var state = CoreInfiniteTransitionKt.AnimateFloat(
                Jvm, initialValue, targetValue, animationSpec, label, composer, 0,
                label is null ? (int)InfiniteFloatAnimationDefault.Label : 0);
            return composer.Remember(
                () => new InfiniteTransitionAnimation(state),
                state);
        }
        finally
        {
            composer.EndReplaceableGroup();
        }
    }

    /// <summary>Adds or updates a repeating float animation at the current implicit-composer call site.</summary>
    public IState<float> AnimateFloat(
        float initialValue,
        float targetValue,
        InfiniteRepeatableSpec animationSpec,
        string? label = null,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = "") =>
        AnimateFloat(ComposableContext.Current, initialValue, targetValue, animationSpec, label, line, file);
}
