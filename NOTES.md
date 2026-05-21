# Notes from the Tier 1 attempt

**Status: the sample builds AND calls a real composable.**
`dotnet build src\ComposeNet.Sample` produces a ~12.7 MB signed APK whose
dex contains `androidx/compose/runtime/Composer`,
`androidx/compose/foundation/layout/BoxKt`,
`androidx/compose/ui/Modifier$Companion`, and our C#-defined
`composenet/sample/HelloComposable` (a `Function2<Composer, Int, Unit>` ACW).

`MainActivity.OnCreate` constructs a `ComposeView` and a `ComposableLambda`
that calls `BoxKt.Box(Modifier.Companion, composer, $changed)` — all from
C#, no Kotlin source files in the repo. `Modifier.Companion` is fetched
via raw JNI because the Kotlin Companion object isn't bound (see "Open
issues" below).

Not deployed — no emulator/device in this environment. Build + dex
inspection verification only. Whether the Box actually paints when the
APK is launched is an open question we can't answer without hardware.

---

## What got built

| Project                                  | What it does                                                                                                       |
|------------------------------------------|--------------------------------------------------------------------------------------------------------------------|
| `ComposeNet.Bindings.Runtime`            | Re-binds `androidx.compose.runtime:runtime-android` 1.9.4 from Google Maven (the Xamarin NuGet ships zero types).  |
| `ComposeNet.Bindings.UI`                 | Re-binds `androidx.compose.ui:ui-android` 1.9.4 (Modifier, ComposeView, AbstractComposeView, layout primitives).    |
| `ComposeNet.Bindings.Foundation.Layout`  | Re-binds `androidx.compose.foundation:foundation-layout-android` 1.9.4 (Box, Column, Row, …). Used by the sample.  |
| `ComposeNet.Bindings.Foundation`         | Re-binds `androidx.compose.foundation:foundation-android` 1.9.4. Not referenced by sample yet.                     |
| `ComposeNet.Bindings.Material3`          | Re-binds `androidx.compose.material3:material3-android` 1.3.2. Not used by the sample (XA4215 — see below).        |
| `ComposeNet.Sample`                      | Minimal app. References Runtime + UI + Foundation.Layout. `MainActivity` calls `BoxKt.Box` from C#.                |

All five binding projects use `<AndroidMavenLibrary Pack="false">` to download
the AAR and run the binding generator over it, while relying on the existing
`Xamarin.AndroidX.Compose.*` NuGets (referenced by the sample) to actually ship
the AAR into the APK. The Pack="false" avoids the AAR being packaged twice.

The sample's `HelloComposable.Invoke` reads its `Composer`/`$changed` params
and calls `BoxKt.Box(modifier, composer, changed)`. The dex contains
`androidx/compose/foundation/layout/BoxKt`,
`androidx/compose/ui/Modifier$Companion`,
`androidx/compose/runtime/Composer`, and our
`composenet/sample/HelloComposable` ACW.

---

## Top surprises

### 1. The `Xamarin.AndroidX.Compose.*` NuGets ship empty (or near-empty) facades on purpose

Every Compose `Transforms/Metadata.xml` in `dotnet/android-libraries` has
`<remove-node path="/api/package" />`, with a comment saying Compose is
Kotlin-only and "does not make sense for C# until parsing of kotlin and
transpiling to c# is ready (if ever)". So the NuGets exist purely so other
Kotlin code can link transitively — they are not callable.

(One exception: `Xamarin.AndroidX.Compose.Material3Android.dll` actually
contains ~265 bound types. Only the `@Composable` functions were stripped.
This makes it overlap with our `ComposeNet.Bindings.Material3` and triggers
XA4215 "Java type bound in two assemblies" if you reference both — see
"Open issues".)

### 2. `<AndroidMavenLibrary>` is the right tool, but generated C# fights inline classes hard

[`AndroidMavenLibrary`](https://github.com/dotnet/android/blob/main/Documentation/docs-mobile/building-apps/build-items.md#androidmavenlibrary)
makes downloading and binding an AAR from Maven trivial. The friction is all
in the generated-C#-doesn't-compile phase. Recurring categories, with the fix
pattern we used:

| Error                                                                      | Pattern in `Transforms/Metadata.xml`                                                                                                                |
|----------------------------------------------------------------------------|-----------------------------------------------------------------------------------------------------------------------------------------------------|
| `CS0111` two overloads differ only by an inline-class (hashed name suffix) | `<remove-node path="/api/package[@name='X']/class[@name='Y']/method[starts-with(@name,'methodName-')]" />` — strips ALL hashed-suffix overloads.    |
| `CS0535` Kotlin `MutableList`/`Map`/`Set` overrides not implemented        | `<remove-node>` the class entirely if it's internal-only (e.g. `LazyLayoutPinnedItemList`).                                                         |
| `CS0535` missing accessor for `androidx.collection.MutableIntList` interop | `<remove-node>` the offending class (e.g. `IntervalList` impls).                                                                                    |
| `CS0234` namespace missing — `java.lang.Enum<KeyCommand>` not generated    | `<remove-node>` the interface AND every `*Kt` class that mentions it.                                                                               |
| `XAJDV7004` duplicate ignored java-dep entries                             | Remove the duplicate from `<AndroidIgnoredJavaDependency Include="..." />`.                                                                         |
| `XA4215` Java type bound in two assemblies (Material3Android stub)         | Drop the Xamarin stub from compile graph: `<PackageReference … ExcludeAssets="compile" />`.                                                         |

### 3. KMP `-jvm` vs `-android` package duplication will bite you twice

`androidx.collection:collection` ships as both `:collection-jvm` and
`:collection-android`. The Xamarin NuGet `Xamarin.AndroidX.Collection`
satisfies the `-android` variant. The `-jvm` variant has the same bytecode but
a different Maven coordinate; the dependency verifier doesn't know they're
interchangeable. Fix:

```xml
<AndroidIgnoredJavaDependency Include="androidx.collection:collection-jvm:1.5.0" />
```

The same thing happens to `androidx.compose.runtime:runtime-annotation`: it
ships as `-android` (AAR) AND `-jvm` (JAR), both containing
`androidx.compose.runtime.Immutable`, `Stable`, etc. R8 fails with
`Type androidx.compose.runtime.Immutable is defined multiple times`. Fix:

```xml
<PackageReference Include="Xamarin.AndroidX.Compose.Runtime.Annotation.Jvm"
                  Version="1.9.4" ExcludeAssets="all" />
```

(`ExcludeAssets="all"` strips the AAR/JAR from the build, not just the
managed DLL. `ExcludeAssets="compile"` keeps the AAR but hides the managed
facade — use that one when you want to override the Xamarin stub with your
own binding.)

### 4. `Pack="false"` doesn't always mean "don't ship the AAR"

In the Runtime binding I originally had two `<AndroidMavenLibrary>` lines —
one for `runtime-android` (the binding target) and one for
`runtime-annotation-android` with `Bind="false" Pack="false"` to satisfy the
Maven dep. Even with `Pack="false"`, the AAR was still being extracted into
the consuming APK's `obj/.../lp/` library-project cache, which collided with
the same AAR shipped by the Xamarin NuGet → R8 duplicate-class. Fix: don't
list it as `AndroidMavenLibrary` at all; instead add a normal
`<PackageReference Include="Xamarin.AndroidX.Compose.Runtime.Annotation.Android" />`
on the binding project. The Xamarin NuGet is the single source of truth for
that AAR.

---

## Open issues

1. **`Modifier.Companion` isn't bound in C#.** The binding generator emits a
   nested `Modifier.Companion` class that conflicts with the C# interface
   `IModifier` in the same namespace (CS0542 — member-same-as-enclosing).
   We `<remove-node>`'d it. The sample fetches the Companion instance via
   raw JNI:

   ```csharp
   IntPtr cls = JNIEnv.FindClass("androidx/compose/ui/Modifier$Companion");
   IntPtr fld = JNIEnv.GetStaticFieldID(cls, "$$INSTANCE", "Landroidx/compose/ui/Modifier$Companion;");
   IntPtr ref_ = JNIEnv.GetStaticObjectField(cls, fld);
   IModifier modifier = Java.Lang.Object.GetObject<IModifier>(ref_, JniHandleOwnership.TransferLocalRef)!;
   ```

   A cleaner fix would be `<attr name="managedName">ModifierCompanion</attr>`
   or moving the companion to a sibling namespace — try those first if you
   want to upstream this.

2. **No styled `Text` / `Button` yet.** Tier 1 paints via the simplest
   composable in the foundation suite: `BoxKt.Box(Modifier, Composer, int)`.
   `BasicText`, `Box(modifier, alignment, propagateMinConstraints, content,
   composer, $changed, $default)` and friends have hashed inline-class
   names (`BasicText-BpD7jsM`, `BasicText-RWo7tUw`, …) that the binding
   generator either drops or surfaces with mangled C# names; every
   per-signature inline-class param needs Metadata.xml triage. Material3
   is mostly bound (Xamarin's stub has 265 types) but referencing our
   Material3 binding alongside the stub explodes with XA4215 dual-emission.

3. **`ComposableLambdaInstance` is `@Composable` in source but NOT in
   bytecode.** This was the big worry. Verified via
   `javap -p -s` on the runtime AAR's `classes.jar`:

   ```
   public static final ComposableLambda composableLambdaInstance(int, boolean, Object);
     descriptor: (IZLjava/lang/Object;)Landroidx/compose/runtime/internal/ComposableLambda;

   public static final ComposableLambda composableLambda(Composer, int, boolean, Object);
     descriptor: (Landroidx/compose/runtime/Composer;IZLjava/lang/Object;)Landroidx/compose/runtime/internal/ComposableLambda;

   public static final ComposableLambda rememberComposableLambda(int, boolean, Object, Composer, int);
     descriptor: (IZLjava/lang/Object;Landroidx/compose/runtime/Composer;I)Landroidx/compose/runtime/internal/ComposableLambda;
   ```

   So `composableLambdaInstance` is the call-from-anywhere factory — the
   compose-compiler lowering uses it for top-level `ComposableSingletons$*`
   constants and it does not require an active composition. Our C# call
   from `OnCreate(...)` is safe. `composableLambda` (no `Instance` suffix)
   takes a `Composer` so it must be inside an active composition;
   `rememberComposableLambda` has the post-lowering tail and is therefore
   the version with `@Composable` semantics.

4. **`$changed` bitmask from C# is best-effort.** The sample's
   `HelloComposable.Invoke` forwards the `$changed` int it receives
   straight into `BoxKt.Box(..., changed)`. For nested composables we'd
   have to compute the bitmask correctly per-arg to get skipping right,
   which is exactly the role of the Tier 2 Roslyn generator. For
   single-call hello-world the forward-through-as-is approach recomposes
   the whole tree on every change, which is correct, just unoptimised.

5. **Material3 XA4215 still open.** Referencing our Material3 binding +
   the Xamarin Material3Android stub triggers dual-emission. Fix is
   either `ExcludeAssets="compile"` on the stub from the sample or
   `<remove-node>` every type my binding duplicates with the stub. Not
   tackled yet — `Box` is enough for tier 1.

7. **`AbstractComposeView` requires a `ComponentActivity` host.** When
   `ComposeView` attaches to the window it calls
   `WindowRecomposer.createLifecycleAwareWindowRecomposer`, which does
   `ViewTreeLifecycleOwner.get(this) ?: error("ViewTreeLifecycleOwner not found")`.
   Plain `android.app.Activity` doesn't install the
   `ViewTreeLifecycleOwner` / `ViewTreeSavedStateRegistryOwner` tags on
   the decor view; `androidx.activity.ComponentActivity` does. Fix:
   derive `MainActivity : ComponentActivity` (via
   `Xamarin.AndroidX.Activity` 1.11.0). Without this you get
   `java.lang.IllegalStateException: ViewTreeLifecycleOwner not found`
   from a background view attach the first time the activity becomes
   visible.

---

## Build / repro

```pwsh
cd src\ComposeNet.Sample
dotnet build
# → builds the 4 binding projects + sample, produces a signed APK
```

To inspect the dex:

```pwsh
Expand-Archive bin\Debug\net10.0-android\com.companyname.ComposeNet.Sample-Signed.apk -DestinationPath dex-inspect
$bt = "$env:LOCALAPPDATA\Android\Sdk\build-tools\<latest>\dexdump.exe"
& $bt dex-inspect\classes.dex | Select-String "androidx/compose/runtime/Composer|composenet/sample/HelloComposable"
```

## Files

```
src/
  ComposeNet.Sample/                          Tier 1 app. Calls Compose from C#.
    ComposeNet.Sample.csproj
    MainActivity.cs
  ComposeNet.Bindings.Runtime/                Binds androidx.compose.runtime 1.9.4
    ComposeNet.Bindings.Runtime.csproj
    Transforms/Metadata.xml
  ComposeNet.Bindings.UI/                     Binds androidx.compose.ui 1.9.4
  ComposeNet.Bindings.Foundation/             Binds androidx.compose.foundation 1.9.4
  ComposeNet.Bindings.Foundation.Layout/      Binds androidx.compose.foundation.layout 1.9.4
  ComposeNet.Bindings.Material3/              Binds androidx.compose.material3 1.3.2
```
