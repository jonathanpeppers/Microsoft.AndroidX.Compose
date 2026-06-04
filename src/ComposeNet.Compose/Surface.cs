using AndroidX.Compose.Runtime;

namespace ComposeNet;

/// <summary>
/// Material 3 non-interactive <c>Surface</c> — applies background color,
/// elevation, and clipping to its content.
/// </summary>
public sealed class Surface : ComposableContainer
{
    internal override void Render(IComposer composer)
    {
        var content = ComposableLambdas.Wrap2(composer, c => RenderChildren(c));
        ComposeBridges.Surface(BuildModifier(), content, composer);
    }
}
