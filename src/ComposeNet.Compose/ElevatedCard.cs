using AndroidX.Compose.Runtime;

namespace ComposeNet;

/// <summary>
/// Material 3 <c>ElevatedCard</c> — like <see cref="Card"/> but uses
/// shadow rather than tonal elevation for separation. Children are added
/// via collection-initializer syntax.
/// </summary>
public sealed class ElevatedCard : ComposableContainer
{
    internal override void Render(IComposer composer)
    {
        var content = new ComposableLambda3(c => RenderChildren(c));
        ComposeBridges.ElevatedCard(content, composer);
    }
}
