using AndroidX.Compose.Runtime;

namespace ComposeNet;

/// <summary>
/// Material 3 <c>OutlinedCard</c> — a card surrounded by a thin outline
/// instead of relying on tonal/shadow elevation for separation.
/// </summary>
public sealed class OutlinedCard : ComposableContainer
{
    internal override void Render(IComposer composer)
    {
        var content = new ComposableLambda3(c => RenderChildren(c));
        ComposeBridges.OutlinedCard(content, composer);
    }
}
