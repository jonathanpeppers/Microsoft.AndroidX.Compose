using AndroidX.Compose.Animation.Core;
using CoreSpring = AndroidX.Compose.Animation.Core.Spring;

namespace AndroidX.Compose;

/// <summary>Finite Compose animation specifications usable for both float and color transitions.</summary>
public static class AnimationSpecs
{
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
