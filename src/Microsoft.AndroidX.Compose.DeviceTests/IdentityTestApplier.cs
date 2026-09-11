using Android.Runtime;
using AndroidX.Compose.Runtime;

namespace Microsoft.AndroidX.Compose.DeviceTests;

// These controlled compositions exercise slots and observers without emitting UI nodes.
[Register("net/compose/devicetests/IdentityTestApplier")]
internal sealed class IdentityTestApplier : Java.Lang.Object, IApplier
{
    public Java.Lang.Object? Current => null;
    public void Clear() { }
    public void Down(Java.Lang.Object? node) => throw new InvalidOperationException("Unexpected node.");
    public void Up() => throw new InvalidOperationException("Unexpected node.");
    public void InsertBottomUp(int index, Java.Lang.Object? instance) => throw new InvalidOperationException("Unexpected node.");
    public void InsertTopDown(int index, Java.Lang.Object? instance) => throw new InvalidOperationException("Unexpected node.");
    public void Move(int from, int to, int count) => throw new InvalidOperationException("Unexpected node.");
    public void Remove(int index, int count) => throw new InvalidOperationException("Unexpected node.");
}
