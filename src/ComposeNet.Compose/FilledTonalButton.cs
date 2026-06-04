using AndroidX.Compose.Runtime;

namespace ComposeNet;

/// <summary>
/// Material 3 <c>FilledTonalButton</c>. Same shape as <see cref="Button"/>:
/// <code>new FilledTonalButton(onClick: () => Apply()) { new Text("Apply") }</code>
/// </summary>
public sealed class FilledTonalButton : ComposableContainer
{
    readonly System.Action _onClick;
    public FilledTonalButton(System.Action onClick) => _onClick = onClick;

    internal override void Render(IComposer composer)
    {
        var click   = new ComposableLambda0(_onClick);
        var content = ComposableLambdas.Wrap3(composer, c => RenderChildren(c));
        ComposeBridges.FilledTonalButton(click, BuildModifier(), content, composer);
    }
}
