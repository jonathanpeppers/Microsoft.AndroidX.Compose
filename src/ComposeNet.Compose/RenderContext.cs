namespace ComposeNet;

/// <summary>
/// Thread-static stash of the current Compose receiver scope (RowScope /
/// ColumnScope) handle. Set by container composables that consume a
/// scope-receiver Function3 content lambda; read by *Item composables
/// whose underlying Kotlin static method takes the scope as its first
/// argument. Composition runs synchronously on a single thread, so a
/// <c>[ThreadStatic]</c> is sufficient — and a struct-disposable scope
/// guard keeps push/pop balanced even when child <c>Render</c> throws.
/// </summary>
internal static class RenderContext
{
    [System.ThreadStatic]
    static IntPtr s_scope;

    public static IntPtr CurrentScope => s_scope;

    public static ScopeFrame PushScope(IntPtr scope)
    {
        var prev = s_scope;
        s_scope = scope;
        return new ScopeFrame(prev);
    }

    internal readonly struct ScopeFrame : System.IDisposable
    {
        readonly IntPtr _previous;
        public ScopeFrame(IntPtr previous) => _previous = previous;
        public void Dispose() => s_scope = _previous;
    }
}
