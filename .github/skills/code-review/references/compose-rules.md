# Compose Runtime Review Rules

## Bindings and JNI

| Check | What to look for |
|-------|------------------|
| Check official bindings first | Verify the exact API in the package's runtime `.Android.dll` with `ilspycmd`. Bridge only the stripped member, not an already bound surrounding type. |
| Match JNI shape exactly | Validate class, mangled JVM name, signature, receiver placement, composer placement, `$changed`, `$default`, continuation, marker, and return type. |
| Clean local references in `finally` | Every local from `GetStaticObjectField`, `CallObjectMethod`, `NewObject`, `NewString`, and similar operations must be consumed or deleted on all paths. |
| Preserve peer lifetimes | Generated bridges and bound calls already keep Java peers alive. Add manual `GC.KeepAlive` only around raw hand-written JNI that needs it. |
| Keep suspend handles raw | Suspend bridges return `IntPtr`; do not wrap `COROUTINE_SUSPENDED` or singleton results with ownership that can invalidate global-backed peers. |

## Compose defaults and change tracking

| Check | What to look for |
|-------|------------------|
| Generate default masks | Add `[ComposeDefaults]` in `ComposeDefaults.cs`; never hand-write `FooDefault` or wide-mask splitting. |
| Preserve Kotlin positions | Default and changed bits follow physical Kotlin parameters, including skipped required slots and receivers where applicable. |
| Prefer conservative change masks | If receivers or all changed-mask groups cannot be represented, emit zero/Uncertain for the entire route. A partial known mask can incorrectly skip real changes. |
| Keep callback identity stable | Event callbacks use remembered mutable adapters. Veto callbacks that participate in `remember` keys need one stable adapter per node instance. |

## Composition identity

| Check | What to look for |
|-------|------------------|
| Wrap synchronous content correctly | Use `ComposableLambdas.Wrap2/Wrap3` for content created and invoked in the same composition pass. |
| Instantiate deferred content correctly | Use `Instantiate4` for lazy DSL content invoked later; captured active composers become stale. |
| Key sibling loops | Render each sibling inside deterministic replaceable groups based on position and type. |
| Use deterministic keys | Use `SourceLocationKey`, `CompositionGroupKey`, and established FNV-based helpers; randomized hashes break saved state across processes. |
| Preserve tracked invalidation | Stable content identity alone is unsafe unless mutable content invalidates prior readers. Do not mark untracked mutable content Static. |

## Public surfaces

| Check | What to look for |
|-------|------------------|
| Preserve facade generation | Existing generated facades stay generated; add generator support for new shapes. |
| Document generated facades | Every generated public facade needs a sibling partial stub with XML documentation. |
| Cover both entry paths | Tree-style and composerless/generated catalog calls need equivalent defaults, callbacks, modifiers, state, and ownership. |
| Add gallery coverage | Every new public facade, state holder, modifier, suspend API, or scope helper needs a visible, bounded gallery demo and catalog registration. |

