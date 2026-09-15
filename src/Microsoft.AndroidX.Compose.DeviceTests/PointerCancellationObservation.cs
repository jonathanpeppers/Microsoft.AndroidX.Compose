namespace Microsoft.AndroidX.Compose.DeviceTests;

internal sealed record PointerCancellationObservation(int Phase, bool WasActive, string[] NativeFrames);
