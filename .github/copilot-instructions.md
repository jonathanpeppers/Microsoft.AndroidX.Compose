# Microsoft.AndroidX.Compose — agent instructions

C#-only .NET-for-Android app hosting Jetpack Compose UI via official
`Xamarin.AndroidX.Compose.*` bindings. No Kotlin sources, no custom bindings,
no `[InterceptsLocation]`. Every C# composable either calls a generated binding
or a JNI bridge in `ComposeBridges.cs`.

See `README.md`, `docs/architecture.md`, `docs/compose-internals.md`,
`docs/NOTES.md` for background. This file is the rule set agents **must**
follow.

## Layout

- `src/Microsoft.AndroidX.Compose/` — public C# facade. **One class per file**, named
  after the class. `ComposeBridges.cs` holds raw-JNI bridges;
  `ComposeDefaults.cs` holds *only* assembly attributes driving the generator.
- `src/Microsoft.AndroidX.Compose.SourceGenerators/` — Roslyn incremental generators
  (`ComposeDefaultsGenerator`, `ComposeBridgeGenerator`,
  `ComposeFacadeGenerator`) emitting `[Flags]` enums for Kotlin `$default`
  bitmasks, JNI bridge bodies, and facade classes.
- `src/Microsoft.AndroidX.Compose.SourceGenerators.Tests/` — xUnit, no Android SDK needed.
- `src/Microsoft.AndroidX.Compose.Gallery/` — runnable Android app (on-device demo harness).

## Build / test / run

```pwsh
dotnet test  src/Microsoft.AndroidX.Compose.SourceGenerators.Tests   # generator unit tests
dotnet build src/Microsoft.AndroidX.Compose                  # facade only
dotnet build src/Microsoft.AndroidX.Compose.Gallery                  # full Android build (needs android workload)
dotnet build src/Microsoft.AndroidX.Compose.Gallery -t:Run           # deploy to device
```

Run generator tests on any change to `Microsoft.AndroidX.Compose.SourceGenerators` or
`ComposeDefaults.cs`. Run the facade build on any change to
`Microsoft.AndroidX.Compose`.

## Generated `$default` enums — DO NOT hand-roll

Every `@Composable` takes a trailing `int $default` bitmask: bit *N* set =
"param *N* not supplied; use Kotlin default." A `[Flags]` enum names each bit
so call sites read `(int)ButtonDefault.All` instead of magic numbers.

**Always generate via `ComposeDefaultsAttribute` in `ComposeDefaults.cs`. Never
hand-write `[Flags] enum FooDefault`.**

### Generic form — preferred when Kt method is bindable

```csharp
[assembly: ComposeDefaults<ColumnKt>("Column", "ColumnDefault")]
```

Generator picks the longest static overload, walks params up to first
`IComposer`, names each bit after the param (PascalCased), skips
`Kotlin.Jvm.Functions.IFunction*` slots (content lambdas — always provided),
emits an `All` constant.

### Declarative form — when Kt method is stripped

The binder strips overloads with mangled JVM names (`Text--4IGK_g`,
`Surface-T9BRK9s`, …) from Kotlin `@JvmInline value class` params (`Color`,
`Dp`, `TextUnit`, `FontWeight`, …). Until [dotnet/java-interop#1440] lands,
hand the generator the Kotlin parameter names:

```csharp
[assembly: ComposeDefaults("ButtonDefault",
    "!onClick", "modifier", "enabled", "shape", "colors",
    "elevation", "border", "contentPadding", "interactionSource", "!content")]
```

- Each name occupies one bit at its positional index.
- `!` prefix consumes the bit but emits no enum member (params the caller
  always provides — `onClick`, content lambdas, required values).
- Keep optional slot lambdas the caller toggles per-call (e.g. `AlertDialog`'s
  `dismissButton`/`icon`/`title`/`text`) *as enum members* — the call site
  clears the bit when supplying the slot.

### Wide masks (> 31 slots) — `: long` + `Split` helper

Kotlin lowers `@Composable` with > 32 defaultable params into a pair of `int`
`$default` slots (`II` in signature) — e.g.
`androidx.compose.material3.lightColorScheme` has 48 slots. When the slot list
exceeds 31 entries the generator switches to a `long`-backed enum and emits a
`Split` extension returning `(int Mask0, int Mask1)`:

```csharp
[Flags]
internal enum ColorSchemeDefault : long
{
    None = 0, Primary = 1L << 0, /* … */ Slot47 = 1L << 47,
    All  = Primary | /* … */,
}

internal static class ColorSchemeDefaultExtensions
{
    public static (int Mask0, int Mask1) Split(this ColorSchemeDefault value) =>
        ((int)((long)value & 0xFFFFFFFFL), (int)(((long)value >> 32) & 0xFFFFFFFFL));
}
```

Call sites build the mask normally (`defaults |= ColorSchemeDefault.Primary`)
and `.Split()` immediately before passing the two ints to JNI — never hand-roll
`1 << N` or the low/high split. Threshold is 32 slots (not 33) so bit 31 lands
as `1L << 31` rather than `int.MinValue`. ≤ 31 slots stay byte-for-byte
identical to pre-wide output. > 63-bit masks not modelled — add `ulong` if/when
a Compose API needs them.

When the upstream binder fix lands, swap a declarative attribute to generic
form one-for-one.

[dotnet/java-interop#1440]: https://github.com/dotnet/java-interop/pull/1440

### Generator diagnostics

| ID     | Meaning                                              |
|--------|------------------------------------------------------|
| CN1001 | Generic form: named static method not found on `T`.  |
| CN1002 | Generic form: method has no `IComposer` parameter.   |
| CN1003 | Either form: attribute arguments couldn't be read.   |

Tests live in `GeneratorTests.cs` — synthetic compilations, no Android refs.
**Add a test for any new generator behaviour.**

## `ComposeBridges.cs` — source-generated JNI bridges

When an overload is stripped from the binding, call it via JNI. Boilerplate
(class/method-id cache, lazy `FindClass`/`GetMethodID`, `JValue` array fill,
`$default` mask, `try`/`finally` + `GC.KeepAlive`, `NewString`/`DeleteLocalRef`
for strings) is emitted by `ComposeBridgeGenerator` from a one-line declaration.
**Do not hand-write the body.**

### Adding a bridge

1. Add `[ComposeBridge(...)]` + `public static partial` method to
   `ComposeBridges.cs`. Shape rules:
   - **`@Composable`**: `composer` **must** be the last C# parameter.
   - **Non-`@Composable` Kotlin extension with `$default`** (e.g.
     `Modifier.background`/`clickable`): no Composer slot; first `IntPtr` user
     param is the extension receiver.
   - **Plain Kotlin static** (no Composer, no `$default`, e.g.
     `Modifier.padding`, `RoundedCornerShape`): positional. First user param is
     receiver iff it's `IntPtr` AND the first JNI sigParam is `L`.
   - **Plain Kotlin instance** (no Composer, no `$default`): set
     `Instance = true`; first C# param must be the `IntPtr` dispatch receiver
     and is excluded from the JNI signature/JValue array. Generator emits
     `GetMethodID` + the return-type-specific `Call*Method`.
   - **Stripped Kotlin constructor**: set `JvmName = "<init>"`; generator emits
     `GetMethodID` + `NewObject` and wraps the handle via
     `Java.Lang.Object.GetObject<TReturn>(.., TransferLocalRef)`. Signature
     must end `V` (JVM ctor); C# return must be non-void. Cannot declare
     `Composer`, `Defaults`, or `InstanceField` (CN2006).
2. **If** the `@Composable` has ≥ 1 defaultable Kotlin param (trailing
   `$default` `I` slot after `$changed`), set `Defaults = typeof(XxxDefault)`
   and add matching `[assembly: ComposeDefaults(...)]`. Otherwise omit —
   generator infers from signature and emits CN2005 on mismatch.
   Non-`@Composable` extensions with `$default` always require `Defaults`.

Canonical example (`@Composable` with `$default`):

```csharp
[ComposeBridge(Class = "androidx/compose/material3/ButtonKt",
               JvmName = "Button-bWB7cM8",
               Signature = "(Lkotlin/jvm/functions/Function0;...)V",
               Defaults = typeof(ButtonDefault))]
public static partial void Button(IFunction0 onClick, IModifier? modifier, bool enabled,
                                  /* ... */ IFunction3 content, IComposer composer);
```

Other shapes (no `$default`, non-`@Composable` extension with `$default`, plain
static, stripped constructor) follow the same `[ComposeBridge]` declaration
pattern; see `ComposeBridges.cs` for live examples (`PainterResource`,
`ModifierBackground`, `ModifierPaddingAll`, `GridCellsAdaptive`).

### Conventions the generator relies on

- `composer` last for `@Composable`; absent for non-`@Composable`.
- Add `int defaults` at end of user params (just before `composer` for
  `@Composable`, last for extension shapes) **only** when caller controls the
  bitmask (state-holders, multi-slot composables toggling bits per call).
  Otherwise omit — generator auto-builds the mask: one bit per
  nullable/optional C# param the caller passed `null`. **Only valid when
  bridge has a `$default` slot.**
- Kotlin extension receivers on `@Composable`: `IntPtr` with name ending
  `Scope` (e.g. `IntPtr rowScope`). Non-`@Composable` `$default` extensions:
  receiver = first `IntPtr` user param. Plain static extensions: first user
  param is receiver iff `IntPtr` AND first JNI sigParam is `L`. Receiver lands
  at `args[0]`, excluded from `$default` count.
- `IModifier?` → `ComposeBridges.ModifierHandle` (`null` → `IntPtr.Zero`).
  `IntPtr?` → `null` → `IntPtr.Zero`; auto-mask clears its `$default` bit only
  when non-null.
- String params hoisted into `IntPtr __ref_<name>`, freed in generated
  `finally`.
- Non-void return (state holders): generator emits
  `return CallStaticObjectMethod(...)` inside `try`/`finally`.
- No-`$default` bridges: user params match JNI slots **positionally**; C#
  parameter order must match Kotlin bytecode order.

### Still hand-written

`ModifierHandle` (managed `IModifier? → IntPtr`), `ModifierCompanionInstance`
(static field lookup), `ModifierClipRoundedCorners` (two-step
`RoundedCornerShape` + `ClipKt.clip` with intermediate `Shape` local ref) don't
fit any `[ComposeBridge]` shape.

### Generator diagnostics

| ID     | Meaning                                                                                              |
|--------|------------------------------------------------------------------------------------------------------|
| CN2001 | `[ComposeBridge]` missing/unmatched `Defaults` enum.                                                 |
| CN2002 | Bridge signature/`Defaults` parameter count disagree.                                                |
| CN2003 | Bridge partial-method param doesn't match any Kotlin name.                                           |
| CN2004 | `[ComposeBridge]` has a malformed JNI signature.                                                     |
| CN2005 | `Defaults` disagrees with the JNI `$default` slot.                                                   |
| CN2006 | Constructor bridge shape requirements not met.                                                       |
| CN2007 | Recognized Compose value type used on a no-`$default` bridge.                                        |
| CN2008 | Value-type parameter lowers to a JNI slot that doesn't match the bridge signature at that position.  |
| CN2009 | `[ComposeBridge(Suspend = true)]` configuration is invalid (missing/misplaced `IContinuation`, wrong return, etc.). |
| CN2010 | `[ComposeBridge]` declares an `int _changed` parameter but the JNI signature has no `$changed` slot (only valid on `@Composable` bridges). |
| CN2011 | `[ComposeBridge(Instance = true)]` configuration is invalid (missing receiver or incompatible constructor/suspend/singleton/default shape). |

**When adding a new diagnostic, update this table (and CN1xxx if relevant).
Source of truth: `src/Microsoft.AndroidX.Compose.SourceGenerators/Diagnostics.cs`.**

**Don't add `[ComposeBridge]` if the binding already exposes the method** —
call the generated C# entry point instead. **Probe the runtime companion
DLL** (`Xamarin.AndroidX.Compose.<Module>.Android.dll`, not the small
façade `…<Module>.dll`) before concluding a symbol isn't bound. See
**Bindings policy** below.

### Compose value types — `@JvmInline value class` lowering

Generator registry at `src/Microsoft.AndroidX.Compose.SourceGenerators/ComposeValueTypes.cs`:

| C# type                 | JNI slot | Lowering                          |
|-------------------------|----------|-----------------------------------|
| `Microsoft.AndroidX.Compose.Dp?`        | `F`      | `Dp.Pack(x)`                      |
| `Microsoft.AndroidX.Compose.Sp?`        | `J`      | `Sp.Pack(x)` (TextUnit, type=0x1) |
| `Microsoft.AndroidX.Compose.TextAlign?` | `I`      | `TextAlign.Pack(x)`               |

Recognition keys on **`Nullable<T>`** of the registered type — bare
non-nullable not recognized. Integrates with auto-default-mask: non-`null`
clears its `$default` bit; `null` leaves it set.

`androidx.compose.ui.graphics.Color` is intentionally NOT registered — it
surfaces as a packed `long` once `Xamarin.AndroidX.Compose.UI.Graphics` is
referenced; build via `AndroidX.Compose.UI.Graphics.ColorKt.Color(r, g, b, a)`
and pass `long` through. Reference-typed Compose wrappers (`FontWeight`,
`TextDecoration`, `Shape`) also NOT registered — they go through the generic
"reference-type → handle with null check" path (subclass `Java.Lang.Object`).

Adding a new value type: append entry to `ComposeValueTypes.Recognized`,
optionally a `Pack(T?)` static helper, plus a generator test in
`BridgeGeneratorTests.cs`.

## Facade generator — `[ComposeFacade]`

`Render()` body for ~17 of the simplest user-facing facades is pure mechanical
boilerplate. `ComposeFacadeGenerator` emits the whole facade class (base,
fields, ctor, `Render`) from the bridge's parameter shape. Add `[ComposeFacade]`
next to `[ComposeBridge]` plus a tiny sibling stub that owns XML docs.

```csharp
// In ComposeBridges.cs
[ComposeBridge(/* … */)]
[ComposeFacade]
public static partial void Button(IFunction0 onClick, IModifier? modifier,
                                  IFunction3 content, IComposer composer);

// In Button.cs (sibling stub — owns docs)
namespace Microsoft.AndroidX.Compose;

/// <summary>Material 3 filled <see cref="Button"/>.</summary>
public sealed partial class Button;
```

Generator does **not** emit a `<summary>`; the stub is the canonical place for
prose docs (cref links, `<code>`, `<remarks>` — none survive attribute
round-trip). The stub is also the override hatch: extra ctor overloads,
helpers, operators.

### Param classification

| Param type                                                                    | Treatment                                                                                                                                                                                  |
|-------------------------------------------------------------------------------|--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `AndroidX.Compose.UI.IModifier?`                                              | Passed as `BuildModifier()` — no ctor param                                                                                                                                                |
| `IFunction0` (onClick-style)                                                  | `System.Action` ctor parameter                                                                                                                                                             |
| `IFunction1` + `[Callback(typeof(T))]`                                        | `Action<T>` ctor; `T` ∈ {`bool`, `string`, `float`}                                                                                                                                        |
| `IFunction2` content (non-nullable, sole content slot)                        | `ComposableLambdas.Wrap2(…)` — container shape                                                                                                                                             |
| `IFunction3` content (non-nullable, sole content slot)                        | `ComposableLambdas.Wrap3(…)` — container shape                                                                                                                                             |
| `IFunction2?` / `IFunction3?` (any nullable, OR `[Slot]`, OR >1 content slot) | Named `ComposableNode?` property — multi-slot leaf                                                                                                                                         |
| `IntPtr` with name ending `Scope`                                             | Kotlin extension receiver; auto-bound to `RenderContext.CurrentScope` (no ctor slot)                                                                                                       |
| `IntPtr` + `[PainterResource]`                                                | Synthetic `int painterResourceId` ctor arg + `PainterResource` resolution + try/finally                                                                                                    |
| `IntPtr` + `[StateHolder(Remember = …, StateType = typeof(…))]`               | State-holder (Phase 4): exposes wrapper as defaulted ctor slot (`StateType? state = null`), calls `RememberXxxState(composer)` on first render, populates `state.Jvm`, forwards JNI handle |
| Primitive (`string`/`int`/`long`/`bool`/`float`/`double`)                     | Ctor parameter, stored in `_<name>`                                                                                                                                                        |
| Anything else                                                                 | Rejected with CN3002                                                                                                                                                                       |

### Class-level options

- `Scope = "Row"`/`"Column"`: when content is `IFunction3`, generator passes
  its first arg into `RenderContext.PushScope(scope, ScopeKind.Row|Column)`.
- `DefaultColorFromTheme = "secondaryContainer"` (Phase 6): matching `long`
  user param becomes a `ContainerColor` property; render falls back to
  `MaterialTheme.Instance.GetColorScheme(composer, 0).<Slot>` when caller
  leaves it `0L`. Use `ColorParameter` to disambiguate when bridge has > 1
  `long` user param.
- `BranchOn = "Subtitle", AlternateBridge = nameof(AltBridge)` (Phase 9): see
  Phase 9 below.
- `Container = true` (Phase 8 variant): see Phase 8 below.
- `ImplicitComposer = true`: emits an additional Tier 2 overload with no
  `IComposer` parameter. Its body reads `ComposableContext.Current`, and
  composable content slots surface as `Action` instead of
  `Action<IComposer>`. Used by the composerless `Text` and `Button`
  prototype APIs.

### Parameter-level attributes

- `[Slot("PropertyName")]` — rename the generated property for an
  `IFunction2`/`IFunction3` slot. Also forces multi-slot shape with one
  content lambda.
- `[Callback(typeof(T))]` — surface `IFunction1` as typed `Action<T>` ctor
  slot. `T` ∈ {`bool`, `string`, `float`}.
- `[FacadeDefault(value)]` — give a primitive generated-facade constructor
  slot a C# default while keeping the bridge parameter and trailing
  `IComposer` required. Use this instead of making bridge parameters optional;
  it avoids `composer = null!` solely for C# optional-parameter ordering.
- `[PainterResource]` — annotate `IntPtr` taking the resolved Painter handle.
  Facade exposes synthetic `int painterResourceId` ctor in its place; emits
  `PainterResource(id, composer)` + try/finally + `DeleteLocalRef` preamble.
- `[StateHolder(Remember = nameof(ComposeBridges.X), StateType = typeof(T))]`
  — `IntPtr` carrying a Kotlin state-holder handle. Facade gains defaulted
  ctor slot `T? state = null`, calls named Remember, populates `state.Jvm` via
  `Java.Lang.Object.GetObject<TJvm>(handle, JniHandleOwnership.DoNotTransfer)`,
  forwards handle to this param.

  Three sub-shapes:
  - **Phase 4** — zero-user-param Remember (`(IComposer) -> IntPtr`, e.g.
    `RememberDatePickerState`). `_state` stays nullable; `Jvm` populated only
    when caller supplies non-null wrapper.
  - **Phase 4b** — parameterised Remember (`(arg1, …, IComposer) -> IntPtr`,
    e.g. `RememberTimePickerState(int initialHour, int initialMinute, bool
    is24Hour, IComposer)`). Each Remember user param resolves to a readable
    instance member on `StateType` by PascalCase first (`initialHour` →
    `InitialHour`), then `Initial<PascalCase>` fallback (`is24Hour` →
    `Is24Hour` first, else `InitialIs24Hour`). `_state` auto-created in ctor;
    guaranteed non-null in `Render`; `Jvm` population unguarded. The
    PascalCase-first rule lets a wrapper's live getter that falls back to its
    initial-value backing field (`Is24Hour => Jvm?.Is24hour() ?? InitialIs24Hour`)
    be the natural match. Requires `StateType` constructible with no args.
  - **Phase 4c** — `SharedState = true` opts in to shared-state caching for
    sibling facades sharing a `StateType` (e.g. `TimePicker` + `TimeInput`).
    Render preamble first checks `_state.Jvm` and reuses the cached JNI handle
    when present; only first-render calls `Remember`. Both Phase 4 and 4b
    honour the flag.

  `StateType` must declare an instance, writable, non-readonly, accessible
  field named `Jvm` whose declared type is the binding-generated state
  interface. Cannot combine with `[PainterResource]`.

- `[ConfirmStateChange(typeof(T))]` (Phase 10) — `IFunction1?` param of a
  `[StateHolder]` Remember bridge. Models per-instance JNI veto adapter for
  Kotlin's `(T) -> Boolean` callback (part of `remember` cache key). Generator:
  - allocates one `readonly` JCW adapter field per facade instance
    (`_<camelCase(PropertyName)>Adapter`);
  - looks up adapter class by convention `Microsoft.AndroidX.Compose.<TName>ConfirmStateChange`
    or via explicit `AdapterType = typeof(...)`;
  - exposes `Func<T, bool>? ConfirmStateChange { get; set; }` (renameable via
    `PropertyName`);
  - in Render preamble — **before** Remember — emits
    `_<adapter>.Callback = ConfirmStateChange;`;
  - excludes the slot from main bridge call args and auto-mask.

  Adapter class must implement `Kotlin.Jvm.Functions.IFunction1`, have a
  public parameterless ctor, and declare a writable
  `public Func<T, bool>? Callback { get; set; }`.

Each facade emitted to a unique hint name (`Microsoft.AndroidX.Compose.Facade.<ClassName>.g.cs`)
so `[CallerFilePath]` + `[CallerLineNumber]` slot keys inside
`ComposableLambdas.Wrap*` stay distinct per facade.

### Phase index (1, 2, 3, 4, 6, 7, 8, 9, 10)

- **Phase 1** — container + content lambda + ctor primitives (`Button`,
  `Card`, `Text`, `Column`, `Row`, `Box`).
- **Phase 2** — `[Callback(typeof(T))]` for `IFunction1` (`TextField`,
  `OutlinedTextField`, `IconToggleButton` family).
- **Phase 3** — multi-slot leaf with named `ComposableNode?` properties
  (`AlertDialog`, `AssistChip`, `ListItem`, `Snackbar`, `BadgedBox`, `Tab`,
  `NavigationBarItem`, top-app-bar family).
- **Phase 4 / 4b / 4c** — `[StateHolder]` shapes above (`DatePicker`,
  `DateRangePicker`, `TimePicker`, `TimeInput`, `ModalBottomSheet`).
  `ModalBottomSheet` exercises Phase 4b (parameterised
  `RememberSheetState(skipPartiallyExpanded, confirmValueChange,
  composer)`) + Phase 4c (`SharedState = true`) + Phase 10
  `[ConfirmStateChange]` simultaneously — the canonical example of all
  three composing. `BottomSheetScaffold` and `SearchBar` stay hand-
  written (two non-nullable `IFunction3` slots — needs hybrid-container
  generator extension). `SnackbarHost` stays hand-written (its body
  `IFunction3` forwards `p0` to a sibling `Snackbar` bridge — no
  current generator option models that).
- **Phase 6** — `DefaultColorFromTheme` for drawer sheets/containers falling
  back to a `ColorScheme` slot.
- **Phase 7** — `[PainterResource]` resource-id facades (`Image`, Painter
  overload of `Icon`).
- **Phase 8** — wrapper-passthrough: stack `[ComposeFacade]` on a `partial`
  method on `ComposeBridges` whose hand-written body delegates to a bound `*Kt`
  method (e.g. `BoxKt.Box`, `CheckboxKt.Checkbox`). Wrapper has trailing
  `int defaults` user param the facade fills via auto-mask, passed to the
  binding's `_changed:` arg. No `[ComposeBridge]` on these wrappers;
  `Defaults = typeof(XDefault)` must reference a declarative-form
  `[assembly: ComposeDefaults(...)]` (the bit-name map). Used for `Box`,
  `Column`, `Row`, `Spacer`, `Checkbox`, `Switch`, `RadioButton`, `Slider`,
  `WideNavigationRailItem`, `TriStateCheckbox`,
  `SingleChoiceSegmentedButtonRow`, `MultiChoiceSegmentedButtonRow` (the
  last two via `IndexedChildren = true`; see below). Same-name 4-param
  overloads on `ComposeBridges` forward to existing 5-param
  `[ComposeBridge]` JNI bridges with `scrollState: null` for
  `PrimaryScrollableTabRow`, `SecondaryScrollableTabRow`.

  **Java enum primitives**: primitive-ctor slot detector also accepts
  reference types deriving transitively from `Java.Lang.Enum`. Compose
  generates enums (e.g. `AndroidX.Compose.UI.State.ToggleableState`) as
  `Java.Lang.Object` subclasses; surface as ctor primitives forwarded to
  bridge — no JNI lowering (see `TriStateCheckbox`).

  **Hybrid container + named slots**: a bridge with exactly one non-nullable
  `IFunction3` body **plus** one or more nullable `IFunction2?`/`IFunction3?`
  slots is allowed when facade declares `Scope = "..."`. Non-nullable
  Function3 stays as container body (`RenderChildren`); nullable Function2/3
  become named `ComposableNode?` properties; class derives from
  `ComposableContainer`. Without `Scope`, bridge classifies as multi-slot leaf
  (preserves `AssistChip`-style where required Function2 is a label). Used for
  `BottomAppBar`.

  **`Container = true`** — wrapper-passthrough variant of the hybrid-container
  rule, for bridges whose body slot is a non-`@Composable` `IFunction2` rather
  than a scope-providing `IFunction3`. Forces facade to derive from
  `ComposableContainer` and wrap children via
  `Wrap2(composer, c => RenderChildren(c))`. Required by
  `ModalWideNavigationRail`.

  **`IndexedChildren = true`** — for container facades whose children read
  their row position (e.g. `SegmentedButton` reads
  `RenderContext.CurrentRowChildIndex/Count` to compute its `start`/
  `middle`/`end` shape). The generated `Wrap2`/`Wrap3` body calls
  `RenderChildrenIndexed(c)` instead of `RenderChildren(c)`; the helper
  publishes a `PushRow + SetIndex` frame per child (still wraps each in
  the same `StartReplaceableGroup` as `RenderChildren`). Requires a
  container body (Phase 1 pure container OR Phase 8 hybrid container);
  CN3006 fires on a leaf. Combine with `Scope` when the container also
  publishes a Kotlin extension-receiver scope (see
  `SingleChoiceSegmentedButtonRow`, `MultiChoiceSegmentedButtonRow`).

- **Phase 9** — `BranchOn`/`AlternateBridge`: one facade routes between two
  `[ComposeBridge]` methods based on whether a single optional slot is
  supplied. Partial method carries the **primary** (smaller) bridge — no
  branched slot. `AlternateBridge` names a sibling `ComposeBridges` method
  whose user params = primary's set + exactly one extra
  `IFunction2`/`IFunction3` slot whose PascalCased name matches `BranchOn`.
  Both bridges must declare trailing `int defaults` and reference their own
  `Defaults = typeof(XxxDefault)`. Generator emits a single facade exposing
  the extra slot as nullable `ComposableNode?` property; renders
  `if (Subtitle is not null) {…alt…} else {…primary…}`. Shared lambdas and
  `__modifier = BuildModifier()` hoisted ABOVE the if/else; branched slot's
  wrapper, per-branch mask, and each bridge call live INSIDE their respective
  branches. Param order may differ between primary and alternate — emitter
  walks each bridge's actual `Parameters` list. Used for `TopAppBar` (→
  `TopAppBarWithSubtitle`), `MediumTopAppBar` (→ `MediumFlexibleTopAppBar`),
  `LargeTopAppBar` (→ `LargeFlexibleTopAppBar`).

- **Phase 10** — `[ConfirmStateChange(typeof(T))]` (described above). Used for
  `ModalNavigationDrawer`, `DismissibleNavigationDrawer`,
  `PermanentNavigationDrawer` (no `[ConfirmStateChange]` — always permanent),
  `ModalWideNavigationRail` (no veto — rail has no `confirmStateChange` in
  Kotlin).

- **Phase 11** — `SecondaryCtor` / `SecondaryDefaults`: one facade exposes
  two ctors that dispatch to two distinct `ComposeBridges` methods. The
  primary bridge carries
  `[ComposeFacade(SecondaryCtor = nameof(SiblingBridge), SecondaryDefaults = typeof(SiblingDefault))]`.
  The secondary is a static method on `ComposeBridges` (with or without
  `[ComposeBridge]`) sharing the primary's user-param shape *by name* plus
  exactly **one** extra non-nullable reference-type slot (the discriminator).
  The generator emits:
  - a nullable backing field `readonly TDiscrim? _<discriminator>;`,
  - an extra ctor whose first arg is the discriminator + remaining primary
    ctor slots,
  - a Render preamble `if (_<discriminator> is not null) { build secondary
    mask with __secDefaults/__secModifier locals → call secondary → return; }`
    (renamed locals avoid CS0136 shadowing of the primary path's
    `__modifier` / `__defaults`).

  Both bridges must declare trailing `int defaults`. The secondary's
  enum bit for the discriminator (if present) is cleared automatically.
  Mutually exclusive with `BranchOn` / `AlternateBridge` (CN3012). Used
  for `Icon` (`IconPainter` primary via `[PainterResource]` ctor +
  `IconImageVector` sibling wrapping the bound `IconKt.Icon(ImageVector,…)`
  overload).

### Hand-written holdouts

- Facades calling a bound binding directly with no `[ComposeBridge]` —
  `DropdownMenuItem`.
- State-holder facades that need two non-nullable content slots
  (hybrid-container with `[Slot]` siblings — not yet modelled):
  `BottomSheetScaffold` (`sheetContent` + `content`) and the `SearchBar`
  family (`inputField` + `content`). `SnackbarHost` stays hand-written
  because its body `IFunction3`'s first arg (`SnackbarData`) is forwarded
  to a sibling bridge — no current generator option models that. (Phase
  4b + 4c + 10 combined — parameterised Remember + SharedState +
  per-instance veto adapter — does work, see `ModalBottomSheet`.)
- Scope facades doing non-trivial work beyond `RenderContext.PushScope`
  (`SegmentedButton` — two ctors route to two physical bridges plus a
  custom `shape = ItemShape(...)` arg computed from the published
  row-position; doesn't fit `BranchOn` or any other phase).
- `TextField`, `OutlinedTextField` expose three ctors that route between two
  physical bridges — the `string` overload and the `TextFieldValue` overload
  (the latter carries `selection`/`composition` so callers can programmatically
  move the cursor, e.g. Jetchat's emoji-tap). Ctor-driven bridge selection
  with **three** ctors / **two** bridges doesn't fit Phase 11's
  one-discriminator shape. The `TextFieldValue` overload uses the **bound**
  `AndroidX.Compose.UI.Text.Input.TextFieldValue` type directly — only the
  Kotlin ctor needs JNI (mangled because `selection: TextRange` is a
  `@JvmInline value class`); everything else (`Text`/`Selection`/`Copy(…)`)
  is exposed by the runtime binding. See issue #204.
- `Layout` exposes a low-level measure-and-place primitive: ctor takes a
  user `Func<MeasureScope, IReadOnlyList<Measurable>, Constraints, MeasureResult>`
  delegate, the `MeasurePolicy` parameter is built once via a tiny Java
  helper that returns a SAM lambda — `MeasurePolicy` is a Kotlin
  `fun interface`, so `javac` resolves the single abstract member
  (`measure-3p2s80s`, mangled because `Constraints` is `@JvmInline value class`)
  by signature via `LambdaMetafactory` and the source never has to spell the
  illegal `-` identifier. Default interface methods (the four
  `IntrinsicMeasureScope.*Intrinsic*` helpers) are inherited correctly by the
  synthesized class. The SAM instance + JCW lambda are cached via
  `composer.Remember` so JNI identity stays stable across recompositions.
  None of (custom user-delegate ctor, wrapper-typed params not in
  `ComposeValueTypes`, JCW with mutable `Body`) fit any `[ComposeFacade]`
  phase. See issue #144.

Applying `[ComposeFacade]` to an unsupported bridge emits CN3002 (unsupported
param), CN3003 (scope misuse), CN3005 (invalid callback type), CN3006 (slot
conflict), CN3007 (color theme bind failed), CN3008 (painter misuse), CN3009
(state-holder misuse), CN3010 (branching misuse), CN3011
(confirmStateChange misuse), or CN3012 (secondary-ctor misuse).

### Adding a new generated facade

1. Add the bridge in `ComposeBridges.cs` (`[ComposeBridge]` + matching
   `[ComposeDefaults]` if `$default` slot). Verify
   `dotnet build src/Microsoft.AndroidX.Compose`.
2. Stack `[ComposeFacade]`. Pass `Scope = "Row"`/`"Column"` only if content is
   `IFunction3` and child facades need that receiver. Pass
   `ClassName = "MyName"` only when public class name must differ from bridge
   method name (rare).
3. Create sibling stub at `src/Microsoft.AndroidX.Compose/<ClassName>.cs` with
   `public sealed partial class <ClassName>;` and a `<summary>`. Use
   `Button.cs`/`IconButton.cs`/`Card.cs` as templates. **Do not omit the
   stub** — without it no XML docs.
4. Build `dotnet build src/Microsoft.AndroidX.Compose.Gallery` to verify. CN3001-CN3012 fire
   on rejection.
5. If CN3002 fires, add the right marker attribute
   (`[Callback]`/`[Slot]`/`[PainterResource]`) or back out and write by hand.
6. **Add a gallery demo for the new facade.** Every new public surface needs a
   demo (see "Gallery demos" below).

### Generator diagnostics

| ID     | Meaning                                                                                                                                                                                                                                                                                                                                                                                                                                                                                          |
|--------|------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| CN3001 | `[ComposeFacade]` on a method not declared on `ComposeBridges`.                                                                                                                                                                                                                                                                                                                                                                                                                                  |
| CN3002 | Bridge parameter type isn't a supported facade slot.                                                                                                                                                                                                                                                                                                                                                                                                                                             |
| CN3003 | `Scope` is set but bridge has no `IFunction3` content slot.                                                                                                                                                                                                                                                                                                                                                                                                                                      |
| CN3004 | `[ComposeFacade]` without an accompanying `[ComposeBridge]`.                                                                                                                                                                                                                                                                                                                                                                                                                                     |
| CN3005 | `[Callback(typeof(T))]` target type unsupported (must be `bool`/`string`/`float`).                                                                                                                                                                                                                                                                                                                                                                                                               |
| CN3006 | `[Slot]` conflicts with classified shape, `[Callback]` on non-`IFunction1`, multiple `[PainterResource]` on one bridge, `int defaults` declared without resolvable `Defaults` enum, or `IndexedChildren = true` on a facade without a non-nullable IFunction2/IFunction3 container body.                                                                                                                                                                                                       |
| CN3007 | `DefaultColorFromTheme` cannot bind to any `long` user param (or `ColorParameter` ambiguous/missing).                                                                                                                                                                                                                                                                                                                                                                                            |
| CN3008 | `[PainterResource]` annotates a non-`IntPtr` parameter.                                                                                                                                                                                                                                                                                                                                                                                                                                          |
| CN3009 | `[StateHolder]` invalid: non-`IntPtr` param, combined with `[PainterResource]`, missing/non-identifier `Remember`/`StateType`, named `Remember` not a static `(IComposer) -> IntPtr` on `ComposeBridges`, or `StateType` has no accessible writable instance field named `Jvm`.                                                                                                                                                                                                                  |
| CN3010 | `BranchOn`/`AlternateBridge` invalid: only one set, primary/alternate missing trailing `int defaults`, named alternate not resolvable/ambiguous on `ComposeBridges`, alternate not a strict superset (missing a primary param or > 1 extra), extra param's PascalCased name doesn't match `BranchOn`, extra param isn't `IFunction2`/`IFunction3`, shared param has incompatible types, branching used on hybrid container shape, or alternate has no resolvable `[ComposeBridge].Defaults` enum. |
| CN3011 | `[ConfirmStateChange(typeof(T))]` invalid: not on `IFunction1` param, missing `typeof(T)` ctor arg, convention adapter `Microsoft.AndroidX.Compose.<TName>ConfirmStateChange` missing (override with `AdapterType = typeof(...)`), adapter doesn't implement `Kotlin.Jvm.Functions.IFunction1`, lacks public parameterless ctor, or no writable `Callback` property of type `System.Func<T, bool>?`.                                                                                                              |
| CN3012 | `SecondaryCtor`/`SecondaryDefaults` invalid: only one set, named secondary not resolvable/ambiguous on `ComposeBridges`, secondary missing trailing `int defaults`, secondary's user params don't share names with the primary, the discriminating extra param is value-type / nullable / not a reference type / there's > 1 unique param / there's none, primary has no slot missing from the secondary (no primary-only discriminator), `SecondaryDefaults` enum unresolvable, or combined with `BranchOn`/`AlternateBridge`.                                                                                                                                                                                                                                                                              |

### Migration rule

When adding `[ComposeFacade]` to a hand-written facade, **replace** the
hand-written file with a 3-line stub
(`namespace Microsoft.AndroidX.Compose; /// <summary>…</summary> public sealed partial class <Name>;`)
in the same commit. Stub must not redeclare members the generator emits —
duplicate-member errors otherwise.

### ⚠️ DO NOT demote a generated facade to hand-written

**Critical rule.** If a facade is `[ComposeFacade]`-generated and the
generator can't model a new shape, **extend the generator** — never delete
`[ComposeFacade]` and reimplement by hand. Hand-written code loses generator
guarantees (slot-key stability via `ComposableLambdas.Wrap*`, correct
`$default` mask construction, consistent disposal ordering).

When you need feature X the generator doesn't emit:

1. Add a new slot kind / attribute / codegen branch to the relevant generator.
   Phases 1–10 are precedents.
2. Add a generator test in `Microsoft.AndroidX.Compose.SourceGenerators.Tests`.
3. Document the new shape in the Phase index; add new CN3xxx diagnostics.
4. Apply the new attribute to the existing facade.

The only legitimate hand-written holdouts are those listed above. If you
think a new facade belongs on the list, propose explicitly and justify —
don't demote silently.

Reverse rule (rare): to make a fully custom `Render()` that genuinely can't be
expressed, remove `[ComposeFacade]`, expand the stub, **and add it to the
"hand-written holdouts" list above with a one-sentence reason**. Requires user
approval.

## Facade conventions (`Composables.cs`)

- All public types derive from `ComposableNode` (single
  `internal abstract void Render(IComposer)`).
- Container composables derive from `ComposableContainer`, which implements
  `IEnumerable` + `Add(ComposableNode?)` so callers use collection-init:
  ```csharp
  new Column { new Text("Hi"), new Button(onClick: …) { new Text("Tap") } }
  ```
- Wrap Kotlin lambdas with `ComposableLambda0/1/2/3/4`; don't hand-roll new
  adapters.
- **Never construct `ComposableLambda2`/`3`/`4` directly inside `Render`.**
  Route every `@Composable` slot lambda through `ComposableLambdas` so
  Compose's own factory owns identity across recompositions.
  `SubcomposeLayout`-backed composables (`Scaffold`, `BottomSheetScaffold`,
  `ModalNavigationDrawer`, …) cache subcomposed content keyed by lambda
  identity; a fresh `new ComposableLambda*(...)` every pass thrashes that
  cache and causes `LayoutNode` insert ops at the wrong index (#42). Helpers
  derive a unique slot-table key from `[CallerLineNumber]` +
  `[CallerFilePath]` — no key arg needed.

  Two non-interchangeable helper families:

  - **`Wrap2(composer, …)` / `Wrap3(composer, …)`** →
    `composableLambda(composer, key, tracked, block)`. Use when the lambda is
    built and invoked **synchronously inside the same composition pass** —
    content slots like `topBar`, `title`, button content, `Column`/`Row`/`Box`
    children. Factory writes wrapper into the active composer's slot table.
  - **`Instantiate4(…)` (no composer)** →
    `composableLambdaInstance(key, tracked, block)`. Use when built during
    `Render` but **invoked later, outside the current composition** —
    `LazyListScope.items`/`LazyGridScope.items` `itemContent`, realized at
    measure time inside `rememberLazyListItemProviderLambda`. The captured
    composer is stale by then; `composableLambda(composer, …)` would crash
    with "Expected applyChanges() to have been called".

  ```csharp
  // Wrong — fresh identity every recomposition:
  var content = new ComposableLambda3(c => RenderChildren(c));
  // Right:
  var content = ComposableLambdas.Wrap3(composer, c => RenderChildren(c));

  // Wrong — Wrap4 needs composer but DSL builder runs at measure time:
  itemContent: new ComposableLambda4((_, idx, c) => …)
  // Right:
  itemContent: ComposableLambdas.Instantiate4((_, idx, c) => …)
  ```

  `ComposableLambda0` (onClick), `ComposableLambda1` (onValueChange /
  onCheckedChange / LazyListScope / LazyGridScope DSL builders) are **not**
  `@Composable` and must stay raw — wrapping injects
  `startRestartGroup`/`endRestartGroup` into code that runs outside
  composition.

- **Sibling `Render()` calls in a loop need per-position slot keys.**
  `ComposableContainer.RenderChildren` and `RenderChildrenIndexed` wrap
  each child in
  `composer.StartReplaceableGroup(HashCode.Combine(i, child.GetType()))` /
  `EndReplaceableGroup()`. Custom loops calling `Children[i].Render(c)`
  directly (e.g. `SegmentedButton`'s label slot) must do the same. The
  type component stops a sibling that swaps subclass-at-the-same-
  position (e.g. tab nav flipping `PullToRefreshBox` for
  `HorizontalUncontainedCarousel`) from re-entering the prior occupant's group
  — otherwise `ClassCastException` from Compose's `rememberSaveable`. Same-
  typed siblings at the same position keep identity and slot state intact.

- The single `ComposableLambdaKt.ComposableLambdaInstance` call in
  `ComposeExtensions.SetContent` is correct there — root content lambda runs
  outside an active composition.

- Multi-slot composables expose **named slot properties** via object-init,
  not extra collection-init `Add` overloads. Pattern: start
  `defaults = XxxDefault.All`, clear the bit for each slot the caller
  supplied.

- Required Kotlin params with no default (e.g. `AlertDialog.confirmButton`)
  validated in `Render` with a clear `InvalidOperationException`.

- **Don't wrap bound binding calls or source-generated `[ComposeBridge]`
  methods in `try`/`finally` + `GC.KeepAlive`.** Both already emit their own
  `GC.KeepAlive` for every `IJavaPeerable` arg. Only needed when calling raw
  `JNIEnv.CallStatic*Method` directly inside a hand-written helper.

- File-scoped namespaces (`namespace Microsoft.AndroidX.Compose;`). One blank line separating
  `// ---- Section ----` banners. XML doc comments on every public type and
  non-trivial member.

### Optional veto / confirm callbacks (`(T) -> Boolean` parameters)

Several Compose APIs take a `(T) -> Boolean` callback the runtime invokes to
ask "should this transition proceed?" — e.g.
`rememberDrawerState(confirmStateChange)`,
`rememberSheetState(confirmValueChange)`. **These are part of the cached
state-holder's `remember` key**, so JNI reference identity must stay stable
across recompositions or the cache is dropped (drawer/sheet forgets state).

Pattern (canonical: `DrawerValueConfirmStateChange`):

1. **For new facades, prefer the generator path** —
   `[ConfirmStateChange(typeof(T))]` + `[ComposeFacade]` (Phase 10).
2. **Hand-written holdouts** (`BottomSheetScaffold`) by
   convention:
   - Expose hook as `Func<T, bool>?` property, default `null` (= "use Kotlin's
     default — always allow"). Document `false` = veto.
   - Allocate JNI adapter **once per node instance** as `readonly` field.
     Never `new` inside `Render` — recreates Java peer every recomposition,
     invalidates `remember` key.
   - Read delegate inside adapter's `Invoke` (not at construction) so caller
     can mutate the property between renders without re-allocating.
   - Treat `null` as "always true" inside adapter — no separate singleton
     fallback needed.

Do **not** use a static singleton — fine for stateless stubs the user can't
override (`NoOpSearchCallback` — `onSearch` is *not* a `remember` key) but
can't host a per-node mutable delegate without becoming shared state.

## `$changed` bitmask propagation

Kotlin's compose-compiler emits an `int $changed` JNI slot alongside `$default`
on every `@Composable`. Three bits per param (Same=0b001 / Different=0b010 /
Static=0b100), bit 0 reserved for the runtime "force" flag. When every param's
bits read **Same** the runtime takes the skip path and re-emits cached output
nodes.

The C# facade computes this mask at Render time. Mechanisms:

- **`composer.DiffSlot<T>(value, bitOffset)`** — per-call-site slot-table diff
  (same shape as `Remember`). First call stashes; equal repeat returns Same;
  unequal stashes + returns Different. `null` is a legal value.
- **`composer.RememberAction(Action)` / `RememberAction<T>(Action<T>)`** —
  caches one `MutableComposableLambda0/1` JCW per call site with a writable
  target. Each render rebinds the target instead of allocating a fresh JCW,
  so the JNI peer's handle is identity-stable; that param's mask bits read
  Static. Use this for `onClick` / `onValueChange` / `onCheckedChange`.
- **`Modifier.StructuralKey`** — `Modifier.cs` factories and
  `ModifierExtensions.cs` ops record `(string OpName, object? Args)` alongside
  each closure so two chains with the same semantic ops hash equal even when
  their captured-locals closures don't. Currently the facade generator leaves
  the modifier slot at Uncertain (`0`) — the Kotlin runtime still does its
  own input compare. Capturing the structural key into a slot via DiffSlot
  is a follow-up (would need to flow `_prepended`/`_appended` side-channels
  too).

### Adding a new `[ComposeBridge]` `@Composable`

Append `int _changed = 0` as the **trailing** partial-method param (after
`IComposer composer`). The bridge generator writes it into the first JNI
`$changed` slot in place of literal 0. Without `_changed`, every slot stays
0 (Uncertain — current pre-bitmask behaviour, never wrong, just slower).

```csharp
[ComposeBridge(/* … */)]
public static partial void Button(IFunction0 onClick, /* … */,
                                  IComposer composer, int _changed = 0);
```

### `[ComposeFacade]` plumbing

The facade generator emits `__changed` into Render automatically when the
bridge declares the trailing `int _changed = 0`. Per-slot contribution table:

| Slot kind                         | Contribution                                          |
|-----------------------------------|-------------------------------------------------------|
| `IModifier?` (BuildModifier)      | Uncertain (0) — currently deferred                    |
| `Action` → `IFunction0`           | `Static << bit` (RememberAction-stable peer)          |
| `Action<T>` → `IFunction1`        | `Static << bit` (RememberAction-stable peer)          |
| `IFunction2`/`IFunction3` content | `Static << bit` (Wrap2/Wrap3 are identity-stable)     |
| `IntPtr` scope receiver           | `Static << bit` (consumed by bridge, not param-id)    |
| `IntPtr` + `[PainterResource]`    | `DiffSlot` on resolved IntPtr                         |
| `IntPtr` + `[StateHolder]`        | `DiffSlot` on wrapper.Jvm reference                   |
| Value types / primitives / refs   | `DiffSlot<T>` via `EqualityComparer<T>.Default`       |

Bit position: `bit = 1 + paramIndex * 3` over user params **excluding**
composer/defaults/scope-receiver/_changed. Up to 10 user params per int —
no current bridge exceeds that.

The bridge call switches to **named arguments** (`composer: composer,
_changed: __changed`) when the bridge has the trailing optional, so the
optional doesn't reorder positionally against composer. Without `_changed`,
positional emission is preserved (back-compat with all existing pin tests).

### Hand-written facade holdouts

Facades not driven by `[ComposeFacade]` (TextField, BottomSheetScaffold,
SnackbarHost, SegmentedButton, SearchBar family) currently default
`_changed: 0` (Uncertain) — back-compat, never incorrect. Opting one in is
a one-line change: compute a per-param mask in Render via `DiffSlot` /
`RememberAction`, pass `_changed: __changed` to the bridge.

### Don't regress correctness

When in doubt emit `Uncertain` (0) — the runtime falls back to its own
input compare. The whole point is to never silently skip a real change.
A new Modifier extension that captures something not value-equatable
(delegate, IntPtr from a `[StateHolder]`, …) should record an opaque
`object` in its `StructuralKey` arg slot — falls back to reference
equality on that op, returns Different, no skip. Correct.

## Bindings policy

Reference official NuGets only: `Xamarin.AndroidX.Compose.*` and
`Xamarin.AndroidX.Compose.Material3`. **Don't bring back custom `*.Compose.*`
binding projects.**

### Always check the binding first — in the `.Android` DLL

Before writing a `[ComposeBridge]`, hand-rolling a wrapper, or adding
a JNI helper: **verify the symbol isn't already bound**. The actual
binding lives in the runtime companion package
(`Xamarin.AndroidX.Compose.<Module>.Android.dll`, ~500 KB), not the
small multi-targeting façade DLL (`Xamarin.AndroidX.Compose.<Module>.dll`,
~15 KB, 1 type). Issue #204 initially shipped hand-rolled
`TextFieldValue` + `TextRange` wrappers because only the façade was
inspected — both types are fully bound in the `.Android` companion.

Use `ilspycmd` (already installed; run `ilspycmd --help`) against the
`.Android.dll` to check what's exposed. Useful flags: `-t <FQN>` to
decompile one type, `-l c` to list classes.

### Inline-class lowering — partial bindings

Even when the type is bound, `@JvmInline value class` params produce
mangled JVM names (`Foo-d9O1mEE`) that strip the corresponding
overloads. Typical pattern:

- **Bound**: the class itself, static factories on the `*Kt` companion
  (e.g. `TextRangeKt.TextRange(int, int)`), methods/properties whose
  signatures don't touch value classes.
- **Stripped**: constructors taking a value-class param, getters
  returning a value class, methods whose params include them.

So check both — type AND specific members. If the ctor is stripped
but everything else is bound, only bridge the ctor; **don't wrap the
whole type just because one method is missing**.

### When a needed Compose API isn't bound

1. File/link a tracking issue against `dotnet/android-libraries`
   (see #1415–#1418 in `README.md`).
2. Add a `[ComposeBridge]` partial (or a hand-written JNI helper in
   `ComposeBridges` if not a `@Composable`).
3. Add matching `[ComposeDefaults]` **only if** the `@Composable` has
   a `$default` slot.
4. **Pass bound types directly** as `[ComposeBridge]` params — the
   generator emits `((Java.Lang.Object)param).Handle` for any
   non-primitive reference type, so
   `AndroidX.Compose.UI.Text.Input.TextFieldValue value` works the
   same as a hand-rolled wrapper would.
5. When upstream fix ships, delete the bridge and switch the facade
   to call the generated binding directly.

### Hand-written JNI: always wrap local refs in `try`/`finally`

Anywhere you call `JNIEnv.GetStaticObjectField`, `CallObjectMethod`,
`NewObject`, `NewString`, etc. and get back a local ref, the
`DeleteLocalRef` (or the call that consumes the ref, like
`Java.Lang.Object.GetObject(local, TransferLocalRef)`) **must** sit in
a `finally` so an exception thrown between the local-ref allocation and
its consumer doesn't leak the ref. The local-ref table is small (~512
slots on most VMs) and a leaked ref can crash the process under
CheckJNI.

Canonical pattern for "fetch + cache a Companion singleton":

```csharp
static SomeCompanion? s_companion;

static SomeCompanion Companion()
{
    if (s_companion is not null) return s_companion;
    IntPtr local = IntPtr.Zero;
    try
    {
        IntPtr cls = JNIEnv.FindClass("foo/bar/Outer");
        IntPtr fid = JNIEnv.GetStaticFieldID(cls, "Companion",
            "Lfoo/bar/Outer$Companion;");
        local = JNIEnv.GetStaticObjectField(cls, fid);
        return s_companion = Java.Lang.Object.GetObject<SomeCompanion>(
            local, JniHandleOwnership.TransferLocalRef)!;
    }
    finally
    {
        // GetObject(.., TransferLocalRef) consumes `local` on success;
        // the explicit DeleteLocalRef only runs if the wrapper threw
        // before taking ownership.
        if (local != IntPtr.Zero && s_companion is null)
            JNIEnv.DeleteLocalRef(local);
    }
}
```

Examples in the repo: `KeyboardOptionsCompanion.cs`,
`KeyboardType.cs`, `TextStyleCompanion.cs`. Also applies to
hand-written helpers in `ComposeBridges.cs` and modifier extensions
that allocate local refs from `Call*ObjectMethod` /
`NewObject` / `JValue.NewString`.

For class refs, **don't** wrap `JNIEnv.FindClass` results in
`NewGlobalRef`/`DeleteLocalRef` — Mono.Android returns stable
globally-registered class refs (re-stated from the suspend-bridge
section).


**Don't add `[ComposeBridge]` if the binding already exposes the method** —
call the generated C# entry point instead.

## `[ComposeCompanion]` — Kotlin Companion singleton wrappers

Surfaces Kotlin `Companion.getXxx()` singletons (`FontWeight.Thin`,
`ContentScale.Crop`, …) as static C# properties without hand-rolling
the `FindClass → GetStaticFieldID("Companion") → NewGlobalRef →
CallObjectMethod` dance per constant.

### Adding a wrapper

1. `Java.Lang.Object` subclass, `partial`, private `(IntPtr,
   JniHandleOwnership)` ctor.
2. `[ComposeCompanion("foo/bar/OuterClass")]` on the class — outer JNI
   name, slash-separated, no `L…;`. Generator appends `$Companion`.
3. Per singleton: `public static partial T Name { get; }` with
   `[ComposeCompanionGetter("getName")]`.

```csharp
[ComposeCompanion("androidx/compose/ui/text/font/FontWeight")]
public sealed partial class FontWeight : Java.Lang.Object
{
    FontWeight(IntPtr handle, JniHandleOwnership transfer) : base(handle, transfer) { }

    /// <summary><c>FontWeight.Thin</c> (W100).</summary>
    [ComposeCompanionGetter("getThin")]
    public static partial FontWeight Thin { get; }
}
```

Generator emits the lazy `Companion()` accessor + global-ref cache,
per-property peer cache, a shared `ResolveSimple(getter, descriptor)`
helper, and each partial-property body.

### Options

- **`InlineClass = true`** on `[ComposeCompanion]` — for `@JvmInline
  value class` wrappers (`TextAlign`, `FontStyle`) whose getters return
  packed `int`. Routes through synthesized `box-impl(I)L<outer>;`.
  Mangled getter names (`getCenter-e0LSkKk`) are forwarded verbatim.
- **`ReturnDescriptor = "L…;"`** on `[ComposeCompanionGetter]` — when
  the getter returns a concrete subtype (e.g.
  `FontFamily.Companion.getSansSerif()` returns `GenericFontFamily`).
  Incompatible with `InlineClass` (CN4007).

### Notes

- `Companion()` is **not** synchronized — racing first calls leak one
  global ref but always resolve to the same singleton. Matches the
  pre-generator code; intentional.
- Hand-written holdouts: `Alignment.cs` (uses bound
  `IAlignment.Companion`, no JNI), `TextStyleCompanion.cs` (`static
  class`, single companion-get).

### Generator diagnostics

| ID     | Meaning                                                                                                                          |
|--------|----------------------------------------------------------------------------------------------------------------------------------|
| CN4001 | Host class isn't `partial`.                                                                                                      |
| CN4002 | `[ComposeCompanion]` `outerJniClass` null/empty.                                                                                 |
| CN4003 | Getter isn't `public static partial T { get; }` — instance, has setter, missing `partial`, or no getter.                         |
| CN4004 | `[ComposeCompanionGetter]` `getterName` null/empty.                                                                              |
| CN4005 | Getter's containing class lacks `[ComposeCompanion]` (orphan; otherwise surfaces only as opaque C# partial-property error).      |
| CN4006 | Return type lacks accessible `(IntPtr, JniHandleOwnership)` ctor.                                                                |
| CN4007 | `ReturnDescriptor` set while host has `InlineClass = true`.                                                                      |

Tests in `CompanionGeneratorTests.cs`. **Add a test for any new
behaviour.**

## Tier 2 — `[Composable]` static methods

User-facing surface for the C# compose-compiler equivalent: a Roslyn
incremental source generator (`ComposableMethodGenerator`) emits a
per-call-site `[InterceptsLocation]` wrapper that opens a Compose
restart group, runs per-parameter `DiffSlot` diffing, and skips the
underlying call when nothing changed. Mirrors `dotnet/maui`'s
`BindingSourceGen` pattern — one user method, generator-emitted
wrappers at every call site, redirected by the C# compiler at the
language level.

### Authoring pattern (canonical)

```csharp
using static AndroidX.Compose.Composables;

public static class Screens
{
    [Composable]
    public static void Counter()
    {
        var count = Remember(
            () => new MutableNumberState<int>(0));

        Column(() =>
        {
            Text($"Count: {count.Value}");
            Button(
                () => count.Value++,
                () => Text("Tap"));
        });
    }
}
```

Rules:

- `[Composable]` method must be `static` (CN5001) with a `void` return
  (CN5002). It may omit `IComposer`; when declared explicitly,
  `IComposer` must be the **first** parameter (CN5003).
- It must be accessible from the generated interceptor (CN5004) and
  cannot be `async` (CN5005), an extension method (CN5006), generic
  (CN5007), or use `ref`/`out`/`in` parameters (CN5008).
- The containing type does **not** need to be `partial`. There is no
  `Impl` companion, no `_changed` parameter, no `int _default` slot.
- Each call site of a `[Composable]` method is rewired by the C#
  compiler to a generator-emitted wrapper under
  `Microsoft.AndroidX.Compose.Generated.ComposableInterceptors`.

The generator emits a single
`Microsoft.AndroidX.Compose.Composable.Interceptors.g.cs` file containing
one wrapper per intercepted call site. For a composerless
`Greeting("world")` call, the emitted wrapper looks roughly like:

```csharp
[global::System.Runtime.CompilerServices.InterceptsLocationAttribute(1, @"...base64...")]
public static void Composable_0_AB12CD34(string name)
{
    Composable_0_AB12CD34_Core(ComposableContext.Current, name, 0);
}

static void Composable_0_AB12CD34_Core(
    IComposer composer, string name, int changed)
{
    var __c = composer.StartRestartGroup(unchecked((int)0xKEY));
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

The restart-group key is FNV-1a over the fully-qualified method name
(stable across processes — matches the `SourceLocationKey` contract).
The Kotlin-shape mask/expected pair for N user params is
`mask = 0b001 | sum(0b101 << (1+3*i))` and
`expected = sum(0b001 << (1+3*i))`; the wrapper takes the skip path
when `(__dirty & mask) == expected && composer.Skipping`. The
`UpdateScope` lambda re-enters the wrapper itself (not the user
method) so the next composition pass re-opens the same restart group,
re-diffs, and skips-or-calls the same way.

### How interception is wired in

The generator emits the `InterceptsLocationAttribute` definition
itself, as a `file`-scoped `sealed class` inside
`System.Runtime.CompilerServices`, with a `[Conditional("DEBUG")]` so
it doesn't bloat Release metadata. The C# compiler resolves
interceptors purely from source attributes — the wrapper method's
`[InterceptsLocation(version, data)]` is enough; the attribute's
runtime presence is irrelevant.

The preview feature is opted into project-wide by
`Directory.Build.props`:

```xml
<InterceptorsPreviewNamespaces>$(InterceptorsPreviewNamespaces);Microsoft.AndroidX.Compose.Generated</InterceptorsPreviewNamespaces>
```

Any project consuming `[Composable]` methods needs that property set
(every project under `src/` inherits it via `Directory.Build.props`).
The generator project itself uses
`SemanticModel.GetInterceptableLocation(InvocationExpressionSyntax, CancellationToken)`
(Roslyn 4.11+, gated under `#pragma warning disable RSEXPERIMENTAL002`
the same way MAUI's BindingSourceGen gates it) to compute the
`(version, data)` pair the compiler will match against each call site.

### Self-interception guard

The generator filters out invocations whose syntax-tree path ends in
`.g.cs` before running its semantic-model probe. Without this guard
the wrapper's own call to the user method would itself match the
generator's "is this targeting a `[Composable]` method?" predicate,
get its own interceptor emitted, and recursively the interceptor's
call to that interceptor, … — infinite recursion at generation time.
The `UpdateScope` lambda calls *the wrapper itself* by hand-rolled
name (not the user method), so it never needs interception.

### Coexistence with the tree-style facade

Both styles can call into each other freely:

- A tree-style facade `Render` (or `SetContent`'s callback) can invoke
  a Tier 2 `[Composable]` method directly — each call site is
  intercepted normally and the wrapper sets up its own restart group
  inside the surrounding tree-style render.
- A Tier 2 method can construct a tree-style `ComposableNode` and call
  `.Render(composer)` on it.
- `ComposeFacadeGenerator` emits a sibling `[Composable]` method on
  `AndroidX.Compose.Composables` for every supported generated facade.
  The method exposes ctor values, modifier, content, named slots,
  optional values, theme color, and state-confirm callbacks as normal
  parameters, maps them onto the existing facade, and renders it. Do
  not hand-write a duplicate entry point; extend the facade generator
  when a generated shape is wrong.
- `Column` and `Row` remain hand-written entry points because their
  facades are generator holdouts. `Text`, `Box`, and `Button` now use
  the richer catalog-generated methods. The generator detects an
  existing same-name method and does not emit a duplicate.
- `SetContent(Action<IComposer>)` hosts a Tier 2 root directly.
  Jetchat, JetNews, and Reply use this overload and expose their
  top-level app boundary as a `[Composable] static void` method.

There is no migration pressure. Hot composables that recompose often
are the natural candidates for Tier 2; one-shot screens can stay
tree-style indefinitely. The proof demo
(`src/Microsoft.AndroidX.Compose.Gallery/Demos/Tier2/Tier2SiblingSkipDemo.cs`)
renders two sibling Tier 2 methods side by side and shows that the
one whose input never changes has its body skipped (its in-process
execution counter stays flat while its sibling's tracks every tap).

### Wiring the generator into a consuming project

The `Microsoft.AndroidX.Compose` NuGet package ships the generator under
`analyzers/dotnet/cs/` and imports the required interceptor namespace through
`buildTransitive/Microsoft.AndroidX.Compose.props`. NuGet consumers need only
the runtime package reference:

```xml
<PackageReference Include="Microsoft.AndroidX.Compose" Version="..." />
```

Projects inside this repository consume the runtime through a
`ProjectReference`, so they also reference
`Microsoft.AndroidX.Compose.SourceGenerators` with
`OutputItemType="Analyzer"` and `ReferenceOutputAssembly="false"`.
Source-generator project references are not transitive. The runtime project
itself also needs that reference because its bridges, defaults, and facades
are generated while compiling the runtime assembly.

### Generator diagnostics

| ID     | Meaning                                                                                                                  |
|--------|--------------------------------------------------------------------------------------------------------------------------|
| CN5001 | `[Composable]` method must be `static` (Tier 2 intercepts call sites; intercepted target must be a static method).       |
| CN5002 | `[Composable]` method must return `void` (Tier 2 currently supports only void composables).                              |
| CN5003 | When present, a `[Composable]` method's `AndroidX.Compose.Runtime.IComposer` must be its first and only composer parameter. |
| CN5004 | `[Composable]` method and its containing types must be accessible from the generated interceptor.                       |
| CN5005 | `[Composable]` method cannot be `async`; continuations would resume after the restart group closes.                     |
| CN5006 | `[Composable]` extension methods are unsupported; use a regular static method with `IComposer` first.                   |
| CN5007 | `[Composable]` generic methods are unsupported.                                                                          |
| CN5008 | `[Composable]` parameters cannot use `ref`, `out`, or `in`.                                                              |

Tests in `ComposableMethodGeneratorTests.cs`. **Add a test for any new
behaviour.**

### Deferred (follow-up)

- Direct `[ComposeBridge]` calls from generated catalog entry points;
  they currently allocate the corresponding facade only when the
  interceptor decides the method must execute.
- Tier 2 modelling for hand-written facade holdouts (`Scaffold`, lazy
  collections, text fields, search, and other custom shapes).
- `$default` parameter injection — lower C# default-parameter syntax
  into Kotlin-style `$default` bitmask.
- Analyzer for "non-`[Composable]` calls `[Composable]`" — compile-
  time enforcement of the colour contract.
- Lambda hoisting via `RememberAction` / `Wrap2` / `Wrap3` inside the
  generator-rewritten body (recursive-call composer threading + lambda
  identity stability).
- `MovableContent` / `key {}` / `Saver` / stability inference / custom
  `Layout {}` — explicit non-goals in the MVP, each tracked as its
  own issue.

## Suspend / async bridges

For Kotlin Compose `suspend` functions (e.g. `ScrollState.scrollTo`,
`LazyListState.animateScrollToItem`, `SnackbarHostState.showSnackbar`,
`DrawerState.open`) surfaced as `Task`/`Task<T>`.

`SuspendBridge.Invoke` allocates one `SuspendContinuation`, calls the raw JNI
bridge, and completes the returned task from the sync result or later Kotlin
resume. `SuspendContinuation` is an internal JCW registered as
`net/compose/SuspendContinuation`; self-roots with a strong `GCHandle`
per call; `Context` returns `AndroidUiDispatcher.Main` (supplies the
`MonotonicFrameClock` required by `withFrameNanos`-based animation suspends).

Both `Invoke` overloads accept trailing `CancellationToken cancellationToken
= default`. Cancel → task transitions to **Canceled**
(`OperationCanceledException` at the awaiter), but the underlying Kotlin
suspend body continues to natural completion — no `Job` wired into
`SuspendContinuation.Context` yet. Eventual resume's boxed result disposed
silently. Surface the parameter on every new `*Async` and document
"awaiter-only" semantics.

### Adding a new `*Async`

1. Add a `[ComposeBridge(Suspend = true)]` partial in `SuspendBridges.cs`.
   The generator detects the trailing
   `Kotlin.Coroutines.IContinuation cont` parameter, emits the cached
   class+method-id boilerplate, the `JValue*` stackalloc, the
   `((Java.Lang.Object)cont).Handle` slot, and the
   `try` / `finally { GC.KeepAlive(cont); }`. Two sub-shapes:

   **Instance suspend** (Kotlin instance `suspend fun`, no `$default`) —
   bridge declares the receiver as its first user `IntPtr`; the JNI
   signature does **not** include it; generator emits
   `GetMethodID` + `CallObjectMethod(receiver, ...)`:
   ```csharp
   [ComposeBridge(Class = "my/package/MyState",
                  JvmName = "doThing",
                  Signature = "(ILkotlin/coroutines/Continuation;)Ljava/lang/Object;",
                  Suspend = true)]
   internal static partial IntPtr MyStateDoThing(IntPtr state, int value, IContinuation cont);
   ```

   **Static `$default` suspend** (Kotlin extension or instance fn with
   `@JvmDefault`) — JNI signature follows the synthetic shape
   `(receiver, ...userParams, Continuation, int $default, Object marker)`;
   bridge declares the receiver as its first `IntPtr` user param (it
   occupies JNI slot 0); generator emits `GetStaticMethodID` +
   `CallStaticObjectMethod`. Always pair with a matching
   `[assembly: ComposeDefaults(...)]` and `Defaults = typeof(XxxDefault)`:
   ```csharp
   [ComposeBridge(Class = "androidx/compose/foundation/ScrollState",
                  JvmName = "animateScrollTo$default",
                  Signature = "(Landroidx/compose/foundation/ScrollState;ILandroidx/compose/animation/core/AnimationSpec;Lkotlin/coroutines/Continuation;ILjava/lang/Object;)Ljava/lang/Object;",
                  Suspend = true,
                  Defaults = typeof(ScrollStateAnimateScrollToDefault))]
   internal static partial IntPtr ScrollStateAnimateScrollTo(
       IntPtr state, int value, IntPtr? animationSpec, IContinuation cont);
   ```

   Generator validates: return must be `IntPtr`/`nint`; JNI return must
   be `Ljava/lang/Object;`; continuation slot must be last (instance) or
   `sigParams.Count - 3` (static `$default`); cannot combine with
   `Composer` / constructor (`JvmName = "<init>"`) / `InstanceField`.
   Violations emit **CN2009**.

2. Add facade method routed through `SuspendBridge.Invoke`. Always
   surface trailing `CancellationToken cancellationToken = default`:
   ```csharp
   public Task<float> DoThingAsync(int value, CancellationToken cancellationToken = default) =>
       SuspendBridge.Invoke<float>(
           cont => ComposeBridges.MyStateDoThing(((Java.Lang.Object)Jvm).Handle, value, cont),
           static boxed => boxed is Java.Lang.Float f
               ? f.FloatValue()
               : throw new InvalidCastException($"Expected java.lang.Float; got '{boxed?.Class?.Name ?? "null"}'"),
           cancellationToken);
   ```

3. Add new public members to `PublicAPI.Unshipped.txt`.

### Hand-written holdouts

Only stay hand-written when no `[ComposeBridge]` shape fits:

- `AndroidUiDispatcherMain` walks a nested `Companion` type via
  `GetStaticFieldID` → `GetMethodID` → `CallObjectMethod`. It's a
  static-field-getter chain, not a single method invocation, so it
  doesn't fit any bridge shape.
- Continuations synthesized by Kotlin's `createCoroutineUnintercepted`
  (`PointerInputBlock.Invoke`) aren't in Mono.Android's peer registry;
  use `cont.JavaCast<IContinuation>()` (mirrors `LaunchedEffectBody`)
  inside the JCW body before forwarding to a generator-emitted bridge.

If you find a third shape, **extend the generator** — don't grow the
hand-written list. See `ComposeBridgeGenerator.cs` (suspend dispatch
introduced for #96).

### Conventions and footguns

- Bridges **must** return raw `IntPtr` and work in raw handles end-to-end.
  The generator enforces this; the runtime relies on it. Do not wrap
  `COROUTINE_SUSPENDED` with `Java.Lang.Object.GetObject(..,
  TransferLocalRef)` — Mono's peer cache resolves Kotlin singletons to
  globally-ref-backed wrappers that crash CheckJNI on later dispose.
- Do not wrap `JNIEnv.FindClass` results in `NewGlobalRef`/`DeleteLocalRef`;
  Mono.Android returns stable globally-registered class refs.
- Instance suspend bridges: facade passes
  `((Java.Lang.Object)Jvm).Handle`; bridge's first `IntPtr` is the
  receiver; the JNI signature does **not** include it.
- Static `$default` suspend: synthetic JVM signature is
  `(receiver, ...userParams, Continuation, int $default, Object marker)`.
  Marker passed as `IntPtr.Zero`; auto-default-mask bits set when the
  corresponding nullable user param is `null`.
- The `unbox` lambda receives the boxed `Java.Lang.Object?` Kotlin
  returned. Use `Java.Lang.Integer.IntValue()`,
  `Java.Lang.Float.FloatValue()` for boxed primitives; use non-generic
  `Invoke` for Kotlin `Unit`.
- Callers handle failures with normal `try`/`catch` around `await`.
  `SuspendBridge` detects `kotlin.Result$Failure` and faults the task
  with the underlying `Throwable`.
- One JCW continuation per call. Never reuse `SuspendContinuation`.

## Central package versioning

All `<PackageReference>` items are **versionless** at the project level.
Versions live in `Directory.Build.targets` as
`<PackageReference Update="..." Version="..." />`.

When adding a package:

1. Add versionless `<PackageReference Include="..." />` (plus metadata like
   `PrivateAssets="All"`) in the project.
2. Add matching `<PackageReference Update="..." Version="..." />` in
   `Directory.Build.targets`, under the appropriate banner.

Do not pin `Version` on `<PackageReference Include>` — bypasses central
versioning.

## Public API tracking

`Microsoft.AndroidX.Compose` uses `Microsoft.CodeAnalysis.PublicApiAnalyzers`. Every
public symbol must be in `PublicAPI.Shipped.txt` (released) or
`PublicAPI.Unshipped.txt` (pending). `RS0016` fires for missing entries,
`RS0017` for entries no longer in source.

When changing public API:

1. Build `src/Microsoft.AndroidX.Compose`; note `RS0016`/`RS0017` warnings. IDE code
   fix can update `Unshipped.txt`, **or**
2. Run:
   ```pwsh
   dotnet format analyzers src/Microsoft.AndroidX.Compose --diagnostics RS0016 RS0017 --severity warn
   ```
3. **`dotnet format` skips source-generated files** (`[ComposeFacade]` ctors,
   Android `Resource` under `obj/`). After running, rebuild and add remaining
   `RS0016` entries by hand — warning message contains the exact line to
   append.
4. Sort `PublicAPI.Unshipped.txt`; keep the leading `#nullable enable` line.

On release, move entries `Unshipped.txt` → `Shipped.txt`. Removing/renaming
public symbols is breaking — prefer new overloads + obsoleting old ones.

## Gallery demos (`src/Microsoft.AndroidX.Compose.Gallery/Demos/`)

On-device acceptance harness. **Every new public surface ships with a demo** —
facades, state holders, modifier extensions, suspend `*Async`, scope helpers.
A feature without a demo is unverified.

### Layout & naming

- One file per demo at `Demos/<CategoryFolder>/<Thing>Demo.cs`.
- Filename **must** end with `Demo.cs` (avoids grep collision with control
  files). Class name matches: `class ChipsDemo`, not `class Chips`.
- Namespace mirrors folder: `Microsoft.AndroidX.Compose.Gallery.Demos.Buttons`.
- Class is `public static`; expose `public static Demo Demo => new(...)` for
  the registry.

### Canonical template

```csharp
using Microsoft.AndroidX.Compose.Gallery.Registry;

namespace Microsoft.AndroidX.Compose.Gallery.Demos.Buttons;

/// <summary>One-line description of what this demo exercises.</summary>
public static class FillStylesDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "buttons-fill-styles",          // kebab-case, globally unique
        CategoryId:  "buttons",                      // must match a Catalog.Categories id
        Title:       "Fill styles",
        Description: "Filled, Elevated, Filled tonal, Outlined, and Text buttons.",
        Build:       c =>
        {
            var count = c.Remember(() => new MutableNumberState<int>(0));
            return new Column
            {
                new Text($"Tapped: {count}"),
                new Button(onClick: () => count++) { new Text("Filled") },
                new ElevatedButton(onClick: () => count++) { new Text("Elevated") },
            };
        });
}
```

### Wiring

1. Pick a category in `Registry/Catalog.cs` `Categories`. Add a new one (and
   folder) if none fits.
2. Register under matching `// ---- <Category> ----` block in `Catalog.Demos`,
   adjacent to related entries.
3. `CategoryId` must match the category record's `Id` — mismatch silently
   hides the demo.

### Authoring rules

- **Make it visible.** Pastel `Box(Modifier.Background(color))` tiles inherit
  `OnSurface` from M3 → light-on-dark, unreadable on dark theme. Set
  `Color = Color.Black` on `Text` inside light tiles, or use `Surface`.
- **Cap infinite heights.** Demo body scrolls vertically; a child requesting
  `FillMaxHeight()` (drawers, `WideNavigationRail`, `SubcomposeLayout`-backed)
  hits `IllegalStateException: Size(W x 2147483647)`. Wrap in
  `Box(Modifier.Height(N))`.
- **Label every tile.** Three identical-looking colored boxes say nothing —
  wrap each in a small `Column` with a caption.
- **Keep `Build` cheap.** Invoked from `composer.Remember` on every
  navigation. Allocate state with `c.Remember(() => ...)` inside the
  lambda, not in a static initializer.
- **One concept per demo.** Split "Buttons & chips & FABs".

### Verifying

```pwsh
dotnet build src/Microsoft.AndroidX.Compose.Gallery -t:Run -c Debug
```

Deep-link to a specific demo:

```pwsh
adb shell am start `
    -n net.compose.gallery/net.compose.gallery.MainActivity `
    --es route "demo/<your-demo-id>"
```

Confirm render, contrast, layout, no crash.

## Style

- TFMs: `net10.0-android` (facade, sample); `netstandard2.0` (generator —
  Roslyn requirement); `net10.0` (generator tests).
- C# 12+, nullable refs enabled, file-scoped namespaces.
- **One class per `.cs` file.** Filename matches type. Applies to every
  project. Only exception: a tiny private nested helper struct (e.g.
  `ScopeFrame` inside `RenderContext`) that's an implementation detail of its
  enclosing type.
- **No `// ---- Section ----` banners.** With one type per file the file is
  the section. Comment code only when specific logic needs clarification;
  never use comments to group classes.
- Public API gets XML docs. Non-negotiable for **any new public type** —
  hand-written class, state holder, Modifier extension, sibling stub for
  `[ComposeFacade]`. Every new `public sealed class`, `public sealed partial
  class`, public ctor/method/property gets a `<summary>` (and `<remarks>`
  when there's nuance). Internal helpers get a one-line `//` when non-
  obvious; otherwise leave bare.
- Use `ArgumentNullException.ThrowIfNull(x)` for **method/ctor parameter**
  null checks — not hand-written
  `if (x is null) throw new ArgumentNullException(nameof(x));`. Applies to
  net10.0 / net10.0-android projects only; the netstandard2.0 source
  generator doesn't have `ThrowIfNull`.
- **Always use collection expressions (`[]`) for empty arrays/lists, never
  `Array.Empty<T>()`.** All projects target `<LangVersion>latest</LangVersion>`
  on a modern compiler so the C# 12 collection-expression literal lowers
  to the same `Array.Empty<T>()` cached singleton at runtime — same byte-
  for-byte allocation, fewer characters, no `using System;` round-trip
  for files that only used `Array.Empty`. Same rule for non-empty
  literals: `[a, b, c]` rather than `new[] { a, b, c }` /
  `new List<T> { a, b, c }`. Compose chained allocations via the spread
  operator: `new T[](_keys.Length + 1) { ... ArrayCopy ... }` →
  `[.._keys, key]`. Two carve-outs: `var x = []` (no target type — keep
  `Array.Empty<T>()` or annotate) and `params T[] args = []` on
  netstandard2.0 surfaces (`params` arrays don't yet support the literal
  there). Sweep the file's `using System;` after removing the last
  `Array.Empty` consumer; the IDE0005 analyzer will flag stragglers.
- **Never use the `!` (null-forgiving postfix) operator.** When a
  property, field, or expression is typed as nullable but the runtime
  contract guarantees it's non-null at the use site, copy it to a local
  with `?? throw new InvalidOperationException(...)` carrying a message
  that names what's missing and where:
  ```csharp
  var virtualView = VirtualView
      ?? throw new InvalidOperationException("VirtualView not set on FooHandler.");
  // …use virtualView…
  ```
  Pick this form over `ArgumentNullException.ThrowIfNull` for inherited
  properties (`VirtualView`, `MauiContext`, `Context`, `PlatformView`),
  Android framework getters (`Resources.System`, `DisplayMetrics`),
  `TaskCompletionSource<T?>` null forwards, and every other non-parameter
  site — the "X not set on FooHandler" wording tells the user *which*
  contract was broken, not just a variable name. Reserve
  `ArgumentNullException.ThrowIfNull` for genuine method/ctor parameters.
  This rule has no carve-outs; the goal is consistency across the repo.
- Don't add markdown planning docs to the repo — use the session artifact
  folder.
- Commit trailer: `Co-authored-by: Copilot <223556219+Copilot@users.noreply.github.com>`.
