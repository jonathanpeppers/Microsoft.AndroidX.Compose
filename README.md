# compose-net

Experiment: can we host **Jetpack Compose** UI from a .NET for Android app using only the existing `Xamarin.AndroidX.Compose.*` bindings — no new compiler, no source generator?

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
`MutableState<Int>` and recomposes the `Text`).

Confirmed on device:

- **MaterialTheme** with Android 12+ dynamic colors (Material You) —
  the blue/teal scheme on a stock emulator, not the Compose-default
  purple.
- A native Android **ActionBar** with the app name as the title
  (`Theme.Material.Light`), with the status-bar icons forced dark
  via `WindowInsetsController.SetSystemBarsAppearance(LightStatusBars, …)`
  so the clock/battery/wifi are readable.
- A `Column` containing two **Material 3 `Text`** composables
  (`"Hello from .NET"`, `"Count: N"`) and a **Material 3 `Button`**
  whose label `"Tap to increment"` correctly inherits
  `LocalContentColor = onPrimary` (white on blue).
- Click → `MutableState.Value = current + 1` → recomposition →
  visible count update.

The bindings — built via `<AndroidMavenLibrary>` because the existing
`Xamarin.AndroidX.Compose.*` NuGets strip every Compose API with
`<remove-node path="/api/package" />` — live in
`src/ComposeNet.Bindings.{Runtime,UI,Foundation,Foundation.Layout,Material3}`.
Read `NOTES.md` for the catalog of binding-generator errors and the
Metadata.xml / `ExcludeAssets` / `AndroidIgnoredJavaDependency` patterns used
to defeat them.

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

### What we have to write in C# today

There is no `@Composable` annotation that means anything to C#,
no `setContent { … }` trailing lambda, and the binding generator
strips every Kotlin-mangled overload of `Text`/`Button`. So we hand-write
each piece of the plugin's lowering by hand. The full
[`MainActivity.cs`](src/ComposeNet.Sample/MainActivity.cs) is ~200
lines; the highlights:

**The activity** — equivalent of `setContent { … }`:

```csharp
[Activity(Label = "@string/app_name", MainLauncher = true,
          Theme = "@android:style/Theme.Material.Light")]
public class MainActivity : ComponentActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        // remember { mutableStateOf(0) } — except `remember` needs to
        // be inside a composition, so we hold the state on the activity.
        var count = SnapshotStateKt.MutableStateOf(
            Java.Lang.Integer.ValueOf(0),
            SnapshotStateKt.StructuralEqualityPolicy());

        var composeView = new ComposeView(this);
        // The trailing-lambda `setContent { … }` from Kotlin is
        // ComposableLambdaInstance(key, tracked, Function2) under the
        // hood — we build that Function2 ourselves as a Java ACW.
        composeView.SetContent(ComposableLambdaKt.ComposableLambdaInstance(
            key: -1, tracked: false, block: new ThemedRoot(count)));

        SetContentView(composeView);
    }
}
```

**Every `{ … }` block in the Kotlin becomes a named C# class** —
because Compose lambdas have to be `Java.Lang.Object`-derived
`Function2`/`Function3` so they can be passed across the JNI boundary
as `ComposableLambda` payloads.

```csharp
// MaterialTheme { … }
public sealed class ThemedRoot : Java.Lang.Object, IFunction2
{
    readonly AppContent _body;
    public ThemedRoot(IMutableState count) => _body = new AppContent(count);

    public Java.Lang.Object? Invoke(Java.Lang.Object? p0, Java.Lang.Object? p1)
    {
        var composer = Android.Runtime.Extensions.JavaCast<IComposer>(p0!);
        var scheme = DynamicTonalPaletteKt.DynamicLightColorScheme(
            Android.App.Application.Context);

        // MaterialTheme(colorScheme, shapes, typography, content,
        //               $composer, $changed, $default)
        // $default = 0b0110 — provide colorScheme (bit 0) + content (bit 3),
        //                    let Compose default shapes (bit 1) + typography (bit 2).
        MaterialThemeKt.MaterialTheme(
            colorScheme: scheme, shapes: null, typography: null,
            content: _body, _composer: composer, p5: 0, _changed: 0b0110);
        return null;
    }
}

// Column { … }
public sealed class AppContent : Java.Lang.Object, IFunction2 { … }

// the body of the Column lambda — IFunction3 because Column's content
// is a (ColumnScope, Composer, Int) -> Unit
public sealed class ColumnContent : Java.Lang.Object, IFunction3
{
    public Java.Lang.Object? Invoke(Java.Lang.Object? p0, Java.Lang.Object? p1, Java.Lang.Object? p2)
    {
        var composer = Android.Runtime.Extensions.JavaCast<IComposer>(p1!);
        int n = ((Java.Lang.Integer)_count.Value!).IntValue();

        ComposeApi.Text("Hello from .NET", composer);
        ComposeApi.Text("Count: " + n, composer);
        ComposeApi.Button(_click, _buttonLabel, composer);
        return null;
    }
}

// onClick = { count++ }
public sealed class ClickHandler : Java.Lang.Object, IFunction0
{
    readonly IMutableState _count;
    public ClickHandler(IMutableState count) => _count = count;

    public Java.Lang.Object? Invoke()
    {
        int current = ((Java.Lang.Integer)_count.Value!).IntValue();
        _count.Value = Java.Lang.Integer.ValueOf(current + 1);
        return null;
    }
}
```

**Material 3 `Text` and `Button` aren't in the generated bindings at all** —
every overload's JVM name is Kotlin-mangled (`Text--4IGK_g`, `Button-LP…`)
because of inline-class params (`Modifier`, `Color`, `TextStyle`, `PaddingValues`),
so the binding generator either skips them or emits empty wrapper classes.
We call them by raw JNI from
[`ComposeApi.cs`](src/ComposeNet.Sample/ComposeApi.cs):

```csharp
// material3.TextKt.Text--4IGK_g(text, modifier, color, fontSize,
//   fontStyle, fontWeight, fontFamily, letterSpacing, decoration,
//   textAlign, lineHeight, overflow, softWrap, maxLines, minLines,
//   onTextLayout, style, $composer, $changed, $changed1, $default)
// 17 user params; pass text only, defaults bitmask = 0x1FFFE.
// color = 0L (Color.Unspecified) → reads LocalContentColor from the
// composition — onPrimary (white) inside a Button, onBackground (dark)
// at the top level.
public static unsafe void Text(string text, IComposer composer)
{
    var cls = JNIEnv.FindClass("androidx/compose/material3/TextKt");
    var mid = JNIEnv.GetStaticMethodID(cls, "Text--4IGK_g",
        "(Ljava/lang/String;Landroidx/compose/ui/Modifier;JJ" +
        "Landroidx/compose/ui/text/font/FontStyle;" +
        "Landroidx/compose/ui/text/font/FontWeight;" +
        "Landroidx/compose/ui/text/font/FontFamily;J" +
        "Landroidx/compose/ui/text/style/TextDecoration;" +
        "Landroidx/compose/ui/text/style/TextAlign;JIZII" +
        "Lkotlin/jvm/functions/Function1;Landroidx/compose/ui/text/TextStyle;" +
        "Landroidx/compose/runtime/Composer;III)V");

    var textRef = JNIEnv.NewString(text);
    JValue* args = stackalloc JValue[21];
    args[0]  = new JValue(textRef);
    args[1]  = new JValue(IntPtr.Zero); // modifier
    args[2]  = new JValue(0L);          // color = Unspecified → LocalContentColor
    /* … 14 more nulls/zeros … */
    args[17] = new JValue(composer.Handle);
    args[18] = new JValue(0);           // $changed
    args[19] = new JValue(0);           // $changed1
    args[20] = new JValue(0x1FFFE);     // $default — every param defaulted except text
    JNIEnv.CallStaticVoidMethod(cls, mid, args);
    JNIEnv.DeleteLocalRef(textRef);
}
```

### What's missing on the C# side (and why)

| Kotlin                                | C# today                                                     | Cost |
| ------------------------------------- | ------------------------------------------------------------ | ---- |
| `setContent { … }` trailing lambda    | Dedicated `Function2` subclass passed to `ComposableLambdaInstance` | Per-lambda boilerplate class |
| `MaterialTheme { Column { … } }` nesting | Each `{ … }` is a separate `IFunctionN` ACW                  | N classes for N composable lambdas |
| `var count by remember { mutableStateOf(0) }` | `IMutableState` field on the activity + manual `(Java.Lang.Integer)Value` boxing | No `by` delegation; no `remember`; counter survives recomposition only because it lives on the activity instance, not because Compose remembered it |
| `Text("Hi")` (auto-themed)            | `ComposeApi.Text(string, composer)` raw-JNI bridge with hard-coded JNI descriptor + `$default = 0x1FFFE` | One bridge per composable, per arity |
| `Button(onClick = { … }) { … }`       | `ComposeApi.Button(IFunction0, IFunction3, composer)` raw-JNI bridge | Same |
| `Modifier.padding(16.dp)`             | `composeView.SetPadding(left, top, right, bottom)` on the host view; no `Modifier` chain | Can't compose modifiers from C# at all (inline-class param chain) |
| Skipping / recomposition optimization | `$changed = 0` everywhere → no skipping, full subtree recomposes on every state change | Correctness ✅, perf 🙁 |
| `@Composable` type-system enforcement | None — calling a non-composable from a composable context fails at runtime, not compile-time | Footgun |

### Why it's like this

The Compose compiler plugin (see top of this README) rewrites Kotlin
IR to inject `$composer`, `$changed`, `$default`, slot-table keys,
restart groups, and skip logic. None of that exists in our C#
pipeline, so we either pay it by hand (per call site) or skip the
optimization (recompose the whole tree). The takeaway from this
tier-1 experiment:

- **Pure C# Compose hosting is feasible.** A real Material 3 UI runs
  end-to-end on device with zero Kotlin in the project.
- **It's unusably tedious by hand.** Even three composables and a
  click handler explodes into ~7 classes and ~200 lines.
- **A Roslyn source generator (tier 2) is the only path** to make this
  developer-grade. The C# above is essentially the *output* of what
  the Compose compiler plugin does for Kotlin — a generator would
  produce it from `[Composable]` C# methods.

## Known issues

- **Hashed inline-class composables aren't in the bindings.** Anything
  with `Modifier`/`Color`/`Dp`/`TextStyle`/`PaddingValues` parameters
  has a Kotlin-compiler-mangled JVM name (`Text--4IGK_g`, `Button-LP…`,
  `BasicText-BpD7jsM`) that the binding generator drops or surfaces as
  an empty wrapper. Each one we use is a hand-written raw-JNI bridge in
  `ComposeApi.cs`. Catalogued in `NOTES.md`.
- **`$changed` bitmasks** — we pass `0` everywhere, so the runtime
  recomposes the whole subtree on every state change. Correct, not
  optimal. Proper bitmask computation per arg is Tier 2 territory.
- **`Modifier.Companion` not bound.** Workaround: raw JNI fetch.
  See `NOTES.md` open issue #1 for the upstream-friendly fix.
- **Material3 XA4215 collision** with the empty stub `Xamarin.AndroidX.Compose.Material3*`
  NuGets. We work around it with `ExcludeAssets="all"` on the stub
  packages in `ComposeNet.Sample.csproj`. Catalogued in `NOTES.md`.
- **No `remember { }` from C#.** State that should survive recomposition
  is stored as fields on the host activity / lambda ACW instead. Fine
  for hello-world; broken for any real lifecycle.

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
