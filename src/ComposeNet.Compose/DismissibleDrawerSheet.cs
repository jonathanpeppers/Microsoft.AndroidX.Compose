using AndroidX.Compose.Runtime;

namespace ComposeNet;

/// <summary>
/// Material 3 <c>DismissibleDrawerSheet</c> — the panel shown by a
/// dismissible-style navigation drawer.
/// </summary>
public sealed class DismissibleDrawerSheet : ComposableContainer
{
    internal override void Render(IComposer composer)
    {
        var content = new ComposableLambda3(c => RenderChildren(c));
        ComposeBridges.DismissibleDrawerSheet(content, composer);
    }
}
