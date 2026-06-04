using AndroidX.Compose.Runtime;

namespace ComposeNet;

/// <summary>
/// Material 3 <c>FilledTonalIconButton</c>. Same shape as <see cref="IconButton"/>.
/// </summary>
public sealed class FilledTonalIconButton : ComposableContainer
{
    readonly System.Action _onClick;
    public FilledTonalIconButton(System.Action onClick) => _onClick = onClick;

    internal override void Render(IComposer composer)
    {
        var click   = new ComposableLambda0(_onClick);
        var content = ComposableLambdas.Wrap2(composer, c => RenderChildren(c));
        ComposeBridges.FilledTonalIconButton(click, BuildModifier(), content, composer);
    }
}
