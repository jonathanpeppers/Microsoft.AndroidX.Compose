# compose-net

Experiment: can we host **Jetpack Compose** UI from a .NET for Android app using only the existing `Xamarin.AndroidX.Compose.*` bindings — no new compiler, no source generator?

<p align="center">
  <img src="docs/images/hello-compose-csharp.png" alt="Hello from .NET running Jetpack Compose UI on Android" width="380" />
</p>

*The Tier 1 sample on an Android emulator: title bar, `Text("Hello from .NET")`, a Material 3 `Button`, and a `Count:` driven by `mutableStateOf` — all authored from C#, no Kotlin in the project.*

## Build &amp; run

Requires the .NET 10 SDK with the `android` workload installed and an Android API 34+ emulator or device.

```pwsh
# from the repo root
dotnet workload restore
dotnet build src/ComposeNet.Sample -c Release
dotnet build src/ComposeNet.Sample -t:Run    # deploys to the connected device/emulator
```

The generator's xUnit tests run without an Android SDK:

```pwsh
dotnet test src/ComposeNet.SourceGenerators.Tests
```

## Progress

Upstream `Xamarin.AndroidX.Compose.*` 1.11.1.1 (and `Material3` 1.4.0.x) now
ship real bindings, so the per-binding projects this repo originally needed
have been deleted. The sample and facade reference the official NuGets
directly. The historical context behind the in-repo bindings is preserved in
[NOTES.md](NOTES.md).

- [dotnet/android-libraries#1418][pr-1418] — PR: ship real bindings for
  `Xamarin.AndroidX.Compose.Runtime` (tracking issue:
  [#1415][issue-1415]).
- [dotnet/android-libraries#1416][issue-1416] — stop stripping
  `Xamarin.AndroidX.Compose.UI` / `Foundation` / `Foundation.Layout`.
- [dotnet/android-libraries#1417][issue-1417] — stop stripping
  `@Composable` functions in `Xamarin.AndroidX.Compose.Material3`.

[pr-1418]: https://github.com/dotnet/android-libraries/pull/1418
[issue-1415]: https://github.com/dotnet/android-libraries/issues/1415
[issue-1416]: https://github.com/dotnet/android-libraries/issues/1416
[issue-1417]: https://github.com/dotnet/android-libraries/issues/1417

## Why this exists

[*Android UI Development is Compose First*](https://android-developers.googleblog.com/2026/05/android-ui-development-is-compose-first.html) (Nick Butcher, May 2026) puts `android.widget.*`, Fragments, RecyclerView, ViewPager and the View-based tooling into **maintenance mode**. All new APIs, libraries, samples, and tools target Compose. For dotnet/android this is roughly equivalent to Apple's UIKit→SwiftUI shift in 2019 — we need a story.

This repo is **tier 1** of a two-tier strategy:

1. **Tier 1 (this repo):** prove a C#-only .NET for Android app can host Compose UI by talking to the existing `androidx.compose.*` runtime through the existing Xamarin bindings. No compiler, no codegen.
2. **Tier 2 (future, separate):** evaluate whether a Roslyn source generator + analyzer + `[InterceptsLocation]` could let a developer author `[Composable]`-attributed C# methods that the runtime treats like Kotlin `@Composable` functions.

---

## How Jetpack Compose actually works

The single most important thing to internalize: **Compose is not KAPT/KSP. It's a Kotlin Compiler Plugin that rewrites Kotlin IR.**

- Plugin id: `org.jetbrains.kotlin.plugin.compose` (Gradle plugin), implemented by the artifact `androidx.compose.compiler:compiler-hosted`. As of Kotlin 2.0 the source lives **in the Kotlin repo itself**, not in AOSP:
  - https://github.com/JetBrains/kotlin/tree/master/plugins/compose/compiler-hosted/src/main/java/androidx/compose/compiler/plugins/kotlin
- Entry point: [`ComposePlugin.kt`](https://github.com/JetBrains/kotlin/blob/master/plugins/compose/compiler-hosted/src/main/java/androidx/compose/compiler/plugins/kotlin/ComposePlugin.kt) — registers a [`ComposeIrGenerationExtension`](https://github.com/JetBrains/kotlin/blob/master/plugins/compose/compiler-hosted/src/main/java/androidx/compose/compiler/plugins/kotlin/ComposeIrGenerationExtension.kt), a K2 FIR extension ([`ComposeFirExtensionRegistrar`](https://github.com/JetBrains/kotlin/tree/master/plugins/compose/compiler-hosted/src/main/java/androidx/compose/compiler/plugins/kotlin/k2)) and several lowerings in [`lower/`](https://github.com/JetBrains/kotlin/tree/master/plugins/compose/compiler-hosted/src/main/java/androidx/compose/compiler/plugins/kotlin/lower).

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
| ---------------------------------------- | --------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| Mark functions/lambdas                   | `@Composable` as a **type annotation** (function type itself becomes `@Composable () -> Unit`) | C# has no annotated function types. Would need `[Composable]` on methods + custom delegate types, plus an analyzer to enforce "only call from another `[Composable]`".                                                                                       |
| Stable function-call keys                | `DurableFunctionKeyTransformer` (source-position hashing)                   | Roslyn `SyntaxTree.GetLineSpan()` — straightforward.                                                                                                                                                                                                         |
| Rewrite signatures to add `$composer, $changed` | IR lowering                                                                 | Source generator can't rewrite existing methods. You'd need an **interceptor** (`[InterceptsLocation]`, preview feature) or a Roslyn Source Transformer (not shipping). Realistic option: generate *partial* counterpart methods and require users to write `static partial`. Awkward. |
| Skipping & `$changed` bitmask            | Body transform                                                              | Generator emits the body. Hard but doable.                                                                                                                                                                                                                   |
| Stability inference (`@Stable`, `@Immutable`) | `StabilityInferencer` walks type graphs                                     | Re-implement against Roslyn `ITypeSymbol`.                                                                                                                                                                                                                   |
| `remember { }` as intrinsic              | `ComposerIntrinsicTransformer`                                              | Generator can recognize the call.                                                                                                                                                                                                                            |
| Live Edit / Live Literals                | Hot-reload via classloader replacement                                      | Would need .NET Hot Reload integration.                                                                                                                                                                                                                      |

Realistic path: a **Roslyn source generator + analyzer + interceptors** that emits the rewritten methods as `partial` peers and intercepts call sites. You'd lose: function-typed `@Composable` (no syntactic equivalent in C#), K2-level type-system enforcement, IDE refactorings. You'd gain: pure C# authoring against the same `androidx.compose.runtime` runtime jar.

This is a multi-engineer-year effort and you'd be perpetually chasing Google's plugin (it changes every Kotlin release).

A more pragmatic middle path: **two-tier strategy**.

1. **Tier 1 (ship soon, this repo):** bindings + a `ComposeView`-hosted "give us a Kotlin file" story, so devs can drop Compose UI into existing .NET for Android apps. No new compiler.
2. **Tier 2 (R&D):** prototype a Roslyn generator that targets only a useful subset (no function-typed composables — methods only) and emits to `Composer` directly. Validate with `Text`, `Column`, `Button`, `remember`, `mutableStateOf` before scaling.

## What APIs are needed on the C# side — and the Maven/AAR/NuGet status

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
- Pass a C# lambda to `setContent`. The lambda parameter type after the plugin runs is `Function2<Composer, Integer, Unit>`, but the runtime additionally expects it to be a `ComposableLambda` instance built via `composableLambdaInstance(key, tracked, block)` from `androidx.compose.runtime.internal`. You can construct that from C# — and **that's the realistic interop seam** this repo will exercise.

---

## What this repo will build

A minimal **.NET for Android** sample app that:

1. Targets `net10.0-android` (or latest available).
2. References the relevant `Xamarin.AndroidX.Compose.*` NuGets.
3. In `MainActivity`, creates a `ComposeView` and calls `SetContent(...)` with a `ComposableLambda` constructed in C#.
4. The composable renders `Text("Hello from .NET")` plus a `Button` with click-counter state via `RememberMutableState`.
5. Documents every API that needed special handling (inline classes, `Modifier` companion, default-value bitmasks, etc.).

The goal isn't beauty — it's to find the **smallest possible "Hello, Compose" from C# without any Kotlin file in the project**, and to enumerate every rough edge so we can decide what tooling, if any, is worth investing in.

## Status

The sample (`src/ComposeNet.Sample`) **builds** with `dotnet build`,
deploys to an Android 16 (API 36) emulator, **renders a real Material
3 UI**, and the counter button is **interactive** (tapping increments
`MutableNumberState<int>` and recomposes the `Text`).

Confirmed on device:

- **MaterialTheme** with Android 12+ dynamic colors (Material You) —
  the blue/teal scheme on a stock emulator, not the Compose-default
  purple.
- The activity is rendered **edge-to-edge** via
  `EdgeToEdge.Enable(this)` in `ComposeActivity.OnCreate`, hosted under
  a `Theme.Material.Light.NoActionBar` framework theme — no framework
  `ActionBar` overlays the content, status / nav bars are transparent,
  and Compose's `WindowInsets` plumbing handles the safe area. Inside a
  `Scaffold` body without a `TopBar`, pad the body with
  `Modifier.Companion.SafeDrawingPadding()` so content doesn't draw
  under the status bar; bare-`Column` roots use the same modifier on
  the root.
- A `Column` containing two **Material 3 `Text`** composables
  (`"Hello from .NET"`, `"Count: N"`) and a **Material 3 `Button`**
  whose label `"Tap to increment"` correctly inherits
  `LocalContentColor = onPrimary` (white on blue).
- Click → `count++` → recomposition → visible count update.

The facade ([`ComposeNet.Compose`](src/ComposeNet.Compose)) and sample
reference the official `Xamarin.AndroidX.Compose.*` 1.11.1.1 and
`Xamarin.AndroidX.Compose.Material3` 1.4.0.x NuGets directly. The
historical context behind the previously in-repo `<AndroidMavenLibrary>`
binding projects (now deleted) is preserved in [NOTES.md](NOTES.md).

### Composables shipped today

The facade currently wraps these Material 3 / Foundation composables
as C# types:

| Category    | Composables                                                                                       |
| ----------- | ------------------------------------------------------------------------------------------------- |
| Layout      | `Column`, `MaterialTheme`, `Surface`, `Card`                                                      |
| Lazy lists  | `LazyColumn<T>`, `LazyRow<T>`, `LazyVerticalGrid<T>`, `LazyHorizontalGrid<T>` (+ `GridCells`)     |
| Buttons     | `Button`, `IconButton`, `FloatingActionButton`                                                    |
| Text        | `Text`, `TextField`, `OutlinedTextField`                                                          |
| Chips       | `AssistChip`, `FilterChip`, `InputChip`, `SuggestionChip`                                         |
| Selection   | `Checkbox`, `TriStateCheckbox`, `RadioButton`, `Switch`, `Slider`, `RangeSlider`                  |
| Navigation  | `NavigationBar` + `NavigationBarItem`, `NavigationRail` + `NavigationRailItem`                    |
| Sheets      | `ModalBottomSheet`, `BottomSheetScaffold`                                                         |
| Pickers     | `DatePicker`, `DatePickerDialog`, `TimePicker`, `TimePickerDialog`                                |
| Overlays    | `AlertDialog`, `Tooltip`                                                                          |
| State       | `Remember`, `MutableState<T>`, `MutableNumberState<T>` (with `++/--`/`ToString` for Kotlin parity)|

---

## C# today vs the equivalent Kotlin

This is the actual app, side by side. Both render the same thing:
a Material 3 themed screen with two text lines and a button that
increments a counter.

### What you'd write in Kotlin

```kotlin
class MainActivity : ComponentActivity() {
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContent {
            MaterialTheme(colorScheme = dynamicLightColorScheme(this)) {
                var count by remember { mutableStateOf(0) }
                Column(Modifier.padding(16.dp)) {
                    Text("Hello from .NET")
                    Text("Count: $count")
                    Button(onClick = { count++ }) {
                        Text("Tap to increment")
                    }
                }
            }
        }
    }
}
```

That's ~14 lines and the Kotlin Compose compiler plugin does **all**
the heavy lifting: every `@Composable` call site gets rewritten to
pass `(Composer, $changed)`, every lambda gets wrapped in a
`ComposableLambda`, every literal gets a stable key, every parameter
gets a `$default` bitmask, `remember`/`mutableStateOf` become slot-table
intrinsics, and `by` desugars to `MutableState.value` access.

### What we write in C# today

We ship a small Tier 1.5 **runtime facade**
([`ComposeNet.Compose`](src/ComposeNet.Compose)) that wraps the raw
bindings. The user-facing C# is now structurally identical to Kotlin —
the only differences are `new`, commas, `() =>`, and `$"…"`:

```csharp
[Activity(Label = "@string/app_name", MainLauncher = true,
          Theme = "@android:style/Theme.Material.Light.NoActionBar")]
public class MainActivity : ComposeActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        SetContent(() =>
        {
            var count = Remember(() => new MutableNumberState<int>(0));
            return new MaterialTheme
            {
                new Column
                {
                    Modifier.Companion.SafeDrawingPadding(),
                    new Text("Hello from .NET"),
                    new Text($"Count: {count}"),
                    new Button(onClick: () => count++)
                    {
                        new Text("Tap to increment"),
                    },
                },
            };
        });
    }
}
```

That's the *entire* [`MainActivity.cs`](src/ComposeNet.Sample/MainActivity.cs).
~27 lines including ceremony, 13 lines for the composition itself.

| Kotlin                                 | C# (this repo)                                      |
| -------------------------------------- | --------------------------------------------------- |
| `override fun onCreate(…)`             | `protected override void OnCreate(…)`               |
| `super.onCreate(…)`                    | `base.OnCreate(…)`                                  |
| `setContent { … }`                     | `SetContent(() => { … })` on `ComposeActivity`      |
| `Text("Hi")`                           | `new Text("Hi")`                                    |
| `Column { … }`                         | `new Column { … }` (collection-initializer)         |
| `Button(onClick = { x++ }) { … }`      | `new Button(onClick: () => x++) { … }`              |
| `MaterialTheme { … }`                  | `new MaterialTheme { … }`                           |
| `var count by remember { mutableStateOf(0) }` | `var count = Remember(() => new MutableNumberState<int>(0))` |
| `count++`                              | `count++` (operator on `MutableNumberState<T>`)     |
| `"Count: $count"`                      | `$"Count: {count}"` (via `MutableState<T>.ToString`)|

### How the facade works

Composables are **types**, not method calls. Each is a
`ComposableNode` subclass; containers (`Column`, `MaterialTheme`,
`Button`) implement `IEnumerable` + `Add(ComposableNode)` so C#
collection-initializer syntax compiles. The tree built by `SetContent`'s
lambda is a pure value; `ComposeActivity` walks it and calls
`Render(IComposer)` on each node, threading the composer at the
implementation layer — invisible to user code, but explicit (no
`ThreadStatic`!) the same way Kotlin's compiler plugin makes
`$composer` an explicit IR parameter.

Inside each container's `Render`, raw-JNI bridges in
[`ComposeBridges.cs`](src/ComposeNet.Compose/ComposeBridges.cs) call the
Kotlin-mangled Compose functions (`Text--4IGK_g`, `Button-LP…`,
`AlertDialog-Oix01E0`, etc.) with their `$default` bitmasks. The bridges
follow a strict pattern — cached `IntPtr` class/method handles, JNI
signature constants, `try { Call… } finally { GC.KeepAlive(…) }` around
every managed wrapper whose `.Handle` was read into a `JValue`, and
`DeleteLocalRef` for any local string refs. The user never sees that;
when [dotnet/java-interop#1440] lands and the binder stops dropping
inline-class overloads, each bridge collapses to a direct generated
binding call.

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

### The `$default` bitmask source generator

Every `@Composable` JVM method takes a trailing `int $default` bitmask
where bit N == 1 means "argument N was not provided; use Kotlin's
default." Hand-writing those bitmasks at every call site is illegible
(`_changed: 0b0111`); writing the `[Flags]` enum by hand is tedious
and bit-rots when the Kotlin signature changes.

[`ComposeNet.SourceGenerators`](src/ComposeNet.SourceGenerators) is a
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

[`ComposeDefaults.cs`](src/ComposeNet.Compose/ComposeDefaults.cs) holds
all of these declarations — every composable shipped today (Button,
Text, IconButton, FloatingActionButton, Surface, AlertDialog, TextField,
OutlinedTextField, Card, AssistChip, FilterChip, InputChip,
SuggestionChip, NavigationBar(Item), NavigationRail(Item),
ModalBottomSheet, BottomSheetScaffold, DatePicker(Dialog),
TimePicker(Dialog), TooltipBox) gets its `$default` enum from this one
file. Unit tests in
[`ComposeNet.SourceGenerators.Tests`](src/ComposeNet.SourceGenerators.Tests)
pin the emitted output. When the upstream binder fix lands, each
declarative attribute can be swapped one-for-one to the generic form.

### What's missing on the C# side (and why)

| Kotlin                                  | C# today                                                       | Cost |
| --------------------------------------- | -------------------------------------------------------------- | ---- |
| `Modifier.padding(16.dp).fillMaxWidth()` | Host-view padding via `ApplySafeAreaPadding`; no `Modifier` chain | Can't compose modifiers from C# yet (inline-class param chain) |
| Skipping / recomposition optimization   | `$changed = 0` everywhere → full subtree recomposes on every state change | Correctness ✅, perf 🙁 |
| Slot-table-backed `remember`            | `Remember(() => …)` with `[CallerLineNumber]` keying into an activity-scoped cache | Works for top-level state; nested-scope / keyed `remember(key1, key2)` is Tier 2 |
| `@Composable` type-system enforcement   | None — calling a non-composable from a composable context fails at runtime, not compile-time | Footgun |
| Per-call-site allocation                | Every recomposition allocates fresh `ComposableNode` objects (no slot-table reuse on the C# side) | Tier 2 codegen fixes |

### Why it's like this

The Compose compiler plugin (see top of this README) rewrites Kotlin
IR to inject `$composer`, `$changed`, `$default`, slot-table keys,
restart groups, and skip logic. None of that exists in our C#
pipeline, so we either pay it by hand (per call site) or skip the
optimization (recompose the whole tree). The takeaways from this
tier-1.5 experiment:

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
  binding generator drops. Each one we use is a hand-written raw-JNI
  bridge in [`ComposeBridges.cs`](src/ComposeNet.Compose/ComposeBridges.cs).
  Tracked upstream in [dotnet/java-interop#1440] — when it lands every
  bridge in this repo can be deleted in favour of a direct generated
  binding call.
- **`$changed` bitmasks** — we pass `0` everywhere, so the runtime
  recomposes the whole subtree on every state change. Correct, not
  optimal. Proper bitmask computation per arg is Tier 2 territory.
- **`Modifier.Companion` not bound.** Workaround: raw JNI fetch.
  See `NOTES.md` open issue #1 for the upstream-friendly fix.
- **`remember(keys, …)` not yet supported.** Top-level `Remember(() =>
  state)` works (state is keyed by `[CallerLineNumber]` into an
  activity-scoped cache), but Compose's keyed/nested `remember` —
  reset-on-key-change semantics, slot-table-scoped lifetime — needs a
  real slot table on the C# side and is parked for Tier 2.

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
