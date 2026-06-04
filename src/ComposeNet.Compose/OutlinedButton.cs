using AndroidX.Compose.Runtime;

namespace ComposeNet;

/// <summary>
/// Material 3 <c>OutlinedButton</c>. Same shape as <see cref="Button"/> —
/// constructor takes an <c>onClick</c> and content goes through the
/// collection initializer:
/// <code>new OutlinedButton(onClick: () => count++) { new Text("Cancel") }</code>
/// </summary>
public sealed class OutlinedButton : ComposableContainer
{
    readonly System.Action _onClick;
    public OutlinedButton(System.Action onClick) => _onClick = onClick;

    internal override void Render(IComposer composer)
    {
        var click   = new ComposableLambda0(_onClick);
        var content = ComposableLambdas.Wrap3(composer, c => RenderChildren(c));
        ComposeBridges.OutlinedButton(click, BuildModifier(), content, composer);
    }
}
