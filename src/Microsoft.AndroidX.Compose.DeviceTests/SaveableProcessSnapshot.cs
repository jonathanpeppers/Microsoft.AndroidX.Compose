namespace Microsoft.AndroidX.Compose.DeviceTests;

internal sealed record SaveableProcessSnapshot(
    string RunId,
    int ProcessId,
    int PreviousProcessId,
    int TaskId,
    bool Restored,
    bool Saved,
    SortedDictionary<string, int> Values);
