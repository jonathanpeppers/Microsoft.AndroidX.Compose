using AndroidX.Compose.Runtime;

namespace ComposeNet;

/// <summary>
/// Material 3 <c>TextButton</c>. Same shape as <see cref="Button"/>:
/// <code>new TextButton(onClick: () => Navigate()) { new Text("Cancel") }</code>
/// </summary>
public sealed class TextButton : ComposableContainer
{
    readonly System.Action _onClick;
    public TextButton(System.Action onClick) => _onClick = onClick;

    internal override void Render(IComposer composer)
    {
        var click   = new ComposableLambda0(_onClick);
        var content = ComposableLambdas.Wrap3(composer, c => RenderChildren(c));
        ComposeBridges.TextButton(click, BuildModifier(), content, composer);
    }
}
