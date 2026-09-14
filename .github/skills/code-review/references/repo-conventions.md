# Repository Conventions

Use `.github/copilot-instructions.md` as the canonical source. This checklist
highlights conventions most likely to reveal review defects.

## Structure and style

| Check | What to look for |
|-------|------------------|
| One type per file | Each type belongs in a matching `.cs` file. Tiny private nested implementation types are the only normal exception. |
| File-scoped namespaces | Use `namespace Microsoft.AndroidX.Compose;`-style namespaces. |
| Public XML documentation | Every new public type and non-trivial public member needs a useful `<summary>`; generated facades need a documented sibling stub. |
| No postfix null-forgiving operator | Replace `value!` with a real guard. Parameters use `ArgumentNullException.ThrowIfNull`; nullable framework or inherited properties use a local plus an actionable `InvalidOperationException`. |
| Collection expressions | Prefer `[]`, `[a, b]`, and spread expressions over `Array.Empty<T>()`, `new[]`, or collection initializers where target typing permits. |
| Minimal comments | Explain non-obvious reasons and contracts, not obvious actions. Do not add section banners or planning comments. |
| Minimal scope | Avoid unrelated cleanup, speculative helpers, unused overloads, and new public surface without a demonstrated consumer. |

## Architecture awareness

| Check | What to look for |
|-------|------------------|
| C#-only implementation | Do not add Kotlin sources, custom Compose bindings, or alternate binding projects. |
| Official bindings first | Before JNI is added, verify the exact member is absent from the runtime `.Android.dll`, not only the small facade DLL. |
| Generated paths stay generated | `$default` enums, supported JNI bridges, generated companions, and generated facades should not be hand-written. |
| No generated-facade demotion | Extend `ComposeFacadeGenerator` for a new shape instead of replacing an existing `[ComposeFacade]` facade with hand-written code. |
| Both authoring styles remain coherent | Tree facades and `[Composable]` methods must preserve equivalent behavior, defaults, ownership, and composition semantics. |
| Complete public feature wiring | A new surface usually needs facade or generated entry point, XML docs, public API baseline, tests, and a gallery demo. |

## Review anti-patterns

- Do not recommend a modern BCL API without checking the changed project's
  target framework; source generators target `netstandard2.0`.
- Do not suggest replacing conservative Compose `Uncertain` changed masks
  with partially known masks.
- Do not request broad refactors when the changed code follows an intentional
  documented holdout.
- Do not assume a binding is missing without inspecting the runtime companion
  assembly and the exact member signature.

