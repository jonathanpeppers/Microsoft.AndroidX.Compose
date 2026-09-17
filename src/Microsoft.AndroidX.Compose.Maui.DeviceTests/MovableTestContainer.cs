using AndroidX.Compose;
using AndroidX.Compose.Runtime;

namespace Microsoft.AndroidX.Compose.Maui.DeviceTests;

internal sealed class MovableTestContainer : ComposableContainer
{
    public override void Render(IComposer composer) => RenderChildren(composer);
}
