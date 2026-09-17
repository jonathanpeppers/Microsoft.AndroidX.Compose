using AndroidX.Compose.Animation.Core;
using CoreSpring = AndroidX.Compose.Animation.Core.Spring;

namespace AndroidX.Compose;

/// <summary>Compose animation specifications usable for finite and infinite transitions.</summary>
public static class AnimationSpecs
{
    /// <summary>Creates an infinite repetition of a duration-based animation.</summary>
    /// <param name="animation">The finite animation to repeat.</param>
    /// <param name="repeatMode">Whether each iteration restarts or reverses. Null uses restart.</param>
    public static InfiniteRepeatableSpec InfiniteRepeatable(
        IDurationBasedAnimationSpec animation,
        RepeatMode? repeatMode = null)
    {
        ArgumentNullException.ThrowIfNull(animation);
        var mode = repeatMode ?? RepeatMode.Restart
            ?? throw new InvalidOperationException("Compose RepeatMode.Restart was unavailable.");
        return AnimationSpecKt.InfiniteRepeatable(animation, mode, 0);
    }

    /// <summary>
    /// Creates a spring with Compose's default damping and stiffness. Lower damping allows
    /// more bounce; stiffness controls how quickly the spring converges.
    /// </summary>
    /// <remarks>
    /// The visibility threshold is left to the native value converter, so this spec can
    /// animate either floats or colors. Remember the spec when creating it in composition.
    /// </remarks>
    public static SpringSpec Spring(
        float dampingRatio = CoreSpring.DampingRatioNoBouncy,
        float stiffness = CoreSpring.StiffnessMedium)
    {
        if (!float.IsFinite(dampingRatio) || dampingRatio <= 0)
            throw new ArgumentOutOfRangeException(nameof(dampingRatio), "Damping ratio must be finite and positive.");
        if (!float.IsFinite(stiffness) || stiffness <= 0)
            throw new ArgumentOutOfRangeException(nameof(stiffness), "Stiffness must be finite and positive.");
        return AnimationSpecKt.Spring(dampingRatio, stiffness, null);
    }

    /// <summary>
    /// Creates a duration-based animation with an optional delay and easing.
    /// Null easing uses Compose's FastOutSlowIn easing.
    /// </summary>
    public static TweenSpec Tween(int durationMillis = 300, int delayMillis = 0, IEasing? easing = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(durationMillis);
        ArgumentOutOfRangeException.ThrowIfNegative(delayMillis);
        return AnimationSpecKt.Tween(durationMillis, delayMillis, easing ?? EasingKt.FastOutSlowInEasing);
    }
}
