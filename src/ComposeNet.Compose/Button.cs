using AndroidX.Compose.Runtime;

namespace ComposeNet;

/// <summary>
/// Material 3 filled <c>Button</c>. Takes an <c>onClick</c> in its
/// constructor and uses collection-initializer syntax for content:
/// <code>new Button(onClick: () => count++) { new Text("Tap") }</code>
/// </summary>
public sealed class Button : ComposableContainer
{
    readonly System.Action _onClick;
    public Button(System.Action onClick) => _onClick = onClick;

    internal override void Render(IComposer composer)
    {
        var click   = new ComposableLambda0(_onClick);
        var content = new ComposableLambda3(c => RenderChildren(c));
        ComposeBridges.Button(click, content, composer);
    }
}
