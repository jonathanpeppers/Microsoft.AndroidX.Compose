using AndroidX.Compose;
using Kotlin.Jvm.Functions;

namespace Microsoft.AndroidX.Compose.DeviceTests;

internal sealed record SurfaceStylingSnapshot(
    int Generation, int Mode, long ContentColor, float AbsoluteElevation,
    object Sentinel, MutableNumberState<int> Counter, int CounterValue,
    object TailSentinel, IFunction2 Content, int Defaults, int Changed);
