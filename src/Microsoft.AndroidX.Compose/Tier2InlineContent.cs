using AndroidX.Compose.Runtime;

namespace AndroidX.Compose;

/// <summary>
/// Adapter <see cref="ComposableNode"/> that delegates its
/// <c>Render</c> to a callback so Tier 2 content lambdas can be
/// embedded inside collection-init tree-style containers.
/// </summary>
internal sealed class Tier2InlineContent : ComposableNode
{
    readonly Action<IComposer> _body;

    public Tier2InlineContent(Action<IComposer> body)
    {
        ArgumentNullException.ThrowIfNull(body);
        _body = body;
    }

    public override void Render(IComposer composer)
    {
        ArgumentNullException.ThrowIfNull(composer);
        using var scope = ComposableContext.Enter(composer);
        _body(composer);
    }
}
