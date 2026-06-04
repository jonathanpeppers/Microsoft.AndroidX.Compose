using AndroidX.Compose.Runtime;

namespace ComposeNet;

/// <summary>
/// Material 3 <c>OutlinedIconButton</c>. Same shape as <see cref="IconButton"/>.
/// </summary>
public sealed class OutlinedIconButton : ComposableContainer
{
    readonly System.Action _onClick;
    public OutlinedIconButton(System.Action onClick) => _onClick = onClick;

    internal override void Render(IComposer composer)
    {
        var click   = new ComposableLambda0(_onClick);
        var content = ComposableLambdas.Wrap2(composer, c => RenderChildren(c));
        ComposeBridges.OutlinedIconButton(click, BuildModifier(), content, composer);
    }
}
