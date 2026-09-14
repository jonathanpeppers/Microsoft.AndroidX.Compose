using AndroidX.Compose;
using Edges = (float Left, float Top, float Right, float Bottom);

namespace Microsoft.AndroidX.Compose.DeviceTests;

internal sealed record ScaffoldInsetsSnapshot(
    int Generation,
    int Mode,
    float Density,
    int WindowWidth,
    int WindowHeight,
    Edges Root,
    Edges Body,
    Edges? TopBar,
    Edges? BottomBar,
    Edges Forwarded,
    Edges BoundDefault,
    Edges ExplicitReader,
    Edges ImplicitReader,
    Edges NavigationBars,
    Edges Ime,
    object BodySentinel,
    IntPtr ContentLambda,
    MutableNumberState<int> Counter,
    int CounterValue,
    int ExpectedMarkerWidth,
    int ActualMarkerWidth,
    Edges? NativeInsetsAtPlacement,
    string Trace,
    int BodyObservationVersion);
