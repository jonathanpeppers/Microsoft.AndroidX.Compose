using System.Runtime.CompilerServices;

// The Microsoft.AndroidX.Compose.Maui backend reaches into the
// internal `UnpackSizeWidth` / `UnpackSizeHeight` helpers on
// `ComposeBridges` when wiring custom-drawn modifiers (border dashes,
// etc.) — Compose `Size` is an inline `@JvmInline value class` over a
// packed `long` and has no bound surface. Keeping these internal
// stops them polluting the developer-facing API while still letting
// the trusted Maui backend depend on them.
[assembly: InternalsVisibleTo("Microsoft.AndroidX.Compose.Maui")]

// Microsoft.AndroidX.Compose.DeviceTests exercises runtime behaviour
// of internal-only types — Modifier.StructuralKey / ModifierOpKey /
// ChangedBits / DiffSlotShift / MutableComposableLambda0 — that we
// don't want in the public API surface.
[assembly: InternalsVisibleTo("Microsoft.AndroidX.Compose.DeviceTests")]
