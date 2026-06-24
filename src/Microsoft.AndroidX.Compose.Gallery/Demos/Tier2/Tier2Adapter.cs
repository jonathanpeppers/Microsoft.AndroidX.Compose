using AndroidX.Compose.Runtime;

namespace AndroidX.Compose.Gallery.Demos.Tier2;

/// <summary>
/// Adapter <see cref="ComposableNode"/> that delegates its
/// <c>Render</c> implementation to a callback. Lets a Tier 2
/// <c>[Composable]</c> static method plug into the gallery's
/// tree-style demo <c>Build</c> contract.
/// </summary>
internal sealed class Tier2Adapter : ComposableNode
{
    readonly Action<IComposer> _body;

    public Tier2Adapter(Action<IComposer> body)
    {
        ArgumentNullException.ThrowIfNull(body);
        _body = body;
    }

    public override void Render(IComposer composer)
    {
        ArgumentNullException.ThrowIfNull(composer);
        _body(composer);
    }
}
