# compose-net — agent instructions

A C#-only .NET-for-Android app hosting Jetpack Compose UI through the
official `Xamarin.AndroidX.Compose.*` bindings. No Kotlin sources, no
custom bindings, no `[InterceptsLocation]` magic. Every C# composable
either calls a generated binding method directly or a JNI bridge in
`ComposeBridges.cs`.

Read `README.md` for the pitch, `docs/architecture.md` for how the
facade is built, and `docs/compose-internals.md` + `docs/NOTES.md` for
the deeper why. This file documents the conventions an agent **must**
follow when changing code.

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
| CN2007  | Recognized Compose value type used on a no-`$default` bridge. |
| CN2008  | Value-type parameter lowers to a JNI slot that doesn't match the bridge signature at that position. |

**When you add a new generator diagnostic, also update this table in
`.github/copilot-instructions.md` (and the matching table for the
`ComposeDefaultsGenerator` above if it's a CN1xxx code). Source of
truth is `src/ComposeNet.SourceGenerators/Diagnostics.cs`.**

**Do not add a `[ComposeBridge]` if the binding already exposes the
method**; call the generated C# entry point instead (see
`Column.cs::Column.Render` for the canonical example).

### Compose value types — `@JvmInline value class` lowering

Compose has a handful of `@JvmInline value class` types whose params
the binder strips because their JVM-mangled names (`Dp`, `TextUnit`,
`TextAlign`, …) surface as raw primitives at the JNI boundary. To let
bridges declare typed parameters and still lower correctly, the
generator hosts a small registry in
`src/ComposeNet.SourceGenerators/ComposeValueTypes.cs`:

| C# type | JNI slot | Lowering |
|---------|----------|----------|
| `ComposeNet.Dp?`          | `F` | `Dp.Pack(x)`                          |
| `ComposeNet.Sp?`          | `J` | `Sp.Pack(x)` (TextUnit, type=0x1)     |
| `ComposeNet.TextAlign?`   | `I` | `TextAlign.Pack(x)`                   |

Recognition keys on **`Nullable<T>` of the registered type** — bare
non-nullable `Dp`/`Sp`/etc. params are not recognized. This integrates
with the existing auto-default-mask flow: a non-`null` value clears
its `$default` bit (Kotlin uses the C# value), `null` leaves the bit
set (Kotlin substitutes its real default).

`androidx.compose.ui.graphics.Color` is intentionally NOT in this
registry. It's a `@JvmInline value class Color(val value: ULong)`
that surfaces as a packed `long` once
`Xamarin.AndroidX.Compose.UI.Graphics` is referenced — call sites
build it via `AndroidX.Compose.UI.Graphics.ColorKt.Color(r, g, b, a)`
and pass the resulting `long` straight through to the bridge, so
there's no C# struct to lower from.

Reference-typed Compose wrappers (`FontWeight`, `TextDecoration`,
`Shape`) are NOT in this registry either — they go through the
generic "reference-type → handle with null check" path, since they
subclass `Java.Lang.Object` directly and the bridge generator already
handles `T?` for `T : Java.Lang.Object` correctly.

Adding a new value type means appending one entry to
`ComposeValueTypes.Recognized`, optionally a `Pack(T?)` static helper
on the value type, and a generator test in `BridgeGeneratorTests.cs`.

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
| `IFunction1` + `[Callback(typeof(T))]`  | Surfaced as `Action<T>` ctor; supports `bool`, `string`, `float` |
| `IFunction2` content (non-nullable, sole content slot) | Wrapped via `ComposableLambdas.Wrap2(…)` — container shape |
| `IFunction3` content (non-nullable, sole content slot) | Wrapped via `ComposableLambdas.Wrap3(…)` — container shape |
| `IFunction2?` / `IFunction3?` (any nullable, OR `[Slot]`, OR >1 content slot) | Surfaced as a named `ComposableNode?` property — multi-slot leaf shape |
| `IntPtr` with name ending in `Scope`    | Kotlin extension receiver; auto-bound to `RenderContext.CurrentScope` (no ctor slot) |
| `IntPtr` + `[PainterResource]`          | Synthetic `int painterResourceId` ctor arg + `PainterResource` resolution + try/finally |
| `IntPtr` + `[StateHolder(Remember = …, StateType = typeof(…))]` | State-holder shape (Phase 4): the facade exposes the wrapper type as a defaulted ctor slot (`StateType? state = null`), calls the named `RememberXxxState(composer)` bridge on first render, populates the wrapper's `Jvm` field, and forwards the JNI handle to this parameter. |
| Primitive (`string`, `int`, `long`, `bool`, `float`, `double`)  | Surfaced as a ctor parameter, stored in `_<name>` |
| Anything else (callbacks, handles, etc.) | Rejected with CN3002                              |

`Scope = "Row"` / `Scope = "Column"` opt-in: when the content slot is
an `IFunction3`, the generator passes its first arg into
`RenderContext.PushScope(scope, ScopeKind.Row|Column)` so child
composables that need a `RowScope`/`ColumnScope` receiver pick it up.

`[ComposeFacade(DefaultColorFromTheme = "secondaryContainer")]` opt-in
(Phase 6): the matching `long` user param is reclassified as a
`ContainerColor` property. The render body falls back to
`MaterialTheme.Instance.GetColorScheme(composer, 0).<Slot>` when the
caller leaves it at `0L`. Use `ColorParameter` to disambiguate when
the bridge has more than one `long` user param; otherwise the sole
`long` is auto-picked.

Parameter-level attributes the generator recognizes:

- `[Slot("PropertyName")]` — rename the generated property for an
  `IFunction2` / `IFunction3` slot (default = `PascalCase` of the
  Kotlin parameter name). Also forces the bridge into multi-slot
  shape even if only one content lambda is present.
- `[Callback(typeof(T))]` — surface an `IFunction1` user param as a
  typed `Action<T>` ctor slot. `T` must be `bool`, `string`, or
  `float`; the generator emits the appropriate `Java.Lang.Boolean`
  / `Java.Lang.Float` unboxing or `ToString()` adapter.
- `[PainterResource]` — annotate the `IntPtr` user param that takes
  the resolved Painter handle. The facade exposes a synthetic
  `int painterResourceId` ctor arg in its place and emits the
  `PainterResource(id, composer)` + try/finally + `DeleteLocalRef`
  preamble around the bridge call.
- `[StateHolder(Remember = nameof(ComposeBridges.X), StateType = typeof(T))]`
  — annotate the `IntPtr` user param that carries a Kotlin
  state-holder handle (e.g. `DatePickerState`,
  `DateRangePickerState`, `TimePickerState`). The facade gains a
  defaulted ctor slot `T? state = null` (appended last), calls the
  named `Remember` bridge at the top of `Render` to obtain the JNI
  handle, populates `state.Jvm` via
  `Java.Lang.Object.GetObject<TJvm>(handle, JniHandleOwnership.DoNotTransfer)`,
  and forwards the handle to this parameter.

  Two shapes are supported:

  - **Phase 4** — zero-user-param Remember
    (`(IComposer) -> IntPtr`, e.g. `RememberDatePickerState`,
    `RememberDateRangePickerState`). `_state` stays nullable; `Jvm`
    is only populated when the caller supplies a non-null wrapper
    (`if (_state is not null && _state.Jvm is null) …`).
  - **Phase 4b** — parameterised Remember
    (`(arg1, arg2, …, IComposer) -> IntPtr`, e.g.
    `RememberTimePickerState(int initialHour, int initialMinute,
    bool is24Hour, IComposer)`). Each Remember user param is
    resolved to a readable instance member (property or field) on
    `StateType` by PascalCase match first
    (`initialHour` → `InitialHour`), then `Initial<PascalCase>`
    fallback (`is24Hour` → `Is24Hour` first, else
    `InitialIs24Hour`). `_state` is auto-created in the ctor
    (`_state = state ?? new T()`), so the field is guaranteed
    non-null inside `Render` and `Jvm` population is unguarded
    (`if (_state.Jvm is null) …`). The PascalCase-first rule lets a
    wrapper's live getter that falls back to its initial-value
    backing field (`Is24Hour => Jvm?.Is24hour() ?? InitialIs24Hour`)
    be the natural match — at remember-time `Jvm` is null, so it
    returns the initial value correctly. The Phase 4b shape
    requires `StateType` to be constructible with no arguments
    (either a declared parameterless ctor, or a ctor whose every
    parameter has a `HasExplicitDefaultValue` default).
  - **Phase 4c** — opt in to shared-state caching with
    `SharedState = true`. When two sibling facades share a single
    `StateType` wrapper (e.g. `TimePicker` + `TimeInput` reading the
    same `TimePickerState`), the second facade to render would
    otherwise call `RememberXxxState` again and overwrite the
    wrapper's `Jvm` field with a new peer — losing the value the
    first facade just bound. With `SharedState = true` the Render
    preamble first checks `_state.Jvm` and reuses the cached JNI
    handle when present, only calling `Remember` on the first-render
    (cache-miss) path. Both Phase 4 (nullable `_state`) and Phase 4b
    (auto-created `_state`) honour the flag — the cache-hit branch
    just casts to `IJavaObject` and reads `.Handle`; the cache-miss
    branch is identical to the non-shared path.

  `StateType` must declare an instance, writable, non-readonly,
  accessible field named `Jvm` whose declared type is the
  binding-generated state interface (e.g. `IDatePickerState`).
  Cannot be combined with `[PainterResource]` on the same parameter.

Each facade is emitted to a unique hint name
(`ComposeNet.Facade.<ClassName>.g.cs`) so the `[CallerFilePath]` +
`[CallerLineNumber]` slot keys baked into `ComposableLambdas.Wrap*`
stay distinct per facade.

### When to use it

The generator now supports a wide range of facade shapes (Phases 1,
2, 3, 4, 6, 7, 8, 9 are implemented):

- **Phase 1** — container with content lambda + ctor primitives
  (`Button`, `Card`, `Text`, `Column`, `Row`, `Box`).
- **Phase 2** — callbacks via `[Callback(typeof(T))]` for
  `IFunction1` user params (`TextField`, `OutlinedTextField`,
  `IconToggleButton` family).
- **Phase 3** — multi-slot leafs with named `ComposableNode?`
  properties (`AlertDialog`, `AssistChip`, `ListItem`, `Snackbar`,
  `BadgedBox`, `Tab`, `NavigationBarItem`, the top-app-bar family).
- **Phase 4** — `[StateHolder(...)]` state-holder facades that need
  a `RememberXxxState(composer)` round-trip and a `.Jvm` population
  for the caller-supplied wrapper (`DatePicker`, `DateRangePicker`).
- **Phase 4b** — `[StateHolder(...)]` with a parameterised Remember
  bridge that takes user parameters resolved against `StateType`
  members (`TimePicker`, `TimeInput`). The facade auto-creates the
  wrapper when the caller leaves `state` null, then forwards
  init-value member reads as the Remember args.
- **Phase 4c** — `[StateHolder(SharedState = true)]` skips the
  `Remember` call when a sibling facade already bound the wrapper's
  `Jvm` field. Use this on every facade that shares a `StateType`
  with another facade — both `TimePicker` and `TimeInput` opt in so
  the user can swap between clock and keyboard entry while keeping a
  single Kotlin state peer; otherwise the second render rebuilds the
  state and loses the entered value. `SearchBar`, `SnackbarHost`,
  `ModalBottomSheet`, `BottomSheetScaffold` still stay hand-written
  — they need either a per-instance `confirmValueChange` veto
  adapter or shared-state semantics beyond what `SharedState` alone
  models (e.g. SearchBar's collapsed-bar + expanded-popup pair also
  needs a hybrid-container generator extension for the expanded
  variants).
- **Phase 6** — `[ComposeFacade(DefaultColorFromTheme = "...")]`
  for drawer sheets and similar containers that fall back to a
  `ColorScheme` slot when the caller doesn't override.
- **Phase 7** — `[PainterResource]` resource-id-style facades
  (`Image`, the Painter overload of `Icon`).
- **Phase 8** — wrapper-passthrough facades for bound bindings the
  facade generator can otherwise call directly. Stack
  `[ComposeFacade]` on a `partial` method on `ComposeBridges` that
  has a hand-written body delegating to a bound `*Kt` method (e.g.
  `BoxKt.Box`, `CheckboxKt.Checkbox`). The wrapper has a
  trailing `int defaults` user param that the facade generator
  fills via the auto-mask and the body passes through to the
  binding's `_changed:` argument. `[ComposeFacade]` recognizes the
  `Defaults = typeof(XDefault)` argument and reads the bit names
  from the matching declarative-form `[assembly: ComposeDefaults(...)]`
  declaration. There is no `[ComposeBridge]` attribute on these
  wrappers; the generator pipeline doesn't see the other generator's
  source-generated enums, so the bit-name map has to come from a
  declarative `ComposeDefaults` declaration (not the generic form).
  Used for `Box`, `Column`, `Row`, `Spacer`, `Checkbox`, `Switch`,
  `RadioButton`, `Slider`, `WideNavigationRailItem`, `TriStateCheckbox`.

  **Java enum primitives** (issue #67): the primitive-ctor slot detector
  also accepts any reference type that derives transitively from
  `Java.Lang.Enum`. The Compose binding generates `@JvmInline`-free
  Kotlin/Java enums (e.g. `AndroidX.Compose.UI.State.ToggleableState`
  with `On`, `Off`, `Indeterminate`) as `Java.Lang.Object` subclasses,
  and these surface as ctor primitives forwarded directly to the
  bridge — no JNI lowering required (see `TriStateCheckbox`).

  **Hybrid container + named slots** (issue #67): a bridge with
  exactly one non-nullable `IFunction3` body **plus** one or more
  nullable `IFunction2?`/`IFunction3?` slots is now allowed when the
  facade declares `[ComposeFacade(Scope = "...")]`. The non-nullable
  Function3 stays as the container body (`RenderChildren`), the
  nullable Function2/3 slots become named `ComposableNode?`
  properties, and the class still derives from `ComposableContainer`
  so caller-side collection-init syntax keeps working. Without
  `Scope`, the bridge still classifies as a multi-slot leaf
  (existing behavior — preserves `AssistChip`-style facades whose
  required Function2 is a label, not a content slot). Used for
  `BottomAppBar`.

  **Container = true** (issue #121): the wrapper-passthrough variant
  of the hybrid-container rule, for bridges whose body slot is a
  non-`@Composable` `IFunction2` rather than a scope-providing
  `IFunction3`. Set `[ComposeFacade(Container = true)]` to force the
  facade to derive from `ComposableContainer` (collection-init
  syntax) and wrap children via `Wrap2(composer, c => RenderChildren(c))`
  on the bridge's body slot. Required by `ModalWideNavigationRail`,
  whose Kotlin signature takes a plain `IFunction2` content lambda.

- **Phase 9** — bridge branching via
  `[ComposeFacade(BranchOn = "Subtitle", AlternateBridge = nameof(AltBridge))]`
  (issue #122). One facade routes between two `[ComposeBridge]`
  methods based on whether a single optional slot is supplied.
  The partial method carries the **primary** (smaller) bridge — no
  branched slot. `AlternateBridge` names a sibling
  `ComposeBridges` method whose user parameters are the primary's
  set plus exactly one extra `IFunction2`/`IFunction3` slot whose
  PascalCased name matches `BranchOn`. Both bridges must declare a
  trailing `int defaults` parameter (the caller-managed-mask shape),
  and each must reference its own `Defaults = typeof(XxxDefault)`
  enum so the per-branch mask can be computed independently.
  The generator emits a single facade that exposes the extra slot
  as a nullable `ComposableNode?` property and renders
  `if (Subtitle is not null) { …alt-bridge call… } else { …primary
  call… }`. Shared lambdas (`__title`, `__navigationIcon`, …) and
  `__modifier = BuildModifier()` are hoisted ABOVE the if/else;
  the branched slot's wrapper, the per-branch mask, and each
  bridge call live INSIDE their respective branches. Parameter
  order may differ between primary and alternate — the emitter
  walks each bridge's actual `Parameters` list (not the slots
  list) to keep arguments correctly positioned. Used for
  `TopAppBar` (branches to `TopAppBarWithSubtitle`), `MediumTopAppBar`
  (→ `MediumFlexibleTopAppBar`), `LargeTopAppBar` (→
  `LargeFlexibleTopAppBar`). Errors are reported as CN3010.

- **Phase 10** — `[ConfirmStateChange(typeof(T))]` on an `IFunction1?`
  parameter of a `[StateHolder]` Remember bridge. Models the
  per-instance JNI veto adapter pattern: Kotlin's
  `rememberDrawerState(confirmStateChange)` (and the rail / sheet
  equivalents) takes a `(T) -> Boolean` callback that's part of the
  `remember` cache key, so the JNI reference identity must stay
  stable across recompositions or the cached state holder is dropped.
  The generator:

  - allocates a single `readonly` JCW adapter field per facade
    instance (default name `_<camelCase(PropertyName)>Adapter`);
  - looks up the adapter class by convention
    `ComposeNet.<TName>ConfirmStateChange`
    (e.g. `DrawerValueConfirmStateChange`), or via an explicit
    `AdapterType = typeof(...)` override;
  - exposes a `Func<T, bool>? ConfirmStateChange { get; set; }`
    property (renameable via `PropertyName = "..."`);
  - in the Render preamble — **before** the Remember call —
    emits `_<adapter>.Callback = ConfirmStateChange;` so a single
    JCW instance dispatches to whatever delegate the user wired
    up this render;
  - excludes the slot from the main bridge call args and the
    `$default` auto-mask, since it only ever flows through Remember.

  The adapter class must implement `Kotlin.Jvm.Functions.IFunction1`,
  have a public parameterless ctor, and declare a writable instance
  property `public Func<T, bool>? Callback { get; set; }`. Used for
  `ModalNavigationDrawer`, `DismissibleNavigationDrawer`,
  `PermanentNavigationDrawer` (no `[ConfirmStateChange]` — drawer
  is always permanent), `ModalWideNavigationRail` (no veto — rail
  has no confirmStateChange in Kotlin).

Facades that still stay hand-written are the ones the generator
can't model:

- Facades that call a bound binding method directly (no underlying
  `[ComposeBridge]` to attach to) — `DropdownMenuItem`, `Icon`
  (`ImageVector` overload). `WideNavigationRailItem` was migrated
  to a Phase 8 wrapper-passthrough in issue #67.
- State-holder facades whose `RememberXxxState` bridge takes user
  parameters AND combine that with a per-instance
  `confirmValueChange` veto adapter:
  `ModalBottomSheet`, `BottomSheetScaffold` (both need a
  `confirmValueChange` adapter on top of parameterised Remember).
  `SearchBar` also stays hand-written until the hybrid-container
  generator extension lands — its expanded variants pair a required
  `inputField IFunction2` slot with a separate `content IFunction3`,
  which the generator can't classify yet. Phase 4 covers the
  zero-user-param shape (`DatePicker`, `DateRangePicker`); Phase 4b
  covers parameterised Remember (`TimePicker`); Phase 4c adds
  shared-state caching for sibling facades (`TimePicker` +
  `TimeInput`); Phase 10 (issue #121) added per-instance
  `confirmStateChange` veto adapters for the drawer / rail family
  via `[ConfirmStateChange(typeof(T))]` — see Phase 10 above. The
  remaining bottom-sheet holdouts need Phase 10 *combined* with
  Phase 4b parameterised Remember, which isn't modelled yet.
- Scope facades whose bodies do non-trivial work beyond
  `RenderContext.PushScope` (`SegmentedButton`,
  `SingleChoice`/`MultiChoiceSegmentedButtonRow`, the segmented
  scrollable tab rows).
- The `Icon` facade, which exposes two distinct constructors
  (`ImageVector` vs. resource-id `Painter`) that route to two
  different bridges. The generator emits one ctor + one `Render`
  per facade, so the dual-shape control stays hand-written.

Trying to apply `[ComposeFacade]` to an unsupported bridge will
emit CN3002 (unsupported parameter), CN3003 (scope misuse), CN3005
(invalid callback type), CN3006 (slot conflict), CN3007 (color
theme binding failed), CN3008 (painter misuse), CN3009
(state-holder misuse), CN3010 (branching misuse), or CN3011
(confirmStateChange misuse) at build time — back out the attribute
and write the facade by hand.

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
   verify the bridge + facade compile together. The supported
   shapes are validated at build time; CN3001-CN3011 will fire if
   the generator can't accept the bridge.
5. **If CN3002 fires**, the bridge has a parameter outside the
   table above — either an unmarked `IFunction1` callback, a
   value-class handle, or some other unsupported type. Either add
   the right marker attribute (`[Callback]` / `[Slot]` /
   `[PainterResource]`) or back out the attribute and write the
   facade by hand (delete the stub or expand it into a full
   hand-written class).
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
| CN3005  | `[Callback(typeof(T))]` target type is unsupported (must be `bool`, `string`, or `float`). |
| CN3006  | `[Slot]` placement conflicts with the facade's classified shape, `[Callback]` placed on a non-`IFunction1` param, multiple `[PainterResource]` params on one bridge, or `int defaults` declared without a resolvable `Defaults` enum. |
| CN3007  | `DefaultColorFromTheme` cannot bind to any `long` user param (or `ColorParameter` is ambiguous / missing). |
| CN3008  | `[PainterResource]` annotates a non-`IntPtr` parameter.            |
| CN3009  | `[StateHolder]` is invalid: applied to a non-`IntPtr` param, combined with `[PainterResource]`, missing or unidentifier-valued `Remember` / `StateType`, the named `Remember` method is not a static `(IComposer) -> IntPtr` on `ComposeBridges`, or `StateType` has no accessible writable instance field named `Jvm`. |
| CN3010  | `BranchOn` / `AlternateBridge` is invalid: only one of the two is set, primary or alternate is missing a trailing `int defaults` parameter, the named alternate is not resolvable or ambiguous on `ComposeBridges`, alternate is not a strict superset (missing a primary param or has more than one extra), the extra param's PascalCased name doesn't match `BranchOn`, the extra param isn't `IFunction2`/`IFunction3`, a shared param has incompatible types, branching is used on a hybrid container shape, or the alternate has no resolvable `[ComposeBridge].Defaults` enum. |
| CN3011  | `[ConfirmStateChange(typeof(T))]` is invalid: not on an `IFunction1` param, missing the `typeof(T)` ctor argument, the convention adapter class `ComposeNet.<TName>ConfirmStateChange` is missing (set `AdapterType = typeof(...)` to override), the adapter doesn't implement `Kotlin.Jvm.Functions.IFunction1`, lacks a public parameterless ctor, or has no writable `Callback` property of type `System.Func<T, bool>?`. |

### Migration rule

When you add `[ComposeFacade]` to a previously hand-written facade,
**replace the hand-written file** with a 3-line stub
(`namespace ComposeNet; /// <summary>…</summary> public sealed
partial class <Name>;`) in the same commit. The sibling stub is
required (it owns the XML docs) but must not redeclare members the
generator emits — otherwise you'll see duplicate-member errors.

### ⚠️ DO NOT demote a generated facade to hand-written

**This is a critical rule with no exceptions in normal work.** If a
facade is currently `[ComposeFacade]`-generated and you discover the
generator can't model a new shape you need (extra ctor, branched
Render path, new slot kind, etc.), the answer is **always to extend
the generator**, never to delete `[ComposeFacade]` and re-implement
the facade by hand. The hand-written code immediately loses the
guarantees the generator provides (slot-key stability via
`ComposableLambdas.Wrap*`, correct `$default` mask construction,
consistent disposal ordering, future-proofing when the generator
gains new features), and creates an orphaned hand-written facade
that future contributors will mistake for a "this can't be
generated" example.

Concretely, when faced with "I need feature X that the generator
doesn't emit":

1. **Add a new slot kind, attribute, or codegen branch** to
   `ComposeFacadeGenerator` / `ComposeBridgeGenerator` /
   `ComposeDefaultsGenerator`. Phases 1–10 in this document are
   precedents for how to scope and name a new shape.
2. **Add a generator test** for the new shape in
   `ComposeNet.SourceGenerators.Tests`. This is how every prior
   phase landed; skipping it makes future regressions invisible.
3. **Document the new shape** in the "Phase N" table above and add
   any new CN3xxx diagnostics to the diagnostics table.
4. **Apply the new attribute/argument** to the existing facade.
   The hand-written sibling stub stays a one-liner.

The only legitimate cases for hand-written facades are documented
in the "Facades that still stay hand-written" list — they're shapes
that genuinely cannot be modelled (multi-ctor `Icon` routing to two
different bridges, scope facades with non-trivial scope-management
logic, state-holders that combine parameterised Remember *with*
per-instance confirm adapters, etc.). If you think a new facade
belongs on that list, propose the addition explicitly and justify
it — don't demote silently.

Reverse rule (rare): if you ever need a fully custom `Render()`
body that genuinely can't be expressed in the generator, remove
the `[ComposeFacade]` attribute first, expand the stub into a
normal hand-written class, **and add the facade to the
"hand-written holdouts" list above with a one-sentence reason**.
This requires user approval; do not do it on autopilot.

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
  in `composer.StartReplaceableGroup(HashCode.Combine(i, child.GetType()))`
  / `EndReplaceableGroup()`. Any custom loop that calls
  `Children[i].Render(c)` directly — e.g. the segmented-row scope loops
  in `SingleChoiceSegmentedButtonRow`, `MultiChoiceSegmentedButtonRow`,
  or `SegmentedButton`'s label slot — must do the same, otherwise same-
  type siblings collide on a single group key and Compose disambiguates
  by position only (brittle combined with any identity churn). The
  type component of the key is what stops a sibling that swaps
  subclass-at-the-same-position (e.g. tab navigation flipping a
  `PullToRefreshBox` for a `HorizontalUncontainedCarousel`) from re-
  entering the prior occupant's group and reading its slot-table
  entries — that path throws `ClassCastException` from inside
  Compose's `rememberSaveable` (`SaverKt$Saver$1 cannot be cast to
  SaveableHolder`). Same-typed siblings at the same position keep
  their identity and slot state intact — that is intentional Compose
  positional identity.
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

The pattern (see `DrawerValueConfirmStateChange` for the canonical
adapter implementation):

1. **For new facades, prefer the generator path.** Annotate the
   Remember bridge's `IFunction1?` `confirmStateChange` (or
   `confirmValueChange`) parameter with
   `[ConfirmStateChange(typeof(T))]` and stack `[ComposeFacade]` on
   the facade bridge — Phase 10 above handles the adapter field,
   property emission, and preamble `Callback` assignment for you.
   Use this for any facade that fits the supported state-holder
   shapes (Phase 4 zero-arg Remember, Phase 4b parameterised
   Remember without combined veto).
2. **Hand-written holdouts** (`ModalBottomSheet`, `BottomSheetScaffold`)
   still follow the same conventions, just written by hand:
   - **Expose the hook as a `Func<T, bool>?` property** on the
     facade type, defaulting to `null` (= "use Kotlin's default —
     always allow"). Document what `false` means (veto / block
     transition).
   - **Allocate the JNI adapter once per node instance** as a
     `readonly` field. Never `new` it inside `Render` — that
     recreates the Java peer on every recomposition and invalidates
     the `remember` key.
   - **Read the delegate inside the adapter's `Invoke`** (not at
     adapter construction), so the developer can mutate the
     property between renders without re-allocating.
   - **Treat `null` as "always true"** inside the adapter — no
     separate singleton fallback is needed; the adapter is already
     allocated, and a single branch in `Invoke` keeps the JNI
     reference identical whether or not the developer wired up a
     callback.

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

## Suspend / async bridges

Use this path when a Kotlin Compose API exposes a `suspend` function
(e.g. `ScrollState.scrollTo`, `LazyListState.animateScrollToItem`,
`SnackbarHostState.showSnackbar`, `DrawerState.open`) and the C# facade
should surface it as `Task` / `Task<T>`.

`SuspendBridge.Invoke` allocates one `SuspendContinuation`, calls the
raw JNI bridge, and completes the returned task from either the
synchronous result or the later Kotlin resume. `SuspendContinuation` is
an internal JCW registered as `composenet/compose/SuspendContinuation`;
it self-roots with a strong `GCHandle` per call and its `Context`
returns `AndroidUiDispatcher.Main`, which supplies the
`MonotonicFrameClock` required by animation suspends that use
`withFrameNanos`.

### Adding a new `*Async` method

1. Add a hand-written bridge to `SuspendBridges.cs`. It returns the
   raw `IntPtr` that Kotlin returns: either the `COROUTINE_SUSPENDED`
   sentinel or a synchronous boxed result.

   Instance suspend method template:
   ```csharp
   static IntPtr s_myStateDoThing_class;
   static IntPtr s_myStateDoThing_method;

   internal static unsafe IntPtr MyStateDoThing(
       IntPtr state, int value, SuspendContinuation cont)
   {
       if (s_myStateDoThing_method == IntPtr.Zero)
       {
           s_myStateDoThing_class = JNIEnv.FindClass("my/package/MyState");
           s_myStateDoThing_method = JNIEnv.GetMethodID(
               s_myStateDoThing_class,
               "doThing",
               "(ILkotlin/coroutines/Continuation;)Ljava/lang/Object;");
       }

       try
       {
           JValue* args = stackalloc JValue[2];
           args[0] = new JValue(value);
           args[1] = new JValue(cont.Handle);
           return JNIEnv.CallObjectMethod(state, s_myStateDoThing_method, args);
       }
       finally
       {
           GC.KeepAlive(cont);
       }
   }
   ```

   Static extension / synthetic `$default` template:
   ```csharp
   internal static unsafe IntPtr MyStateAnimate(
       IntPtr state, int value, SuspendContinuation cont)
   {
       if (s_myStateAnimate_method == IntPtr.Zero)
       {
           s_myStateAnimate_class = JNIEnv.FindClass("my/package/MyStateKt");
           s_myStateAnimate_method = JNIEnv.GetStaticMethodID(
               s_myStateAnimate_class,
               "animate$default",
               "(Lmy/package/MyState;ILmy/package/AnimationSpec;" +
               "Lkotlin/coroutines/Continuation;ILjava/lang/Object;)Ljava/lang/Object;");
       }

       try
       {
           JValue* args = stackalloc JValue[6];
           args[0] = new JValue(state);
           args[1] = new JValue(value);
           args[2] = new JValue(IntPtr.Zero); // AnimationSpec — defaulted
           args[3] = new JValue(cont.Handle);
           args[4] = new JValue(0b010);       // $default mask
           args[5] = new JValue(IntPtr.Zero); // synthetic marker
           return JNIEnv.CallStaticObjectMethod(
               s_myStateAnimate_class, s_myStateAnimate_method, args);
       }
       finally
       {
           GC.KeepAlive(cont);
       }
   }
   ```

2. Add the public facade method and route the bridge through
   `SuspendBridge.Invoke`:
   ```csharp
   public Task<float> DoThingAsync(int value) =>
       SuspendBridge.Invoke<float>(
           cont => ComposeBridges.MyStateDoThing(
               ((Java.Lang.Object)Jvm).Handle, value, cont),
           static boxed => boxed is Java.Lang.Float f
               ? f.FloatValue()
               : throw new InvalidCastException(
                   $"Expected java.lang.Float; got '{boxed?.Class?.Name ?? "null"}'"));

   public Task AnimateAsync(int value) =>
       SuspendBridge.Invoke(cont =>
           ComposeBridges.MyStateAnimate(
               ((Java.Lang.Object)Jvm).Handle, value, cont));
   ```
3. Add the new public member(s) to `PublicAPI.Unshipped.txt`.

### Conventions and footguns

- Bridges **must** return raw `IntPtr` and work in raw handles
  end-to-end. Do not wrap `COROUTINE_SUSPENDED` with
  `Java.Lang.Object.GetObject(..., TransferLocalRef)` — Mono's peer
  cache resolves Kotlin singletons to globally-ref-backed wrappers that
  crash CheckJNI when disposed later.
- Do not wrap `JNIEnv.FindClass` results in `NewGlobalRef` /
  `DeleteLocalRef`; Mono.Android already returns stable, globally
  registered class refs.
- For instance-method bridges, the facade passes
  `((Java.Lang.Object)Jvm).Handle` as the receiver; the bridge's first
  `IntPtr` parameter is that receiver.
- For Kotlin extension functions / `$default` synthetic overloads, the
  JVM signature is `(receiver, ...userParams, AnimationSpec,
  Continuation, int $default, Object marker)`. The marker is always
  `IntPtr.Zero`; the `$default` mask has bits set for defaulted
  parameters.
- The `unbox` lambda receives the boxed `Java.Lang.Object?` success
  value Kotlin handed back. Use `Java.Lang.Integer.IntValue()`,
  `Java.Lang.Float.FloatValue()`, etc. for boxed primitives; use the
  non-generic `Invoke` overload for Kotlin `Unit` results.
- Callers handle failures with normal `try` / `catch` around `await`.
  `SuspendBridge` detects `kotlin.Result$Failure` and faults the task
  with the underlying `Throwable` automatically.
- Allocate one JCW continuation per call. Never reuse
  `SuspendContinuation` instances.

Do not add a `[ComposeBridge]` generator path yet. v1 intentionally
uses hand-written bridges for the two ScrollState suspend calls; once a
third suspend API is needed, formalise the pattern as
`ComposeBridgeAttribute(Suspend = true)` instead of copying more
boilerplate.

The `Why raw JNI` comments in `SuspendBridges.cs` and `SuspendBridge.cs`
explain the current `dotnet/java-interop#1440` dependency and what can
be replaced with cleaner binding calls after the upstream binder fixes
land. Check those comments before assuming a binding is truly missing.

## Central package versioning

All `<PackageReference>` items in this repo are **versionless** at the
project level. Versions live in `Directory.Build.targets` as
`<PackageReference Update="..." Version="..." />` items so the entire
repo stays on a consistent set of dependency versions.

When adding a new package dependency:

1. Add a versionless `<PackageReference Include="..." />` (plus any
   metadata like `PrivateAssets="All"`) in the project file.
2. Add a matching `<PackageReference Update="..." Version="..." />`
   in `Directory.Build.targets`, grouped under the appropriate
   comment banner (Roslyn analyzers / Kotlin / AndroidX core /
   AndroidX Compose / etc.). Add a new banner if none fits.

Do not pin a `Version` attribute on `<PackageReference Include>` in
the project file — that bypasses central versioning and is the
opposite of what reviewers want.

## Public API tracking

`ComposeNet.Compose` has `Microsoft.CodeAnalysis.PublicApiAnalyzers`
wired up. Every public symbol must be listed in
`src/ComposeNet.Compose/PublicAPI.Shipped.txt` (released surface) or
`src/ComposeNet.Compose/PublicAPI.Unshipped.txt` (pending the next
release). The build fires `RS0016` for any public symbol that isn't
in either file, and `RS0017` for entries in the file that no longer
exist in source.

When you add or change public API:

1. Build `src/ComposeNet.Compose` and note the `RS0016`/`RS0017`
   warnings. The IDE code fix can update `Unshipped.txt` for you,
   **or**
2. Run from the repo root:
   ```pwsh
   dotnet format analyzers src/ComposeNet.Compose --diagnostics RS0016 RS0017 --severity warn
   ```
   This rewrites `PublicAPI.Unshipped.txt` to match the current
   surface.
3. **`dotnet format` skips source-generated files** (e.g. the
   `[ComposeFacade]` ctors and the Android `Resource` class under
   `obj/`). After running it, rebuild and add any remaining `RS0016`
   entries by hand — the warning message contains the exact line to
   append (`Symbol '<entry>' is not part of the declared public API`).
4. Sort `PublicAPI.Unshipped.txt` and keep the leading
   `#nullable enable` line.

On release, move entries from `Unshipped.txt` to `Shipped.txt`.
Removing or renaming a public symbol is a breaking change — prefer
adding new overloads and obsoleting the old ones.

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
- Public API gets XML doc comments. This is non-negotiable for **any
  new public type** — whether it's a hand-written class (like
  `ScrollState`, `SnackbarHostState`), a state holder, a Modifier
  extension method, or the sibling stub for a `[ComposeFacade]`. Every
  new `public sealed class`, `public sealed partial class`, public
  constructor, public method, and public property gets a
  `<summary>` (and a `<remarks>` block when there's nuance worth
  capturing — e.g. lifecycle, threading, "not yet bound"). Internal
  helpers get a one-line `//` comment when they're non-obvious;
  otherwise leave them bare.
- Don't add markdown planning docs to the repo — use the session
  artifact folder.
- Commit trailer: `Co-authored-by: Copilot <223556219+Copilot@users.noreply.github.com>`.
