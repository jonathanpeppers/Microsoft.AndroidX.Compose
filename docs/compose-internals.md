# How Jetpack Compose actually works

Background reading for anyone wondering *why* a "C#-only Compose"
project needs a facade, JNI bridges, and a source generator instead of
just calling `Text("Hello")` directly. For the architecture of the
facade itself, see [architecture.md](architecture.md).

## The Kotlin compiler plugin

The single most important thing to internalize: **Compose is not
KAPT/KSP. It's a Kotlin Compiler Plugin that rewrites Kotlin IR.**

- Plugin id: `org.jetbrains.kotlin.plugin.compose` (Gradle plugin),
  implemented by the artifact `androidx.compose.compiler:compiler-hosted`.
  As of Kotlin 2.0 the source lives **in the Kotlin repo itself**, not
  in AOSP:
  - https://github.com/JetBrains/kotlin/tree/master/plugins/compose/compiler-hosted/src/main/java/androidx/compose/compiler/plugins/kotlin
- Entry point: [`ComposePlugin.kt`](https://github.com/JetBrains/kotlin/blob/master/plugins/compose/compiler-hosted/src/main/java/androidx/compose/compiler/plugins/kotlin/ComposePlugin.kt)
  — registers a [`ComposeIrGenerationExtension`](https://github.com/JetBrains/kotlin/blob/master/plugins/compose/compiler-hosted/src/main/java/androidx/compose/compiler/plugins/kotlin/ComposeIrGenerationExtension.kt),
  a K2 FIR extension ([`ComposeFirExtensionRegistrar`](https://github.com/JetBrains/kotlin/tree/master/plugins/compose/compiler-hosted/src/main/java/androidx/compose/compiler/plugins/kotlin/k2))
  and several lowerings in [`lower/`](https://github.com/JetBrains/kotlin/tree/master/plugins/compose/compiler-hosted/src/main/java/androidx/compose/compiler/plugins/kotlin/lower).

The IR pipeline (from `ComposeIrGenerationExtension.generate()`) is roughly:

1. **`ClassStabilityTransformer`** — infers stability and writes a synthetic `$stable` static field on each class. The runtime uses this to decide if a `@Composable` call can be **skipped** when its arguments are equal.
2. **`LiveLiteralTransformer`** — replaces literals with calls to mutable state holders (powers Live Edit).
3. **`DurableFunctionKeyTransformer`** — assigns each composable call site a stable integer key derived from source position; this is the "identity" used by the slot table.
4. **`ComposerLambdaMemoization`** — wraps lambdas in `ComposableLambda` / `rememberComposableLambda` so they participate in recomposition.
5. **`ComposerParamTransformer`** — *the big one.* Rewrites every `@Composable fun Foo(x: Int)` into `fun Foo(x: Int, $composer: Composer, $changed: Int)`. Every call site is rewritten too. This is why you can't call `@Composable` functions from Java/Kotlin without the plugin — the JVM signature is different.
6. **`ComposableFunctionBodyTransformer`** — injects the `startRestartGroup` / `endRestartGroup` / `skipToGroupEnd` / `updateScope` calls around the body and around every nested composable call, plus the `$changed` bitmask plumbing that drives skipping.
7. **`ComposableAnnotationRemover`** — strips `@Composable` from IR (it was only a marker).

The runtime side it targets is in AOSP `frameworks/support`:

- `androidx.compose.runtime.Composer` — [Composer.kt](https://android.googlesource.com/platform/frameworks/support/+/refs/heads/androidx-main/compose/runtime/runtime/src/commonMain/kotlin/androidx/compose/runtime/Composer.kt) (slot table, recompose scopes, `startRestartGroup`, etc.)
- `androidx.compose.runtime.Composable` — [annotation reference](https://developer.android.com/reference/kotlin/androidx/compose/runtime/Composable) (just `@Retention(BINARY)` marker; the plugin does all the work).

So a `@Composable fun Greeting(name: String)` you wrote becomes, after compilation, something like:

```kotlin
fun Greeting(name: String, $composer: Composer, $changed: Int) {
    $composer.startRestartGroup(<stableKey>)
    val %dirty = $changed or if (($changed and 0b0110) == 0)
                              if ($composer.changed(name)) 0b0100 else 0b0010
                            else $changed
    if (%dirty and 0b1011 != 0b0010 || !$composer.skipping) {
        Text(name, $composer, 0)
    } else {
        $composer.skipToGroupEnd()
    }
    $composer.endRestartGroup()?.updateScope { c, n -> Greeting(name, c, n or 1) }
}
```

That generated code is what makes recomposition incremental.

### Pinned `$changed` ABI and Static compatibility

`ChangedBits` follows Kotlin's
[`ParamState` and `bitsForSlot`](https://github.com/JetBrains/kotlin/blob/3286b986762de1ee616fb239d5e1f1fdefeb82e8/plugins/compose/compiler-hosted/src/main/java/androidx/compose/compiler/plugins/kotlin/lower/ComposableFunctionBodyTransformer.kt):
Uncertain = `0b000`, Same = `0b001`, Different = `0b010`,
Static = `0b011`. `0b100` is **Unknown**, not Static. Slot *i* starts at
`1 + 3*i` within a group of ten; bit zero is force.
For one parameter `(dirty & 0xB) == 0x2` accepts Same (`2`) and Static (`6`)
but rejects Different (`4`), Uncertain (`0`), Unknown (`8`), and force.
The generated C# predicates use that contract for every supported slot.
Do not OR Same and Different into the same slot: that would produce Static.

This was cross-checked against the **actual pinned AARs**, not just a copied
C# constant: `Xamarin.AndroidX.Compose.Runtime.Android` **1.11.3.1** and
`Xamarin.AndroidX.Compose.Material3Android` **1.4.0.5** in
`Directory.Build.targets`. `javap -c -p` on their `classes.jar` shows
`ButtonKt.Button` using `6` for a static first slot, `2`/`4` for same/different,
and physical slot 9 for content. That newer compiler output uses
`Composer.shouldExecute` with the ten-slot low-bit mask `0x12492493` and
expected `0x12492492`, rather than the older stability-aware `0x5B6DB6DB`
mask used by our interceptor. Both accept Static and honour force; do not
assume every compiled Kotlin function uses an identical predicate.
`SearchBarKt` puts expanded docked content at slot 8 and expanded fullscreen
content at slot 9 (slot 3 is shape). Implicit receivers also count as Kotlin
changed slots; `$default` positions exclude receivers.

**Skipping safety:** `RememberAction` peers dispatch event callbacks through
the latest rebound target even if the owner skips. `ComposableLambdas.Wrap2/3`
use `tracked: true`: the pinned `ComposableLambdaImpl.update` calls
`trackWrite`, invalidating scopes registered by `trackRead`. A skipped parent
must still visit those invalidated descendants. Stable identity without this
invalidation is not sufficient for mutable content. The device rendering
regression checks tracked content under a skipped static parent, generated
tree/direct Button content, retained zero-/one-argument event peers, and
unchanged versus changed generated siblings. Its bounded execution counts
are correctness evidence, not a frame-time or CPU speedup measurement.
On the shared Pixel7, the embedded Debug regression APK passed all 14
filtered mask/callback/render tests: five input updates produced zero
additional stable-sibling executions, five changing-sibling executions,
zero static-parent body executions, and five parent skips. Content updates,
retained callback targets, and an independently invalidated generated
restart core were asserted separately. No before/after timing was measured.

**Compatibility:** the public enum previously declared `Static = 4`.
C# compilers inline enum constants, including generated call sites. Rebuild
consumers with the corrected runtime **and generator**; replacing a runtime
DLL alone does not update old compiled masks. No new enum member or public
API is introduced. Until all groups/receiver positions are modelled, wide
and receiver-bearing bridge calls remain entirely Uncertain, never a
partially forwarded first group.

### Live FAB defaults

The four Material 3 FAB wrappers distinguish the caller's logical omission
mask from the mask sent to Kotlin. In Material3Android 1.4.0.5, comparers
before `startDefaults` depend on default bits; their results also determine
which comparers run in the private FAB implementation. Changing that slot
footprint can place a cached `Long` where the private FAB expects its
remembered semantics `Function1` (`FloatingActionButton.kt:151`).

`FabStyleDefaults` resolves omitted modifier, family-specific shape, colors,
and elevation through the existing native companion/bound factories.
Composable default reads occupy always-present, separate caller-owned
groups. Supplied values, including zero and explicit null, are not replaced
by defaults. The nullable interaction source passes through unchanged, so
Kotlin still owns its internal source when none is supplied.
The final bound FAB call supplies every parameter with constant generated
`Default.None` and an Uncertain `$changed` mask. This also keeps live
modifier/shape omission changes from adding or removing native comparers;
it does not remount the FAB or its children.

Direct catalog named slots enter `ComposableContentNode.RenderDirect` with
the composer's value supplied to each invocation, just like container
content. A slot can recompose independently after its original caller has
returned (for example, an Extended FAB label inside `AnimatedVisibility`).
Both explicit-composer and composerless delegates need that invocation's
ambient `ComposableContext`; capturing or relying on the original caller's
context fails during the independent animation pass.

Native FAB acceptance checks revalidate lifecycle, focus, phase, and pending
composition/layout work after a committed frame with no intervening FAB,
icon, or label placement callback. An instantaneous idle reading alone can
admit intermediate animated geometry. PixelCopy and exact width assertions
use the post-commit bounds, without sleeps or polling for an expected size.
The fixture caches naturally committed frames from a pre-draw observer,
so a quiescent window needs neither a forced invalidation nor an extra draw.

## Can we "just call" the Kotlin plugin?

**No, not in any practical sense.** The plugin only runs *inside `kotlinc`* — it hooks into Kotlin's FIR/IR APIs (`FirExtensionRegistrar`, `IrGenerationExtension`). It cannot operate on:

- Roslyn C# syntax/semantic models
- .NET IL or Java bytecode after the fact
- MSBuild items

To "use" it you would have to: (a) write Kotlin, (b) run `kotlinc` with the plugin, (c) bind the resulting `.class`/`.jar` from C#. The output is normal JVM bytecode whose entry-point methods all have the extra `Composer, Int` parameters. From C# you'd be calling `Greeting(name, composer, 0)` — but you'd need to be inside an active composition to have a valid `Composer`, and to make a C# method *participate* in composition (its own `startRestartGroup`/skipping/etc.) you'd have to emit the same IR transform yourself.

**Verdict:** calling it as-is only buys you the "wrap Compose from C# like any Java library" scenario — you can host Compose UI but you can't *write* `@Composable` functions in C#.

## Port the plugin to a Roslyn source generator?

Technically feasible, *very* substantial. The Compose compiler plugin is ~30k lines of Kotlin and depends on Kotlin IR semantics (inline classes, default-value bitmasks, `noinline`/`crossinline` lambdas, K2 FIR resolution rules for `@Composable` as a *type* attribute, etc.). The pieces:

| Concern                                  | Kotlin plugin uses                                                          | Roslyn equivalent                                                                                                                                                                                                                                            |
| ---------------------------------------- | --------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Mark functions/lambdas                   | `@Composable` as a **type annotation** (function type itself becomes `@Composable () -> Unit`) | C# has no annotated function types. Would need `[Composable]` on methods + custom delegate types, plus an analyzer to enforce "only call from another `[Composable]`".                                                                                       |
| Stable function-call keys                | `DurableFunctionKeyTransformer` (source-position hashing)                   | Roslyn `SyntaxTree.GetLineSpan()` — straightforward.                                                                                                                                                                                                         |
| Rewrite signatures to add `$composer, $changed` | IR lowering                                                                 | Source generator can't rewrite existing methods. You'd need an **interceptor** (`[InterceptsLocation]`, preview feature) or a Roslyn Source Transformer (not shipping). Realistic option: generate *partial* counterpart methods and require users to write `static partial`. Awkward. |
| Skipping & `$changed` bitmask            | Body transform                                                              | Generator emits the body. Hard but doable.                                                                                                                                                                                                                   |
| Stability inference (`@Stable`, `@Immutable`) | `StabilityInferencer` walks type graphs                                     | Re-implement against Roslyn `ITypeSymbol`.                                                                                                                                                                                                                   |
| `remember { }` as intrinsic              | `ComposerIntrinsicTransformer`                                              | Generator can recognize the call.                                                                                                                                                                                                                            |
| Live Edit / Live Literals                | Hot-reload via classloader replacement                                      | Would need .NET Hot Reload integration.                                                                                                                                                                                                                      |

Realistic path: a **Roslyn source generator + analyzer + interceptors** that emits the rewritten methods as `partial` peers and intercepts call sites. You'd lose: function-typed `@Composable` (no syntactic equivalent in C#), K2-level type-system enforcement, IDE refactorings. You'd gain: pure C# authoring against the same `androidx.compose.runtime` runtime jar.

This is a multi-engineer-year effort and you'd be perpetually chasing Google's
plugin (it changes every Kotlin release). The pragmatic middle path is to ship
the tree-style facade against the existing runtime and evaluate generated
`[Composable]` methods separately.

## What APIs are needed on the C# side — Maven/AAR/NuGet status

There **is no single "Compose base View" type**. The bridge between the View system and Compose is:

- **`androidx.compose.ui.platform.AbstractComposeView`** (extends `android.view.ViewGroup`) — base class you subclass to host a composition inside a View hierarchy. [Reference](https://developer.android.com/reference/kotlin/androidx/compose/ui/platform/AbstractComposeView).
- **`androidx.compose.ui.platform.ComposeView`** — the concrete subclass with `setContent { … }`.
- **`ComponentActivity.setContent { … }`** (extension in `androidx.activity:activity-compose`) — the entry point most apps use; under the hood it creates a `ComposeView`.

Minimum Maven artifacts an app needs:

| Maven artifact                          | Role                                                              |
| --------------------------------------- | ----------------------------------------------------------------- |
| `androidx.compose.runtime:runtime`      | `Composer`, `remember`, `mutableStateOf`, `@Composable` annotation |
| `androidx.compose.ui:ui`                | `AbstractComposeView`/`ComposeView`, `Modifier`, layout, input    |
| `androidx.compose.ui:ui-graphics`       | `Color`, `Brush`, drawing                                         |
| `androidx.compose.ui:ui-text`           | text layout                                                       |
| `androidx.compose.foundation:foundation`| `Column`, `Row`, `Box`, `LazyColumn`, gestures                    |
| `androidx.compose.material3:material3`  | M3 widgets (`Button`, `Text`, `Scaffold`)                         |
| `androidx.activity:activity-compose`    | `setContent` extension                                            |

**Good news: dotnet/android-libraries already binds all of these.** From [`docs/artifact-list.md`](https://github.com/dotnet/android-libraries/blob/main/docs/artifact-list.md):

```
androidx.activity:activity-compose            → Xamarin.AndroidX.Activity.Compose
androidx.compose.runtime:runtime              → Xamarin.AndroidX.Compose.Runtime
androidx.compose.ui:ui                        → Xamarin.AndroidX.Compose.UI
androidx.compose.ui:ui-graphics               → Xamarin.AndroidX.Compose.UI.Graphics
androidx.compose.ui:ui-text                   → Xamarin.AndroidX.Compose.UI.Text
androidx.compose.ui:ui-tooling[-preview]      → Xamarin.AndroidX.Compose.UI.Tooling[.Preview]
androidx.compose.foundation:foundation        → Xamarin.AndroidX.Compose.Foundation
androidx.compose.foundation:foundation-layout → Xamarin.AndroidX.Compose.Foundation.Layout
androidx.compose.material:material            → Xamarin.AndroidX.Compose.Material
androidx.compose.material3:material3          → Xamarin.AndroidX.Compose.Material3
androidx.compose.animation:animation          → Xamarin.AndroidX.Compose.Animation
androidx.compose.runtime:runtime-livedata     → Xamarin.AndroidX.Compose.Runtime.LiveData
androidx.compose.runtime:runtime-saveable     → Xamarin.AndroidX.Compose.Runtime.Saveable
```

So from a binding standpoint a .NET for Android dev can already:

- Subclass `AbstractComposeView` in C#
- Reference `Composer`, `ComposableLambda`, etc.

What they **cannot** do today:

- Write a method that is meaningfully `@Composable`. The Kotlin annotation is `@Retention(BINARY)` and the magic isn't in the annotation — it's in the IR rewrite. A C# method with a `[Register]`'d `@Composable` attribute won't get rewritten, will have the wrong JVM signature, and will throw when called from a composition.

Passing a C# lambda to `setContent` is the realistic interop seam, and the one this repo exercises: the runtime expects a `ComposableLambda` built via `composableLambdaInstance(key, tracked, block)` from `androidx.compose.runtime.internal`, and that we can construct from C#. `ComponentActivity.SetContent(c => …)` (an extension method in this repo) is the canonical entry point today.

## Key references

- Blog: <https://android-developers.googleblog.com/2026/05/android-ui-development-is-compose-first.html>
- Compose compiler plugin source (Kotlin repo): <https://github.com/JetBrains/kotlin/tree/master/plugins/compose/compiler-hosted>
- `ComposePlugin.kt`: <https://github.com/JetBrains/kotlin/blob/master/plugins/compose/compiler-hosted/src/main/java/androidx/compose/compiler/plugins/kotlin/ComposePlugin.kt>
- `ComposeIrGenerationExtension.kt` (the pipeline): <https://github.com/JetBrains/kotlin/blob/master/plugins/compose/compiler-hosted/src/main/java/androidx/compose/compiler/plugins/kotlin/ComposeIrGenerationExtension.kt>
- Lowerings: <https://github.com/JetBrains/kotlin/tree/master/plugins/compose/compiler-hosted/src/main/java/androidx/compose/compiler/plugins/kotlin/lower>
- Compose runtime `Composer.kt`: <https://android.googlesource.com/platform/frameworks/support/+/refs/heads/androidx-main/compose/runtime/runtime/src/commonMain/kotlin/androidx/compose/runtime/Composer.kt>
- `@Composable` annotation: <https://developer.android.com/reference/kotlin/androidx/compose/runtime/Composable>
- `AbstractComposeView`: <https://developer.android.com/reference/kotlin/androidx/compose/ui/platform/AbstractComposeView>
- dotnet/android-libraries artifact list: <https://github.com/dotnet/android-libraries/blob/main/docs/artifact-list.md>
