namespace Microsoft.AndroidX.Compose.DeviceTests;

/// <summary>Density values observed by a composition read or a native measure pass.</summary>
internal sealed record LocalDensitySnapshot(float Density, float FontScale);
