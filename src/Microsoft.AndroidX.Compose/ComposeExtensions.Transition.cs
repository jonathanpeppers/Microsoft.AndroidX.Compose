using System.Runtime.CompilerServices;
using AndroidX.Compose.Runtime;
using CoreTransitionKt = AndroidX.Compose.Animation.Core.TransitionKt;

namespace AndroidX.Compose;

public static partial class ComposeExtensions
{
    /// <summary>Remembers a native typed transition and updates its target on every composition pass.</summary>
    /// <remarks>
    /// Call on every pass, not inside a Remember factory. Native Compose owns the transition's
    /// lifetime. Equal targets retain identity; unequal targets animate all registered values
    /// together. Label is diagnostic metadata, captured when the native transition is created.
    /// </remarks>
    public static Transition<T> UpdateTransition<T>(
        this IComposer composer,
        T targetState,
        string? label = null,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = "") where T : notnull
    {
        ArgumentNullException.ThrowIfNull(composer);
        ArgumentNullException.ThrowIfNull(targetState);
        composer.StartReplaceableGroup(CompositionGroupKey.Compute(
            SourceLocationKey.Compute(line, file), typeof(Transition<T>)));
        try
        {
            var boxed = composer.Remember(() => new ManagedBox(targetState), targetState);
            var transition = CoreTransitionKt.UpdateTransition(
                (Java.Lang.Object)boxed, label, composer, 0,
                label is null ? (int)UpdateTransitionDefault.Label : 0);
            return composer.Remember(() => new Transition<T>(transition), transition);
        }
        finally
        {
            composer.EndReplaceableGroup();
        }
    }
}
