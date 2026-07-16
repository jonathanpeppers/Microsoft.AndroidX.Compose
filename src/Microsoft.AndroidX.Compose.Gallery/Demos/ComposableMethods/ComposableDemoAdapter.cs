using AndroidX.Compose.Runtime;

namespace AndroidX.Compose.Gallery.Demos.ComposableMethods;

/// <summary>
/// Adapter <see cref="ComposableNode"/> that delegates its
/// <c>Render</c> implementation to a callback. Lets a
/// <c>[Composable]</c> static method plug into the gallery's
/// tree-style demo <c>Build</c> contract, which currently requires
/// every demo to return a <see cref="ComposableNode"/>. This is gallery
/// compatibility glue, not part of the composable-method runtime model; it can be
/// removed once the registry accepts direct composable callbacks.
/// </summary>
internal sealed class ComposableDemoAdapter : ComposableNode
{
    readonly Action _body;

    public ComposableDemoAdapter([ComposableContent] Action body)
    {
        ArgumentNullException.ThrowIfNull(body);
        _body = body;
    }

    public override void Render(IComposer composer)
    {
        ArgumentNullException.ThrowIfNull(composer);
        using var scope = ComposableContext.Enter(composer);
        _body();
    }
}
