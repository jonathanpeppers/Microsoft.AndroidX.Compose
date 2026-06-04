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

    [System.ThreadStatic]
    static int s_rowChildIndex;

    [System.ThreadStatic]
    static int s_rowChildCount;

    public static IntPtr CurrentScope => s_scope;

    /// <summary>
    /// Index of the currently-rendering child within an enclosing row
    /// container. Read by composables (<see cref="SegmentedButton"/>)
    /// whose Kotlin defaults depend on position (e.g. start/end shape).
    /// Only meaningful while <see cref="CurrentRowChildCount"/> &gt; 0.
    /// </summary>
    public static int CurrentRowChildIndex => s_rowChildIndex;

    /// <summary>Total child count of the enclosing row container, or 0.</summary>
    public static int CurrentRowChildCount => s_rowChildCount;

    public static ScopeFrame PushScope(IntPtr scope)
    {
        var prev = s_scope;
        s_scope = scope;
        return new ScopeFrame(prev);
    }

    /// <summary>
    /// Publish row-position state for child composables. Returns a frame
    /// that restores the previous values on dispose. The row container
    /// calls <see cref="RowFrame.SetIndex"/> before each child Render.
    /// </summary>
    public static RowFrame PushRow(int count)
    {
        var prevIndex = s_rowChildIndex;
        var prevCount = s_rowChildCount;
        s_rowChildCount = count;
        s_rowChildIndex = 0;
        return new RowFrame(prevIndex, prevCount);
    }

    internal readonly struct ScopeFrame : System.IDisposable
    {
        readonly IntPtr _previous;
        public ScopeFrame(IntPtr previous) => _previous = previous;
        public void Dispose() => s_scope = _previous;
    }

    internal readonly struct RowFrame : System.IDisposable
    {
        readonly int _prevIndex;
        readonly int _prevCount;
        public RowFrame(int prevIndex, int prevCount)
        {
            _prevIndex = prevIndex;
            _prevCount = prevCount;
        }
        public void SetIndex(int index) => s_rowChildIndex = index;
        public void Dispose()
        {
            s_rowChildIndex = _prevIndex;
            s_rowChildCount = _prevCount;
        }
    }
}
