using System.Collections;
using System.Collections.Generic;
using AndroidX.Compose.Runtime;

namespace ComposeNet;

/// <summary>
/// Base class for container composables that take a single content
/// lambda holding zero or more children. Implements
/// <see cref="IEnumerable"/> + <see cref="Add(ComposableNode)"/> so
/// C# collection-initializer syntax
/// (<c>new Column { new Text("Hi"), new Text("There") }</c>) compiles.
/// </summary>
public abstract class ComposableContainer : ComposableNode, IEnumerable
{
    readonly List<ComposableNode> _children = new();

    public void Add(ComposableNode? child)
    {
        if (child is not null)
            _children.Add(child);
    }

    /// <summary>
    /// Collection-initializer overload that lets callers set
    /// <see cref="ComposableNode.Modifier"/> inline alongside children:
    /// <code>new Column { Modifier.Companion.Padding(16), new Text("Hi") }</code>
    /// (C# disallows mixing object-initializer assignments with
    /// collection-initializer items in the same braces, so the modifier
    /// is set via <c>Add</c> instead.)
    /// </summary>
    public void Add(Modifier modifier) => Modifier = modifier;
    IEnumerator IEnumerable.GetEnumerator() => _children.GetEnumerator();

    /// <summary>Internal accessor for <see cref="Render"/> impls.</summary>
    private protected IReadOnlyList<ComposableNode> Children => _children;

    /// <summary>
    /// Renders this container's children sequentially into
    /// <paramref name="composer"/>, wrapping each child in a per-index
    /// <c>StartReplaceableGroup(i)</c> so sibling renders get distinct
    /// slot-table group keys. Without this, three sibling
    /// <c>SegmentedButton</c>s (same C# call site → same group key)
    /// rely on Compose's positional disambiguation, which combined with
    /// lambda-identity churn elsewhere can land Reuse/Move ops at the
    /// wrong tree index.
    /// </summary>
    private protected void RenderChildren(IComposer composer)
    {
        for (int i = 0; i < _children.Count; i++)
        {
            composer.StartReplaceableGroup(i);
            try { _children[i].Render(composer); }
            finally { composer.EndReplaceableGroup(); }
        }
    }
}
