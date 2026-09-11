namespace Microsoft.AndroidX.Compose.DeviceTests;

internal sealed record CompositionIdentityProcessSnapshot(
    string RunId,
    int ProcessId,
    int PreviousProcessId,
    int TaskId,
    bool Restored,
    bool Saved,
    int Phase,
    Dictionary<string, int> Values,
    Dictionary<string, int> OrdinaryValues);
