namespace Microsoft.AndroidX.Compose.DeviceTests;

internal sealed record NavSaveableProcessSnapshot(
    string RunId,
    int ProcessId,
    int PreviousProcessId,
    int TaskId,
    bool Factory,
    bool Restored,
    bool Saved,
    int? Value,
    string? Label);
