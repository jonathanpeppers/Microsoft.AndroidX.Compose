using Kotlin.Jvm.Functions;

namespace Microsoft.AndroidX.Compose.DeviceTests;

sealed record NavContentObservation(string Label, IFunction0 Callback, object Remembered);
