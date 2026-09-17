namespace Microsoft.AndroidX.Compose.DeviceTests;

internal sealed record JetchatRecordingPulseSnapshot(
    int Phase,
    float Value,
    long ElapsedMilliseconds);
