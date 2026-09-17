using AndroidX.Compose;

namespace Microsoft.AndroidX.Compose.DeviceTests;

internal sealed record InfiniteTransitionValueSnapshot(
    int Phase,
    InfiniteTransition Transition,
    InfiniteTransitionAnimation Animation,
    float Value,
    long ElapsedMilliseconds);
