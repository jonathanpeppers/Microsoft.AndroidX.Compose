# Source Generator Review Rules

## Generator contracts

| Check | What to look for |
|-------|------------------|
| Incremental and deterministic | Outputs, hint names, diagnostics, and ordering must be stable across runs and independent of filesystem or hash randomization. |
| `netstandard2.0` compatible | Generator implementation cannot rely on newer runtime APIs merely because consuming projects target .NET 10. |
| Symbols over text | Resolve types, members, attributes, conversions, accessibility, and nullability through Roslyn symbols rather than fragile syntax strings. |
| No broad diagnostic suppression | Report a precise CN diagnostic for unsupported user shapes; do not silently skip malformed declarations. |
| Update diagnostic documentation | New or changed CN1xxx-CN5xxx diagnostics must be tested and reflected in `.github/copilot-instructions.md`. |

## Defaults, bridges, and facades

| Check | What to look for |
|-------|------------------|
| Pin emitted behavior with tests | Every new generator behavior needs a synthetic compilation test that verifies output or diagnostics. |
| Validate JNI before emission | Bridge parsing must reject malformed signatures, mismatched slots, invalid receiver/constructor/suspend shapes, and incompatible managed types. |
| Keep wide masks correct | The threshold, backing type, bit constants, and `.Split()` behavior must cover bit 31 and later groups without sign or truncation bugs. |
| Preserve omission information | Direct lowering distinguishes omitted arguments from explicit `null`; do not reconstruct omission solely from runtime values. |
| Keep unique facade hint names | Per-facade files preserve caller file/line slot keys and prevent collisions. |
| Extend, do not bypass | Unsupported generated facade behavior should become a modeled slot/option with diagnostics and tests, not a hand-written replacement. |

## Composable method interception

| Check | What to look for |
|-------|------------------|
| Keep self-interception guards | Generated `.g.cs` invocations must not recursively generate interceptors. |
| Keep envelope and restart boundaries | Movable call-site envelopes and occurrence groups stay outside restart callbacks; callbacks re-enter only the restart core. |
| Preserve stable call-site identity | Keys include stable syntax location and constructed target identity without process-random hashes. |
| Enforce safe delegate flow | Composerless calls may flow only through callbacks proven synchronous and marked for composable content. |
| Preserve generic shape | Generated wrappers must retain type parameters, constraints, extension receivers, accessibility, and nullability. |

