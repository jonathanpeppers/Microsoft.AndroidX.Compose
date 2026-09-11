using AndroidX.Compose;
using AndroidX.Compose.Runtime;

namespace Microsoft.AndroidX.Compose.DeviceTests;

sealed class NavSaveableProbeNode(NavSaveableProcessTestActivity activity, string label) : ComposableNode
{
    public override void Render(IComposer composer)
    {
        var state = composer.RememberSaveable(() => new MutableNumberState<int>(0));
        int value = state.Value;
        new Text($"{label}: {value}").Render(composer);
        composer.SideEffect(() => activity.Record(state, value, label));
    }
}
