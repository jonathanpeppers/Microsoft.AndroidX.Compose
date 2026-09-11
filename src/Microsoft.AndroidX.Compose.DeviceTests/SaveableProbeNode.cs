using AndroidX.Compose;
using AndroidX.Compose.Runtime;

namespace Microsoft.AndroidX.Compose.DeviceTests;

internal sealed class SaveableProbeNode(
    SaveableProcessTestActivity activity, string name, int wrapper = 0) : ComposableNode
{
    public override void Render(IComposer composer)
    {
        switch (wrapper)
        {
            case 0:
                RenderValue(composer);
                break;
            case 1:
            case 2:
                ComposableContentNode.RenderDirect(composer, RenderValue, indexed: wrapper == 2);
                break;
            case 3:
            case 4:
                ComposableContentNode.RenderDirect(composer,
                    () => RenderValue(ComposableContext.Current), indexed: wrapper == 4);
                break;
            default:
                throw new InvalidOperationException($"Unknown saveable probe wrapper: {wrapper}.");
        }
    }

    void RenderValue(IComposer composer)
    {
        var state = composer.RememberSaveable(() => new MutableNumberState<int>(0));
        int value = state.Value;
        new Text($"{name}: {value}").Render(composer);
        composer.SideEffect(() => activity.Record(name, state, value));
    }
}
