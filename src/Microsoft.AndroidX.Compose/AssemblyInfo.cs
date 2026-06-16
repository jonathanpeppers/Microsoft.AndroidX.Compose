using System.Runtime.CompilerServices;

// The Microsoft.AndroidX.Compose.Maui backend reaches into a small set
// of low-level JNI plumbing helpers on `ComposeBridges` (DrawScope →
// native Canvas walk, Size unpackers) when wiring custom-drawn
// modifiers (border dashes, etc.). Exposing those publicly would
// pollute the developer-facing surface; this attribute keeps them
// internal to the assembly while still letting the trusted Maui
// backend depend on them.
[assembly: InternalsVisibleTo("Microsoft.AndroidX.Compose.Maui")]
