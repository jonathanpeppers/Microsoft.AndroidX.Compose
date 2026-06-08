namespace ComposeNet;

/// <summary>
/// C# wrapper around
/// <c>androidx.compose.material3.TopAppBarState</c> — the mutable
/// state holder for a Material 3 top app bar's collapse / expand /
/// scroll offset. Pair with a
/// <see cref="TopAppBarScrollBehavior"/> from
/// <see cref="TopAppBarDefaults"/> to wire a top app bar to a
/// scrolling content area; the bar will collapse and overlap based
/// on the values this state holder reports.
/// </summary>
/// <remarks>
/// <para>
/// The Kotlin class has a public 3-argument constructor and exposes
/// rich getters/setters for height offsets and the derived
/// <see cref="CollapsedFraction"/> / <see cref="OverlappedFraction"/>
/// — the binder ships it unmangled, so this wrapper just adapts the
/// bound type to the rest of the ComposeNet state-holder convention
/// (constructible from C#, internal <c>Jvm</c> peer).
/// </para>
/// <para>
/// Construct one inside a
/// <see cref="Compose.Remember{T}(System.Func{T}, int, string)"/>
/// call so the scroll position survives recompositions:
/// <code>
/// var state = Compose.Remember(() =&gt; new TopAppBarState());
/// var behavior = TopAppBarDefaults.PinnedScrollBehavior(state);
///
/// new Scaffold
/// {
///     TopBar = new TopAppBar(new Text("Title")) { ScrollBehavior = behavior },
///     Content = new LazyColumn
///     {
///         Modifier.Companion.NestedScroll(behavior.NestedScrollConnection),
///         // ... lots of items ...
///     },
/// }
/// </code>
/// </para>
/// </remarks>
public sealed class TopAppBarState
{
    /// <summary>The bound Kotlin peer this wrapper adapts.</summary>
    internal AndroidX.Compose.Material3.TopAppBarState Jvm { get; }

    /// <summary>
    /// Construct a new <see cref="TopAppBarState"/>.
    /// </summary>
    /// <param name="initialHeightOffsetLimit">
    /// Minimum (most-collapsed) height offset, in pixels. Defaults to
    /// <see cref="float.NegativeInfinity"/>, matching Kotlin's default;
    /// Material 3 layouts overwrite this with the bar's measured collapse
    /// range during layout.
    /// </param>
    /// <param name="initialHeightOffset">
    /// Initial vertical offset of the bar, in pixels. Negative values
    /// scroll the bar up (collapse it); <c>0</c> means fully expanded.
    /// </param>
    /// <param name="initialContentOffset">
    /// Initial accumulated scroll delta of the content, in pixels.
    /// Compose uses this to decide between "show elevation when not at
    /// top" (<c>contentOffset &lt; 0</c>) and "flat" (<c>0</c>).
    /// </param>
    public TopAppBarState(
        float initialHeightOffsetLimit = float.NegativeInfinity,
        float initialHeightOffset = 0f,
        float initialContentOffset = 0f)
    {
        Jvm = new AndroidX.Compose.Material3.TopAppBarState(
            initialHeightOffsetLimit, initialHeightOffset, initialContentOffset);
    }

    /// <summary>
    /// Lower bound for <see cref="HeightOffset"/>, in pixels. Updated
    /// by the bar's layout pass to match the actual collapse range
    /// (typically a negative value equal to <c>-(expandedHeight -
    /// collapsedHeight)</c>).
    /// </summary>
    public float HeightOffsetLimit
    {
        get => Jvm.HeightOffsetLimit;
        set => Jvm.HeightOffsetLimit = value;
    }

    /// <summary>
    /// Current vertical offset of the bar, in pixels. Bounded by
    /// <see cref="HeightOffsetLimit"/> (most negative) and <c>0</c>
    /// (fully expanded). The scroll behavior writes this in response
    /// to <see cref="Modifier.NestedScroll(NestedScrollConnection)"/>
    /// callbacks.
    /// </summary>
    public float HeightOffset
    {
        get => Jvm.HeightOffset;
        set => Jvm.HeightOffset = value;
    }

    /// <summary>
    /// Accumulated content scroll delta, in pixels. Negative when the
    /// content has scrolled down; used by some scroll behaviors to
    /// drive elevation or color changes.
    /// </summary>
    public float ContentOffset
    {
        get => Jvm.ContentOffset;
        set => Jvm.ContentOffset = value;
    }

    /// <summary>
    /// Fraction of the bar's collapse range that's been consumed —
    /// <c>0f</c> when fully expanded, <c>1f</c> when fully collapsed.
    /// Derived from <see cref="HeightOffset"/> /
    /// <see cref="HeightOffsetLimit"/>; read-only.
    /// </summary>
    public float CollapsedFraction => Jvm.CollapsedFraction;

    /// <summary>
    /// Fraction of the bar that's currently overlapped by scrolled
    /// content, in <c>[0f, 1f]</c>. Used by scroll behaviors that
    /// raise the bar's surface when content scrolls under it.
    /// </summary>
    public float OverlappedFraction => Jvm.OverlappedFraction;
}
