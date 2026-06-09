using AndroidX.Compose.Runtime;

namespace AndroidX.Compose.Gallery.Screens;

/// <summary>
/// Thin <see cref="ComposableNode"/> wrapper that invokes a
/// <see cref="Func{IComposer, ComposableNode}"/> producing the actual tree
/// inside its <see cref="Render(IComposer)"/>. Allows registry entries to
/// allocate <c>composer.Remember</c> state inside the lambda body — the
/// composer is only available during composition, so the factory must
/// run there, not at registry-construction time.
/// </summary>
public sealed class DemoContent : ComposableNode
{
    readonly Func<IComposer, ComposableNode> _factory;

    /// <summary>
    /// Wrap a tree factory. Throws when <paramref name="factory"/> is
    /// <see langword="null"/>.
    /// </summary>
    public DemoContent(Func<IComposer, ComposableNode> factory)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
    }

    public override void Render(IComposer composer)
    {
        var node = _factory(composer)
            ?? throw new InvalidOperationException(
                "DemoContent factory returned null; demos must produce a non-null root composable.");
        node.Render(composer);
    }
}
