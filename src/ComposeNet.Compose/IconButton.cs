using AndroidX.Compose.Runtime;

namespace ComposeNet;

/// <summary>
/// Material 3 <c>IconButton</c>. Children render into a Function2 content
/// slot (no RowScope). Typical use: <c>new IconButton(...) { new Text("☆") }</c>.
/// </summary>
public sealed class IconButton : ComposableContainer
{
    readonly System.Action _onClick;
    public IconButton(System.Action onClick) => _onClick = onClick;

    internal override void Render(IComposer composer)
    {
        var click   = new ComposableLambda0(_onClick);
        var content = ComposableLambdas.Wrap2(composer, c => RenderChildren(c));
        ComposeBridges.IconButton(click, BuildModifier(), content, composer);
    }
}
