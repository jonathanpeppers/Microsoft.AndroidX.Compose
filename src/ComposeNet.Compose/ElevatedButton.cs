using AndroidX.Compose.Runtime;

namespace ComposeNet;

/// <summary>
/// Material 3 <c>ElevatedButton</c>. Same shape as <see cref="Button"/>:
/// <code>new ElevatedButton(onClick: () => Save()) { new Text("Save") }</code>
/// </summary>
public sealed class ElevatedButton : ComposableContainer
{
    readonly System.Action _onClick;
    public ElevatedButton(System.Action onClick) => _onClick = onClick;

    internal override void Render(IComposer composer)
    {
        var click   = new ComposableLambda0(_onClick);
        var content = ComposableLambdas.Wrap3(composer, c => RenderChildren(c));
        ComposeBridges.ElevatedButton(click, BuildModifier(), content, composer);
    }
}
