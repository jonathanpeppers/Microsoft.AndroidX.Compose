using AndroidX.Compose.Runtime;

namespace ComposeNet;

/// <summary>
/// Material 3 <c>FilledIconButton</c>. Same shape as <see cref="IconButton"/>:
/// <code>new FilledIconButton(onClick: () => Save()) { new Text("✓") }</code>
/// </summary>
public sealed class FilledIconButton : ComposableContainer
{
    readonly System.Action _onClick;
    public FilledIconButton(System.Action onClick) => _onClick = onClick;

    internal override void Render(IComposer composer)
    {
        var click   = new ComposableLambda0(_onClick);
        var content = ComposableLambdas.Wrap2(composer, c => RenderChildren(c));
        ComposeBridges.FilledIconButton(click, BuildModifier(), content, composer);
    }
}
