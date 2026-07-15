# Notes from the Tier 1 attempt

**Status: the gallery builds, renders, and is interactive on device.**
`dotnet build src\Microsoft.AndroidX.Compose.Gallery` produces a signed APK that on launch
displays a `Column` of three `BasicText` composables ("Hello from .NET",
"Count: N", "Tap to increment") wired to a `MutableState<Int>`. Tapping
the third text invokes a C# `Function0` click handler that mutates the
state; Compose then recomposes the second `BasicText` showing the new
counter value. **End-to-end Compose + state + recomposition + input,
authored entirely in C# with no Kotlin source files in the repo.**

---

## What got built

The sample now renders a **Material 3 themed UI** entirely from C#:

* Android `Theme.Material.Light` ActionBar at the top showing the app name
  ("Microsoft.AndroidX.Compose Gallery") — the title bar is the native Material ActionBar,
  not a Compose `TopAppBar` (see issue #13).
* A Compose `MaterialTheme { Column { … } }` body inside a `ComposeView`,
  with proper safe-area padding for the status bar / nav bar.
* Two `BasicText` labels ("Hello from .NET", "Count: N").
* A real Material 3 **`Button`** — purple FilledButton with rounded
  corners, ripple, and elevation, all from the MaterialTheme defaults.
* Tapping the Button increments a `MutableState<Int>` and the second
  label recomposes.

13. **Material3 `Text` and `Button` are stripped from managed bindings,
    even after the AAR binds cleanly.** The generated `TextKt.cs` and
    `ButtonKt.cs` are *empty wrapper classes* with no `static void Text(…)`
    or `static void Button(…)` at all — every overload had at least one
    inline-class parameter (Color → long, TextStyle, ButtonColors,
    PaddingValues with inline Dp insets, etc.) that the binding generator
    couldn't map.

    Workaround: call them via **raw JNI** (same pattern as `BasicText`).
    `androidx.compose.material3.ButtonKt.Button` happens to use only
    reference types and primitives, so its descriptor is straightforward:

    ```
    (Lkotlin/jvm/functions/Function0;Landroidx/compose/ui/Modifier;Z
     Landroidx/compose/ui/graphics/Shape;Landroidx/compose/material3/ButtonColors;
     Landroidx/compose/material3/ButtonElevation;Landroidx/compose/foundation/BorderStroke;
     Landroidx/compose/foundation/layout/PaddingValues;
     Landroidx/compose/foundation/interaction/MutableInteractionSource;
     Lkotlin/jvm/functions/Function3;Landroidx/compose/runtime/Composer;II)V
    ```

    Pass `null` for everything except `onClick` and `content`, then set
    `$default = 0b0111111110` (all 8 middle bits set ⇒ "use defaults").
    Bonus: because the params are reference types, this is *not* a
    `Button-XXXXX` mangled name — it's literally `"Button"`.

    `MaterialThemeKt.MaterialTheme(colorScheme, shapes, typography,
    content, composer, 0, $default = 0b0111)` *did* survive the binding
    generator and can be called from C# directly.

14. **`Theme.Material.Light` ActionBar overlays the content area on API 36.**
    Even though the theme is not `…ActionBarOverlay`, the
    `decor_content_parent` puts the action_bar_container and the content
    `FrameLayout` as siblings starting at `y=0`. The ActionBar visually
    paints on top of any Compose content from y=0 to roughly
    `statusBarHeight + actionBarSize` (≈327px on a Pixel-class density).

    Workaround: read `?attr/actionBarSize` and the
    `android:dimen/status_bar_height` resource and apply both as top
    `View.SetPadding` on the `ComposeView` before `SetContentView`. The
    bottom navigation bar still needs `navigation_bar_height` padding
    too in edge-to-edge mode.


---

| Project                                  | What it does                                                                                                       |
|------------------------------------------|--------------------------------------------------------------------------------------------------------------------|
| `Microsoft.AndroidX.Compose.Bindings.Runtime`            | Re-binds `androidx.compose.runtime:runtime-android` 1.9.4 from Google Maven (the Xamarin NuGet ships zero types).  |
| `Microsoft.AndroidX.Compose.Bindings.UI`                 | Re-binds `androidx.compose.ui:ui-android` 1.9.4 (Modifier, ComposeView, AbstractComposeView, layout primitives).    |
| `Microsoft.AndroidX.Compose.Bindings.Foundation.Layout`  | Re-binds `androidx.compose.foundation:foundation-layout-android` 1.9.4 (Box, Column, Row, …). Used by the sample.  |
| `Microsoft.AndroidX.Compose.Bindings.Foundation`         | Re-binds `androidx.compose.foundation:foundation-android` 1.9.4. Not referenced by sample yet.                     |
| `Microsoft.AndroidX.Compose.Bindings.Material3`          | Re-binds `androidx.compose.material3:material3-android` 1.3.2. **Used by the sample** (MaterialTheme + Button). |
| `Microsoft.AndroidX.Compose.Gallery`                     | Minimal app. References Runtime + UI + Foundation.Layout. `MainActivity` calls `BoxKt.Box` from C#.                |

All five binding projects use `<AndroidMavenLibrary Pack="false">` to download
the AAR and run the binding generator over it, while relying on the existing
`Xamarin.AndroidX.Compose.*` NuGets (referenced by the sample) to actually ship
the AAR into the APK. The Pack="false" avoids the AAR being packaged twice.

The sample's `HelloComposable.Invoke` reads its `Composer`/`$changed` params
and calls `BoxKt.Box(modifier, composer, changed)`. The dex contains
`androidx/compose/foundation/layout/BoxKt`,
`androidx/compose/ui/Modifier$Companion`,
`androidx/compose/runtime/Composer`, and our
`net/sample/HelloComposable` ACW.

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
This makes it overlap with our `Microsoft.AndroidX.Compose.Bindings.Material3` and triggers
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

8. **`JNIEnv.FindClass` returns a *global* ref in .NET-for-Android.**
   Calling `JNIEnv.DeleteLocalRef` on the result trips ART's CheckJNI
   with `JNI DETECTED ERROR IN APPLICATION: expected reference of kind
   Local but found Global` (SIGABRT). Don't free it.
   `GetStaticObjectField` *does* return a real local ref — wrap it in
   `JniHandleOwnership.TransferLocalRef` when handing to
   `Java.Lang.Object.GetObject<T>`.

9. **Compose `@Composable` functions don't get `$default` overloads.**
   Plain Kotlin functions with default args get a synthetic `foo$default`
   sibling. The Compose compiler plugin handles defaults differently:
   the *regular* method has a trailing `int $default` mask parameter
   (after the synthetic `Composer $composer, int $changed` tail). The
   .NET-for-Android binding generator doesn't know about this convention,
   so it surfaces only the full-arg signature and you must pass every
   inline-class argument yourself — or skip the binding entirely and
   call via raw JNI with `$default = 0b...` to opt into defaults
   server-side. The sample's `ComposeApi.BasicText` does exactly this:

   ```csharp
   // androidx.compose.foundation.text.BasicTextKt.BasicText-BpD7jsM
   // descriptor: (Ljava/lang/String;Landroidx/compose/ui/Modifier;
   //              Landroidx/compose/ui/text/TextStyle;
   //              Lkotlin/jvm/functions/Function1;IZI
   //              Landroidx/compose/runtime/Composer;II)V
   // Pass null/0 for params 1..6 and set bits 1..6 in $default to
   // make the impl substitute the real defaults internally.
   ```

10. **`$default` mask convention: bit set = "use the DEFAULT".** Easy to
    invert mentally. Bit `i` set means "I didn't pass param `i`,
    substitute the default in the body"; bit `i` clear means "use what I
    passed". For `Column(modifier, vertArr, horizAlign, content)` to
    default the first three slots and accept the caller's content, mask
    is `0b0111`. Original `0b1110` (provided modifier/defaulted everything
    else) bypassed Compose's default-substitution for the null modifier
    we passed and tripped an NPE inside `ComposedModifierKt.materializeImpl`.

11. **No managed `Modifier.systemBarsPadding` / `Modifier.padding`.** The
    `androidx.compose.foundation.layout.PaddingKt.padding-*` overloads
    take inline-class `Dp` (= `value class Dp(val value: Float)`) so the
    binding generator strips them. Until we bind those (or ship a
    pre-baked padding modifier), the cheap workaround for safe areas is
    to set Android-level padding on the `ComposeView` itself:

    ```csharp
    composeView.SetPadding(left, statusBarHeight + dp16,
                           right, navBarHeight + dp16);
    ```

    Compose lays out inside the padded area like any other Android View.
    Status / navigation bar heights are readable from
    `android:dimen/status_bar_height` and `navigation_bar_height` via
    `Resources.GetIdentifier`. NOTE: `WindowInsetsCompat` listeners
    didn't fire reliably when registered after `SetContentView` on
    `ComponentActivity` (it appears to consume insets first); the
    System-resource lookup approach is uglier but works.

12. **No Material styling reachable from C# yet.** `BasicText` is the
    foundation primitive — it draws raw glyphs at `TextStyle.Default`
    (system default font, ~14sp, black). `androidx.compose.material3.Text`
    is the styled wrapper, but the `Xamarin.AndroidX.Compose.Material3`
    NuGet ships an empty stub (no managed types), and our local
    `Microsoft.AndroidX.Compose.Bindings.Material3` binding still hits XA4215
    dual-emission against that stub. The cleanest paths forward are
    (a) finish the Metadata.xml `<remove-node>`s on our Material3
    binding so it can coexist with the stub, (b) `ExcludeAssets="compile"`
    the stub from the sample, or (c) construct an explicit
    `TextStyle(fontSize = …, color = …)` via raw JNI and pass it to
    `BasicText` — TextStyle has ~25 inline-class params, so this is
    proportionally more JNI than I wanted to write for hello-world.


---

## Build / repro

```pwsh
cd src\Microsoft.AndroidX.Compose.Gallery
dotnet build
# → builds the 4 binding projects + gallery, produces a signed APK
```

To inspect the dex:

```pwsh
Expand-Archive bin\Debug\net10.0-android\net.compose.gallery-Signed.apk -DestinationPath dex-inspect
$bt = "$env:LOCALAPPDATA\Android\Sdk\build-tools\<latest>\dexdump.exe"
& $bt dex-inspect\classes.dex | Select-String "androidx/compose/runtime/Composer|net/gallery/HelloComposable"
```

## Files

```
src/
  Microsoft.AndroidX.Compose.Gallery/                         Tier 1.5 app. Uses Microsoft.AndroidX.Compose facade.
    Microsoft.AndroidX.Compose.Gallery.csproj
    MainActivity.cs                           ~27 lines total, mirrors Kotlin line-for-line.
  Microsoft.AndroidX.Compose/                         Tier 1.5 runtime facade (no codegen).
    Microsoft.AndroidX.Compose.csproj
    ComposableNode.cs                         Abstract AST base.
    Composables.cs                            Text / Column / Button / MaterialTheme nodes
                                                + ComposableContainer base with Add/IEnumerable.
    ComposableLambdas.cs                      [Register]'d ACW adapters
                                                (ComposableLambda0 / 2 / 3).
    ComposeBridges.cs                         Raw-JNI bridges to Material3 Text / Button.
    MutableState.cs                           MutableState<T> + MutableNumberState<T>
                                                (with operator ++/-- via INumber&lt;T&gt;).
    ComposeExtensions.SetContent.cs           Extension methods on ComponentActivity /
                                                ComposeView providing SetContent +
                                                EnableEdgeToEdge — no subclassing required.
  Microsoft.AndroidX.Compose.Bindings.Runtime/                Binds androidx.compose.runtime 1.9.4
    Microsoft.AndroidX.Compose.Bindings.Runtime.csproj
    Transforms/Metadata.xml
  Microsoft.AndroidX.Compose.Bindings.UI/                     Binds androidx.compose.ui 1.9.4
  Microsoft.AndroidX.Compose.Bindings.Foundation/             Binds androidx.compose.foundation 1.9.4
  Microsoft.AndroidX.Compose.Bindings.Foundation.Layout/      Binds androidx.compose.foundation.layout 1.9.4
  Microsoft.AndroidX.Compose.Bindings.Material3/              Binds androidx.compose.material3 1.3.2
```

## Tier 1.5 facade: lessons from making the C# look like Kotlin

After getting the raw bindings working (Tier 1, ~200-line
`MainActivity.cs` with five hand-written `IFunctionN` ACWs), we built
a thin runtime facade (`Microsoft.AndroidX.Compose`) to make the user-facing
code mirror Kotlin. Notes from that work:

### 5. Composables as types + collection-initializers gets us trailing-lambda-free nesting

C# doesn't have Kotlin trailing-lambda syntax, but it *does* have
collection-initializer syntax. By making every composable a
`ComposableNode` subclass and giving containers `IEnumerable` +
`Add(ComposableNode)`, the user gets:

```csharp
new MaterialTheme {
    new Column {
        new Text("Hi"),
        new Button(onClick: () => x++) { new Text("Tap") }
    }
}
```

…which is character-for-character the closest C# can get to:

```kotlin
MaterialTheme { Column { Text("Hi"); Button(onClick = { x++ }) { Text("Tap") } } }
```

The cost is one `ComposableNode` allocation per call site per
recomposition — fine for hello-world, not for production. A Tier 2
source generator would lower `[Composable]` C# methods to direct
composer-threading calls (no AST allocation).

### 6. The composer stays explicit internally; Tier 2 can hide it in source

Explicit overloads pass the composer directly. The composerless surface
additionally publishes that explicit parameter through a
dynamically scoped `[ThreadStatic]` while synchronous user code runs.
Generated interceptor cores, `SetContent` callbacks, and
`Tier2InlineContent.Render` push and restore the scope; they never replace
the explicit `IComposer` parameter used by the implementation.

Kotlin's Compose compiler plugin (`ComposerParamTransformer`) rewrites
every `@Composable fun Foo(x)` to `fun Foo(x, $composer, $changed)`.
The composer is **always** an explicit parameter. Our facade mirrors
this honestly: `internal abstract void Render(IComposer composer)` on
`ComposableNode`, with each container threading its received composer
into child renders. User code need not see `IComposer`; the implementation layer always does.
Deferred callbacks invoked after the dynamic scope closes cannot use
implicit-composer APIs. `[Composable]` methods are synchronous, and parallel
composition threads have independent ambient slots.
CN5009 traces composable delegates through local variables, local functions,
private returns, argument forwarding, method groups, and conditional/coalescing
expressions. Every path must end at a synchronous `[ComposableContent]` sink or
an immediate invocation; field storage, public/unknown forwarding, and
async/deferred callbacks are rejected at the escape site.

### 7. Nested content lambdas can reuse the outer composer reference

Inside a single composition pass, Compose threads the *same `Composer`
reference* through every nested content lambda — it's a mutable cursor
through the slot table, not a fork. That means container nodes can do:

```csharp
internal override void Render(IComposer composer)
{
    var content = new ComposableLambda3(c => RenderChildren(c));
    ColumnKt.Column(..., content: content, _composer: composer, ...);
}
```

…and `RenderChildren(c)` works correctly: `c` (the composer the runtime
hands the content lambda) is the same physical object as the outer
`composer` we passed into `ColumnKt.Column`. This lets each container's
`Render` be a few lines.

The exception is real subcomposition (`SubcomposeLayout`, `MovableContent`)
which *do* hand you a different composer — those would need an
explicit-composer overload. Tier 2 problem.

### 8. `MutableNumberState<T>` with `operator ++/--` is the killer feature for Kotlin parity

The Kotlin idiom is:

```kotlin
var count by remember { mutableStateOf(0) }
Text("Count: $count")
Button(onClick = { count++ }) { ... }
```

C# can't do `by` (delegated properties) or trailing lambdas, but
**operator overloading** plus a sensible `ToString()` carries enough
of the load:

```csharp
public class MutableState<T>
{
    public override string ToString() => Value?.ToString() ?? string.Empty;
    // ...
}

public class MutableNumberState<T> : MutableState<T> where T : INumber<T>
{
    public static MutableNumberState<T> operator ++(MutableNumberState<T> s) { s.Value++; return s; }
    public static MutableNumberState<T> operator --(MutableNumberState<T> s) { s.Value--; return s; }
}
```

Result:
- `$"Count: {count}"` interpolates via the base `ToString()` override
  (which renders `null` as the literal `"null"`, matching Kotlin)
- `count++` mutates via the overloaded operator. The class is
  constrained to `INumber<T>`, but `MutableState<T>` only boxes the
  built-in numeric primitives
  (`sbyte`/`byte`/`short`/`ushort`/`int`/`uint`/`long`/`ulong`/`float`/`double`);
  `decimal`, `Half`, `BigInteger`, `nint`, and `nuint` compile but
  throw at construction since they have no clean Java box.
- `count.Value` is still available for explicit `set` (e.g.
  `count.Value = 42`)

C# *cannot* express `implicit operator T` on `MutableState<T>` (CS0553
forbids user-defined conversions involving an enclosing type's own
type parameter), but the `ToString()` route is good enough — string
interpolation and `Text(...)` callsites both go through it. The only
thing you give up vs. an int-only specialization is using `count`
directly in arithmetic without `.Value`, which is rare in idiomatic
Compose code.

### 9. `[CallerLineNumber]` is a usable shim for `remember`

The real `remember { … }` in Kotlin uses Compose's slot table to cache
values across recompositions, keyed by call-site position in the
group hierarchy. A Tier 1.5 facade doesn't have access to the slot
table from C#, so we use:

```csharp
protected T Remember<T>(Func<T> factory, [CallerLineNumber] int key = 0)
{
    if (!_remembered.TryGetValue(key, out var v))
        _remembered[key] = v = factory()!;
    return (T)v!;
}
```

…with an activity-scoped `Dictionary<int, object>`. Limitations:
- Only works at the top of `SetContent`'s lambda, not inside nested
  composables (no positional context across container boundaries).
- Survives recompositions but **not** activity recreation
  (rotate-the-device loses state — real fix is `rememberSaveable` +
  `Composer.cache` integration).

Good enough for tier 1.5; tier 2 codegen should map `[Composable]`
locals to real slot-table reads.

### 10. `ComponentActivity.SetContent` extension keeps user `OnCreate` looking like Kotlin `onCreate`

The user override is:

```csharp
protected override void OnCreate(Bundle? state)
{
    base.OnCreate(state);
    this.EnableEdgeToEdge();
    this.SetContent(c => /* composition */);
}
```

…which mirrors:

```kotlin
override fun onCreate(state: Bundle?) {
    super.onCreate(state)
    enableEdgeToEdge()
    setContent { /* composition */ }
}
```

…almost verbatim — no subclassing of any custom base required. `SetContent`
is an extension method on `ComponentActivity` (and on `ComposeView`, for
View-hierarchy interop) that handles ComposeView creation and the
`ComposableLambdaKt.ComposableLambdaInstance` wrapping which invokes the
user lambda — passing in the ambient `IComposer` — on every recomposition.
