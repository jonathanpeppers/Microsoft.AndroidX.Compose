using AndroidX.Compose;
using AndroidX.Compose.Runtime;
using Kotlin.Jvm.Functions;

namespace Microsoft.AndroidX.Compose.DeviceTests;

internal sealed record SurfaceStylingSnapshot(
    int Generation, int Mode, long ContentColor, float AbsoluteElevation,
    object Sentinel, MutableNumberState<int> Counter, IMutableState CounterPeer, int CounterValue,
    MutableNumberState<int> OutsideCounter, int OutsideCounterValue,
    object TailSentinel, IFunction2 Content, int Defaults, int NativeDefaults,
    long NativeColor, long NativeContentColor, int Changed);
