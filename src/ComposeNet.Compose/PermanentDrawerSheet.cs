using AndroidX.Compose.Runtime;

namespace ComposeNet;

/// <summary>
/// Material 3 <c>PermanentDrawerSheet</c> — the always-visible panel
/// shown by a <see cref="PermanentNavigationDrawer"/>.
/// </summary>
public sealed class PermanentDrawerSheet : ComposableContainer
{
    internal override void Render(IComposer composer)
    {
        var content = new ComposableLambda3(c => RenderChildren(c));
        ComposeBridges.PermanentDrawerSheet(content, composer);
    }
}
