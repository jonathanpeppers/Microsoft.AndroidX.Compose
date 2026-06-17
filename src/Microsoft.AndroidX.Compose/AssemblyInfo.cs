using System.Runtime.CompilerServices;

// The Microsoft.AndroidX.Compose.Maui backend reaches into a small set
// of low-level JNI plumbing helpers on `ComposeBridges` (DrawScope →
// native Canvas walk, Size unpackers) when wiring custom-drawn
// modifiers (border dashes, etc.). Exposing those publicly would
// pollute the developer-facing surface; this attribute keeps them
// internal to the assembly while still letting the trusted Maui
// backend depend on them.
[assembly: InternalsVisibleTo("Microsoft.AndroidX.Compose.Maui")]

// Microsoft.AndroidX.Compose.DeviceTests exercises runtime behaviour
// of internal-only types — Modifier.StructuralKey / ModifierOpKey /
// ChangedBits / DiffSlotShift / MutableComposableLambda0 — that we
// don't want in the public API surface.
[assembly: InternalsVisibleTo("Microsoft.AndroidX.Compose.DeviceTests")]
