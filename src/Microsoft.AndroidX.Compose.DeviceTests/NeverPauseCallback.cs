using Android.Runtime;
using AndroidX.Compose.Runtime;

namespace Microsoft.AndroidX.Compose.DeviceTests;

[Register("net/compose/devicetests/NeverPauseCallback")]
internal sealed class NeverPauseCallback : Java.Lang.Object, IShouldPauseCallback
{
    public bool ShouldPause() => false;
}
