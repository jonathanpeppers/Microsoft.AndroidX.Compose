using Android.Runtime;
using AndroidX.Compose.Runtime;

namespace Microsoft.AndroidX.Compose.Maui.DeviceTests;

[Register("net/compose/maui/devicetests/StateOnlyApplier")]
internal sealed class StateOnlyApplier : Java.Lang.Object, IApplier
{
    readonly Stack<Java.Lang.Object> _nodes = new();

    public Java.Lang.Object Current => _nodes.TryPeek(out var node) ? node : this;

    public void Clear() => _nodes.Clear();

    public void Down(Java.Lang.Object? node)
    {
        _nodes.Push(node
            ?? throw new InvalidOperationException("The composition attempted to descend into a null UI node."));
    }

    public void Up()
    {
        if (!_nodes.TryPop(out _))
            throw new InvalidOperationException("The composition attempted to leave the applier root.");
    }

    public void InsertBottomUp(int index, Java.Lang.Object? instance) { }
    public void InsertTopDown(int index, Java.Lang.Object? instance) { }
    public void Move(int from, int to, int count) { }
    public void Remove(int index, int count) { }
}
