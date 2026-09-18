# Architecture: how the C# facade works

This doc covers the internals of [`Microsoft.AndroidX.Compose`](../src/Microsoft.AndroidX.Compose)
and its sibling source generators. For the *why* behind the project and a
tour of how Jetpack Compose itself works under the hood, see
[compose-internals.md](compose-internals.md).

## Flow overflow compatibility

`FlowRow.Overflow` and `FlowColumn.Overflow` accept managed `FlowRowOverflow`
and `FlowColumnOverflow` configurations. `Clip` is also the omitted Kotlin
default. `ExpandIndicator` supplies one indicator; `ExpandOrCollapseIndicator`
supplies both indicators and optional minimum row/column and height/width
thresholds. There is no separate collapse-only factory in Foundation.
Content accepts nodes, node factories, or `[ComposableContent]` callbacks.
Emit one layout root per indicator and update the owning flow's `maxLines`
from its click handler; overflow does not mutate application expansion state.
Both indicators can be composed for intrinsic measurement even when only one
is placed, so composition is not evidence of visibility.

The actual pinned **Foundation Layout 1.11.3** contract matters here. Its
overflow overloads are deprecated ("FlowLayout overflow is no longer
maintained"), but are still present and fully bound by the **1.11.3.1
runtime `.Android.dll`**. The facade intentionally retains this compatibility
surface; it does not substitute contextual or custom layouts. Factory and
layout calls use official bindings, including the mangled bound
`ExpandOrCollapseIndicator__jt2gSs` method. Only the omitted `Companion` field
requires a cached lookup through bound Java reflection; no new raw JNI
bridge or private-state reflection is used.

`FlowOverflowScope.TotalItemCount` and `ShownItemCount` forward the native
scope getters, excluding indicators. **Read counts in drawing/post-layout
callbacks, not composition.** In particular, shown count throws before
measurement; total count may not yet be initialized. Kotlin lazily caches
each scope's first count read, so scopes are invocation-local and must not be
saved across indicator invocations. No managed eager snapshot, zero fallback,
or promise of snapshot-observable live state is added.
Even a new managed callback invocation can receive the same cached native scope.
Changing regular content from eight items to five at an unchanged line limit can
therefore retain a native total of eight. Changing `maxLines` recreates the native
indicator scope and exposes the new total. This is an explicit pinned-native
compatibility limitation, not a freshness guarantee supplied by the C# facade.

The flow facades remain generated. An explicit managed-reference registry
allows nullable wrapper-passthrough options without classifying arbitrary
reference types as JNI peers. `[FacadeAdded]` preserves old catalog CLR arity
and direct targets while new overloads carry the overflow option. Omitted
overflow leaves Kotlin bit 6 set; explicitly supplied null in a direct call
clears that bit and is rejected, rather than silently becoming Clip, including
public calls that bypass interception. The overflow slots opt into
`[FacadeAdded(PreserveArgumentPresence = true)]`: a generated overload uses
`CallerArgumentExpression` to capture whether overflow was supplied. Its
compiler-only `__overflowArgument` parameter must not be set manually.
Existing CLR overloads and direct-helper arities remain available; the old
rich overload's required overflow argument is always considered supplied.
Unrelated facades retain their existing conservative nullable fallback.
Tree `Overflow = null` follows the existing tree optional-property convention
and omits the option. Generated enums describe the eight-slot overflow
overloads and four-slot indicator factories. Native changed masks remain
conservative because managed configuration is resolved during composition.
Tracked indicator lambdas enter their invocation composer and native Row or
Column receiver, then restore the outer receiver even on exceptions.

The `containers-flow-overflow` and `containers-flow-overflow-direct` Gallery
routes demonstrate both directions/styles. `FlowOverflowFacadeTests` pins
legacy signatures and omission/null masks. `FlowOverflowTests` includes direct
bound-native controls alongside managed exact-count, clipping, interaction
and nested-layout cases. Its test-only admission gate associates each actual
activity instance with a resumed, attached, laid-out and focused native window
and process. Clicks reacquire the package/window-owned accessibility root and
require one actionable target. Draw snapshots carry the fixture/process identity
and generation; count assertions are not substituted with host focus polling.
The native characterization and managed parity cases assert the observed
`8/2 -> 8/8 -> 8/2 -> 8/2 -> 5/5` scope sequence. At the fourth step they
separately verify that five regular items actually composed and retained their
remembered state; the cached total of eight is not described as fresh.
The earlier strict-freshness failure evidence remains valid for its original
source/APK and is not relabeled as a passing run.
`FlowOverflowOptionsTests` separately covers plain-node indicator overloads in
both directions and authoring styles. It varies the explicit line threshold
and the height/width threshold independently, then verifies native collapse
actions and retained node state. Companion-resolution host regressions cover
same-peer and different-peer success, null conversion, and throwing conversion:
temporary peers are disposed on every non-retained path.

### Verified native parity

On 2026-09-16, source tree `6f7da91d07a06781b5e13a7c8fdc09b9f57dc091`
passed both native characterization cases and all 16 managed cases on Pixel 7
using one immutable target-36, arm64 APK. This verifies the explicitly approved
native-compatible contract, not a repair of Kotlin's cached scope counts.

| Suite | Passed | Native PID | TRX run ID | Device UTC start / finish |
| --- | --- | --- | --- | --- |
| Pinned native characterization | 2 | 5039 | `21160f6d-862a-4646-82ef-e68d45ca6295` | 22:12:00.0599967 / 22:12:03.6251421 |
| Managed tree/composerless parity, expand-only and clip | 16 | 5230 | `3559e12a-84a8-45fb-a49e-f0959380c1e7` | 22:12:06.2446093 / 22:12:19.7608739 |

Both runs had zero failed, error or skipped cases. All four managed
direction/style combinations recorded requested item count five with cached
native scope `8/2`, then `5/5` after changing `maxLines`. They also verified the
five-item composition, retained item and indicator state, native accessibility
clicks, nested opposite-direction `4/1` counts, premature-read errors and
GC/indicator-only recomposition. The eight clip cases and four expand-only
cases passed without altering their original assertions.

The installed base APK was pulled and reverified before each invocation:
SHA-256 `28602343DB428CE405A79F6F3A5F8A686B1832CE8AB785343BC12DD159813F7D`.
Embedded app and runtime DLL payloads matched the build outputs exactly:
`E15CC3CCAE3B7E3EA8B7B80C858032A55163F919A93939405F03D65A38A49917`
and `C65D43EB5C3C63B15A472B62E38BB603FF4644F5342AC8208FACC8CEE36E1A50`.
No assembly overrides were present. Each test process was stopped and its PID
absence verified; all device commands ended at host time
`2026-09-16T22:12:22.2866745Z`.

The native and managed TRX SHA-256 values are respectively
`0EB3A9FEEB9C76A0FCDE1774F67CD02BB36339354791B737F61FA8DDD1923A14`
and `523EB861948BB513F3A21DB4719AF94594C71507D6A92E5D56F944D322AA6B1E`.
Raw TRXs, serial-specific command records, installed-payload proofs and scoped
logs are retained in the session's `flow-parity-native-171131` evidence folder.
The earlier failed device runs and rejected host-JVM observer prototype remain
historical evidence under their original source identities.

### Review follow-up verification

Source tree `ee5275c66e28a3bf6bcd916aba1f2f94d254f756` additionally verifies
the plain `ComposableNode` overloads and explicit collapse thresholds. The
original 18 cases were rerun alongside eight new node-route cases, without
changing the approved native-parity contract.

| Suite | Passed | Native PID | TRX run ID | Device UTC start / finish (2026-09-17) |
| --- | --- | --- | --- | --- |
| Native characterization | 2 | 16503 | `571ae00e-3950-4dc5-a43e-4742730b427e` | 02:04:29.9556219 / 02:04:33.2879147 |
| Original 16 managed cases plus 8 node/threshold cases | 24 | 16646 | `59759e3b-232f-4766-9b22-47d9c63c719f` | 02:04:36.3263983 / 02:04:56.6134726 |

All 26 cases passed with zero failures, errors or skips. Both directions and
authoring styles exercised plain-node expand-only and expand/collapse factories.
The latter independently suppressed collapse below four lines and below 200dp,
then displayed it above the two-line/96dp thresholds. Real native actions and
retained node state were verified. The same source passed 468 host tests,
including deterministic companion-peer cleanup for same-peer, different-peer,
null and throwing cast paths.

The installed/pulled APK SHA-256 was
`CDC1A4CF2B6FF51F12D692968A33AD212AF035221E35A5B9AFAC7C51740A1C92`;
the embedded app/runtime DLL hashes were
`D64A98CED5CA9CB3658BA9B91F94AA320298534CDAAE6DFD1CE72310761B8200`
and `84ECC6C59C1E4808B9AFFF0F8C0EB3F6FF74DF75CCD525C9AED2FCE737BE49EA`.
Both runs reverified those identities and absence of override files. Both
processes were stopped and PID absence verified. Host all-commands-ended time
was `2026-09-17T02:04:55.2593844Z`; TRX times use the device clock, which was
slightly ahead of the host clock.

The native/managed TRX hashes are
`7840CFF09A48B5FF24330E9D2473CBA79EE10D9DF412481635BB00FD45E8747F`
and `60696B03644261715E9A6455821446C8A75C4DA1F6EEE9609AF4D4D905035412`.
Raw evidence is retained in the session's `flow-review-native-210400` folder.

## Typed transition values

`composer.UpdateTransition<T>(targetState)` and
`Composables.UpdateTransition<T>(targetState)` remember one native transition
and update it on **every** composition pass. They must not be put inside a
`Remember` factory. Targets are non-null immutable values (including bools,
enums and records), boxed through the existing non-generic `ManagedBox` JCW
with managed `object.Equals` equality. Native snapshot state holds both targets;
no separate managed target field can get ahead of a speculative composition.

Each `transition.AnimateFloat(...)` / `AnimateColor(...)` call owns a stable
call-site group and returns a remembered `IState<float>` / `IState<Color>`.
The explicit overload takes an `IComposer` first; the composerless overload
uses the active context and is checked by CN5009. Target mappings are
synchronous `[ComposableContent]` functions: they may read snapshot state or
composition locals. The callback-time composer is entered and restored.
`ComposableLambda3` supports value-returning bodies as well as its existing
Unit-returning constructors. Unlike Unit content, `Wrap3Result` remembers and
rebinds that adapter inside a **replaceable**, not restartable, group. Snapshot
reads belong to the caller that consumes the returned value. A tracked
`ComposableLambdaImpl` would restart the mapping by itself and discard its new
return value, leaving the transition target stale. Native value functions invoke
these callbacks synchronously; their conservative zero changed masks ensure
replacement mappings are evaluated. The spec callback slot remains present even
when switching between omitted and supplied specs.
Rebinding a result callback also refreshes its captured animated visibility
scope, including null, so its returned values use the current lexical receiver.
Invocation restores both the callback-time composer and the prior animated scope.

The 1.11.3.1 **runtime companion DLLs** expose `TransitionKt.UpdateTransition`,
core `AnimateFloat`, animation `AnimateColor`, the lifecycle properties, and
`AnimationSpecKt.Spring` / `Tween`; these are bound calls, not new JNI bridges.
Generated `UpdateTransitionDefault` / `TransitionAnimationDefault` enums carry
defaults (animation extension receivers do not consume default bits).
All changed masks remain conservative zero. The existing stripped
`Color.box-impl` call now uses a generated bridge; the bound `Color.Value`
unboxes without new JNI. Color interpolation remains native and color-space
aware, never integer interpolation of the packed representation.

`AnimationSpecs.Spring(dampingRatio, stiffness)` and
`AnimationSpecs.Tween(durationMillis, delayMillis, easing)` create bound finite
specs usable for floats and colors. Remember supplied specs when constructing
them in composition; null uses each native animation's default spring.
The public signatures also accept bound `IFiniteAnimationSpec` values.
Kotlin generic type erasure still applies to externally constructed specs:
their visibility thresholds must match the animated value type.

`CurrentState`, `TargetState`, `IsRunning`, and `IsIdle` are snapshot-observable
native lifecycle reads. Idle means not running with equal current/target
states. For completion evidence, observe a committed running transition and
then idle for the same request while its animations remain in composition.
A same-target mapping update can schedule an animation before `IsRunning`
becomes true; removal calls native `onDisposed`/`onTransitionEnd` and is **not**
successful value completion. Neither recomposer idle, frame awaiters, placement,
quiet frames, nor eventual expected-value polling proves animation completion.

`TransitionValueTests` uses committed running/idle lifecycle signals before
asserting exact float/color endpoints, spec selection, wrapper/callback/native
peer identity, target interruption, same-target mapping changes (including
dependencies read only inside mappings), and
remove/re-add ownership. The Gallery route `transition-values` demonstrates the
same spring scale and two tweens used by the pinned Jetchat record button.
Gesture triggers remain separate from the visual derivations.

## Infinite float animation

`composer.RememberInfiniteTransition()` and
`Composables.RememberInfiniteTransition()` wrap Compose's bound native
`rememberInfiniteTransition`. Each `AnimateFloat` call registers one bound
`InfiniteTransition.animateFloat` state at a stable composition location.
Native Compose owns its frame loop, duration-scale policy, cancellation when the
call leaves composition, and fresh state when it re-enters; there is no managed
timer or frame cadence. Generated `RememberInfiniteTransitionDefault` and
`InfiniteFloatAnimationDefault` enums carry the bound composables' label defaults.

`AnimationSpecs.InfiniteRepeatable` wraps the bound `infiniteRepeatable` factory
and accepts a bound duration-based spec plus `RepeatMode.Restart` or
`RepeatMode.Reverse`. Jetchat's recording indicator uses a 2000 ms tween in
reverse mode, matching the pinned sample's 2000 ms in each direction. Its
elapsed recording timer remains separate snapshot state and is not derived from
animation progress. The Gallery route `infinite-float-animation` makes native
removal and re-entry observable.

### Combined recording-input acceptance

Source `530450602798776cde304ed02a0b7239e73a2992` passed **20 distinct native
cases** on Pixel 7 on 2026-09-15, using the same immutable DeviceTests APK
throughout. The seven transition cases were retained from their verified run;
the remaining nine scope and four input cases ran in subsequent, separately
authorized invocations. Every TRX was captured before the next invocation,
with zero failures, errors, or skipped cases.

| Suite | Passed | PID | Fresh TRX run ID | UTC start / finish |
| --- | --- | --- | --- | --- |
| Transition values | 7 | 18233 | `afbc0cc3-e3dd-4190-b945-22d397d4c700` | 17:45:59.311 / 17:46:16.640 |
| Animated scopes | 9 | 19528 | `ae3aae2a-168d-40f2-892e-e0e752362cc3` | 18:03:25.642 / 18:03:33.386 |
| Long-press / linked RecordButton | 4 | 19641 | `19c61be4-46de-4bb3-aaa5-9501a5109a6a` | 18:03:39.972 / 18:03:50.063 |

Installed APK SHA-256 matched
`E1B7D1FCB931C11E09ACC352CB8BABC0D93CF04EF2EDD9BA56E198B56D208497`,
with no private/external assembly override files. The embedded arm64 app and
runtime DLL payloads matched the build outputs; runtime SHA-256 was
`9C8AFC6386D9CCA31A693A583E45C7B71DFFB2824DB40F2A7E1BE422CBA41F18`.

The earlier combined source `8d98269` exposed an unhandled managed Row-scope
exception in the linked recording fixture: the tooltip anchor recomposed
independently, but its inner button tried to apply Row-only vertical alignment.
The fix moves that alignment to the enclosing Tooltip, the actual Row child,
without widening scope propagation or bypassing the guard. The original failing
fixture first passed in isolation (run `86a4785b-2b5b-40d3-b542-5a87bd500e88`);
that extra confirmation is not counted as a twenty-first distinct case.

The transition run's subsequent unbounded log read timed out after its fresh
passing TRX had been saved. Later bounded-tail captures returned no owned log
records; these limitations are explicit, and no complete phase-log archive is
claimed. Instrumentation PIDs, exact command vectors, hashes and fresh TRXs
remain available. The original 47-line managed AndroidRuntime exception and
the isolated regression's 77 owned log lines were preserved separately.
No successful suite was repeated solely to recover logs. The final test process
was stopped and all device commands ended at 13:03:55.152 CDT.

### Verified transition evidence

On 2026-09-15, source `ceb21412b8ce08011cb0da46f7a6d9ccc4236405`
passed all **7 `TransitionValueTests` cases** on Pixel 7: one finite-spec
contract case, plus explicit/composerless pairs for synchronized
values/specs/identity, mapping-local snapshot dependencies, and
interruption/removal/re-addition. The fresh TRX ran from
15:33:30.087 to 15:33:47.321 UTC with zero failures or skips. Current-process
`TransitionValues` logs (PID 10005) preserve the committed running/idle
transitions and exact final float/color values. Completion assertions used
the native lifecycle, not elapsed delays or idle-frame heuristics.

| Frozen APK | SHA-256 |
| --- | --- |
| DeviceTests | `3BFE1F025E71B52CFFBA8B5CD10520CE5904BFE0E2C3F7F3BA767C2660B3BF3B` |
| Gallery | `739A5C93A6257DE7884D949E1F3AECEB1BD7A7E1EA9A08191A4AD6B906DE23D9` |
| Jetchat | `B13013D2CC0E26B7443A43D307D16AF73FF898863D0F3BD4C06307D302E7D185` |

Installed APK hashes matched and no private/external assembly override files
were present. Embedded arm64 app/runtime DLL ELF payloads matched their build
outputs; the common runtime DLL SHA-256 was
`7EC4AB45F60575017C61E37788158FF63C66B73435ED5F22904C8CA53CDE50B0`.
Gallery's `demo/transition-values` route and Jetchat cold-launched into resumed
activities without current-PID AndroidRuntime fatal errors. This is a native
API acceptance run and bounded startup smoke, **not** screenshot/visual parity
or post-integration evidence for #333's animated scopes or #337's gestures.
All owned app processes were stopped and the device lease released.

### Post-merge transition and scope acceptance

After merging main's animated scopes and BasicTextField changes, source
`c7467bb4ac915bff76cbe8a0d5b5489feacce2b7` passed **16/16 native cases**
on Pixel 7 on 2026-09-15. Two separate instrumentation invocations ran
`TransitionValueTests` (7/7) and then `AnimatedScope` (9/9), with zero failures
or skips. The latter includes the new result-callback regression for refreshed
first/second/null lexical scopes, preserved return values, and restoration on
exceptions.

| Suite | PID | Fresh TRX run ID | UTC start / finish |
| --- | --- | --- | --- |
| Transition values | 14787 | `be93502f-75ee-4b85-ba71-58edd31e525d` | 16:31:47.567 / 16:32:04.789 |
| Animated scopes | 14911 | `067e684c-6a64-4609-9258-0057387323ac` | 16:32:43.603 / 16:32:51.325 |

Each TRX was captured before the next invocation could replace it, and the
instrumentation output preserves each PID. The attempted per-PID logcat captures
were empty and are not log evidence for this run. Installed DeviceTests APK
SHA-256 matched
`B45D9050121D996A9B1027117B1E6B90F957050A4E741FD9A839EDFB94BEF534`;
private/external assembly override files were absent. The frozen arm64 ELF
payloads matched the build outputs, including runtime DLL SHA-256
`9A22F2A0C25F8A5785D1ED4DABC6A3736F23376E08161830626290036ACC0CD2`.
The post-merge host suite passed 442 tests and all required builds passed.
This lease was tests-only: Gallery/Jetchat smoke evidence above remains
pre-merge. The test process was stopped and all device commands ended at
11:33:21.346 CDT, before the lease deadline.

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
Removal can cancel a gesture via a synthetic consumed-up event and then cancel
the idle coroutine during detach. Tests account for those raw idle notifications
separately from the single active cancellation required per recognized gesture.
They also retain phase-specific raw counts and Java call stacks: the first
removal callback must originate in `onCancelPointerInput`, and the subsequent
idle callback in `onDetach` / `resetPointerInputHandler`. This verifies the native
cause and ordering rather than simply labeling every duplicate as idle.
Temporary JNI result references are released in `finally`, and the coroutine
suspended singleton uses the existing raw-handle sentinel check.

`LongPressDragTests` injects real native MotionEvents and waits for detector
callbacks, with bounded frame barriers for recomposition. It checks short taps,
long press, two-axis deltas, latest callbacks, native handler/callback identities
across recomposition and GC, key reset, modifier removal, MotionEvent cancellation
and activity disposal. No synthetic animation-clock helper is involved.
Gallery routes `modifiers-long-press-drag` and `buttons-tooltips` expose the
lifecycle and competing-tooltip-input control.

## Animated child transitions

`Modifier.AnimateEnterExit(enter: ..., exit: ..., label: ...)` adds child
transitions inside `AnimatedVisibility` or `AnimatedContent<T>`. It can start
a chain or extend one. Null and omission both mean Kotlin's defaults:
`fadeIn()`, `fadeOut()`, and `"animateEnterExit"`; an explicit transition
(including the bound `None` singleton) or empty label remains explicit.
These effects combine with the parent's transitions rather than replacing
them. `Composables.AnimatedVisibility` supports both an explicit composer and
a generated composerless overload; existing tree constructors and
`AnimatedContent` signatures are unchanged.

```csharp
// Inside a [Composable] method:
Composables.AnimatedVisibility(visible, () =>
    Composables.Column(() =>
        Composables.Text("Independent child",
            modifier: Modifier.AnimateEnterExit(
                enter: Transitions.ScaleIn(0.2f),
                exit: Transitions.ScaleOut(0.2f)))));
```

Tree-style children use the same modifier:
`new AnimatedVisibility(visible) { new Text("Child") { Modifier = Modifier.AnimateEnterExit() } }`.
Chains can be constructed before composition and reused; the receiver is resolved
when the chain is materialized, not when its managed description is built.
Materializing outside compatible animated content throws a managed
`InvalidOperationException`. Applying the modifier to a top-level animated
container itself does not make that container its own child.

The native callbacks publish their actual `IAnimatedVisibilityScope` in a
separate `RenderContext` channel. `IAnimatedContentScope` inherits that
interface, so outgoing and incoming values each publish their own native
receiver. Row/Column/Box scopes do not hide this channel. Nested animated
callbacks replace it only for their children, with disposable frames restoring
the previous receiver even on failure.

Every composable arity-2/3/4 adapter captures the **typed peer**, not a borrowed
JNI handle, when created. Its invocation reinstalls that lexical animated
receiver (including null) and the **callback-time composer**, never the outer
composer. This covers independently invoked nested content, tracked named
slots, and generated `UpdateScope` restart callbacks. It does not grant event
handlers composable scope: raw non-composable arity-0/1 adapters do not capture
it. The receiver lives with the composition-owned callback; no global cache or
self-rooting GC handle is added. Modifier diff keys are deliberately opaque
because the effective receiver is dynamic, so equal supplied arguments cannot
incorrectly suppress a receiver change.

The runtime binding `Xamarin.AndroidX.Compose.Animation.Android` **1.11.3.1**
exposes `IAnimatedVisibilityScope.AnimateEnterExit` and `Transition`, but not
the synthetic `animateEnterExit$default` method (nor does its bound
`AnimatedVisibilityScopeDefaultImpls` expose that default dispatcher).
All-explicit modifier calls use the bound instance method. Any omitted value
uses a generated bridge to the synthetic entry. Its **two** receivers
(dispatch scope and extension modifier) precede the user arguments and consume
no default bits: enter = 1, exit = 2, label = 4. The new
`ComposeBridge.ReceiverCount` option models this without a handwritten JNI body
or hand-rolled enum. Typed receivers participate in generated `GC.KeepAlive`.
The missing synthetic entry is recorded in this repo's #333. This is distinct
from Kotlin inline-class name mangling tracked by dotnet/java-interop#1440.

Pinned evidence: Google Maven's
[`animation-1.11.3-sources.jar`](https://dl.google.com/dl/android/maven2/androidx/compose/animation/animation/1.11.3/animation-1.11.3-sources.jar)
(SHA-256 `A75C099FF3E786FD3E0A55A016B32AC5E6CB11638DF5E3BCC3A34FF6C2E2EDB6`),
`AnimatedVisibility.kt`, `AnimatedVisibilityScope.animateEnterExit` and
`AnimatedVisibilityImpl`/`AnimatedEnterExitImpl`; the actual runtime
`classes.jar` SHA-256 is
`9BE622AA1D57383B482F00FD3BFEACC0411FFF77476AAFEEBD76C3C3CB9F6A94`.
`javap -p -c -s` confirms the two-receiver descriptor and bits above.
Upstream waits for the associated transition's exit before disposing content,
including child `animateEnterExit` animations; unrelated independent animations
are not covered by that guarantee.

Gallery route `anim-children` shows a quick parent fade and slower child scale,
default child fades in composerless content, and child transitions per
`AnimatedContent` state. `AnimatedScopeTests` checks Kotlin-dispatched defaults,
explicit arguments, scope rejection, and later callback/exception restoration.
`AnimatedScopeRenderingTests` checks actual child placement, independent
recomposition, outgoing/incoming and recursive scope identity, exit disposal
and re-entry. Its completion evidence is native transition lifecycle and
`DisposableEffect` disposal, **not** sleeps, expected-pixel polling, recomposer
pending flags, raw awaiter counts, or quiet frames. It is not a frame-by-frame
visual parity claim against
[`android/compose-samples` at `4c1fe758`](https://github.com/android/compose-samples/tree/4c1fe7586e2fbf1c934925ef8ab64d3803361423).

### Native acceptance evidence

On 2026-09-15, Pixel 7 passed all **8** `AnimatedScope` cases, with no
failures or skips, using source `e70f2c1` and embedded DeviceTests APK SHA-256
`DBFBEEACCD7C0A8DC89ED1C660E0CAA9B8889FD3D537DD20129AADD55B1AE331`.
The ARM64 app/runtime ELF payload sections matched the built managed DLLs
byte-for-byte. Both tree and direct routes reported native exit duration
**800,000,000 ns**, versus the parent's 100 ms effect, while the child was
still installed; each measured six children across the state change.
Independent recomposition, nested/outgoing scope identity, disposal/re-entry,
callback exception restoration, and the full default/explicit argument matrix
passed.

An earlier run passed 7/8 because the default comparison used managed wrapper
identity. The correction calls the bound `Java.Lang.Object.Equals(Java.Lang.Object)`
overload, which dispatches to Kotlin's transition-data equality, with positive
and negative controls. No runtime/default behavior or timing assertions changed.

Gallery APK SHA-256
`09C1EB6701FD07AA1319A1D516E0D9C463001DBEBD13E76C9AA483649BFD740C`
rendered `demo/anim-children` with readable labels. Owned screenshots captured
shown/hidden content; native UI hierarchies confirmed removal and re-entry
of both tree and composerless children. A transient adb transport failure
interrupted further Gallery interaction, so its separate next-state button
was not exercised. AnimatedContent state changes were covered by the native
suite instead. Evidence was recovered without restarting the shared adb server,
both repo-owned apps were stopped, and all device commands ended at
10:57:44.612 CDT, inside the granted lease.

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

The reflection declarations need `-keep class ... { <named fields>; }`, not
just `-keepclassmembers`. In R8 full mode, member-only rules do not make an
otherwise uninstantiated class count as instantiated. A class literal can
survive as an empty shell while its instance fields disappear. The helper
eagerly calls `getDeclaredField` for **both** backends at initialization, even
when only Gap is instantiated. Consequently, the class and the named fields
must remain on their original declaring class; an inherited field is not an
equivalent contract. These rules do not keep every method or field of Compose.
See the [R8 full-mode compatibility contract](https://r8.googlesource.com/r8/+/refs/heads/main/compatibility-faq.md#r8-full-mode).

On current-main `56076632`, a fresh .NET 11 RC1 NativeAOT Jetchat Release build
reproduced missing `LinkComposer.changeListWriter`, Link
`ComposerChangeListWriter.changeList`, and Link `ChangeList.operations`.
The consumer rules reached the R8 task; R8 9.1.31 ran with shrinking enabled
and `-dontobfuscate`. Mapping and DEX retained the declaring classes as empty
shells, not renamed fields or relocated superclass declarations. Explicit
class-plus-field retention restores all eleven eager reflection declarations.
`dotnet run scripts/check-shared-state-dex.cs -- <apk> [<apk> ...]` checks the final APK's
DEX class-data declarations (not mere field references) against the Java
helper's actual reflection calls. CI runs it on the NativeAOT template APK.
It also requires the JNI-only Java entry points retained by `shared-state-lifetime.pro`:
`PointerInputEventHandlerImpl`'s `Function2` constructor and public suspend
`invoke(PointerInputScope, Continuation)`, plus `MeasurePolicyFactory.create(Function3)`.
These helpers are compiled with `Bind=false`; managed `FindClass`/method lookup
does not make them reachable to R8. Exact class-plus-member rules preserve their
JNI names and signatures without keeping unrelated members. The checker requires
concrete method declarations with the correct direct/virtual and static/instance
shape, not just DEX method references or an input JAR containing the classes.
This is a new fix/validation population, not a reclassification of the frozen
`7330d039` NativeAOT admission failure or startup measurements in #346.

Use a clean Release build for R8 validation on the installed .NET for Android
Windows SDK 36.1.69.
Its incremental AAR-import path can omit consumer rules from
`libraryprojectimports.cache` when an archive's hash is unchanged, even though
the extracted `proguard.txt` remains present. This removed both
`SharedStateLifetime` and the JNI-only `TimePickerKt` factories in a reproduced
incremental build. A clean build restores collection of the existing narrow
rules; verify the actual R8 `--pg-conf` inputs and final DEX, not just the AAR.
The library exports its rules, but this work does not fix the SDK's incremental
cache behavior. That historical clean-build coverage did not establish incremental,
AOT, obfuscated, or full Release UI compatibility.

The `DeviceTests` project always compiles the complete test source set and
resources, including both the normal MSTest instrumentation and the bounded
direct-call shared-state runner. Use the standard SDK properties:
`UseMonoRuntime=false` for CoreCLR, or `PublishAot=true` for NativeAOT.
Project references set `GlobalPropertiesToRemove="PublishAot"` so app-level
native publishing does not reach the runtime library or netstandard source
generator. Removing the property, rather than setting it to `false`, preserves
the same MSBuild project identity as other solution references where it is
unset; separate instances would race while writing the same intermediate files.
The test project enables the standard `RestoreUseStaticGraphEvaluation`
setting so restore honors this per-reference removal too; the default
restore traversal otherwise forwards the app's global `PublishAot=true` and
fails on the netstandard generator. No generator-local property override is
needed. Use separate, clean artifact directories and application IDs when
comparing the runtimes:

```powershell
dotnet publish src\Microsoft.AndroidX.Compose.DeviceTests -c Release -r android-arm64 `
    -p:UseMonoRuntime=false -p:PublishAot=false -p:AndroidLinkTool=r8 `
    -p:ApplicationId=net.compose.devicetests.coreclr -p:ArtifactsPath=artifacts\state-coreclr
dotnet publish src\Microsoft.AndroidX.Compose.DeviceTests -c Release -r android-arm64 `
    -p:PublishAot=true `
    -p:ApplicationId=net.compose.devicetests.nativeaot -p:ArtifactsPath=artifacts\state-nativeaot
```

After installation, run
`adb -s <serial> shell am instrument -w -e composeBackend gap <package>/net.compose.devicetests.SharedStateTestInstrumentation`,
then repeat with `composeBackend link`. Each invocation reports four tests:
eager helper initialization, abandoned owner replacement, commit-time veto
publication, and sibling sharing/removal/reentry/disposal. Require `passed=4`,
`failed=0`, and instrumentation result code `-1` for each backend/runtime.
Add `-e aotCompatibility true` to the bounded invocation to also run the
failed-occurrence-publication regression and three host-probe JSON contract
tests (`passed=8`, `failed=0`, result code `-1`). This does not run the entire
MSTest suite. The publication fixture uses a statically implemented composer
double rather than `DispatchProxy`; process snapshots use source-generated
JSON metadata while preserving the host scripts' property names and values.
Both runtime configurations reference `MSTest` 4.4.1 with its build targets
enabled. Explicit SDK imports let the project restore Android's `Library`
output after the transitive desktop test SDK resets it to `Exe`.
Compiling the full suite is not evidence that every test has run under
NativeAOT. The original device evidence below used 4.3.2 and a bounded compile
selection, before this full-source configuration.
The smoke runner's `shared-state-smoke.pro` keeps its JNI-selected
`ComposeRuntimeFlags.isLinkBufferComposerEnabled` switch and the transaction
tests' raw-JNI `DrawerState.getConfirmStateChange$material3` inspection getter.
CoreCLR/R8 otherwise removes these members. These test-only rules are not
exported by the runtime library or used by the Jetchat APK.
Use `dotnet run scripts/check-shared-state-dex.cs -- --smoke-harness <test-apk>` to also check
the switch's declaring class, boolean type, and public/static flags, and the
getter's exact instance-method declaration/signature.

Validation on 2026-09-17 used a Pixel 7, Android 17/API 37, SDK
`11.0.100-rc.1.26425.128`, Android workload `37.0.0-rc.1.2257`, and the exact
NativeAOT runtime pack `11.0.0-rc.1.26428.117` from the public dotnet11 feed.
All candidates were arm64 Release, with actual APK compile/target SDK 37 and
R8 shrinking enabled; no runtime substitution or trimming/R8 disablement.

- Jetchat APK SHA-256
  `0fac20c990d5126be67c249ba7923d21ddd02a8bc1bc19524f32a40ecf199e82`,
  native `libJetchat.so` BuildID `2bc0ff8322131b4c603b21951772dbfc187e7427`:
  installed APK hash matched; native loading, two normal starts in distinct
  processes, resumed activity, composed content, and drawer open/scrim-close
  passed. These are correctness checks, **not startup measurements**.
- The same `DeviceTests` smoke selection passed **4/4 in each of CoreCLR Gap,
  CoreCLR Link, NativeAOT Gap, and NativeAOT Link** (16 total, zero failures).
  Each invocation returned `passed=4`, `failed=0`, result code `-1`.
  Final APK hashes were NativeAOT
  `d4a7a3415847478cee14424b5c66f9befbd95b118b3dd330539eb9fce6f3a130`
  and CoreCLR `abac7f460f26d4faa018f6229eeb19aa53b77388d24d111256ca40fffc01b817`.
  Installed hashes matched; both final DEX filesets passed the complete smoke
  contract check. The final test run ended at `2026-09-17T14:36:21.754208Z`,
  with both test packages stopped and their PIDs absent.

The three isolated validation packages were subsequently hash-verified and
normally uninstalled under a separate cleanup grant, ending at
`2026-09-17T14:39:40.701139Z`; package paths and retained-data registrations
were absent.

Earlier diagnostic runs remain failures, not retroactive passes: the first
UI driver used system Back (which left the sample) instead of its native
close-menu scrim; the first sibling fixture changed the owner's lexical
remember location; CoreCLR then exposed missing test-only backend-selector
and veto-getter retention. The corrected fixture keeps one owner call site
and toggles only the borrower, without weakening identity, retirement, or
reentry assertions. The final test-only rules do not contribute to the
independent Jetchat startup result. This does not establish full-suite NativeAOT,
incremental-build, obfuscated-build, or performance coverage; #346 stays open.

The subsequent MSTest 4.4.1 full-source configuration compiled all 154 project
C# files (199 inputs including linked sample sources) identically for CoreCLR
and NativeAOT. Default Debug build and both clean Release publishes passed.
With `aotCompatibility=true`, the bounded runner passed **8/8 for each runtime
and backend** (32 total), including the static composer double and exact JSON
contracts. This is focused execution of a fully compiled suite, not execution
of every test. Final R8 APK hashes were NativeAOT
`a2b1eaceb231ac64158cab27d0924dc7ceb5dcddd55b19b85f01dfe18fb28ee6`
and CoreCLR `38a7b38793eeca15882c1c67d26eebccf19bc4f8da4aef9c3f99ecab8f1a17f6`;
installed hashes and final DEX contracts matched. The separate validation and
normal uninstall of both new isolated packages ended at
`2026-09-17T15:27:33.164250Z`, with no retained package data or owned processes.

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

## Foundation editor decoration

`BasicTextField` uses the **bound** Foundation Android companion overloads
for `TextFieldValue` and `string`; no JNI bridge or Material field substitution
is involved. The string route leaves selection/composition state with Foundation,
not a managed adapter reconstructing `TextFieldValue` on every render.
The facade generator supports `[Callback(typeof(T))]` for bound
`Java.Lang.Object` types. It resolves the real callback peer without taking
ownership, preserving text selection/composition rather than converting the
value to a string. Nullable callbacks become optional properties (for example,
`OnTextLayout`) and still use identity-stable `RememberAction` adapters.

`[DecorationBox] IFunction3?` receives the native `Function2<Composer, Int, Unit>`
inner editor, unlike an ordinary slot or scope receiver. Tree callers supply
`Func<ComposableNode, ComposableNode>`; explicit-composer callers receive
`Action<IComposer>` plus the decoration composer; ambient callers receive
`Action`. Render that inner editor exactly once and do not retain it outside
the decoration. All routes use tracked `Wrap3WithValue` identity and the
composer active at inner-editor invocation, including nested containers and
restarts. Null tree properties select Kotlin defaults; direct calls retain
the generated omission-aware mask contract. The two native changed groups
remain zero (Uncertain).

`SecondaryCtor` can replace a required callback's managed payload type when the
alternate bridge annotates that shared parameter with `[Callback(typeof(T))]`.
The generator maintains separate nullable, strongly typed callback fields, emits
route-specific constructor/direct signatures, and guards access to the selected
route's fields. An optional property's callback type cannot change across routes
(CN3012), because both constructors share that public property.

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
while ordinary state resets. The original node-order acceptance fixture
used fixed pixels after a CheckJNI abort in a Constraints measurement callback;
that avoidance was not a runtime fix. The fixture now uses the real getters
and clamps again, with GC before structural updates. See
[Constraints JNI lifetime](constraints-jni-lifetime.md) for the separate
ownership investigation and source-specific measurement regression.

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
  cache also uses keyed `Remember` over the exact values marshalled to
  Kotlin: equal keys retain the wrapper and its current value without invoking
  the factory; changed keys run the current factory and return a replacement
  wrapper. The previous wrapper is not rebound to the replacement's state.
  Key vectors are shallow-snapshotted, so changing an element in the caller's
  array invalidates the cache according to that element's key semantics.
  For saveable inputs, keyless and empty-array calls both mean no inputs;
  a single null element is a distinct input vector. The array overload
  rejects a null array container.
  Null, immutable primitive/string keys, and Java peers retain their natural
  Kotlin equality. Managed comparison tokens mirror the existing Java boxing:
  `byte`/`short`, `ushort`/`int`, and `uint`/`long`/`ulong` values compare in
  their shared Java numeric class; floating-point signed zeroes are distinct
  and NaN payloads compare equal. Caller-owned Java peers use their virtual
  Java `equals` and are never disposed by key lowering. Other managed key
  objects preserve the legacy `ToString()` JNI marshalling, and the resulting
  string snapshot now also controls the managed wrapper cache. `ToString()` is
  evaluated once per key per `RememberSaveable` invocation, so mutating a
  custom key's string between compositions invalidates against the previous
  frozen snapshot. Their managed `Equals` implementation is not used:
  distinct objects with the same string are equal saveable inputs, while
  `Equals`-equal objects with different strings invalidate. This deliberately
  does not invent deep-array equality. An array supplied as one key falls back
  to its type-name string; an array supplied to `RememberSaveableKeyed` is the
  key vector, whose elements are independently snapshotted and compared.
  This is an intentional behavior correction: compared with versions whose
  managed cache used `object.Equals`, same-string custom replacements now keep
  the managed wrapper and do not run its factory, while different-string
  replacements invalidate even when their managed objects compare equal.

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
  `MutableState<AndroidX.Compose.UI.Text.Input.TextFieldValue>` additionally
  uses Kotlin's `TextFieldValue.Saver` at the inner-value boundary of the
  mutable-state overload. This saves annotated text and selection, but not
  the IME-owned composition range, focus or keyboard visibility. Ordinary
  edits and recompositions do not invoke restore or clear composition.
  The UI Text Android 1.11.3.1 binding exposes the value and its copy/getters,
  but omits the Saver-typed companion getter; `TextFieldValueSaver` uses the
  existing companion generator to access it. No managed serialization format
  or general custom-saver API is introduced. Other wrappers and scalar values
  keep their existing auto-saver behavior.
  `TextFieldValueSaveableTests` covers annotations, reversed selection,
  restore-only composition clearing, fresh wrappers, key changes and sibling
  isolation; `JetchatRestorationTests` exercises the actual sample activity.
- **State primitives.** `MutableManagedState<T>` provides synchronized
  managed values that invalidate Compose readers without pretending to be a
  Kotlin flow. `MutableStateList<T>`, `MutableStateMap<K,V>`,
  `ComposeExtensions.DerivedStateOf<T>(Func<T>)`, and
  `composer.ProduceState<T>(initialValue, [keys…], producer)` are
  available. `ProduceState` keeps its observable state in a stable slot and
  gives each keyed producer lifetime a separate `IRememberObserver` JCW.
  Compose therefore starts replacements only after successful apply, leaves
  committed work running when a replacement is abandoned, and fences writes
  from retired producers. The producer is a plain
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
