using AndroidX.Compose.Runtime;

namespace ComposeNet;

/// <summary>
/// Material 3 non-clickable <c>Card</c> — a tonal surface with rounded
/// corners that lays its children out as a Column. Children are added
/// via collection-initializer syntax:
/// <code>
/// new Card { new Text("Title"), new Text("Subtitle") }
/// </code>
/// </summary>
public sealed class Card : ComposableContainer
{
    internal override void Render(IComposer composer)
    {
        var content = new ComposableLambda3(c => RenderChildren(c));
        ComposeBridges.Card(BuildModifier(), content, composer);
    }
}
