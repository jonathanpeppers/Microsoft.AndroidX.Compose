using AndroidX.Compose;
using AndroidX.Compose.Runtime;

namespace Microsoft.AndroidX.Compose.DeviceTests;

sealed class NavHostLifetimeProbe(NavHost host) : ComposableNode
{
    public override void Render(IComposer composer)
    {
        composer.DisposableEffect("nav-host-probe",
            static () => NavContentTestActivity.MarkUnmounted);
        host.Render(composer);
    }
}
