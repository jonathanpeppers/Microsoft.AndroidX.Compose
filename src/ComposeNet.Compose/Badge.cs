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
        var content = new ComposableLambda3((scope, c) =>
        {
            using var _ = RenderContext.PushScope(scope);
            RenderChildren(c);
        });
        ComposeBridges.Badge(BuildModifier(), content, composer);
    }
}
