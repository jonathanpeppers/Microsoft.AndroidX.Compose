using AndroidX.Compose.Runtime;

namespace ComposeNet;

/// <summary>Material 3 <c>Text</c> composable.</summary>
public sealed class Text : ComposableNode
{
    readonly string _text;
    public Text(string text) => _text = text;
    internal override void Render(IComposer composer) =>
        ComposeBridges.Text(_text, BuildModifier(), composer);
}
