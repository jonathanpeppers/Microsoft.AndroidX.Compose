# compose-net — agent instructions

A C#-only .NET-for-Android app hosting Jetpack Compose UI through the
official `Xamarin.AndroidX.Compose.*` bindings. No Kotlin sources, no
custom bindings, no `[InterceptsLocation]` magic. Every C# composable
either calls a generated binding method directly or a JNI bridge in
`ComposeBridges.cs`.

Read `README.md` and `NOTES.md` for the why. This file documents the
conventions an agent **must** follow when changing code.

## Layout

- `src/ComposeNet.Compose/` — the public C# facade (`Text`, `Column`,
  `Button`, `MaterialTheme`, `AlertDialog`, …). **One class per file**,
  named after the class (`Text.cs`, `Column.cs`, `AlertDialog.cs`, …).
  `ComposeBridges.cs` holds raw-JNI bridges, `ComposeDefaults.cs` holds
  *only* assembly attributes that drive the source generator.
- `src/ComposeNet.SourceGenerators/` — Roslyn incremental generator
  (`ComposeDefaultsGenerator`) that emits `[Flags]` enums for Kotlin's
  `$default` bitmask.
- `src/ComposeNet.SourceGenerators.Tests/` — xUnit tests, **no Android
  SDK required**. Run with `dotnet test src/ComposeNet.SourceGenerators.Tests`.
- `src/ComposeNet.Sample/` — runnable Android app. Build with
  `dotnet build src/ComposeNet.Sample` (needs the `android` workload).

## Build / test / run

```pwsh
dotnet test  src/ComposeNet.SourceGenerators.Tests   # generator unit tests
dotnet build src/ComposeNet.Compose                  # facade only
dotnet build src/ComposeNet.Sample                   # full Android build
dotnet build src/ComposeNet.Sample -t:Run            # deploy to device
```

Run the generator tests on every change to `ComposeNet.SourceGenerators`
or `ComposeDefaults.cs`. Run the facade build on every change to
`ComposeNet.Compose`.

## Generated `$default` enums — DO NOT hand-roll

Every Compose `@Composable` function takes a trailing `int $default`
bitmask: bit *N* set means "parameter *N* was not supplied; use the
Kotlin default." We need a `[Flags]` enum naming each bit so call sites
read as `(int)ButtonDefault.All` instead of magic numbers.

**Always generate these enums via `ComposeDefaultsAttribute`. Never
write a `[Flags] enum FooDefault { … }` by hand.** All such attributes
live in `src/ComposeNet.Compose/ComposeDefaults.cs`.

Two forms, pick by whether the binding exposes the Kt method:

### 1. Generic form — preferred when the Kt method is bindable

```csharp
[assembly: ComposeDefaults<ColumnKt>("Column", "ColumnDefault")]
```

The generator picks the longest static overload of `ColumnKt.Column`,
walks parameters up to the first `IComposer`, names each bit after the
parameter (PascalCased), skips `Kotlin.Jvm.Functions.IFunction*` slots
(content lambdas — always provided), and emits an `All` constant.

### 2. Declarative form — when the Kt method is stripped

The dotnet/android-libraries binder strips Compose overloads with
mangled JVM names (`Text--4IGK_g`, `Surface-T9BRK9s`, `AlertDialog-Oix01E0`,
`FloatingActionButton-X-z6DiA`). These come from Kotlin `@JvmInline value
class` parameters (`Color`, `Dp`, `TextUnit`, `FontWeight`, …). Once
[dotnet/java-interop#1440] lands the generic form will work for them
too — until then, hand the generator the Kotlin parameter names:

```csharp
[assembly: ComposeDefaults("ButtonDefault",
    "!onClick", "modifier", "enabled", "shape", "colors",
    "elevation", "border", "contentPadding", "interactionSource", "!content")]
```

- Each name occupies one bit at its positional index.
- Prefix with `!` to consume the bit but emit no enum member (params
  the caller always provides — `onClick`, content lambdas, required
  values like `text`, `value`).
- For optional slot lambdas the caller toggles per-call (e.g.
  `AlertDialog`'s `dismissButton`/`icon`/`title`/`text`), keep them
  *as enum members* — the call site clears the bit when the user
  supplies the slot.

When the upstream binder fix lands, a declarative attribute can be
swapped to the generic form one-for-one and the comment in
`ComposeDefaults.cs` can be updated.

[dotnet/java-interop#1440]: https://github.com/dotnet/java-interop/pull/1440

### Generator diagnostics

| ID      | Meaning                                                |
|---------|--------------------------------------------------------|
| CN1001  | Generic form: named static method not found on `T`.    |
| CN1002  | Generic form: method has no `IComposer` parameter.     |
| CN1003  | Either form: attribute arguments couldn't be read.     |

Tests live in `GeneratorTests.cs` and run against synthetic
compilations — no Android references. **Add a test for any new
generator behaviour.**

## `ComposeBridges.cs` — source-generated JNI bridges

When an overload is stripped from the binding we call it via JNI. The
boilerplate (cache fields, lazy `FindClass` + `GetStaticMethodID`,
`JValue` array fill, `$default` mask, `try`/`finally` with
`GC.KeepAlive`, `NewString`/`DeleteLocalRef` for string params) is
emitted by `ComposeBridgeGenerator` from a one-line declaration.
**Do not hand-write the body.** Add a `partial` method and let the
generator fill it in.

### Adding a bridge

1. Add `[ComposeBridge(...)]` + a `public static partial` method
   declaration to `ComposeBridges.cs`. For `@Composable` bridges,
   `composer` **must** be the last C# parameter — the generator detects
   the composer slot by inspecting the trailing param. For
   non-`@Composable` Kotlin extension functions with `$default`
   (e.g. `Modifier.background`, `Modifier.border`, `Modifier.clickable`),
   there is no `Composer` slot at all; just declare the user params
   directly with the extension receiver as the first `IntPtr` param.
   For plain Kotlin static methods with no `Composer` and no `$default`
   (e.g. `Modifier.padding`, `RoundedCornerShape`), declare the params
   positionally — the generator treats the first user param as an
   extension receiver only when both it's `IntPtr` and the first JNI
   sigParam is an object (`L`). For stripped Kotlin constructors whose
   parameters were mangled by inline-class compilation
   (e.g. `GridCells.Adaptive(Dp)`), set `JvmName = "<init>"`; the
   generator emits `GetMethodID` + `NewObject` and wraps the returned
   handle with `Java.Lang.Object.GetObject<TReturn>(.., TransferLocalRef)`
   so the declared C# return type is the constructed object, not `void`.
2. **If** the underlying `@Composable` has at least one defaultable
   Kotlin parameter (i.e. the JNI signature has a trailing `$default`
   `I` slot after `$changed`), set `Defaults = typeof(XxxDefault)` and
   add a matching `[assembly: ComposeDefaults(...)]` to
   `ComposeDefaults.cs` naming each `$default` bit. Prefix with `!` to
   consume a bit but suppress the enum member (params the caller
   always provides). **If** every Kotlin parameter is required (no
   `$default` slot in the bytecode), omit `Defaults` entirely — the
   generator infers `$default` presence from the signature itself and
   emits CN2005 if attribute and signature disagree. Non-`@Composable`
   extensions with `$default` always require `Defaults`.
3. The generator parses the JNI signature, walks the C# parameters,
   and emits everything else.

Example (with `$default`):
```csharp
[ComposeBridge(
    Class     = "androidx/compose/material3/ButtonKt",
    JvmName   = "Button-bWB7cM8",
    Signature = "(Lkotlin/jvm/functions/Function0;...)V",
    Defaults  = typeof(ButtonDefault))]
public static partial void Button(
    IFunction0 onClick, IModifier? modifier, bool enabled,
    /* ...other Kotlin params... */
    IFunction3 content, IComposer composer);
```

Example (no `$default` — all params required):
```csharp
[ComposeBridge(
    Class     = "androidx/compose/ui/res/PainterResources_androidKt",
    JvmName   = "painterResource",
    Signature = "(ILandroidx/compose/runtime/Composer;I)Landroidx/compose/ui/graphics/painter/Painter;")]
public static partial IntPtr PainterResource(int id, IComposer composer);
```

Example (non-`@Composable` Kotlin extension with `$default`):
```csharp
[ComposeBridge(
    Class     = "androidx/compose/foundation/BackgroundKt",
    JvmName   = "background-bw27NRU$default",
    Signature = "(Landroidx/compose/ui/Modifier;JLandroidx/compose/ui/graphics/Shape;ILjava/lang/Object;)Landroidx/compose/ui/Modifier;",
    Defaults  = typeof(ModifierBackgroundDefault))]
public static partial IntPtr ModifierBackground(IntPtr modifier, long color, IntPtr? shape);
```
The trailing `I L<marker>` slots are the `$default` bitmask plus a
synthetic-overload `Object` marker; the generator emits both
automatically (the marker is always `IntPtr.Zero` / `null`).

Example (plain Kotlin static — no `Composer`, no `$default`):
```csharp
[ComposeBridge(
    Class     = "androidx/compose/foundation/layout/PaddingKt",
    JvmName   = "padding-3ABfNKs",
    Signature = "(Landroidx/compose/ui/Modifier;F)Landroidx/compose/ui/Modifier;")]
internal static partial IntPtr ModifierPaddingAll(IntPtr modifier, float dp);
```
The leading `IntPtr modifier` is auto-bound to the JNI extension
receiver slot because the first sigParam is `L`. For a non-extension
plain static (e.g. `RoundedCornerShape(Dp)` whose signature starts
with `F`, not `L`), the first user param is just a regular argument.

Example (Kotlin constructor — `JvmName = "<init>"`):
```csharp
[ComposeBridge(
    Class     = "androidx/compose/foundation/lazy/grid/GridCells$Adaptive",
    JvmName   = "<init>",
    Signature = "(F)V")]
internal static partial IGridCells GridCellsAdaptive(float minSizeDp);
```
The signature must end with `V` (JVM constructors return void at the
bytecode level even though the call hands back a handle), and the C#
return type must be non-`void` — that's the type the generator passes
to `Java.Lang.Object.GetObject<T>(.., TransferLocalRef)`. Ctor bridges
cannot declare a Composer parameter, `Defaults`, or `InstanceField`
(the generator rejects each with CN2006). All user params map
positionally to ctor argument slots; there is no extension receiver.

### Conventions the generator relies on

- For `@Composable` bridges, `composer` is the **last** C# parameter.
  For non-`@Composable` extension bridges (no `Composer` in the JNI
  signature) there is no `composer` parameter — the last param is
  just a regular user param.
- Add an `int defaults` parameter at the end of the user params (just
  before `composer` for `@Composable` shapes, or as the very last
  param for extension shapes) only when the caller controls the
  bitmask (state-holders, multi-slot composables that toggle bits per
  call). Otherwise omit it and the generator builds the mask
  automatically: one bit per nullable / optional C# param the caller
  passed `null` for. **Only valid when the bridge has a `$default`
  slot**; do not declare `int defaults` on a no-`$default` bridge.
- Kotlin extension receivers on `@Composable` functions: declare as
  `IntPtr` with a name ending in `Scope` (e.g. `IntPtr rowScope`).
  Non-`@Composable` extensions with `$default`: declare the receiver
  as the first `IntPtr` user parameter (any name — the generator binds
  it positionally to the first JNI slot). Plain Kotlin static
  extensions (no `Composer`, no `$default`): the first user param is
  treated as the receiver iff it is `IntPtr` AND the first JNI
  sigParam is an object (`L`); otherwise every user param is a regular
  positional argument (e.g. `RoundedCornerShape(float dp)` over
  `(F)Shape;` has no receiver). In all cases the receiver is placed at
  `args[0]` and excluded from the `$default` count.
- `IModifier?` is special-cased to call `ComposeBridges.ModifierHandle`
  (handles `null` → `IntPtr.Zero`). `IntPtr?` is also recognized:
  `null` → `IntPtr.Zero` for the JNI arg, and the auto-mask only
  clears the corresponding `$default` bit when the value is non-null.
- String params are hoisted into a `IntPtr __ref_<name>` and freed in
  the generated `finally`.
- Non-void return (state holders): the generator emits
  `return CallStaticObjectMethod(...)` inside the `try`/`finally`.
- For no-`$default` bridges, user params are matched to JNI slots
  **positionally** (no `[ComposeDefaults]` lookup); make sure the C#
  parameter order matches the Kotlin parameter order in the bytecode.

### What still lives hand-written

`ModifierHandle` (a managed-side `IModifier? → IntPtr` conversion) and
`ModifierCompanionInstance` (a static field lookup, not a method
invocation) don't fit any of the five `[ComposeBridge]` shapes and
remain raw JNI. Two-step bridges like `ModifierClipRoundedCorners`
(which composes `RoundedCornerShape` + `ClipKt.clip` and manages an
intermediate `Shape` local ref) also stay hand-written — their shape
isn't a single Kotlin call.

### Generator diagnostics

| ID      | Meaning                                                     |
|---------|-------------------------------------------------------------|
| CN2001  | `[ComposeBridge]` is missing/unmatched `Defaults` enum.     |
| CN2002  | Bridge signature/`Defaults` parameter count disagree.       |
| CN2003  | Bridge partial-method param doesn't match any Kotlin name.  |
| CN2004  | `[ComposeBridge]` has a malformed JNI signature.            |
| CN2005  | `Defaults` disagrees with the JNI `$default` slot.          |
| CN2006  | Constructor bridge shape requirements not met.              |

**When you add a new generator diagnostic, also update this table in
`.github/copilot-instructions.md` (and the matching table for the
`ComposeDefaultsGenerator` above if it's a CN1xxx code). Source of
truth is `src/ComposeNet.SourceGenerators/Diagnostics.cs`.**

**Do not add a `[ComposeBridge]` if the binding already exposes the
method**; call the generated C# entry point instead (see
`Column.cs::Column.Render` for the canonical example).

## Facade generator — `[ComposeFacade]`

For ~17 of the simplest user-facing facades the `Render()` body is
pure mechanical boilerplate — wrap `_onClick` in `ComposableLambda0`,
wrap children in `ComposableLambdas.Wrap2/3`, call `BuildModifier()`,
forward to the bridge. `ComposeFacadeGenerator` emits the whole
facade class (base class, backing fields, ctor, `Render` body) from
the bridge's own C# parameter shape — the user just adds
`[ComposeFacade]` next to the bridge's existing `[ComposeBridge]`
and provides a tiny hand-written sibling stub that owns the XML
docs (and any extra members the facade needs).

```csharp
// In ComposeBridges.cs
[ComposeBridge(/* … */)]
[ComposeFacade]
public static partial void Button(IFunction0 onClick, IModifier? modifier,
                                  IFunction3 content, IComposer composer);

// In Button.cs (sibling stub — owns the docs)
namespace ComposeNet;

/// <summary>Material 3 filled <see cref="Button"/>.</summary>
public sealed partial class Button;
```

The generator deliberately does **not** emit a `<summary>` of its
own; the stub is the canonical place for prose docs (XML cref links,
`<code>` blocks, `<remarks>`, etc., none of which would survive the
attribute round-trip). The stub is also the override hatch — if a
facade later needs an extra ctor overload, helper method, or
operator, add it to the same `partial class` declaration.

The generator classifies each user param of the bridge:

| Param type                              | Treatment                                         |
|-----------------------------------------|---------------------------------------------------|
| `AndroidX.Compose.UI.IModifier?`        | Passed as `BuildModifier()` — no ctor param      |
| `IFunction0` (onClick-style)            | Surfaced as a `System.Action` ctor parameter      |
| `IFunction2` content                    | Wrapped via `ComposableLambdas.Wrap2(…)`          |
| `IFunction3` content                    | Wrapped via `ComposableLambdas.Wrap3(…)`          |
| Primitive (`string`, `int`, `long`, `bool`, `float`, `double`)  | Surfaced as a ctor parameter, stored in `_<name>` |
| Anything else (callbacks, handles, etc.) | Rejected with CN3002                              |

`Scope = "Row"` / `Scope = "Column"` opt-in: when the content slot is
an `IFunction3`, the generator passes its first arg into
`RenderContext.PushScope(scope, ScopeKind.Row|Column)` so child
composables that need a `RowScope`/`ColumnScope` receiver pick it up.

Each facade is emitted to a unique hint name
(`ComposeNet.Facade.<ClassName>.g.cs`) so the `[CallerFilePath]` +
`[CallerLineNumber]` slot keys baked into `ComposableLambdas.Wrap*`
stay distinct per facade.

### When to use it

Only when the bridge fits the Phase 1 shapes above. Multi-slot
composables (`AlertDialog`, `Scaffold`, `ModalBottomSheet`),
callback-bearing leafs (`Checkbox`, `Slider`, `Switch`,
`RadioButton`), scope-consuming facades (`Tab`, `NavigationBarItem`,
`SegmentedButton`), and anything calling a Kt method directly (not
via `ComposeBridges`) stay hand-written. Trying to apply
`[ComposeFacade]` to them will emit CN3002 (unsupported parameter)
or CN3003 (scope misuse) at build time.

### Adding a new generated facade

When a Material 3 control fits the Phase 1 shapes (modifier +
onClick + content + primitives — see the table above), the
end-to-end recipe is:

1. **Add the bridge** in `ComposeBridges.cs` exactly as documented
   in the "Adding a bridge" section above (`[ComposeBridge]` +
   matching `[ComposeDefaults]` if the Kt method has a `$default`
   slot). Verify it compiles in isolation by building
   `src/ComposeNet.Compose`.
2. **Stack `[ComposeFacade]` on the same partial method.** Place
   it on the line directly above (or below) `[ComposeBridge]`. Pass
   `Scope = "Row"` / `"Column"` only if the content lambda is an
   `IFunction3` and child facades need that receiver via
   `RenderContext.PushScope`. Pass `ClassName = "MyName"` only when
   the public-facing class name must differ from the bridge method
   name (rare).
3. **Create the sibling stub** at
   `src/ComposeNet.Compose/<ClassName>.cs` with a single-line
   `public sealed partial class <ClassName>;` and the `<summary>`
   doc comment. Use the existing facades (`Button.cs`,
   `IconButton.cs`, `Card.cs`) as templates — keep summaries short,
   prefer `<see cref="…"/>` over inline names, and cross-reference
   the closest sibling for "same shape as X" facades. **Do not omit
   the stub** — without it the generated class has no XML docs.
4. **Build the sample** (`dotnet build src/ComposeNet.Sample`) to
   verify the bridge + facade compile together. The Phase 1 shapes
   are validated at build time; CN3001-CN3004 will fire if the
   generator can't accept the bridge.
5. **If CN3002 fires**, the bridge has a parameter outside the
   table above — either a non-content `IFunction1` callback, a
   value-class handle, or the manual `int defaults` hatch. Stop
   here, drop `[ComposeFacade]`, and write the facade by hand
   (delete the stub or expand it into a full hand-written class).
6. **Use the facade from the sample** (`MainActivity.cs` or one of
   the screen files) and confirm the rendered tree matches what a
   hand-written facade would have produced.

### Generator diagnostics

| ID      | Meaning                                                            |
|---------|--------------------------------------------------------------------|
| CN3001  | `[ComposeFacade]` on a method not declared on `ComposeBridges`.    |
| CN3002  | Bridge parameter type isn't a supported facade slot.               |
| CN3003  | `Scope` is set but bridge has no `IFunction3` content slot.        |
| CN3004  | `[ComposeFacade]` without an accompanying `[ComposeBridge]`.       |

### Migration rule

When you add `[ComposeFacade]` to a previously hand-written facade,
**replace the hand-written file** with a 3-line stub
(`namespace ComposeNet; /// <summary>…</summary> public sealed
partial class <Name>;`) in the same commit. The sibling stub is
required (it owns the XML docs) but must not redeclare members the
generator emits — otherwise you'll see duplicate-member errors.
Conversely, if you ever need a fully custom `Render()` body again,
remove the `[ComposeFacade]` attribute first and expand the stub
into a normal hand-written class.

## Facade conventions (`Composables.cs`)

- All public types derive from `ComposableNode` (a single `internal
  abstract void Render(IComposer)`).
- Container composables derive from `ComposableContainer`, which
  implements `IEnumerable` + `Add(ComposableNode?)` so callers can use
  C# collection-initializer syntax:
  ```csharp
  new Column { new Text("Hi"), new Button(onClick: …) { new Text("Tap") } }
  ```
- Wrap Kotlin lambdas with `ComposableLambda0/1/2/3/4` (existing
  helpers — don't hand-roll new lambda adapters).
- **Never construct `ComposableLambda2` / `ComposableLambda3` /
  `ComposableLambda4` directly inside a `Render(IComposer composer)`
  body.** Route every `@Composable` slot lambda through the
  appropriate helper in `ComposableLambdas` so Compose's own factory
  owns identity across recompositions. `SubcomposeLayout`-backed
  composables (`Scaffold`, `BottomSheetScaffold`, `ModalNavigationDrawer`,
  …) cache subcomposed content keyed by lambda identity; a fresh
  `new ComposableLambda*(...)` every pass thrashes that cache and
  causes `LayoutNode` insert ops to land at the wrong index
  (see #42). The helpers derive a unique slot-table key from
  `[CallerLineNumber]` + `[CallerFilePath]` automatically — no key
  argument needed.

  Two helper families exist because Compose has two factory functions
  that are not interchangeable:

  | Helper | Factory it calls | When to use |
  |--------|------------------|-------------|
  | `Wrap2(composer, …)` / `Wrap3(composer, …)` | `composableLambda(composer, key, tracked, block)` | The lambda is built and invoked **synchronously inside the same composition pass** as `Render` — content slots like `topBar`, `title`, button content, `Column`/`Row`/`Box` children. The factory writes the wrapper into the active composer's slot table. |
  | `Instantiate4(…)` (no composer param) | `composableLambdaInstance(key, tracked, block)` | The lambda is built during `Render` but **invoked later, outside the current composition** — `LazyListScope.items` / `LazyGridScope.items` `itemContent`, which Compose realizes at measure time inside the lazy list's `rememberLazyListItemProviderLambda`. The composer captured by the closure is no longer active by then, so calling `composableLambda(composer, …)` crashes with "Expected applyChanges() to have been called". `Instantiate4` skips the slot table and just allocates — exactly what Kotlin's inline `LazyListScope.items(…)` expands to. |

  ```csharp
  // Wrong — fresh identity every recomposition:
  var content = new ComposableLambda3(c => RenderChildren(c));
  // Right — stable identity owned by the runtime:
  var content = ComposableLambdas.Wrap3(composer, c => RenderChildren(c));

  // Wrong — Wrap4 would need a composer, but the call site (inside
  // scope.Items) runs at measure time, after the outer composer is stale:
  itemContent: new ComposableLambda4((_, idx, c) => …)
  // Right — composer-less factory, safe to call from a DSL builder:
  itemContent: ComposableLambdas.Instantiate4((_, idx, c) => …)
  ```

  `ComposableLambda0` (onClick), `ComposableLambda1` (onValueChange /
  onCheckedChange / LazyListScope / LazyGridScope DSL builders) callbacks
  are **not** `@Composable` and must stay raw — wrapping them would
  inject `startRestartGroup`/`endRestartGroup` machinery into code that
  runs outside composition.
- **Sibling `Render()` calls inside a loop need per-position slot
  keys.** `ComposableContainer.RenderChildren` already wraps each child
  in `composer.StartReplaceableGroup(i)` / `EndReplaceableGroup()`. Any
  custom loop that calls `Children[i].Render(c)` directly — e.g. the
  segmented-row scope loops in `SingleChoiceSegmentedButtonRow`,
  `MultiChoiceSegmentedButtonRow`, or `SegmentedButton`'s label slot —
  must do the same, otherwise same-type siblings collide on a single
  group key and Compose disambiguates by position only (brittle
  combined with any identity churn).
- The single call to `ComposableLambdaKt.ComposableLambdaInstance` in
  `ComposeActivity.SetContent` is the right shape there — it's the
  call-from-anywhere factory for the root content lambda, which runs
  outside an active composition. Leave it alone.
- Multi-slot composables (e.g. `AlertDialog`) expose **named slot
  properties** set via object-initializer syntax, not extra
  collection-init Add overloads. Pattern: start `defaults =
  XxxDefault.All`, clear the bit for each slot the caller actually
  supplied. See `AlertDialog.Render` — this is the template the future
  `ModalBottomSheet`, `DatePickerDialog`, etc. will follow.
- Required Kotlin parameters with no default (e.g. `AlertDialog.confirmButton`)
  are validated in `Render` with a clear `InvalidOperationException`.
- **Don't wrap calls to bound binding methods (or to source-generated
  `[ComposeBridge]` methods) in `try`/`finally` + `GC.KeepAlive`.** The
  binder-generated wrapper and the bridge generator both already emit
  their own `GC.KeepAlive` for every `IJavaPeerable` argument. Adding a
  second one is dead code. `GC.KeepAlive` is only needed when calling
  raw `JNIEnv.CallStatic*Method` directly (e.g. inside a hand-written
  helper in `ComposeBridges.cs` that the generator can't handle yet).
- File-scoped namespaces (`namespace ComposeNet;`). One blank line
  separating `// ---- Section ----` banners. XML doc comments on every
  public type and every non-trivial member.

### Optional veto / confirm callbacks (`(T) -> Boolean` parameters)

Several Compose APIs take a `(T) -> Boolean` callback the runtime
invokes to ask the caller "should this transition proceed?" — e.g.
`rememberDrawerState(confirmStateChange)` and
`rememberSheetState(confirmValueChange)`. **These are part of the
cached state holder's `remember` key**, so the JNI reference identity
has to stay stable across recompositions or the cache is dropped and
the state holder is rebuilt (the drawer / sheet forgets whether it
was open).

The pattern (see `DrawerConfirmStateChange` + `ModalNavigationDrawer`
for the canonical implementation):

1. **Expose the hook as a `Func<T, bool>?` property** on the facade
   type, defaulting to `null` (= "use Kotlin's default — always
   allow"). Document what `false` means (veto / block transition).
2. **Allocate the JNI adapter once per node instance** as a
   `readonly` field. Never `new` it inside `Render` — that recreates
   the Java peer on every recomposition and invalidates the
   `remember` key.
3. **Read the delegate inside the adapter's `Invoke`** (not at
   adapter construction), so the developer can mutate the property
   between renders without re-allocating.
4. **Treat `null` as "always true"** inside the adapter — no
   separate singleton fallback is needed; the adapter is already
   allocated, and a single branch in `Invoke` keeps the JNI reference
   identical whether or not the developer wired up a callback.

Do **not** use a static singleton for these callbacks: the singleton
trick is fine for genuinely stateless stubs that the user can't
override (`NoOpSearchCallback` — `onSearch` is *not* a `remember`
key), but it can't host a per-node mutable delegate without becoming
shared state across all instances.

## Bindings policy

The repo used to ship its own `*.Compose.*` binding projects. **Don't
bring those back.** Reference the official NuGets only:
`Xamarin.AndroidX.Compose.*` and `Xamarin.AndroidX.Compose.Material3`.
If a needed Compose API isn't bound, the workflow is:

1. File / link a tracking issue against `dotnet/android-libraries` (see
   the existing #1415–#1418 references in `README.md`).
2. Add a `[ComposeBridge]` partial method in `ComposeBridges.cs` (see
   above) — the generator handles all the JNI plumbing.
3. Add the matching `[ComposeDefaults]` declaration **only if** the
   underlying `@Composable` has a `$default` slot (see the "Adding a
   bridge" section above).
4. When the upstream binding fix ships, delete the bridge declaration
   and switch the facade to call the generated binding method
   directly.

## Style

- Target framework: `net10.0-android` for the facade and sample;
  `netstandard2.0` for the source generator (Roslyn requirement);
  `net10.0` for the generator tests.
- C# 12+, nullable reference types enabled, file-scoped namespaces.
- **One class per `.cs` file.** The file name matches the type name
  (`Button.cs` for `class Button`). This applies to every project in
  the repo — facade, source generator, generator tests, sample. No
  multi-class "kitchen sink" files; if you add a new type, add a new
  file. The only exception is a tiny private nested helper struct
  (e.g. `ScopeFrame` inside `RenderContext`) that exists solely as an
  implementation detail of its enclosing type.
- **No `// ---- Section ----` banners or other "stupid" section
  separator comments.** With one type per file, the file itself is the
  section. Comment code only when a specific bit of logic needs
  clarification; never use comments to label or group classes.
- Public API gets XML doc comments. Internal helpers get a one-line
  `//` comment when they're non-obvious; otherwise leave them bare.
- Don't add markdown planning docs to the repo — use the session
  artifact folder.
- Commit trailer: `Co-authored-by: Copilot <223556219+Copilot@users.noreply.github.com>`.
