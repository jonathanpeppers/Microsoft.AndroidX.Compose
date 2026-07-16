using System.Runtime.CompilerServices;
using AndroidX.Compose.Runtime;
using Kotlin.Jvm.Functions;

namespace AndroidX.Compose;

public static partial class ComposeExtensions
{
    /// <summary>
    /// Allocate one identity-stable
    /// <see cref="MutableComposableLambda0"/> per call site, cached in
    /// the composer's slot table; subsequent renders rebind its target
    /// without allocating a fresh JCW. Returns the same
    /// <see cref="IFunction0"/> JNI peer across recompositions, so
    /// Kotlin's <c>$changed</c> can read it as
    /// <see cref="ChangedBits.Static"/> for <c>onClick</c>-style
    /// callback ctor params.
    /// </summary>
    /// <remarks>
    /// Mirrors Kotlin Compose's <c>rememberUpdatedState</c> /
    /// <c>remember { ... }</c> idiom for callback lambdas: the wrapper
    /// is identity-stable but the body it dispatches to is updated to
    /// the latest <paramref name="action"/> on every render. Callers
    /// can rely on the same JNI handle being reused, while still
    /// closing over fresh local state every recomposition.
    /// </remarks>
    internal static IFunction0 RememberAction(
        this IComposer composer,
        Action action,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = "")
    {
        ArgumentNullException.ThrowIfNull(composer);
        ArgumentNullException.ThrowIfNull(action);

        composer.StartReplaceableGroup(SourceLocationKey.Compute(line, file));
        try
        {
            if (composer.RememberedValue() is MutableComposableLambda0 existing)
            {
                existing.Target = action;
                return existing;
            }
            var wrapper = new MutableComposableLambda0(action);
            composer.UpdateRememberedValue(wrapper);
            return wrapper;
        }
        finally
        {
            composer.EndReplaceableGroup();
        }
    }

    /// <summary>
    /// One-arg variant of
    /// <see cref="RememberAction(IComposer, Action, int, string)"/>
    /// for <c>onValueChange</c>/<c>onCheckedChange</c>-style
    /// callbacks. The wrapped <c>Action&lt;Java.Lang.Object?&gt;</c>
    /// receives the raw boxed Kotlin arg and is responsible for
    /// unboxing it.
    /// </summary>
    internal static IFunction1 RememberAction(
        this IComposer composer,
        Action<Java.Lang.Object?> action,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = "")
    {
        ArgumentNullException.ThrowIfNull(composer);
        ArgumentNullException.ThrowIfNull(action);

        composer.StartReplaceableGroup(SourceLocationKey.Compute(line, file));
        try
        {
            if (composer.RememberedValue() is MutableComposableLambda1 existing)
            {
                existing.Target = action;
                return existing;
            }
            var wrapper = new MutableComposableLambda1(action);
            composer.UpdateRememberedValue(wrapper);
            return wrapper;
        }
        finally
        {
            composer.EndReplaceableGroup();
        }
    }
}
