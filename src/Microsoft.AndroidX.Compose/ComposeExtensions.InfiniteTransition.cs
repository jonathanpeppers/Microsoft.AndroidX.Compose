using System.Runtime.CompilerServices;
using AndroidX.Compose.Animation.Core;
using AndroidX.Compose.Runtime;

namespace AndroidX.Compose;

public static partial class ComposeExtensions
{
    /// <summary>Remembers a composition-owned native infinite transition.</summary>
    /// <remarks>
    /// Call on every composition pass, not inside a Remember factory. Label is diagnostic
    /// metadata captured when the native transition is created.
    /// </remarks>
    public static InfiniteTransition RememberInfiniteTransition(
        this IComposer composer,
        string? label = null,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = "")
    {
        ArgumentNullException.ThrowIfNull(composer);
        composer.StartReplaceableGroup(CompositionGroupKey.Compute(
            SourceLocationKey.Compute(line, file), typeof(InfiniteTransition)));
        try
        {
            var transition = InfiniteTransitionKt.RememberInfiniteTransition(
                label, composer, 0,
                label is null ? (int)RememberInfiniteTransitionDefault.Label : 0);
            return composer.Remember(
                () => new InfiniteTransition(transition),
                transition);
        }
        finally
        {
            composer.EndReplaceableGroup();
        }
    }
}
