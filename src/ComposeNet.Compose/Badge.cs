using AndroidX.Compose.Runtime;

namespace ComposeNet;

/// <summary>
/// Material 3 <c>Badge</c>. A small colored marker, typically dropped
/// inside a <see cref="BadgedBox.Badge"/> slot. Children are the badge
/// content (usually a count), laid out in a <c>RowScope</c>:
/// <code>
/// new Badge { new Text("3") }
/// </code>
/// Pass no children for a plain dot.
/// </summary>
public sealed class Badge : ComposableContainer
{
    internal override void Render(IComposer composer)
    {
        var content = ComposableLambdas.Wrap3(composer, (scope, c) =>
        {
            using var _ = RenderContext.PushScope(scope, ScopeKind.Row);
            RenderChildren(c);
        });
        ComposeBridges.Badge(BuildModifier(), content, composer);
    }
}
