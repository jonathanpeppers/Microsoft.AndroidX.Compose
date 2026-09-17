using AndroidX.Compose;

namespace Microsoft.AndroidX.Compose.DeviceTests;

internal sealed record FlowOverflowSnapshot(Guid InstanceId, int NativePid, int Generation, int Tick, bool Expand,
    int Total, int Shown, object Identity, int Counter, ScopeKind Kind);
