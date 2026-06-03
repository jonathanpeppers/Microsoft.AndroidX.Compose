using AndroidX.Compose.Runtime;

namespace ComposeNet;

/// <summary>Material 3 <c>FloatingActionButton</c>.</summary>
public sealed class FloatingActionButton : ComposableContainer
{
    readonly System.Action _onClick;
    public FloatingActionButton(System.Action onClick) => _onClick = onClick;

    internal override void Render(IComposer composer)
    {
        var click   = new ComposableLambda0(_onClick);
        var content = new ComposableLambda2(c => RenderChildren(c));
        ComposeBridges.FloatingActionButton(click, BuildModifier(), content, composer);
    }
}
