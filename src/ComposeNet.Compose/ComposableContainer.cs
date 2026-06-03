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
    IEnumerator IEnumerable.GetEnumerator() => _children.GetEnumerator();

    /// <summary>Internal accessor for <see cref="Render"/> impls.</summary>
    private protected IReadOnlyList<ComposableNode> Children => _children;

    /// <summary>
    /// Renders this container's children sequentially into
    /// <paramref name="composer"/>. Containers call this from inside
    /// their content lambda.
    /// </summary>
    private protected void RenderChildren(IComposer composer)
    {
        for (int i = 0; i < _children.Count; i++)
            _children[i].Render(composer);
    }
}
