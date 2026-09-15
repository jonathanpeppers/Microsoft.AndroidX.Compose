# Architecture: how the C# facade works

This doc covers the internals of [`Microsoft.AndroidX.Compose`](../src/Microsoft.AndroidX.Compose)
and its sibling source generators. For the *why* behind the project and a
tour of how Jetpack Compose itself works under the hood, see
[compose-internals.md](compose-internals.md).

## Pointer-input long-press lifecycle

`Modifier.PointerInput(handler, key)` calls the official runtime binding's
`SuspendingPointerInputFilterKt.PointerInput` with a bound
`IPointerInputEventHandler`. Callers keep that handler remembered and must not
dispose it while installed. Managed keys retain `Equals` through `ManagedBox`;
they are not coerced to strings. Keys should be immutable.

`Modifier.DetectDragGesturesAfterLongPress` adds a native `Modifier.composed`
factory. Every materialized location remembers a handler and all four callback
peers, keyed by the explicit pointer-input key. A successful composition's
`SideEffect` publishes the latest immutable delegate bundle; a speculative
composition never changes the installed callbacks. Rebuilding/reusing the C#
chain therefore neither resets a gesture nor shares a handler between locations.
The factory returns a Modifier rather than Unit, so it has a specialized
`IFunction3` adapter instead of a Unit-returning content lambda.

The `UI.Android` 1.11.3.1 runtime binding exposes both `PointerInput` and the
handler's suspend `Invoke`. `Foundation.Android` 1.11.3.1 still omits
`DetectDragGesturesAfterLongPress`; one generated `$default` suspend bridge
supplies its callbacks and forwards the **native outer continuation**. There
is no managed Task or awaiter-only cancellation token. Native key changes,
density/view-configuration changes, detach and disposal cancel the actual job.
The detector reports start position, per-event X/Y pixel deltas, end/release and
cancel, and consumes movement in Kotlin. Its cancellation callback can also run
while waiting for the next gesture, not only after a recognized long press.
Temporary JNI result references are released in `finally`, and the coroutine
suspended singleton uses the existing raw-handle sentinel check.

`LongPressDragTests` injects real native MotionEvents and waits for detector
callbacks, with bounded frame barriers for recomposition. It checks short taps,
long press, two-axis deltas, latest callbacks, native handler/callback identities
across recomposition and GC, key reset, modifier removal, MotionEvent cancellation
and activity disposal. No synthetic animation-clock helper is involved.
Gallery routes `modifiers-long-press-drag` and `buttons-tooltips` expose the
lifecycle and competing-tooltip-input control.

## Bound baseline modifiers

`Modifier.AlignBy(HorizontalAlignmentLine)` and `AlignByBaseline()` resolve the
active Row receiver at materialization time; `AlignBy(VerticalAlignmentLine)`
requires Column. Flow containers use the same published scope kinds. Chains
can be constructed outside composition and reused; applying one with a missing
or incompatible scope throws an `InvalidOperationException` naming the operation
and actual scope. The baseline constants and alignment-line contracts come from
the official `AndroidX.Compose.UI.Layout` binding (`AlignmentLineKt.FirstBaseline`
and `LastBaseline`), not a parallel enum or integer selector.

`PaddingFrom(line, before, after)` and `PaddingFromBaseline(top, bottom)` take
nullable `Dp`: null maps to Kotlin's `Dp.Unspecified` float/NaN representation.
Zero stays specified, which matters under minimum constraints when Compose chooses
whether the before or after distance positions the content. Native constraints,
missing-line fallback and validation are retained. `ClipToBounds()` uses the
bound rectangular draw clip without changing measurement; place it before a
child transform to clip overflow rather than moving the viewport.

All these methods call the **runtime companion** bindings (`1.11.3.1`), including
the Dp-mangled padding overloads. No additional JNI/default masks are needed.
`AppendBound` retains captured line peers, and the generated binding keeps
receivers/arguments alive across calls. Their structural keys record line
identity, nullable distances and chain order; `AlignByBaseline` shares its key
with `AlignBy(FirstBaseline)`. Callers must not dispose a captured line while a
chain is in use. Gallery routes `modifiers-baseline-alignment`,
`modifiers-baseline-padding` and `modifiers-clip-to-bounds` demonstrate the surface.
`BaselineModifierTests` measures placed text baselines, constrained padding,
tree/composable/flow scope dispatch and native PixelCopy overflow, rather than
inferring correctness from a successful build.

The cold scope-failure regression also protects `ModifierCompanionInstance`.
It reads the **outer** `Modifier.Companion` static field. Initializing
`Modifier$Companion.$$INSTANCE` first triggers a JVM default-interface
initialization cycle in the pinned bytecode and can permanently leave the outer
field null; subsequent native Row/Column defaults then crash. Initializing the
outer interface first avoids the cycle. The cached global reference and
fresh-local return contract stay unchanged, with local cleanup in `finally`.

### Measured regression evidence

On 2026-09-14, the embedded DeviceTests APK (SHA-256
`430D6BE336F64A3521F6D20B37A9F0A9DBAC68444CBE5845D6F8AA18A923C014`)
passed all seven `BaselineModifierTests` cases in one fresh instrumentation
process on Pixel 7, including rejected cold scope builds before rendering.
At density 2.625, the native measurements were:

| Contract | Observed result |
| --- | --- |
| First / last text baselines | Equal absolute baselines at 78 / 375 px in tree and composerless rows; unchanged after managed and Java GC |
| Published vertical lines | Column and FlowColumn placed children at X=53 / 0 with lines at 26 / 79 px, both meeting at X=79 |
| Baseline-relative padding | 32 dp before = 84 px; 24 dp after = 63 px; minimum-constraint null/zero distinction and maximum-height limits passed |
| Rectangular clipping | Both viewports measured 263x126 px; native PixelCopy found red inside both, red overflow without clipping, and white outside the clipped viewport |

The three Gallery demos also rendered with readable labels. Jetchat recording,
shifted cancellation content, and the unavailable-selector panel were captured;
an interior drag clipped the cancellation arrow at the fixed viewport while
retaining the text label, and a further drag cancelled recording successfully.
These checks do not establish whole-sample parity or resolve profile parallax.

## The facade: composables as types

Composables are **types**, not method calls. Each is a
`ComposableNode` subclass; containers (`Column`, `MaterialTheme`,
`Button`) implement `IEnumerable` + `Add(ComposableNode)` so C#
collection-initializer syntax compiles. The tree built by `SetContent`'s
lambda is a pure value; the host `ComponentActivity` (or `ComposeView`)
walks it and calls `Render(IComposer)` on each node, threading the
composer at the implementation layer the same way Kotlin's compiler
plugin makes `$composer` an explicit IR parameter. Public APIs support both
explicit composer threading (`SetContent(c => …)`, `c.Remember(…)`) and
composerless calls (`SetContent(() => …)`, `Remember(…)`). The composerless
surface uses a dynamically scoped `ThreadStatic` lookup at user-facing call
boundaries; the implementation still passes `IComposer` explicitly through
every `Render` and generated interceptor core.

Inside each container's `Render`, JNI bridges declared in
[`ComposeBridges.cs`](../src/Microsoft.AndroidX.Compose/ComposeBridges.cs) call the
Kotlin-mangled Compose functions (`Text--4IGK_g`, `Button-LP…`,
`AlertDialog-Oix01E0`, etc.) with their `$default` bitmasks. Each
bridge is a one-line `[ComposeBridge]` partial-method declaration; the
boilerplate (cached `IntPtr` class/method handles, signature constants,
`try { Call… } finally { GC.KeepAlive(…) }` around every managed
wrapper whose `.Handle` was read into a `JValue`, and `DeleteLocalRef`
for local string refs) is emitted by `ComposeBridgeGenerator` in
[`Microsoft.AndroidX.Compose.SourceGenerators`](../src/Microsoft.AndroidX.Compose.SourceGenerators). For
the simplest facade shapes, the entire `Render` body itself is also
generated by `ComposeFacadeGenerator` from a one-line `[ComposeFacade]`
attribute stacked on the bridge — see
[`.github/copilot-instructions.md`](../.github/copilot-instructions.md)
("Facade generator — `[ComposeFacade]`") for the ~10 phases of facade
shapes the generator covers. Only three outliers stay hand-written at
the JNI layer: `ModifierHandle` (a managed `IModifier? → IntPtr`
conversion that none of the bridge shapes fit),
`ModifierCompanionInstance` (a `Companion` static field lookup, not a
method invocation), and `ModifierClipRoundedCorners` (a two-step
`RoundedCornerShape` ctor + `ClipKt.clip` with an intermediate `Shape`
local ref). The user never sees any of this; when
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

## Shared state ownership

A managed state wrapper and its native composition owner have different
lifetimes. Keeping `TimePickerState.Jvm` reachable does not keep Kotlin's
`rememberSaveable` registration alive. The shared-state generator therefore
remembers an `IRememberObserver` directly in the owner's slot and calls the
native `RememberXxxState` on every execution of that owner. Siblings sharing
the wrapper consume the same peer without creating independent native state.
The same preamble is used by tree facades and direct composable helpers.

A strong `GCHandle` released by `OnForgotten` or `OnAbandoned` is not a
bounded composition resource. The pinned native
dispatcher can stop after an earlier observer throws, leaving another owner
without its retirement callback after its slots have already been removed.
Tests that make the owner's own release callback throw do not cover this path.
The skipped-retirement regression uses a public owner followed by a generated
child with throwing cleanup, then probes collection with resurrection-tracking
weak references across managed and Java GC.

`SharedStateOwner` has no self-root. The wrapper's conditional weak-table entry
holds only a resurrection-tracking weak reference to its owner. Active native
slots preserve the original managed JCW through the runtime GC bridge; an
attempt to activate an empty replacement throws rather than silently losing
arbitration and callbacks.

First acquisition is serialized by a per-wrapper managed gate and an exact weak
claim identity. Native lifetime queries, release callbacks, and invalidation
notifications run outside that gate. The winner publishes an initializing claim
before entering its native remember subtree, then acknowledges a published peer
only after Remember, managed peer conversion, and pending-value binding succeed.
An exception before that acknowledgement retires only the new initial claim,
before native unwinding. A later failure of an already-published owner still
uses native abandonment/forgetting and consumer invalidation.

The native monitor alone is not an initialization acknowledgement: the runtime
can release it before dispatching abandonment callbacks. Borrowers therefore
capture the acknowledged peer under the arbitration gate rather than rereading
the mutable wrapper's `Jvm` field after acquisition. The acquisition keeps that
peer alive through the component bridge call. This is binding readiness, not
successful composition application; native snapshot values still require the
normal commit boundary. Retirement publishes completion before notifying
consumers, so a waiting claimant does not depend on subsequent invalidation.

Before borrowing a token, `Java/SharedStateLifetime.java` checks its native
registration under the owning `CompositionImpl` lock. For an installed owner,
the token-keyed marker scope must be valid and belong to that composition's
installed slot storage. The marker is a sibling of the native remember subtree,
not part of its save-key ancestry. Tokens without a marker use exact installed
`RememberObserverHolder` membership. For an insertion not yet installed, the
query searches executable registration operations in the current main, late,
and writer change lists, following only nested `ApplyChangeList` operations.
It matches the holder's wrapped token by Java identity, never an arbitrary
auxiliary-key reference. Native abandonment clears these operation prefixes
before dispatching callbacks, even if a later callback throws.

This is a read-only compatibility layer for the pinned Compose Runtime 1.11.3
Gap and Link implementations, not a public Compose lifecycle API. It caches
reflection metadata, not compositions or queues; it neither installs a tooling
observer nor mutates native storage. Missing fields or unsupported queue shapes
throw explicit compatibility errors. Runtime upgrades must re-audit the
registration operations and rerun both backend regressions. Consumer keep rules
in `shared-state-lifetime.pro` preserve reflected fields, operation identities,
and the JNI-only shared time/sheet entry points through R8.

Use a clean Release build for R8 validation on the installed .NET for Android
Windows SDK 36.1.69.
Its incremental AAR-import path can omit consumer rules from
`libraryprojectimports.cache` when an archive's hash is unchanged, even though
the extracted `proguard.txt` remains present. This removed both
`SharedStateLifetime` and the JNI-only `TimePickerKt` factories in a reproduced
incremental build. A clean build restores collection of the existing narrow
rules; verify the actual R8 `--pg-conf` inputs and final DEX, not just the AAR.
The library exports its rules, but this work does not fix the SDK's incremental
cache behavior. Clean-build native coverage does not establish incremental,
AOT, obfuscated, or full Release UI compatibility.

Concurrent compositions can already hold their own native monitor when
borrowing another composition's state. The query registers a transient
consumer-monitor-to-owner-monitor dependency before acquiring a foreign monitor.
The dependency graph uses Java object identity and a short gate that never
encloses native monitor acquisition. Before publishing a new token or newly
acquired owner, the runtime records its monitor in a weak-identity catalogue.
A query registers dependencies from every observed native monitor actually
held by its thread, including enclosing compositions, not just the immediate
consumer. Every possible foreign owner target must have been catalogued before
publication; an internal query violating that invariant throws before waiting.
Unreachable weak entries are pruned, and the catalogue retains neither native
monitors nor compositions. Actual same-thread monitor ownership recognizes
reentrant queries, which cannot introduce a wait. Acyclic contention
waits for the exact locked membership check; contention is never interpreted as
an absent or live owner.

If a new dependency closes a sharing cycle, the call throws
`Java.Lang.IllegalStateException` with
`Shared state ownership cycle detected between concurrent compositions. Retry composition sequentially.`
The failed composition propagates the error through native abandonment.
Callers may retry their composition sequentially after the participating
concurrent calls have returned; there is no automatic retry loop. Existing
committed peers remain owned. This restriction concerns detected concurrent
sharing cycles, not all cross-thread sharing. It is not a detector for arbitrary
application locks or unrelated nested native `ComposeContent` lock acquisitions.
Edges are removed on return or throw, before releasing an
acquired target monitor, and the graph retains no completed calls or timers.

Scope validity alone is insufficient for abandoned insertions: their anchors
can remain valid after the batch is discarded. `HasPendingChanges` may describe
an unrelated newer attempt. The native-only reentry, same-composition recovery,
unrelated pending-attempt recovery, and disjoint provisional sharing tests pin
these distinctions.

Paused work can install slots before final application. Cancellation first
discards its registration set, then dispatches abandonment; a thrown callback
can leave both installed membership and the cancelled transaction reachable.
Each token therefore captures the native paused transaction's cancellation
cell at registration, and separately when it first acquires ownership. The latter also
covers a previously committed borrower acquiring a peer during a later pause.
Ordinary owning rerenders never overwrite either origin. Every positive
membership result is qualified by both captured cells' atomic state values.
The pinned runtime never replaces these `AtomicReference` cells. Each retains
only a state enum, unlike the transaction itself, whose final content delegate
would retain obsolete content for the surviving owner's lifetime. An unchanged
committed owner is not rejected merely because unrelated
paused work in its composition was cancelled. The origin bridge normalizes its
owned JNI local into a managed peer and releases the local in `finally`.

If a callback was skipped, the next consumer retires the stale token before
running its native factory. If the weak token has already been collected,
the incoming wrapper cleanup snapshots and unbinds the orphaned peer before
claiming ownership. Neither recovery requires finalizer timing; cleanup errors
propagate. Until another consumer executes, a retained wrapper can still expose
the orphaned peer, but it cannot keep the absent owner or composition rooted.

For conditional consumers, hoist the typed owner before the condition:

```csharp
var state = composer.RememberTimePickerState();
return new Column
{
    new Box { showClock ? new TimePicker(state) : null },
    new Box { showKeyboard ? new TimeInput(state) : null },
};
```

The equivalent composerless helper is `Composables.RememberTimePickerState()`.
Typed helpers also cover date, date-range, drawer, sheet, and navigation-suite
state. They accept an optional existing wrapper; when omitted, a wrapper is
remembered at the owner location. Do not put a typed owner call inside a
`Remember` factory: it must participate in every owning composition execution.
Keep it outside the lifetime of any consumer that may disappear. This retains
the exact native peer and save provider even when all its visual consumers
are hidden. Saving/recreating the activity uses a fresh managed wrapper and
the native saver, not a managed reference to the old activity's peer.

For generator consumers, a zero-argument Remember bridge also supports wrappers
without an accessible default constructor. Its typed helpers require a supplied,
non-null wrapper instead of silently omitting the ownership API; the facade's
existing optional wrapper remains supported. Shared declarations using the same
Remember bridge and wrapper type must agree on `Bind` and `Unbind`, including
whether a hook is omitted. Conflicts within one facade or across siblings report
CN3009 before generation, so renaming a facade cannot select a different cleanup
or binding policy. Confirm-callback metadata belongs to their common Remember
bridge, not to the sibling chosen to emit the deduplicated helper.

Confirm callbacks belong to the owner. Their JNI adapters are remembered in
the owning native group, so reconstructing tree nodes does not change callback
identity or invalidate native state. A new adapter receives its initial veto
before native state creation. Updates to an existing adapter capture the
current delegate and publish it through `SideEffect` after successful
application; abandoned renders cannot change the committed native policy. Configure the
callback on the typed owner when using one; consumer callbacks do not override
another location's ownership. Pending picker writes are applied once when a
peer is first attached, not replayed on every owning execution.

Replacing a supplied wrapper at the same location replaces its native remember
subtree, rather than binding the new wrapper to the previous wrapper's peer.
The generator uses `StartReusableGroup` with a fixed positional key and the
owner token as auxiliary data. Auxiliary identity invalidates remembered values
without making the save-state ancestry depend on object hashes, peer handles,
or process-specific IDs. Omitted parameterized wrappers are remembered in
composition in both tree and direct paths, so rebuilding an ordinary tree node
does not look like an explicit wrapper replacement.

If an implicit owner leaves, its observer captures transferable live values,
clears the wrapper's binding, and invalidates remaining consumers. A remaining
consumer then becomes the owner of a **new** native peer. Returning consumers
share that successor; if all consumers left, later re-entry initializes a new
peer from the retained wrapper values. This intentionally replaces the old
hide/show behavior that kept an unregistered native object alive. Imperative
operations requiring a peer cannot run while the wrapper is unbound.

| Wrapper | Values retained after native ownership ends |
| --- | --- |
| Time picker | Hour, minute, 12/24-hour mode |
| Date picker | Selection, displayed month, display mode, year range, selection policy |
| Date-range picker | Both selections, displayed month, display mode, year range, selection policy |
| Drawer | Current settled drawer value |
| Sheet | Current settled sheet value; construction options remain on the wrapper |
| Navigation suite | Current settled visibility |

Layout anchors, gesture offsets, and in-flight animation progress belong to the
disposed native scope and are not transferred. Callback policy belongs to the
new owner after handoff. `BottomSheetScaffold` participates in the same ownership
protocol without changing its standard-sheet construction defaults; modal and
standard sheet owners have different construction constraints. Cross-family
sheet handoff is an explicit exception to transferring values without clamping:
the receiving standard factory maps retained `Hidden` to `PartiallyExpanded`
because `skipHiddenState = true` disallows hiding. This **makes the sheet
visible**. The receiving modal factory maps retained `PartiallyExpanded` to
`Expanded` only when the holder's `SkipPartiallyExpanded` is true. All compatible
values, including `Expanded`, pass through unchanged. Unbound `CurrentValue`,
`TargetValue`, and `IsVisible` continue to expose the raw last-settled value;
normalization is confined to the receiving factory's initial-value argument.
A live common-ancestor owner still preserves its peer and construction options
when switching consumers; these mappings do not replace an active shared peer.

Native saved-state keys are positional. An implicit owner handoff is **not**
a movable save-state key: fresh composition may choose a different first
consumer after ordering/visibility changes. Use the explicit common-ancestor
owner for save/recreation across conditional or reordered consumers, and keep
that owner alive if state must be saved while all consumers are hidden.

`SharedStateOwnershipTests` exercises repeated execution followed by activity
recreation with a fresh wrapper, plus independently removed/reintroduced
consumers under a surviving common ancestor. Its native-only control
distinguishes a missing save provider from a broken recreation harness.
The omitted direct-consumer case calls `Composables.TimeInput()` without a
wrapper or ancestor-owner argument, edits its native accessibility fields to
19:27, repeats three executions, and checks the fields after recreation.
`StateHolderLifecycleTests` and `SharedStateTransferTests` verify owner loss,
pending writes, new-peer initialization, and native confirm-callback refresh.
`SheetStateTransferDomainTests` checks the native constructor invariants and
unbound getter semantics separately from `SheetStateHandoffTests`, which renders
real modal sheets and standard scaffolds through tree and direct APIs, retires
each owner, and checks both handoff directions, round trips, repeated renders,
and compatible expanded-value controls.
Initial picker readiness, like removal/re-entry, acquires the existing
`SideEffect` render-completion counter before reading native values. A non-null
peer alone can still expose an uncommitted snapshot: the same DateRange peer
returned null endpoints before commit and the requested range after commit.
Replacement regressions exercise A-to-B-to-A transitions, pending writes,
surviving siblings, and recreation. `SharedStateTransactionTests` uses native
controlled compositions to check initial abandonment, abandoned owner
replacement, initial veto availability, and commit-only callback publication.
The lifetime fixture also covers skipped earlier cleanup and abandonment,
takeover while the obsolete token is still strongly retained, native pending
sharing, and mixed-GC preservation of active peers. Collection probes use
resurrection-tracking weak references across both runtimes. Probe factories
whose Release stack can retain incidental values run on a thread that exits
before collection; all original exception guards and collection assertions
remain in place.
`SharedStatePausedLifetimeTests` verifies failed cancellation with an installed
owner marker and skipped abandonment, preservation of an older committed
sibling, successful paused application, null-marker borrower registration,
later acquisition by a committed borrower, and collection of the cancelled
transaction and owner graph. A successful-application control replaces the
content but keeps its owner and composition alive while the old callback,
captured payload, and paused transaction must collect.
Both Gap and Link backends are selected explicitly
through the instrumentation's `composeBackend` argument.
`SharedStateConcurrentLifetimeTests` exercises two- and three-composition cycles,
native abandonment, sequential retry with original peer identity, acyclic
contention, original callback error propagation, same-thread nested borrowing,
and empty dependency graphs after completion. Nested crossed borrowing is
tested in both wait orders, including first publication of the outer owner
after entering the nested composition. Native weak-reference probes require
retired monitors to collect independently of their managed composition peers.
The controlled concurrency fixtures use one stable native
`composableLambdaInstance` root per composition, matching the compiler-style
restart envelope used by `SetContent`. Their native TimePicker control also
wraps each no-own-group remember factory in a fixed replaceable group. Raw
`Function2` roots or ungrouped native remembers can produce unrelated Link slot
corruption after abandonment and retry; they are not equivalent controls for
these public helpers. Matched native and managed controls retain the committed
peer and edited hour through nested failure, successful sequential retry/apply,
and a subsequent render, without replacing the composition or root lambda.

`SharedStateFirstOwnerTests` coordinates concurrent first claims of one fresh
wrapper and counts actual native factories across 64 bounded rounds. A separate
pending-claim control holds the winner before initialization while the loser
waits on its native monitor, then exercises successful binding and failures
before/after partial binding. It checks original exceptions, stale callbacks,
empty monitor graphs, and surviving-peer identity on sequential retry. The
public typed-helper control distinguishes a failure after initialization has
returned from a pre-publication failure, and verifies reacquisition after native
abandonment rather than assuming the native monitor also serializes callbacks.

Visual inspection of the hoisted-owner Gallery demo preserves 19:25 in its
label and both numeric displays while hiding and restoring either or both
consumers. A separate dial mismatch in the pinned native-call path remains: reintroducing the
clock while minute selection is active can point its hand at the hour angle
(19 maps to the 35-minute position), despite retaining minute 25 and native
selection `Minute`. `DialReentry_RetainsSelectedMinuteAndPeer` compares a raw
native remembered-state/bridge control with the generated owner/consumer,
checks unchanged peer, values, and native selection, and saves before/after
screenshots. Both paths reproduce the same mismatch. The pinned
`AnalogTimePickerState` constructor initializes its animation from `hourAngle`
regardless of selection; this is consistent with the observation, not a
shared-owner state reset. The test's pass result verifies state and selection,
not dial geometry. Both controls share the C# bridge/runtime invocation
boundary; no independent pure-Kotlin reproduction has been run, so a Google
upstream root cause is not established solely by this comparison.
No synthetic tab switch is applied to conceal the defect.

## Focus ownership

`Modifier.FocusTarget()` uses the official UI runtime binding. It is the
low-level target used by Jetchat's emoji panel, not a replacement for
`Focusable()` on accessible interactive controls. Install `FocusRequester`
and `OnFocusChanged` before the target, and remember one requester per logical
target. Request focus after attachment (an event or selector-keyed
`LaunchedEffect`), never on every render. Existing text fields already own a
target; do not append another.

`LocalFocusManager.Current(composer)` and `Current()` read the owner at the
current composition position and return the bound `UI.Focus.IFocusManager`.
Capture it in composition for later UI callbacks; implicit lookup outside
composition throws the standard active-composer error and is diagnosed by
CN5009. `Provides` supports a scoped override. No manager is globally cached.
`ClearFocus()` defaults to `force: false`; captured focus is retained unless
the caller passes `true`. Clearing input focus is distinct from moving
accessibility focus or issuing a keyboard show/hide command.

## The `$default` bitmask source generator

Every `@Composable` JVM method takes a trailing `int $default` bitmask
where bit N == 1 means "argument N was not provided; use Kotlin's
default." Hand-writing those bitmasks at every call site is illegible
(`_changed: 0b0111`); writing the `[Flags]` enum by hand is tedious
and bit-rots when the Kotlin signature changes.

[`Microsoft.AndroidX.Compose.SourceGenerators`](../src/Microsoft.AndroidX.Compose.SourceGenerators) hosts
three Roslyn incremental generators that together eliminate the
boilerplate:

- **`ComposeDefaultsGenerator`** — emits a `[Flags] enum` per
  composable, one bit per Kotlin parameter, driven by
  `[ComposeDefaults]` (described below). All declarations live in
  [`ComposeDefaults.cs`](../src/Microsoft.AndroidX.Compose/ComposeDefaults.cs).
- **`ComposeBridgeGenerator`** — emits the JNI body of each bridge
  partial method declared in
  [`ComposeBridges.cs`](../src/Microsoft.AndroidX.Compose/ComposeBridges.cs)
  from a single `[ComposeBridge(Class=…, JvmName=…, Signature=…)]`
  attribute.
- **`ComposeFacadeGenerator`** — emits the full
  `ComposableNode`-derived facade class (ctor + `Render` body) for
  bridges whose shape it recognizes, driven by `[ComposeFacade]`
  stacked on the bridge declaration. See
  [`.github/copilot-instructions.md`](../.github/copilot-instructions.md)
  for the ~10 facade-shape "phases" (containers, callbacks, named
  slots, painter resources, state holders with parameterised
  Remember, default-from-theme color slots, bridge branching, JCW
  confirm-state-change adapters, …).

The `$default` generator supports two forms.

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

[`ComposeDefaults.cs`](../src/Microsoft.AndroidX.Compose/ComposeDefaults.cs) holds
all of these declarations — every composable in the facade gets its
`$default` enum from this one file. Unit tests in
[`Microsoft.AndroidX.Compose.SourceGenerators.Tests`](../src/Microsoft.AndroidX.Compose.SourceGenerators.Tests)
pin the emitted output. When the upstream binder fix lands, each
declarative attribute can be swapped one-for-one to the generic form.

## Compose value types

The Kotlin `@JvmInline value class` types that surface as primitives
across JNI (`Color`, `Dp`, `Sp`, `TextOverflow`, `TransformOrigin`) are
mirrored as typed C# structs in
[`Microsoft.AndroidX.Compose`](../src/Microsoft.AndroidX.Compose). The bridge generator
keeps a tiny registry in
[`ComposeValueTypes.cs`](../src/Microsoft.AndroidX.Compose.SourceGenerators/ComposeValueTypes.cs)
that maps each value type to its JNI slot (`F` / `J` / `I`) and an
internal lowering expression, so a bridge can declare `Dp? width`,
`Sp? size`, or `TransformOrigin? origin` and the generator emits the
correct primitive lowering plus the `$default`-bit clear on non-null.
The packed representation stays out of the user-facing API. `Color` is
a 64-bit packed ULong with
an implicit conversion to `long`, so call sites pass `Color.FromRgb(…)`
or one of the named constants straight through to bridges and bound
binding methods that already take a packed color — no per-call
conversion required. Reference-typed wrappers (`FontWeight`,
`TextDecoration`, `Shape`) go through the generic
"reference-type → handle with null check" path the bridge generator
already supports.

## What's missing on the C# side (and why)

| Kotlin                                  | C# today                                                       | Cost |
| --------------------------------------- | -------------------------------------------------------------- | ---- |
| Skipping / recomposition optimization   | Per-param `$changed` bitmask computed at Render time via `composer.DiffSlot` / `composer.RememberAction` / `Modifier.StructuralKey`; bridge generator threads it into the JNI `$changed` slot. Modifier slot still emits Uncertain (Kotlin runtime falls back to its own input compare). | Composable methods mostly delivered ✅ |
| Slot-table-backed `remember`            | `Remember(() => …)` with `[CallerLineNumber]` keying into an activity-scoped cache; `Remember(factory, key1, …)` (1–3 keys or `RememberKeyed(factory, keys[])`) resets the slot on key change | Lifetime is per call site, not per nested-scope as in Kotlin |
| `@Composable` type-system enforcement   | None — calling a non-composable from a composable context fails at runtime, not compile-time | Footgun (the composable-method analyzer is a follow-up) |
| Per-call-site allocation                | Tree-style facade: every recomposition allocates fresh `ComposableNode` objects. `[Composable]` methods skip unchanged calls and lower executed generated-catalog calls directly to Compose bridges, so neither path allocates a facade node. | Resolved for generated facades; hand-written holdouts retain their custom allocation behavior |

## Composable methods — `[Composable]` C# methods

Composition ancestry must also remain deterministic across process recreation.
See [Composition keys and saved task state](saved-state-keys.md) for the runtime
type/position key contract, upgrade compatibility, and the saved-task regression
procedure.

Composable methods are the C# equivalent of Kotlin's compose-compiler plugin:
a Roslyn incremental source generator (`ComposableMethodGenerator`)
that emits a per-call-site `[InterceptsLocation]` wrapper for every
invocation of a `[Composable]`-marked static method. The wrapper opens
a Compose restart group, runs per-parameter `DiffSlot` diffing, and
skips the underlying call when nothing changed. The result is a render-loop shape Compose can skip the same way it
skips a Kotlin `@Composable`. A skipped method performs no
`ComposableNode` allocation. Generated catalog entry points also lower
executed calls directly to their bound API or `[ComposeBridge]`, reusing
the facade metadata for modifier materialization, callback and content
wrapping, state holders, `$default`/`$changed` masks, branch routing, and
painter ownership without constructing the matching tree facade.

Mirrors the design `dotnet/maui` uses for its
[`BindingSourceGen`](https://github.com/dotnet/maui/blob/main/src/Controls/src/BindingSourceGen):
one user method per composable, generator-emitted interceptors at every
call site, the C# compiler's interceptors preview feature does the
rewiring at the language level.

### Authoring shape

```csharp
public static class Screens
{
    [Composable]
    public static void Greeting(string name)
    {
        // Plain static method. No partial, no Impl companion, no
        // _changed parameter. Same shape as a Kotlin @Composable.
        Composables.Text($"Hello {name}");
    }
}
```

Extension composables are supported in both extension and explicit-static
syntax. An `IComposer` receiver is the composer slot and is excluded from
argument diffing; any other extension receiver is an ordinary first user
parameter and is diffed:

```csharp
[Composable]
public static void Greeting(this IComposer composer, string name) { }

composer.Greeting("Ada");
Screens.Greeting(composer, "Ada");
```

### What the generator emits

The generator emits a single
`Microsoft.AndroidX.Compose.Composable.Interceptors.g.cs` file
containing one wrapper per intercepted call site, plus a `file`-scoped
`InterceptsLocationAttribute` definition in
`System.Runtime.CompilerServices`. For each `Greeting("x")`
call site the generator emits roughly:

```csharp
[global::System.Runtime.CompilerServices.InterceptsLocationAttribute(1, @"...base64...")]
public static void Composable_0_AB12CD34(
    string name)
{
    ComposableCallSite.Start(ComposableContext.Current, callSiteKey,
        cachedCallSiteString ??= new Java.Lang.String(callSiteIdentity));
    Composable_0_AB12CD34_Core(ComposableContext.Current, name, 0);
    ComposableCallSite.End(ComposableContext.Current);
}

static void Composable_0_AB12CD34_Core(
    IComposer composer,
    string name,
    int changed)
{
    var __c = composer.StartRestartGroup(unchecked((int)0x9A1B2C3D));
    using var scope = ComposableContext.Enter(__c);
    int __dirty = changed;
    __dirty |= __c.DiffSlot<string>(name, 1);
    if ((__dirty & 0xB) != 0x2 || !__c.Skipping)
        global::App.Screens.Greeting(name);
    else
        __c.SkipToGroupEnd();
    __c.EndRestartGroup()?.UpdateScope(new global::AndroidX.Compose.ComposableLambda2(
        (__c2, force) => Composable_0_AB12CD34_Core(__c2, name, force | 1)));
}
```

The C# compiler's interceptors feature rewires each user call site at
the language level — the user writes `Greeting("x")` and the
compiler resolves the invocation to the generator-emitted wrapper.
The key — `0x9A1B2C3D` — is FNV-1a over the fully-qualified method
name so it stays stable across processes (matches the
`SourceLocationKey` contract). Each user parameter gets a
`DiffSlot<T>(value, offset)` call at the canonical bit offset
(`1 + paramIndex * 3`); the Kotlin-shape skip pair
(`mask = 0b001 | sum(0b101 << (1+3*i))`,
`expected = sum(0b001 << (1+3*i))`) is computed at generation time and
inlined as a literal. The `UpdateScope` lambda re-enters only the restart
core, not the entry's movable group: Compose has already restored the
restart anchor **inside** that group. Reopening the envelope from the
callback would change the anchored subtree.

### Structural call-site identity

The target signature alone is not a structural identity. In
`if (show) Counter("optional"); Counter("permanent");`, the two restart
groups previously shared a key; hiding the first call could transfer its
state to the second. Giving them different restart keys is also insufficient:
Compose Runtime 1.11.3's `startRestartGroup` uses `startReplaceGroup`, which
can replace the unexpected sibling rather than search for a surviving group.

Each intercepted **entry** therefore calls the editor-hidden compiler helper
`ComposableCallSite.Start`, opening a movable call-site envelope with a
remembered occurrence slot and an inner ordinal-keyed replaceable group.
That inner group contains exactly one ordinary target-keyed restart core.
The lexical identity is
the syntax-tree path, invocation source offset (including same-line
distinctions), and constructed target signature. The integer key is FNV-1a;
the non-null data key is a lazily cached JVM string of the full identity.
The string disambiguates integer-key collisions and its deterministic JVM
hash avoids the varying sibling ordinal in saveable compound keys. The
cache contains only immutable call-site metadata, never remembered state.

Within the current parent, the runtime matches surviving envelopes, orders
their nodes, inserts new groups, and forgets unused groups and effects.
This supports conditional calls, both branches, nesting, early exits, and
repeated calls without rewriting C# bodies. It does not move content between
parents or retain removed branches offscreen. Re-entering a removed branch
creates new ordinary state; its disposed effects are not resurrected.

Repeated execution of **one lexical call site** is positional among that
site's occurrences in its current parent. Loop occurrences remain independent
and match in FIFO order; changing a loop's count cannot consume a following,
distinct lexical site's state. This is not business-keyed list identity.
FIFO alone is insufficient for saveable state. Independent review found
that repeating the lexical data key collapses descendant compound keys:
if only B initially has a child, then A's child is inserted, the registry's
B,A provider-registration order disagrees with A,B restoration traversal.
The executed pre-ordinal regression restored B's `202` into A instead of
`101`. The original seven lockstep/trailing-loop cases did not detect this.

The occurrence slot now directly holds an `IRememberObserver` with a
zero-based ordinal. A pool scoped by the actual `IControlledComposition`,
the parent's native 64-bit composite hash, and the **full** lexical identity
assigns the lowest free ordinal only when a new envelope is inserted.
Retained envelopes keep it: recomposition, skipped bodies, and isolated
restart callbacks do not allocate or advance a counter. The inner ordinal
group gives repeated parents different saveable ancestry before any child
registers state. No save provider, random token, ambient execution frame,
or process-wide ordinal sequence is introduced.

Runtime FIFO matching retains the used prefix of occurrences for each
parent/site. New occurrences append; removed occurrences form the unused
suffix. `OnForgotten` releases them during apply, not while composing.
New slots abandoned before apply release through `OnAbandoned`; failure
while publishing the slot also releases the allocation. Thus a later
composition reconstructs the same ordinal prefix after shrink/grow/re-add.
Submitting another composition with pending changes is not an alternative
ordering protocol: callers must follow native `ControlledComposition`'s
apply/abandon contract. Moving content between parents is outside this
protocol, as before. Parent-hash collisions inherit native Compose's
compound-key limits; full lexical strings prevent the allocator from
introducing an additional integer site-hash collision.

The live pool strongly retains each stateful observer through a
`ConditionalWeakTable` keyed by the managed `IControlledComposition` peer.
Native slots keep their observer peers and composition reachable through
the Java/managed GC bridge; repeated bound composition projections must
preserve this managed key identity. Unexpected peer activation fails explicitly.
Normal callbacks remove empty pools and composition entries.

The table must be an ephemeron, not a strong dictionary or an unconditional
`GCHandle`: native `RememberEventDispatcher.dispatchRememberObservers`
forgets in reverse order, and an earlier child's throwing cleanup can skip
the enclosing occurrence's callback. Slot removal precedes dispatch;
clearing the dispatcher loses that callback, and later composition disposal
cannot recover an already-removed slot. A device regression reproduced the
resulting permanent root with the strong dictionary. Ephemeron ownership
allows the composition/observer cycle to collect when the composition is no
longer independently reachable, even if cleanup was never delivered.
It does not suppress the original exception or replay skipped user effects.
The cost is one managed/JVM observer slot and one inner group per entry,
with pool work on insertion/removal rather than every invocation.
The public helper is compiler plumbing, not a new application grouping API.

Extract a `[Composable]` method for a desired per-iteration boundary; an
ordinary helper or delegate invocation is not automatically a new boundary.
Likewise, conditionally executing raw `Remember`/effect APIs is not a C#
control-flow transformation: place branch-owned state inside a composable
call. Existing delegate-scope diagnostics and omission/default contracts
remain unchanged. Identity is stable across processes of the same build,
not promised across source edits or builds at different source paths.

This is an interception-specific protocol, not Kotlin compiler parity:
Kotlin 2.4.0's `ComposableFunctionBodyTransformer.handleLoop` and `visitWhen`
can insert enclosing/per-iteration/branch groups into function bodies.
The pinned runtime sources establish the alternate protocol:
`GapComposer.start` and `GapPending.getNext` reconcile groups/FIFO duplicate
keys; `end` removes unused groups and moves node ranges;
`endRestartGroup` anchors the callback at the restart group;
`updateCompositeKeyWhenWeEnterGroup` distinguishes null data-key positional
hashing from non-null data-key hashing.
Sources: [runtime 1.11.3 source archive](https://dl.google.com/dl/android/maven2/androidx/compose/runtime/runtime/1.11.3/runtime-1.11.3-sources.jar),
[saveable 1.11.3 source archive](https://dl.google.com/dl/android/maven2/androidx/compose/runtime/runtime-saveable/1.11.3/runtime-saveable-1.11.3-sources.jar),
[Kotlin 2.4.0 lowering](https://github.com/JetBrains/kotlin/blob/v2.4.0/plugins/compose/compiler-hosted/src/main/java/androidx/compose/compiler/plugins/kotlin/lower/ComposableFunctionBodyTransformer.kt).

`CompositionIdentityTests` exercises real slot-table retention, saveable
state, isolated leaf restarts, effect disposal, and applier node order on
Android. After installing an embedded-assemblies DeviceTests APK, run
`scripts\composition-identity-process.ps1 -Adb <adb.exe> -Serial <serial>`
under an exclusive device lease to verify Android saved-task restoration
after `am kill` in a different PID. Add `-Scenario selective-nested` to
insert the earlier child independently under two nested repeated parents
before saving. The output JSON is observation-only;
the activity never reads it to seed state. The script verifies the original
task and saved Bundle provenance, four distinct saved values (including
duplicate lexical loop sites), or five values in the nested-selective case,
and ordinary state resetting to zero. The root uses the bound Kotlin content
API without a managed ambient frame. Nested callbacks use `Composables.Column`
and the deterministic runtime ancestor keys from #353; a correct call-site
envelope cannot repair a randomized key higher in the tree.

Validation on Pixel 7: the audited pre-fix generator failed all seven
identity cases, including permanent saved value `123` becoming `202`
from a loop sibling. The ordinal correction also passes both selective
insertion orders, removal/re-add, nested repeated parents, leaf-only restart,
and activity recreation. Controlled-composition tests exercise delayed
forget, aborted insertion and retry, missing saveable registries, independent
compositions, and observer survival/collection across managed and Java GC.
Fresh-process loop and nested-selective probes retain distinct saved values
while ordinary state resets. The node-order fixture
uses fixed pixel constraints, avoiding an unrelated cached JNI class-reference
failure exposed by repeatedly calling the current Constraints getter bridges.

On integrated main `c49b14e`, the final consolidated suite passed 24 device
cases and 338 host tests. The injected slot-publication failure releases the
uninstalled observer as well as its pool. The throwing-cleanup regression
preserves the original exception and verifies that later disposal cannot
deliver the missing callback. It then observes resurrection-tracking weak
references to the composition, actual table key, site map, pool, occurrence,
and captured payload. In the recorded run, the first mixed-GC round cleared
the key and occurrence but left the map, pool, and payload alive; the second
cleared all roots and the table count. A key/token-only collection check
would have ended too early. Active Activity-owned peers survive both GCs
without the fixture retaining a managed composition, and the bound
composition projections preserve reference identity.
The Gallery's **Conditional child identity** demo also exercises the
contract interactively.

`HundredRowFootprint_RecordsCompositionCosts` compares the old envelope
and ordinal helper in the same Debug/Mono APK while actually rendering
100 changing `Text` rows. It records one initial composition and 15 updates,
including raw per-pass managed allocations and composition-body timings in
the TRX. The ordinal case retained 104 observer peers versus the baseline's
4 common surrounding peers; both returned to zero on teardown. Late updates
in both cases allocated 130,448 managed bytes. Initial samples were 387,512
versus 395,776 bytes; the last-ten-update median body times were 44.326 versus
35.559 ms (baseline versus ordinal). These sequential samples still show
warmup effects and are **not** evidence of a speedup or a calibrated
regression bound. They exclude complete frame/startup time and total
Java/JNI/native memory. The guaranteed additional structure is one observer
slot and one group per intercepted entry, not one saveable provider.

### Coexistence with the tree-style facade

Both styles can call into each other freely:

- A tree-style facade's `Render` (or the `SetContent` callback) can
  invoke a `[Composable]` method directly — each call site is
  intercepted normally and the wrapper sets up its own restart group
  inside the surrounding tree-style render.
- A `[Composable]` method can construct a tree-style `ComposableNode` and call
  `.Render(composer)` on it.
- `ComposeFacadeGenerator` emits a sibling method on `Composables` for
  every supported generated facade. It maps constructor values,
  modifiers, named slots, optional values, content, and state-holder
  callbacks back onto the existing facade, then renders it. The
  interceptor skip path runs before that adapter allocation.
- `ComponentActivity.SetContent(Action<IComposer>)` and
  `ComposeView.SetContent(Action<IComposer>)` host a `[Composable]` root
  directly. Jetchat, JetNews, and Reply use this shape for their
  top-level app composables, matching the corresponding upstream
  Kotlin boundary.

There is no migration pressure. Hot composables that recompose often
(animation, list items, drag handles) are the natural candidates for
composable methods; one-shot screens can stay tree-style indefinitely.

### Lambda adapter lowering

Generated rendering uses one shared execution-mode classifier instead of
inferring behavior from delegate arity alone:

- synchronous `IFunction2`/`IFunction3` content uses composer-owned
  `ComposableLambdas.Wrap2`/`Wrap3`;
- synchronous `IFunction4` content must opt in with `[ComposableContent]`
  and lowers to `Wrap4`;
- lazy item `IFunction4` content must use
  `[DeferredComposableContent]` and lowers to composerless `Instantiate4`;
- events use `RememberAction`, retaining JNI peer identity while rebinding
  the managed target each composition;
- non-composable DSL callbacks use `[RawCallback]` and raw
  `ComposableLambda0`/`ComposableLambda1` adapters.

Unmarked `IFunction1` and `IFunction4` parameters are rejected because their
execution timing cannot be determined safely from arity. Deferred and raw
shapes are available to direct bridge and hand-written-holdout lowering; the
tree-facade generator continues to reject them until those public delegate
surfaces are modeled.

### Hand-written holdout inventory

- **Completed:** `AnimatedContent<T>`, `Crossfade<T>`,
  `HorizontalPager<T>`, `VerticalPager<T>`,
  `HorizontalUncontainedCarousel<T>`,
  `HorizontalMultiBrowseCarousel<T>`,
  `HorizontalCenteredHeroCarousel<T>`, `LazyColumn<T>`, `LazyRow<T>`,
  `LazyVerticalGrid<T>`, `LazyHorizontalGrid<T>`,
  `LazyVerticalStaggeredGrid<T>`, and
  `LazyHorizontalStaggeredGrid<T>`. Generic interceptor lowering preserves
  type parameters and constraints; their composable adapters continue through
  the existing facades so the facade's existing lambda-identity behavior is
  retained. Lazy facades keep their deferred item bodies on
  `ComposableLambdas.Instantiate4`; pager, carousel, and animation facades keep
  synchronous content on `Wrap3`/`Wrap4`. `ComposableContentNode` only restores
  the ambient composer while rendering the managed node and does not replace
  either Kotlin lambda factory. Collection parameters are treated as unstable
  and force execution so in-place list edits cannot be hidden by reference
  equality. `MaterialTheme`, `Scaffold`, `SnackbarHost`, and both
  `SegmentedButton` modes are also complete. Their internal explicit-composer
  adapters remain the sole rendering implementation and delegate to the existing
  handwritten facades. `[GenerateImplicitComposable]` derives the ambient
  sibling, removing the trailing `IComposer` from each
  `[ComposableContent] Action<..., IComposer>` while preserving nullable
  slots and defaults. This keeps Scaffold's borrowed `PaddingValues`,
  SnackbarHost's `SnackbarData` forwarding, and SegmentedButton's row-index
  dispatch inside their established facade implementations. The composable
  `SegmentedButton` takes explicit `index`/`count`, matching Kotlin's
  `itemShape(index, count)` contract; the adapter publishes that position
  while retaining the enclosing row receiver scope. `Layout`, `TextField`,
  and `OutlinedTextField` are complete through the same adapter path.
  Text-field overloads cover string callbacks, `MutableState<string>`, and
  selection-aware `MutableState<TextFieldValue>` while leaving bridge
  selection and slot wrapping in the existing facades. `Layout` likewise
  retains its composer-remembered Java measure-policy peer. The complete
  state-based search family is also available: collapsed/top bars,
  docked/full-screen expanded content, and shared-state input fields.
  `BottomSheetScaffold` completes the issue-listed holdouts: its composable
  adapter remembers the existing facade keyed by `SheetStateHolder`, keeping
  the per-node veto JCW stable while replacing sheet/body/slot nodes on each
  executed composition.
- **NavHost / NavDestination:** need a stable, remembered raw graph-builder
  callback plus route registration and destination-argument forwarding; this
  is a navigation DSL rather than a normal composable content slot.

### Diagnostics

| ID     | Meaning                                                          |
|--------|------------------------------------------------------------------|
| CN5001 | `[Composable]` method must be `static`.                          |
| CN5002 | `[Composable]` method must return `void`.                        |
| CN5003 | If `[Composable]` declares `IComposer`, it must be the first and only composer parameter. |
| CN5004 | Method and containing types must be interceptor-accessible.     |
| CN5005 | `async` composables are unsupported.                            |
| CN5008 | `ref`, `out`, and `in` parameters are unsupported.              |
| CN5009 | A composerless API may execute outside `[Composable]` code or a `[ComposableContent]` callback; the diagnostic points to the unsafe delegate escape. |
| CN5010 | `[GenerateImplicitComposable]` was applied to an unsupported explicit-composer adapter shape. |

### Optional arguments and Kotlin default masks

The interceptor records omitted C#
  arguments as a surfaced-parameter bitmap, preserving explicit `null`,
  and `KotlinDefaultMaskPlan` maps that bitmap to route-specific generated
  default enums (including `Split()` for wide masks). Direct bridge helpers
  consume this contract before their route-neutral bridge call.
  `ComposeBridgeGenerator` emits an internal `<Bridge>ExplicitDefaults`
  sibling for bridges whose public partial declaration relies on nullable
  auto-masking. The declared bridge still computes defaults from runtime
  nullability for existing callers; the direct helper calls the sibling with
  its precomputed mask, so explicit `null` clears the Kotlin bit while an
  omitted argument leaves it set. Wide bridges take the generated
  `Split()` pair directly. No facade or adapter fallback participates in
  this path. When any C# argument is omitted, the direct helper clears the
  restart force bit before entering Kotlin while retaining remapped per-slot
  changed bits. Kotlin can then recompute composition-scoped defaults instead
  of assuming its own restart lambda captured their resolved values.
  If a generated catalog method is invoked from code that is not intercepted
  (for example, a synchronous content method reached through delegate-flow
  lowering), its method body derives a conservative omission bitmap from
  optional-parameter sentinel values instead of passing zero; nullable
  defaults therefore remain Kotlin-defaulted rather than being forwarded as
  explicit nulls.

### Real-app migration benchmark

Issue [#299](https://github.com/jonathanpeppers/Microsoft.AndroidX.Compose/issues/299)
migrated the Jetchat, JetNews, and Reply activity roots to composerless
`SetContent`, state, ViewModel, and lazy-list state APIs. The top-level
`[Composable]` methods no longer thread `IComposer`; each keeps one
parameterless tree-style `Render()` escape hatch while the nested screen shapes
remain blocked on [#301](https://github.com/jonathanpeppers/Microsoft.AndroidX.Compose/issues/301).

`ComposableMethodBenchmarkDemo` measures equivalent Reply-style cards through
three paths: direct tree construction, a composable-method interceptor whose body builds
the legacy tree adapter, and a generated catalog entry point lowered directly
to its Compose bridge. Thirteen fresh-process Pixel 10 / Android 16 Debug runs
randomized all three lane orders; each lane ran first at least three times and
every run forced ten recompositions. Cold figures use only runs where that lane
executed first, while recomposition figures use all runs:

- Adapter method cold initial composition (four adapter-first runs):
  **7,208 B**; median **9.03 ms**.
- Direct method cold initial composition (four direct-first runs):
  **16,512 B**; median **11.52 ms**.
- Tree cold initial composition (five tree-first runs):
  **4,200 B**; median **6.01 ms**.
- Adapter method recomposition skip: **920 B**; median **3.19 ms**.
- Direct method recomposition skip: **928 B**; median **3.90 ms**.
- Tree recomposition: **2,184 B**; median **12.90 ms**.
- Adapter and direct bodies each executed **2** times; the tree body executed
  **12** times.

The adapter lane deliberately constructs the generated tree facade behind a
composable-method restart-group skip. The direct lane calls generated catalog methods,
which the interceptor lowers to the direct helper; the tree lane constructs
the same facade without a skip wrapper. Direct lowering removes the facade from
the executing path; its measured skip boundary allocates 928 B versus the
adapter lane's 920 B. Timings are directional only because this is an
on-device Debug smoke benchmark rather than a warmed microbenchmark.

Tree and direct `$changed` remapping is all-or-nothing. A bridge with more than ten
Kotlin slots needs multiple changed-mask integers; while the generated bridge
surface exposes only the first, direct helpers pass `0` (Uncertain) instead of
forwarding a partial first group. Unmodelled receiver-bearing calls also
remain Uncertain. The bridge enforces this for hand-written callers too.
This keeps Compose on its runtime comparison path during forced recomposition.
`ChangedBits.Static` is Kotlin's `0b011`, not Unknown (`0b100`); previously
compiled consumers must rebuild to replace the inlined enum value.
See [the pinned ABI and skipping-safety evidence](compose-internals.md#pinned-changed-abi-and-static-compatibility).

### Deferred — follow-up issues

- **Composable entry points outside the issue-listed holdouts.** Navigation DSLs
  still need stable deferred/raw graph builders and destination-argument
  forwarding beyond ambient-overload generation.
- **`MovableContent` / `key {} ` / `Saver` / `Layout {}` / stability
  inference.** Explicit non-goals for composable methods — each gets its
  own follow-up issue.

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
  Roslyn source generator that lowers `[Composable]` C#
  methods to direct composer-threading calls is the next step.
- **Explicitness still matches Kotlin internally.** The composer remains
  an explicit parameter at the implementation layer (`Render(IComposer)`
  and generated interceptor cores). The composerless surface uses a
  `ThreadStatic` only as a synchronous dynamic-scope lookup for user-facing
  calls; every interceptor, `SetContent` boundary, and
  `ComposableContentNode.Render` pushes and restores it. Deferred callbacks
  invoked after those scopes close must not call implicit-composer APIs;
  they need an explicit composer-bearing boundary. `[Composable]` methods
  cannot be `async`, and parallel composition threads get independent
  ambient slots.

### What it looked like before the facade

For reference, the pre-facade Tier 1 sample was ~200 lines and required
**five** named ACW classes (`ThemedRoot`, `AppContent`, `ColumnContent`,
`ButtonLabel`, `ClickHandler`) just for one screen. The before/after is
preserved in git history at the commit that introduced
`Microsoft.AndroidX.Compose` — every `{ … }` block from the Kotlin version had
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
  [`ComposeBridges.cs`](../src/Microsoft.AndroidX.Compose/ComposeBridges.cs);
  `ComposeBridgeGenerator` emits the JNI plumbing. Tracked upstream in
  [dotnet/java-interop#1440] — when it lands every bridge declaration
  in this repo can be deleted in favour of a direct generated binding
  call.
- **`$changed` bitmasks** — generated facades now compute a per-param
  `$changed` mask at Render time and thread it through the bridge to
  the Kotlin runtime's first `$changed` JNI slot. Three mechanisms
  cooperate:
  - `composer.DiffSlot<T>(value, bitOffset)` — slot-table-backed
    structural diff, returns `Same`/`Different` shifted into place.
  - the internal `composer.RememberAction(action)` helper — caches one
    `MutableComposableLambda0/1` JCW per call site with a writable
    target, so onClick/onValueChange callbacks have a JNI-handle-stable
    peer; the corresponding param contributes `Static` to the mask.
  - `Modifier.StructuralKey` — every op factory in `Modifier.cs` /
    `ModifierExtensions.cs` records a `(string OpName, object? Args)`
    alongside the closure so two semantically equal chains hash equal.
    The modifier slot diffs the complete structural snapshot captured before
    building the modifier and consuming its side channels.

  Tracked content wrappers contribute Static only because body updates
  invalidate their readers even beneath skipped parents. Hand-written narrow
  SnackbarHost/SearchBar masks are supported; partial wide/receiver masks
  from TextField, BottomSheetScaffold, and SegmentedButton are suppressed at
  the bridge boundary.
- **`Modifier.Companion` not bound upstream.** Wrapped by the
  `Modifier` class via a one-time JNI fetch of the `$$INSTANCE` field
  — invisible to callers. See [NOTES.md](NOTES.md) open issue #1 for
  the upstream-friendly fix.
- **Theming reads landed (#61).** Use
  `composer.ColorScheme()`,
  `composer.Typography()`, and `composer.Shapes()` from
  inside `Render` to read the active theme; mirror of Kotlin's
  `MaterialTheme.colorScheme / typography / shapes` reads.
  `MaterialTheme` itself takes `ColorScheme`/`Typography`/`Shapes`/`Dark`/`UseDynamicColor`
  as settable properties — see `MaterialTheme.LightColorScheme()` /
  `DarkColorScheme()` / `DynamicLightColorScheme(...)` /
  `DynamicDarkColorScheme(...)` factories.
- **`CompositionLocal` / `CompositionLocalProvider` landed (#59).**
  `CompositionLocalProvider` is a `ComposableContainer` — list one or
  more `LocalFoo.Provides(value)` entries first, then the children that
  should see them:
  ```csharp
  new CompositionLocalProvider {
      LocalMyTheme.Provides(customTheme),
      LocalContext.Provides(scopedContext),
      new Text("inherits theme + context"),
  }
  ```
  Provided values must precede every child in the initializer (enforced
  at runtime). The built-in locals `LocalContext`, `LocalConfiguration`,
  `LocalResources`, `LocalLifecycleOwner`, `LocalView`, and
  `LocalColorScheme` are exposed as top-level classes.
- **WindowInsets padding modifiers landed (#69).** `Modifier.ImePadding()`,
  `NavigationBarsPadding()`, `StatusBarsPadding()`, `CaptionBarPadding()`,
  `DisplayCutoutPadding()`, `WaterfallPadding()`, `SystemGesturesPadding()`,
  `MandatorySystemGesturesPadding()`, `SafeContentPadding()`,
  `SafeGesturesPadding()` round out the existing
  `SafeDrawingPadding()` / `SystemBarsPadding()` pair. The complete
  `WindowInsets` facade adds live composition-aware inset reads, fixed
  `Dp` construction, `Add` / `Union` / `Exclude` / `Only` set operations,
  `AsPaddingValues`, generic `WindowInsetsPadding` / `ConsumeWindowInsets`,
  and inset-sized width/height modifiers. These call the official runtime
  bindings directly; `Modifier` can replay managed binding operations
  alongside generated raw-handle bridges without duplicating JNI surfaces.
  `Scaffold.ContentWindowInsets` and both adapters preserve null/omitted Kotlin
  defaults while forwarding supplied zero or transformed values. The pinned
  Material3Android 1.4.0.5 binding exposes `Scaffold-TvnljyQ` and
  `ScaffoldDefaults.GetContentWindowInsets`, so Scaffold uses these bound
  entry points rather than a duplicate JNI bridge. Its declarative
  `ScaffoldDefault` enum remains necessary because the binding misnames
  `FabPosition` and the trailing compiler arguments: the final managed
  `floatingActionButtonPosition`/`_changed` arguments are JVM `$changed`/`$default`.
  Kotlin's omission bits are fixed at a compiled call site. Changing its
  content-insets bit during recomposition changes the number of internal
  `composer.changed` slots and corrupts the following remembered lambda.
  Therefore Scaffold resolves the actual bound default getter unconditionally
  and supplies either that value or the caller's insets with the generated
  `ContentWindowInsets` bit cleared. Null still means Kotlin's live default;
  zero remains explicit. This stable native call shape preserves the Scaffold
  subtree across default/zero/excluded transitions rather than recreating it.
  Other defaults, padding forwarding and `Wrap2`/`Wrap3` slot identities are unchanged.
  The device regressions measure body bounds and forwarded padding, retain
  ordinary and saveable body state across transitions, and exercise activity
  recreation. In Debug, a scoped internal observer additionally captures the
  exact content lambda passed to the bound Scaffold call; this diagnostic
  hook is absent from Release builds and does not replace or wrap the lambda.
  `ScaffoldInsetsTests` covers 16 native cases across `Body`, `BodyContent`,
  explicit adapters, and implicit adapters, with and without app bars. Snapshot
  readiness requires the actual resumed/focused window, platform inset delivery
  observed on a test-owned parent, matching body/marker placement, and no pending
  native composition, snapshot, or layout work. `Instrumentation.WaitForIdleSync`
  runs off the UI thread; live values are then read together on the UI thread.
  Expected geometry is asserted afterward, never used as a readiness condition.
  A preserved window after `Activity.Recreate` can remain focused without sending
  the new activity a positive focus callback, so live `HasWindowFocus` is
  authoritative. Counter mutations require a new composed observation, and
  restoration must produce fresh ordinary state while recovering the saved value.
  Tests keep only their own window awake; they do not change device settings.
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
  For `MutableState<T>` and `MutableNumberState<T>`, the managed wrapper
  cache also uses keyed `Remember`: equal keys retain the wrapper and its
  current value without invoking the factory; changed keys run the current
  factory and return a replacement wrapper. The previous wrapper is not
  rebound to the replacement's state. Key arrays are shallow-snapshotted,
  so changing an element in the caller's array invalidates the cache;
  mutating an object used as an individual key is not a deep-value snapshot.
  For saveable inputs, keyless and empty-array calls both mean no inputs;
  a single null element is a distinct input vector. The array overload
  rejects a null array container.
  Use immutable primitive/string keys, null, or Java peers with appropriate
  equality. Other managed key objects still use the existing `ToString()`
  JNI marshalling, not arbitrary managed-object equality on the Kotlin side.

  On activity recreation, the factory constructs a fresh managed wrapper
  and the saveable holder rebinds it to the restored JVM state. Numeric
  wrappers must support the default saver's boxed mutable-state peer as
  well as their initially primitive-specialized peer. As in Kotlin,
  **inputs are not saved or compared against pre-recreation inputs**:
  a restored value can be used even when the new activity supplies different
  keys. Subsequent input changes reset it normally. Scalar saveable values
  bypass the managed-wrapper cache and restore without running their factory.
  `RememberSaveableTests` exercises key equality, nulls, key-array mutation,
  factory counts, and scalar controls against real Compose;
  `RememberSaveableRestoreTests` covers recreation and post-restore resets.
- **State primitives.** `MutableManagedState<T>` provides synchronized
  managed values that invalidate Compose readers without pretending to be a
  Kotlin flow. `MutableStateList<T>`, `MutableStateMap<K,V>`,
  `ComposeExtensions.DerivedStateOf<T>(Func<T>)`, and
  `composer.ProduceState<T>(initialValue, [keys…], producer)` are
  available. `ProduceState` is implemented purely in C# via an
  `IRememberObserver` JCW — the producer is a plain
  `Func<MutableState<T>, CancellationToken, Task>`, not a Kotlin
  suspend lambda. `composer.SnapshotFlow<T>(Func<T>)` bridges Compose's
  `snapshotFlow { producer() }` to `IAsyncEnumerable<T>`. The separate real
  Kotlin `Xamarin.KotlinX.Coroutines.Flow.IStateFlow`
  `CollectAsStateWithLifecycle<T>(composer)` and `IFlow`
  `CollectAsStateWithLifecycle<T>(initialValue, composer)` APIs wrap the
  lifecycle-aware collectors. Still missing: custom `Saver<T, S>`
  (only `autoSaver` is exposed today).
- **Effects.** `composer.LaunchedEffect`, `composer.DisposableEffect`,
  and `composer.SideEffect` are bound (#57 / #128). `LaunchedEffect`
  takes a plain `Func<CancellationToken, Task>` and a key list
  (rather than a Kotlin suspend lambda); cancellation happens on key
  change / leaving composition just like Kotlin.
  `composer.RememberCoroutineScope()` wraps the bound Kotlin
  `rememberCoroutineScope`; its `scope.Launch(ct => ...)` method starts a
  real child Kotlin `Job`, projects the managed body to a `Task`, and
  cancels the token when the call site leaves composition. This is the
  event-handler path for suspend APIs such as animated scrolling.
- **Suspend functions.** `SuspendBridge` (PR #97) lets a
  hand-written bridge return Kotlin's `COROUTINE_SUSPENDED` sentinel
  and complete a `Task<T>` from the eventual resume. Each continuation
  with a cancellable token owns a bound
  `kotlinx.coroutines.CompletableJob`, combines it with
  `AndroidUiDispatcher.Main`, and cancels that job when the token fires,
  so scroll and animation work stops at the next cancellable suspend
  point. Used by `ScrollState.ScrollToAsync`,
  `LazyListState.AnimateScrollToItemAsync`,
  `DrawerStateHolder.OpenAsync` / `CloseAsync` (#140), and the
  `SheetStateHolder` animation methods.
- **Compose Navigation.** `NavHost` / `NavController` /
  `NavBackStackEntry` are bound (#60). Pass route lambdas via
  `NavGraphBuilderLambda`; deep links are not yet exposed.
  `NavHostGraph` remembers the graph-builder identity separately from
  destination content (#352). Each registered deferred-composable wrapper
  reads its own `MutableManagedState<NavDestinationContent?>`; a successful
  parent render publishes fresh factories/static-child snapshots in
  `SideEffect`, invalidating visible destinations without subscribing the
  parent or replacing the graph/back stack. Inactive routes see the latest
  captures on re-entry. Old managed content is replaced, not accumulated;
  publication callbacks consume their pending payload after applying it,
  rather than retaining a render's host/tree through the native adapter.
  `DisposableEffect` clears registrations on host removal, including when an
  external controller still retains the graph. Deferred wrappers remain
  graph-owned and aren't disposed while Kotlin may still reference them.
  As before, the first render defines the registered routes and their order.
  Later renders refresh matching route strings regardless of list order;
  new routes are ignored and omitted routes retain their last published
  content (still needed by the registered graph). Repeated definitions of a
  route use the last supplied content, matching Kotlin's last registration.
  Changing the start destination retains Kotlin's existing graph-replacement
  semantics using the original topology (not a guarantee of back-stack
  preservation). Topology changes require removing the host from composition
  first; no new runtime rejection is imposed on previously accepted updates.
  Content may switch between factory and static
  children; same-position, same-type nodes retain their composition state.
  `NavContentTests` and `NavHostGraphTests` in the device-test project cover
  render-local replacements, callbacks, active/revisited routes, graph and
  entry identity, topology changes, and capture release. Run the navigation
  subset with instrumentation filter `FullyQualifiedName~Nav`.
  The disposal fixture uses a stable `Box` root and conditional typed child,
  and checks both enclosing-host and deferred-destination disposal before GC;
  arbitrary ungrouped C# root conditionals are not repaired by this change.
  Destination child groups (including a factory's root) use #353's shared
  `CompositionGroupKey`, and the deferred navigation wrapper uses
  `SourceLocationKey`. `scripts/test-nav-saveable-process.ps1 -Serial <serial>`
  independently checks updated static and factory content after real process
  death: state `0 -> 101`, render-local capture `Account 0 -> Account 1`, then
  save/background/kill/focus the same Android task without reinstalling.
  That isolated probe has no other tree-container ancestors; it is not a
  substitute for #353's broader tree-path restoration suite.
- **Drawing.** `Canvas`, managed `DrawScope` / `ContentDrawScope` /
  `CacheDrawScope` callbacks, `drawBehind` / `drawWithContent` /
  `drawWithCache`, mutable `Path`, gradient `Brush` factories, and shape
  factories are exposed (#64). Core drawing calls and path mutation use the
  current runtime bindings directly; only the binder-omitted
  `CacheDrawScope` instance methods use a small raw-JNI helper.

## Still missing (tracked)

- M3 Expressive newcomers (SplitButton, ButtonGroup,
  LoadingIndicator) — see
  [#54](https://github.com/jonathanpeppers/Microsoft.AndroidX.Compose/issues/54).
- M3 Expressive FAB variants (MediumFAB, FAB menu, ToggleFAB) — see
  [#103](https://github.com/jonathanpeppers/Microsoft.AndroidX.Compose/issues/103).
- Custom `Layout {}` primitive — Measurable / Placeable / MeasureScope —
  see [#144](https://github.com/jonathanpeppers/Microsoft.AndroidX.Compose/issues/144).
- Adaptive `TwoPane` / `NavigableListDetailPaneScaffold` + Jetpack
  `WindowManager` (`WindowLayoutInfo`/`FoldingFeature`) — see
  [#168](https://github.com/jonathanpeppers/Microsoft.AndroidX.Compose/issues/168).
