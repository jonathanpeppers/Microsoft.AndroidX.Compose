# Notes from the Tier 1 attempt

Status: **blocked at the binding-generation layer**, not at runtime.

## What I tried, in order

### 1. Scaffolded `src/ComposeNet.Sample` with the existing NuGets

```xml
<PackageReference Include="Xamarin.AndroidX.Activity.Compose" Version="1.13.0" />
<PackageReference Include="Xamarin.AndroidX.Compose.Runtime" Version="1.11.1" />
<PackageReference Include="Xamarin.AndroidX.Compose.UI" Version="1.11.1" />
<PackageReference Include="Xamarin.AndroidX.Compose.UI.Graphics" Version="1.11.1" />
<PackageReference Include="Xamarin.AndroidX.Compose.Foundation" Version="1.11.1" />
<PackageReference Include="Xamarin.AndroidX.Compose.Foundation.Layout" Version="1.11.1" />
<PackageReference Include="Xamarin.AndroidX.Compose.Material3" Version="1.4.0.2" />
```

`dotnet build -c Release` succeeds. The AAR is packaged into the APK.

**But there is no managed C# surface to call.** Inspection of e.g.
`Xamarin.AndroidX.Compose.Runtime.dll` shows a 16 KB empty assembly — zero
public types.

### 2. Confirmed why — dotnet/android-libraries deliberately strips it

Every Compose-related `Transforms/Metadata.xml` in dotnet/android-libraries
contains:

```xml
<remove-node path="/api/package" />
```

With this comment in
[`source/androidx.compose.runtime/runtime/Transforms/Metadata.xml`](https://github.com/dotnet/android-libraries/blob/main/source/androidx.compose.runtime/runtime/Transforms/Metadata.xml):

> not surfacing/generating MCWs — Compose does work for kotlin only via
> Annotations and kotlin defined UI. it does not make sense for C# until
> parsing of kotlin and transpiling to c# is ready (if ever)

So the NuGets exist purely so transitive Kotlin code that consumes Compose
can link. They are not meant to be called from C#.

The same `<remove-node path="/api/package" />` is in:

- `androidx.compose.ui/ui/Transforms/Metadata.xml`
- `androidx.activity/activity-compose/Transforms/Metadata.xml`
- `androidx.compose.material3/material3/Transforms/Metadata.xml`
- …and the rest of the Compose packages.

### 3. Tried to bind the AAR ourselves with `<AndroidMavenLibrary>`

`src/ComposeNet.Bindings.Runtime/ComposeNet.Bindings.Runtime.csproj` is a
binding-project that pulls `androidx.compose.runtime:runtime-android` 1.9.4
directly from `maven.google.com` via the
[`AndroidMavenLibrary`](https://github.com/dotnet/android/blob/main/Documentation/docs-mobile/building-apps/build-items.md#androidmavenlibrary)
build item and lets the binding generator run **without** dotnet/android-libraries'
`<remove-node>` censoring.

This **does** produce managed C# classes — `ComposerKt`, `RememberKt`,
`SnapshotStateKt`, etc. all show up in `obj/Release/.../generated/src/`.

But it produces **17 distinct C# compile errors** in just that one (smallest)
AAR:

| Error                                                                                   | Root cause                                                                                                                                |
|-----------------------------------------------------------------------------------------|-------------------------------------------------------------------------------------------------------------------------------------------|
| CS0111 `ComposerKt.IsAfterFirstChild` already defined                                   | Two Kotlin overloads differ only by an inline-class parameter (`Composer`-vs-`Composer-impl`) that erases to the same JVM signature.       |
| CS0535 `IMutableIntStateInvoker.IMutableState.Value.set` missing (and Long/Float/Double) | `IMutableIntState` inherits `MutableState<Integer>` — the `intValue`/`Value` bridge isn't emitted for inline-class-backed primitive state. |
| CS0535 `BroadcastFrameClock.ICoroutineContextElement.Key` missing (and Pausable…)        | Kotlin `val key: CoroutineContext.Key<*>` not surfaced as a C# property override.                                                          |
| CS0535 `AbstractApplier.IApplier.InsertBottomUp/TopDown` missing                         | Abstract member with generic parameter `T` doesn't satisfy the bound erased interface in C#.                                              |
| CS0534 `MutableSnapshot.Snapshot.ReadObserver` not implemented                          | Kotlin `typealias ReadObserver = (Any) -> Unit` projects oddly across the binding generator.                                              |
| CS0535 `SnapshotStateMap/List/Set.Size/Values/EntrySet` missing or wrong return         | Kotlin `MutableMap/List/Set` override + KMP `collection-jvm` interface erasure mismatch with `java.util.Map`/`List`/`Set`.                |

These are exactly the issues that
[`docs/development-tips.md`](https://github.com/dotnet/android-libraries/blob/main/docs/development-tips.md#troubleshooting)
in dotnet/android-libraries says require hand-written `Transforms/Metadata.xml`
+ `Additions/*.cs` partial classes per error.

All 17 errors are individually tractable. **But** this is just the
`runtime` AAR. To get a working "Hello, Compose from C#" sample we need
working bindings for:

1. `androidx.compose.runtime:runtime`
2. `androidx.compose.ui:ui` *(much larger surface — every layout/Modifier/etc.)*
3. `androidx.compose.ui:ui-graphics`
4. `androidx.compose.foundation:foundation`
5. `androidx.compose.foundation:foundation-layout`
6. `androidx.activity:activity-compose`
7. `androidx.compose.material3:material3` *(Text, Button, etc. live here)*

Each will hit the same class of errors, scaled to its surface area. And
**even if we resolve every compile error**, the resulting C# methods will
still have their `$composer: Composer, $changed: Int` parameters generated
by the Kotlin compose-compiler plugin baked into the JVM signature — and
the C# code calling them won't have a valid `Composer` instance unless it
is already inside a composition started by Kotlin-generated wrappers
(because `ComposableLambdaKt.composableLambdaInstance` itself is `@Composable`
and was lowered by the plugin). That part still needs to be validated.

## Top 3 surprises

1. **The existing `Xamarin.AndroidX.Compose.*` NuGets ship zero callable
   types — by design.** The 16 KB assemblies are placeholders. This isn't
   documented in `dotnet/android-libraries`' `artifact-list.md` which the
   README cites.
2. **`<AndroidMavenLibrary>` makes regenerating the bindings trivially easy
   *up to* the C# compile step.** The binding generator runs, emits MCWs,
   and downloads transitive deps from Maven — the friction is entirely in
   the generated-C#-doesn't-compile phase.
3. **`androidx.collection:collection-jvm` vs `:collection-android` is a
   recurring KMP gotcha** — `Xamarin.AndroidX.Collection` satisfies the
   `-android` variant but not the `-jvm` one even though they are the same
   bits at runtime. Needed an `AndroidIgnoredJavaDependency` override.

## Open questions for the human reviewer

1. **Power through, or stop here?** Resolving 17 errors on runtime is
   maybe 1–2 hours of metadata work. UI alone is probably 5–10× that.
   Total estimate to all-7-packages-compile: 1–3 working days. We will
   not know whether a `setContent(...)` call from C# actually drives a
   composition until then.

2. **Even if we get all bindings to compile, the realistic interop seam
   the README describes — passing a C#-constructed `ComposableLambda` into
   `setContent` — depends on `ComposableLambdaKt.composableLambdaInstance(...)`
   which is itself `@Composable`.** Its compiled signature has the extra
   `Composer, Int` parameters and **requires being called inside an active
   composition** (it asserts `composer.startReplaceableGroup(...)`). So we
   *also* need a C#-callable non-`@Composable` entry point. The pragmatic
   one is `ComponentActivity.setContent(...)` from `activity-compose`, but
   under the hood *that* extension function is `@Composable` too. We may
   need a tiny Kotlin shim — `fun csharpEntryPoint(activity: ComponentActivity, content: Function2<Composer, Int, Unit>)` —
   purely as the launcher. **That would violate the "no Kotlin files"
   constraint.** Do you want to relax the constraint to "one Kotlin
   bootstrap file, everything else C#"?

3. **Or pivot directly to Tier 2 R&D?** The cost/value of doing Tier 1
   "the hard way" looks worse than just standing up the Roslyn generator
   PoC, because:
   - The compiled Compose bytecode the bindings expose is **already
     post-`ComposerParamTransformer`** — calling `Text(string, composer, $changed)`
     from C# requires the caller to track `$changed` bitmasks correctly,
     which is what the source generator would do anyway.
   - The Kotlin Compose authors keep mangling inline-class signatures
     between versions; every Compose bump risks breaking the
     painstakingly hand-maintained Metadata.xml.

## Repro

```pwsh
cd src/ComposeNet.Bindings.Runtime
dotnet build -c Release
# → 17 CS0xxx errors in generated/src/Androidx.Compose.Runtime.*.cs
```

The full list of distinct errors and the `build.log` reproduction live in
`src/ComposeNet.Bindings.Runtime/build.log`.

## Files in this attempt

```
src/
  ComposeNet.Sample/                    # Tier 1 app shell (builds, no Compose calls yet)
    ComposeNet.Sample.csproj            # References the censored Xamarin Compose NuGets
    MainActivity.cs                     # Stock template; Compose hookup not yet possible
    …
  ComposeNet.Bindings.Runtime/          # Re-binding attempt via AndroidMavenLibrary
    ComposeNet.Bindings.Runtime.csproj  # Pulls androidx.compose.runtime:runtime-android 1.9.4 from Google Maven
    build.log                           # Last-attempt error output
```
