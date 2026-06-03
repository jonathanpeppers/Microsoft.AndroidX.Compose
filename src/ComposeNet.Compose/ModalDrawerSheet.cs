using AndroidX.Compose.Runtime;

namespace ComposeNet;

/// <summary>
/// Material 3 <c>ModalDrawerSheet</c> — the panel shown by a
/// modal-style navigation drawer. Lays out children as a Column.
/// Typically holds nav items; in this facade any
/// <see cref="ComposableNode"/> works.
/// </summary>
public sealed class ModalDrawerSheet : ComposableContainer
{
    internal override void Render(IComposer composer)
    {
        var content = new ComposableLambda3(c => RenderChildren(c));
        ComposeBridges.ModalDrawerSheet(content, composer);
    }
}
