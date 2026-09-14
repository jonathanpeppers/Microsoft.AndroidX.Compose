# MAUI Backend Review Rules

Read `.github/instructions/compose-maui.instructions.md` for the canonical,
detailed architecture before reviewing this area.

| Check | What to look for |
|-------|------------------|
| One page composition | `PageHandler` owns the page `ComposeView`; converted descendants contribute nodes through `IComposeHandler` instead of attaching more roots. |
| Preserve dual mode | `ComposeElementHandler<T>` must work both when folded into a Compose-aware parent and when hosted as its own fallback `ComposeView` under a stock parent. |
| Keep handler registration complete | New handlers are registered through `UseAndroidXCompose()` after `UseMauiApp`; last registration for a virtual-view type wins. |
| Type mappers concretely | Property and command mappers use the concrete handler type so MAUI's callback cast cannot fail at attachment. |
| Use supported snapshot state | Store Compose-readable primitives/reference peers in `MutableState<T>`; use version counters for unsupported MAUI structs and enums. |
| Guard lifecycle properties | Copy `VirtualView`, `MauiContext`, `PlatformView`, and similar nullable framework properties to guarded locals with actionable messages. |
| Preserve stock fallback | Unconverted controls and containers continue through `ComposeWalker` and `AndroidView` rather than disappearing or forcing consumers onto Compose APIs. |
| Keep consumer API pure MAUI | Consumers opt in with `UseAndroidXCompose()` and should not need to choose roots, import facade internals, or manipulate `ComposeView`. |
| Respect package compatibility pins | Do not casually bump MAUI or trim the Android dependency alignment documented in the scoped instructions and `Directory.Build.targets`. |
| Update coverage | New or changed handlers should update registration, focused tests/sample behavior, and MAUI coverage artifacts where applicable. |

