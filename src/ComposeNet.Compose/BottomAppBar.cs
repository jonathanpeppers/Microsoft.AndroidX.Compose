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
        var actions = ComposableLambdas.Wrap3(composer, (scope, c) =>
        {
            using var _ = RenderContext.PushScope(scope);
            RenderChildren(c);
        });
        var fab = FloatingActionButton is null ? null
            : ComposableLambdas.Wrap2(composer, c => FloatingActionButton.Render(c));

        ComposeBridges.BottomAppBar(actions, BuildModifier(), fab, composer);
    }
}
