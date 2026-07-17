using AndroidX.Compose;
using AndroidX.Compose.Runtime;

namespace Microsoft.AndroidX.Compose.DeviceTests;

sealed class ProgressIndicatorRenderMarker(ComposableNode child) : ComposableNode
{
    public override void Render(IComposer composer)
    {
        child.Render(composer);
        ProgressIndicatorTestActivity.MarkRenderCompleted();
    }
}
