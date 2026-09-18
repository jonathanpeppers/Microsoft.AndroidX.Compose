using AndroidX.Compose;
using AndroidX.Compose.Runtime;

namespace Microsoft.AndroidX.Compose.Maui.DeviceTests;

internal sealed class MovableStateProbeNode(
    string id,
    IDictionary<string, object> observed,
    IList<string> order,
    IDictionary<string, int> disposals) : ComposableNode
{
    public override void Render(IComposer composer)
    {
        observed[id] = composer.Remember(static () => new object());
        order.Add(id);
        composer.DisposableEffect(id, () => () =>
        {
            disposals.TryGetValue(id, out int count);
            disposals[id] = count + 1;
        });
    }
}
