using AndroidX.Compose.Runtime;

namespace ComposeNet;

/// <summary>
/// Material 3 <c>BottomAppBar</c>. The actions slot is laid out in a
/// <c>RowScope</c> and filled from this bar's children;
/// <see cref="FloatingActionButton"/> is an optional trailing slot:
/// <code>
/// new BottomAppBar
/// {
///     FloatingActionButton = new FloatingActionButton(onClick: ...) { ... },
///
///     new IconButton(onClick: ...) { new Icon(painter, "Search") },
///     new IconButton(onClick: ...) { new Icon(painter, "Settings") },
/// }
/// </code>
/// </summary>
public sealed class BottomAppBar : ComposableContainer
{
    /// <summary>Optional: trailing slot, typically a <see cref="ComposeNet.FloatingActionButton"/>.</summary>
    public ComposableNode? FloatingActionButton { get; set; }

    internal override void Render(IComposer composer)
    {
        var actions = new ComposableLambda3((scope, c) =>
        {
            using var _ = RenderContext.PushScope(scope);
            RenderChildren(c);
        });
        ComposableLambda2? fab = FloatingActionButton is null ? null
            : new ComposableLambda2(c => FloatingActionButton.Render(c));

        ComposeBridges.BottomAppBar(actions, BuildModifier(), fab, composer);
    }
}
