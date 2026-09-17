using System.Text.Json.Serialization;

namespace Microsoft.AndroidX.Compose.DeviceTests;

[JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Serialization)]
[JsonSerializable(typeof(CompositionIdentityProcessSnapshot))]
[JsonSerializable(typeof(NavSaveableProcessSnapshot))]
[JsonSerializable(typeof(SaveableProcessSnapshot))]
internal partial class ProcessSnapshotJsonContext : JsonSerializerContext;
