# Notes from the Tier 1 attempt

**Status: the sample builds.** `dotnet build src\ComposeNet.Sample` produces a
~12 MB signed APK whose dex contains `androidx/compose/runtime/Composer`,
`androidx/compose/ui/platform/ComposeView`, and our C#-defined
`composenet/sample/HelloComposable` (a `Function2<Composer, Int, Unit>` ACW).
The C# `MainActivity` constructs a `ComposeView`, builds a `ComposableLambda`
via `ComposableLambdaKt.ComposableLambdaInstance(...)` wrapping the C#
`HelloComposable`, and calls `composeView.SetContent(lambda)` — all from C#,
no Kotlin source files in the repo.

What the lambda body actually does is **nothing** (returns `null`). Calling
`Text(...)` / `Button(...)` from inside it requires `androidx.compose.ui.text` +
`androidx.compose.material3`'s `@Composable` functions, neither of which we
have callable C# bindings for yet (see "Open issues" below). The plumbing
through `setContent` is real; the painted UI isn't.

Not deployed — no emulator/device in this environment. Build-only verification.

---

## What got built

| Project                                  | What it does                                                                                                       |
|------------------------------------------|--------------------------------------------------------------------------------------------------------------------|
| `ComposeNet.Bindings.Runtime`            | Re-binds `androidx.compose.runtime:runtime-android` 1.9.4 from Google Maven (the Xamarin NuGet ships zero types).  |
| `ComposeNet.Bindings.UI`                 | Re-binds `androidx.compose.ui:ui-android` 1.9.4 (Modifier, ComposeView, AbstractComposeView, layout primitives).    |
| `ComposeNet.Bindings.Foundation.Layout`  | Re-binds `androidx.compose.foundation:foundation-layout-android` 1.9.4. Not used by the sample yet.                |
| `ComposeNet.Bindings.Foundation`         | Re-binds `androidx.compose.foundation:foundation-android` 1.9.4. Not used by the sample yet.                       |
| `ComposeNet.Bindings.Material3`          | Re-binds `androidx.compose.material3:material3-android` 1.3.2. Not used by the sample yet (XA4215 — see below).    |
| `ComposeNet.Sample`                      | Minimal app. References Runtime + UI bindings only. `MainActivity` calls Compose.                                  |

All five binding projects use `<AndroidMavenLibrary Pack="false">` to download
the AAR and run the binding generator over it, while relying on the existing
`Xamarin.AndroidX.Compose.*` NuGets (referenced by the sample) to actually ship
the AAR into the APK. The Pack="false" avoids the AAR being packaged twice.

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

1. **The Function2 body is empty.** Wiring `Text("Hello from .NET")` and a
   `Button` with a counter requires binding either `androidx.compose.material3`
   or `androidx.compose.ui.text`. Material3 is mostly bound (265 types from
   the stub plus our re-binding) but referencing our Material3 binding from
   the sample explodes with XA4215 dual-emission errors because the Xamarin
   stub binds the same Java types. We worked around it by *not* referencing
   our Material3 binding from the sample, so we have no way to actually call
   `Text(...)`. Next step is one of:
   - `ExcludeAssets="compile"` on `Xamarin.AndroidX.Compose.Material3Android`
     in the sample (we already do this on the binding side) and re-test.
   - Surgically `<remove-node>` the duplicated types from
     `ComposeNet.Bindings.Material3` so it only emits the `@Composable`
     functions the stub omits.

2. **`ComposableLambdaInstance` is itself `@Composable`.** The Kotlin source
   declares it `@Composable`, so its bytecode signature has the extra
   `Composer, Int` parameters. Our C# call passes `null` Composer because
   we're called from `OnCreate`, not from inside a composition. At runtime
   this probably no-ops (the lambda gets stashed for later replay by
   `setContent`'s `AbstractComposeView` flush) — but unverified. The
   "officially correct" non-`@Composable` entry point is
   `androidx.activity.compose.ComponentActivityKt.setContent(...)`, which
   would need `ComposeNet.Bindings.Activity.Compose`.

3. **Compose-compiler `$changed` bitmasks.** When we do start calling
   `TextKt.Text(...)`, the C# caller will be responsible for passing the
   correct `$changed: Int` bitmask that the Compose runtime uses for
   skipping. That's the job a Roslyn source generator (Tier 2) is supposed
   to take on. Doing it by hand from C# will work for static UI but will
   not recompose correctly when inputs change.

4. **Inline-class parameter values.** `Modifier`, `Color`, `Dp`, `TextStyle`,
   etc. are Kotlin inline classes that erase to `long`/`int`. The binding
   generator either emits both overloads (CS0111) or hides the inline form
   entirely. Each one needs a Metadata.xml hand-decision. We dodged this
   entirely because the sample composable is empty.

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
