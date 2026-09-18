using System.Collections;
using AndroidX.Compose.Runtime;

namespace AndroidX.Compose;

/// <summary>
/// Base class for container composables that take a single content
/// lambda holding zero or more children. Implements
/// <see cref="IEnumerable"/> + <see cref="Add(ComposableNode)"/> so
/// C# collection-initializer syntax
/// (<c>new Column { new Text("Hi"), new Text("There") }</c>) compiles.
/// </summary>
public abstract class ComposableContainer : ComposableNode, IEnumerable
{
    readonly List<ComposableNode> _children = [];
    readonly List<Java.Lang.Object?> _movableKeys = [];

    public void Add(ComposableNode? child)
    {
        if (child is not null)
        {
            _children.Add(child);
            _movableKeys.Add(null);
        }
    }

    /// <summary>
    /// Collection-initializer overload that lets callers drop a
    /// composer-aware builder lambda inline as a child:
    /// <code>
    /// new Column
    /// {
    ///     c =&gt; new Text(c.ColorScheme().OnSurface.ToString()),
    /// }
    /// </code>
    /// The lambda is wrapped in a <see cref="Composed"/> node, so it
    /// runs once per composition pass and may return <see langword="null"/>
    /// to skip rendering. This overload exists alongside the implicit
    /// conversion on <see cref="Composed"/> because C# can only target-
    /// type a bare lambda when the call-site parameter is itself a
    /// delegate — the implicit operator is otherwise invisible.
    /// </summary>
    public void Add(
        [ComposableContent] Func<IComposer, ComposableNode?> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        _children.Add(new Composed(builder));
        _movableKeys.Add(null);
    }

    // Internal backends can preserve a dynamic child's composition identity
    // across insert/remove operations by supplying a deterministic data key.
    internal void AddMovable(Java.Lang.Object key, ComposableNode child)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(child);
        _children.Add(child);
        _movableKeys.Add(key);
    }

    /// <summary>
    /// Collection-initializer overload that lets callers set
    /// <see cref="ComposableNode.Modifier"/> inline alongside children:
    /// <code>new Column { Modifier.Padding(16), new Text("Hi") }</code>
    /// (C# disallows mixing object-initializer assignments with
    /// collection-initializer items in the same braces, so the modifier
    /// is set via <c>Add</c> instead.)
    /// </summary>
    public void Add(Modifier modifier) => Modifier = modifier;
    IEnumerator IEnumerable.GetEnumerator() => _children.GetEnumerator();

    /// <summary>Accessor for derived <c>Render</c> impls.</summary>
    protected IReadOnlyList<ComposableNode> Children => _children;

    /// <summary>
    /// Renders this container's children sequentially into
    /// <paramref name="composer"/>. Normal children use a per-position
    /// <c>StartReplaceableGroup</c> whose key combines the sibling index
    /// and a deterministic identity for the child's runtime
    /// <see cref="Type"/>. Internal dynamic children added through
    /// <c>AddMovable</c> instead use their deterministic data key so
    /// remembered state follows the same child through insertion and
    /// removal of siblings. Without per-position groups, three sibling
    /// <c>SegmentedButton</c>s (same C#
    /// call site → same group key) rely on Compose's positional
    /// disambiguation, which combined with lambda-identity churn
    /// elsewhere can land Reuse/Move ops at the wrong tree index.
    /// Without the type component, a sibling that swaps to a different
    /// <see cref="ComposableNode"/> subclass at the same position
    /// (e.g. tab navigation flipping a <c>PullToRefreshBox</c> for a
    /// <c>HorizontalUncontainedCarousel</c>) would re-enter the prior
    /// occupant's group and read its slot-table entries, throwing
    /// <c>ClassCastException</c> from inside <c>rememberSaveable</c>
    /// when the prior slot held an incompatible type. Same-typed
    /// siblings at the same position keep their identity and slot
    /// state intact — that is intentional Compose positional identity.
    /// </summary>
    protected void RenderChildren(IComposer composer)
    {
        for (int i = 0; i < _children.Count; i++)
            RenderChild(composer, i);
    }

    /// <summary>
    /// Renders this container's children sequentially while publishing
    /// the per-child row position via
    /// <see cref="RenderContext.PushRow"/> + <c>SetIndex</c>. Child
    /// composables (e.g. <see cref="SegmentedButton"/>) read
    /// <see cref="RenderContext.CurrentRowChildIndex"/> and
    /// <see cref="RenderContext.CurrentRowChildCount"/> to compute
    /// Kotlin defaults that depend on their position in the row
    /// (start/end shape, etc.). Each child gets the same positional or
    /// movable group policy as <see cref="RenderChildren"/>.
    /// </summary>
    private protected void RenderChildrenIndexed(IComposer composer)
    {
        using var rows = RenderContext.PushRow(_children.Count);
        for (int i = 0; i < _children.Count; i++)
        {
            rows.SetIndex(i);
            RenderChild(composer, i);
        }
    }

    void RenderChild(IComposer composer, int index)
    {
        var child = _children[index];
        var movableKey = _movableKeys[index];
        if (movableKey is null)
        {
            composer.StartReplaceableGroup(CompositionGroupKey.Compute(index, child.GetType()));
            try { child.Render(composer); }
            finally { composer.EndReplaceableGroup(); }
            return;
        }

        composer.StartMovableGroup(CompositionGroupKey.Compute(0, child.GetType()), movableKey);
        try { child.Render(composer); }
        finally { composer.EndMovableGroup(); }
    }
}
