# Architecture: how the C# facade works

This doc covers the internals of [`ComposeNet.Compose`](../src/ComposeNet.Compose)
and its sibling source generators. For the *why* behind the project and a
tour of how Jetpack Compose itself works under the hood, see
[compose-internals.md](compose-internals.md).

## The facade: composables as types

Composables are **types**, not method calls. Each is a
`ComposableNode` subclass; containers (`Column`, `MaterialTheme`,
`Button`) implement `IEnumerable` + `Add(ComposableNode)` so C#
collection-initializer syntax compiles. The tree built by `SetContent`'s
lambda is a pure value; `ComposeActivity` walks it and calls
`Render(IComposer)` on each node, threading the composer at the
implementation layer — invisible to user code, but explicit (no
`ThreadStatic`!) the same way Kotlin's compiler plugin makes
`$composer` an explicit IR parameter.

Inside each container's `Render`, JNI bridges declared in
[`ComposeBridges.cs`](../src/ComposeNet.Compose/ComposeBridges.cs) call the
Kotlin-mangled Compose functions (`Text--4IGK_g`, `Button-LP…`,
`AlertDialog-Oix01E0`, etc.) with their `$default` bitmasks. Each
bridge is a one-line `[ComposeBridge]` partial-method declaration; the
boilerplate (cached `IntPtr` class/method handles, signature constants,
`try { Call… } finally { GC.KeepAlive(…) }` around every managed
wrapper whose `.Handle` was read into a `JValue`, and `DeleteLocalRef`
for local string refs) is emitted by `ComposeBridgeGenerator` in
[`ComposeNet.SourceGenerators`](../src/ComposeNet.SourceGenerators). Only
two outliers stay hand-written: `ModifierHandle` (a managed
`IModifier? → IntPtr` conversion that none of the bridge shapes fit)
and `ModifierCompanionInstance` (a `$$INSTANCE` static field lookup,
not a method invocation). The user never sees any of this; when
[dotnet/java-interop#1440] lands and the binder stops dropping
inline-class overloads, each bridge declaration collapses to a direct
generated binding call.

[dotnet/java-interop#1440]: https://github.com/dotnet/java-interop/pull/1440

`MutableNumberState<T>` is the killer feature for Kotlin parity —
`MutableState<T>.ToString()` lets `$"Count: {count}"` interpolate
without `.Value`, and `operator ++/--` (constrained to
`INumber<T>`) lets `count++` mutate the underlying `IMutableState`
directly. So the Kotlin idiom `var count by remember {
mutableStateOf(0) } ; count++` becomes
`var count = Remember(() => new MutableNumberState<int>(0)) ; count++` —
character-for-character equivalent after substituting Kotlin keywords
for C# ones. It works for any built-in numeric primitive
(`sbyte`/`byte`/`short`/`ushort`/`int`/`uint`/`long`/`ulong`/`float`/`double`).
Other `INumber<T>` implementations (`decimal`, `Half`, `BigInteger`,
`nint`, `nuint`) compile but throw at construction since they have no
clean Java box.

## The `$default` bitmask source generator

Every `@Composable` JVM method takes a trailing `int $default` bitmask
where bit N == 1 means "argument N was not provided; use Kotlin's
default." Hand-writing those bitmasks at every call site is illegible
(`_changed: 0b0111`); writing the `[Flags]` enum by hand is tedious
and bit-rots when the Kotlin signature changes.

[`ComposeNet.SourceGenerators`](../src/ComposeNet.SourceGenerators) is a
small Roslyn incremental generator triggered by an assembly-level
attribute. It supports two forms.

**Generic form** — when the binder exposes the Kt method:

```csharp
[assembly: ComposeDefaults<ColumnKt>("Column", "ColumnDefault")]
[assembly: ComposeDefaults<MaterialThemeKt>("MaterialTheme", "MaterialThemeDefault")]
```

The generator reads the longest overload of the named method, emits a
`[Flags] enum` with one bit per real parameter (skipping Compose
`content: () -> Unit` lambdas, which are always supplied), and adds an
`All` constant.

**Declarative form** — for overloads the binder strips. Anything taking
a Kotlin `@JvmInline value class` (`Color`, `Dp`, `TextUnit`,
`FontWeight`, …) gets a mangled JVM name like `Text--4IGK_g`,
`Button-LP…`, `AlertDialog-Oix01E0`, `NavigationBar-HsRjFd4`, and is
dropped from the managed binding. Until [dotnet/java-interop#1440]
exposes them we hand the generator the Kotlin parameter names directly:

```csharp
[assembly: ComposeDefaults("ButtonDefault",
    "!onClick", "modifier", "enabled", "shape", "colors",
    "elevation", "border", "contentPadding", "interactionSource", "!content")]
```

Names prefixed with `!` consume a bit position but emit no enum member
(parameters the caller always provides — `onClick`, `text`, `content`).
Optional slot lambdas the caller toggles per-call (e.g. `AlertDialog`'s
`dismissButton`/`icon`/`title`/`text`, `NavigationBarItem.label`) stay
as enum members so the call site can OR them in. Call sites collapse to
`(int)ButtonDefault.All`.

[`ComposeDefaults.cs`](../src/ComposeNet.Compose/ComposeDefaults.cs) holds
all of these declarations — every composable in the facade gets its
`$default` enum from this one file. Unit tests in
[`ComposeNet.SourceGenerators.Tests`](../src/ComposeNet.SourceGenerators.Tests)
pin the emitted output. When the upstream binder fix lands, each
declarative attribute can be swapped one-for-one to the generic form.

## What's missing on the C# side (and why)

| Kotlin                                  | C# today                                                       | Cost |
| --------------------------------------- | -------------------------------------------------------------- | ---- |
| Skipping / recomposition optimization   | `$changed = 0` everywhere → full subtree recomposes on every state change | Correctness ✅, perf 🙁 |
| Slot-table-backed `remember`            | `Remember(() => …)` with `[CallerLineNumber]` keying into an activity-scoped cache; `Remember(factory, key1, …)` (1–3 keys or `RememberKeyed(factory, keys[])`) resets the slot on key change | Lifetime is per call site, not per nested-scope as in Kotlin |
| `@Composable` type-system enforcement   | None — calling a non-composable from a composable context fails at runtime, not compile-time | Footgun |
| Per-call-site allocation                | Every recomposition allocates fresh `ComposableNode` objects (no slot-table reuse on the C# side) | Tier 2 codegen fixes |

### Why it's like this

The Compose compiler plugin (see [compose-internals.md](compose-internals.md))
rewrites Kotlin IR to inject `$composer`, `$changed`, `$default`,
slot-table keys, restart groups, and skip logic. None of that exists
in our C# pipeline, so we either pay it by hand (per call site) or
skip the optimization (recompose the whole tree). The takeaways from
this tier-1.5 experiment:

- **Pure C# Compose hosting is feasible *and* ergonomic.** A real
  Material 3 UI runs end-to-end on device with zero Kotlin in the
  project, in a syntax that mirrors Kotlin almost line-for-line.
- **The facade trades perf for ergonomics.** Tree allocation per
  recomposition is acceptable for hello-world; for real apps a
  Roslyn source generator (Tier 2) that lowers `[Composable]` C#
  methods to direct composer-threading calls is the next step.
- **Explicitness matches Kotlin.** The composer is an explicit
  parameter at the implementation layer (`Render(IComposer)`), same
  honest mirror of `ComposerParamTransformer` that Kotlin uses —
  not a `[ThreadStatic]` which would silently break
  `SubcomposeLayout` / `MovableContent` / parallel composition.

### What it looked like before the facade

For reference, the pre-facade Tier 1 sample was ~200 lines and required
**five** named ACW classes (`ThemedRoot`, `AppContent`, `ColumnContent`,
`ButtonLabel`, `ClickHandler`) just for one screen. The before/after is
preserved in git history at the commit that introduced
`ComposeNet.Compose` — every `{ … }` block from the Kotlin version had
to be its own `Java.Lang.Object`-derived `IFunction2`/`IFunction3`
class.

## Known issues

- **Hashed inline-class composables aren't in the bindings.** Anything
  with `Modifier`/`Color`/`Dp`/`TextStyle`/`PaddingValues` parameters
  has a Kotlin-compiler-mangled JVM name (`Text--4IGK_g`, `Button-LP…`,
  `AlertDialog-Oix01E0`, `NavigationBar-HsRjFd4`,
  `FloatingActionButton-X-z6DiA`, `ModalBottomSheet-dYc4hso`) that the
  binding generator drops. Each one we use is a `[ComposeBridge]`
  partial-method declaration in
  [`ComposeBridges.cs`](../src/ComposeNet.Compose/ComposeBridges.cs);
  `ComposeBridgeGenerator` emits the JNI plumbing. Tracked upstream in
  [dotnet/java-interop#1440] — when it lands every bridge declaration
  in this repo can be deleted in favour of a direct generated binding
  call.
- **`$changed` bitmasks** — we pass `0` everywhere, so the runtime
  recomposes the whole subtree on every state change. Correct, not
  optimal. Proper bitmask computation per arg is Tier 2 territory.
- **`Modifier.Companion` not bound upstream.** Wrapped by the
  `Modifier` class via a one-time JNI fetch of the `$$INSTANCE` field
  — invisible to callers. See [NOTES.md](NOTES.md) open issue #1 for
  the upstream-friendly fix.
- **`remember(keys, …)` is supported.** Use the keyed overloads
  `Remember(factory, key1)`, `Remember(factory, key1, key2)`,
  `Remember(factory, key1, key2, key3)`, or
  `RememberKeyed(factory, object?[] keys)` — same shape for
  `RememberSaveable`. Compose resets the slot when any key changes
  (structural equality). The slot key still comes from
  `[CallerLineNumber]/[CallerFilePath]`, so the slot's identity is
  per call site (not per nested scope as in Kotlin). For
  rotation/process-death survival use `RememberSaveable` — keys are
  forwarded to Kotlin's `rememberSaveable(vararg inputs)` array so
  the saveable registry uses the same invalidation semantics.
- **State primitives.** `MutableStateList<T>`, `MutableStateMap<K,V>`,
  `Compose.DerivedStateOf<T>(Func<T>)`, and
  `Compose.ProduceState<T>(initialValue, [keys…], producer)` are
  available. `ProduceState` is implemented purely in C# via an
  `IRememberObserver` JCW — the producer is a plain
  `Func<MutableState<T>, CancellationToken, Task>`, not a Kotlin
  suspend lambda. Tracked in #62. Still missing:
  `snapshotFlow { … }` (Flow → `IAsyncEnumerable` bridge) and custom
  `Saver<T, S>` (only `autoSaver` is exposed today).
