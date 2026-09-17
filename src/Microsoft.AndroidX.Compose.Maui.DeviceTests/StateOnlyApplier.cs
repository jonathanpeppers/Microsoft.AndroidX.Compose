using Android.Runtime;
using AndroidX.Compose.Runtime;

namespace Microsoft.AndroidX.Compose.Maui.DeviceTests;

[Register("net/compose/maui/devicetests/StateOnlyApplier")]
internal sealed class StateOnlyApplier : Java.Lang.Object, IApplier
{
    public Java.Lang.Object Current => this;
    public void Clear() { }
    public void Down(Java.Lang.Object? node) => throw new InvalidOperationException("Unexpected UI node.");
    public void Up() => throw new InvalidOperationException("Unexpected UI node.");
    public void InsertBottomUp(int index, Java.Lang.Object? instance) => throw new InvalidOperationException("Unexpected UI node.");
    public void InsertTopDown(int index, Java.Lang.Object? instance) => throw new InvalidOperationException("Unexpected UI node.");
    public void Move(int from, int to, int count) => throw new InvalidOperationException("Unexpected UI node.");
    public void Remove(int index, int count) => throw new InvalidOperationException("Unexpected UI node.");
}
