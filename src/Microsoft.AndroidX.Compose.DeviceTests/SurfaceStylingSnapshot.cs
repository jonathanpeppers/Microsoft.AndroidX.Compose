using AndroidX.Compose;
using AndroidX.Compose.Runtime;
#if DEBUG
using Kotlin.Jvm.Functions;
#endif

namespace Microsoft.AndroidX.Compose.DeviceTests;

internal sealed record SurfaceStylingSnapshot(
    int Generation, int Mode, long ContentColor, float AbsoluteElevation,
    object Sentinel, MutableNumberState<int> Counter, IMutableState CounterPeer, int CounterValue,
    MutableNumberState<int> OutsideCounter, int OutsideCounterValue,
    object TailSentinel)
{
#if DEBUG
    internal required IFunction2 Content { get; init; }
    internal required int Defaults { get; init; }
    internal required int NativeDefaults { get; init; }
    internal required long NativeColor { get; init; }
    internal required long NativeContentColor { get; init; }
    internal required int Changed { get; init; }
#endif
}
