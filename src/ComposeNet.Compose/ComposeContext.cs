using AndroidX.Compose.Runtime;

namespace ComposeNet;

/// <summary>
/// Holds the active <see cref="IComposer"/> for the current composition pass
/// so APIs like <see cref="Compose.Remember{T}"/> can reach it without every
/// helper composable having to thread <c>composer</c> through its signature.
///
/// Compose's composition runs synchronously on a single thread per pass, so
/// the active composer is stored in <c>[ThreadStatic]</c> rather than
/// <c>AsyncLocal</c> — the latter would leak a stale composer into async
/// continuations spawned during composition (timers, <c>await</c>, etc.)
/// and let post-composition code accidentally read or mutate slot state.
///
/// Every <see cref="ComposableLambda2"/> / <see cref="ComposableLambda3"/> /
/// <see cref="ComposableLambda4"/> <c>Invoke</c> wraps the user body with
/// <see cref="Push(IComposer)"/> + <see cref="Scope.Dispose"/> so the active
/// composer is correct both for the synchronous initial composition pass
/// and for delayed callbacks (lazy item content, subcomposed scaffolds).
/// </summary>
internal static class ComposeContext
{
    [System.ThreadStatic]
    static IComposer? t_current;

    /// <summary>The composer for the currently running composition pass on this thread, or <c>null</c> if not composing.</summary>
    public static IComposer? Current => t_current;

    /// <summary>Push <paramref name="composer"/> as the active composer; the returned scope restores the previous value when disposed.</summary>
    public static Scope Push(IComposer composer)
    {
        var prev = t_current;
        t_current = composer;
        return new Scope(prev);
    }

    /// <summary>Restores the previous composer on dispose. Used via <c>using var _ = ComposeContext.Push(composer);</c>.</summary>
    public struct Scope : System.IDisposable
    {
        readonly IComposer? _prev;
        internal Scope(IComposer? prev) => _prev = prev;
        public void Dispose() => t_current = _prev;
    }
}
