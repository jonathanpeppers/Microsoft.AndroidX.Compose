using Android.Runtime;
using AndroidX.Compose.Runtime;

namespace Microsoft.AndroidX.Compose.DeviceTests;

[Register("net/compose/devicetests/ThrowingAbandonObserver")]
internal sealed class ThrowingAbandonObserver : Java.Lang.Object, IRememberObserver
{
    internal int AbandonedCalls { get; private set; }
    internal bool ThrowOnAbandoned { get; set; } = true;

    // Inserted first, hash zero selects the first bucket in the pinned ScatterSet.
    public override int GetHashCode() => 0;

    public void OnRemembered() { }
    public void OnForgotten() { }

    public void OnAbandoned()
    {
        AbandonedCalls++;
        if (ThrowOnAbandoned)
            throw new Java.Lang.IllegalStateException("Expected earlier abandon failure.");
    }
}
