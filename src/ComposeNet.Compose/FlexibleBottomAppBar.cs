using AndroidX.Compose.Runtime;

namespace ComposeNet;

/// <summary>
/// Material 3 <c>FlexibleBottomAppBar</c>. Like <see cref="BottomAppBar"/>
/// but with a fully customizable expanded height and horizontal
/// arrangement. The content slot is laid out in a <c>RowScope</c> and
/// filled from this bar's children:
/// <code>
/// new FlexibleBottomAppBar
/// {
///     new IconButton(onClick: ...) { new Icon(painter, "Search") },
///     new IconButton(onClick: ...) { new Icon(painter, "Settings") },
/// }
/// </code>
/// </summary>
public sealed class FlexibleBottomAppBar : ComposableContainer
{
    internal override void Render(IComposer composer)
    {
        var content = new ComposableLambda3((scope, c) =>
        {
            using var _ = RenderContext.PushScope(scope);
            RenderChildren(c);
        });
        ComposeBridges.FlexibleBottomAppBar(BuildModifier(), content, composer);
    }
}
