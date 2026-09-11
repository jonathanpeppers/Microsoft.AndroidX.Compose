using AndroidX.Compose;
using AndroidX.Compose.Runtime;

namespace Microsoft.AndroidX.Compose.DeviceTests;

sealed class NavContentProbe(string route, string label, Action callback) : ComposableNode
{
    public override void Render(IComposer composer)
    {
        composer.DisposableEffect("nav-destination-probe", NavContentTestActivity.MarkDestinationMounted);
        var remembered = composer.Remember(() => new object());
        var action = composer.RememberAction(callback);
        new global::AndroidX.Compose.Button(callback) { new Text(label) }.Render(composer);
        composer.SideEffect(() => NavContentTestActivity.Observe(
            route, new NavContentObservation(label, action, remembered)));
    }
}
