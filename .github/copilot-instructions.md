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
   declaration to `ComposeBridges.cs`. `composer` **must** be the last
   C# parameter — the generator detects the composer slot by
   inspecting the trailing param.
2. Add a matching `[assembly: ComposeDefaults(...)]` to
   `ComposeDefaults.cs` naming each `$default` bit. Prefix with `!` to
   consume a bit but suppress the enum member (params the caller
   always provides).
3. The generator parses the JNI signature, walks the C# parameters,
   and emits everything else.

Example:
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

### Conventions the generator relies on

- `composer` is the **last** C# parameter (always).
- Add a `int defaults` parameter immediately before `composer` only
  when the caller controls the bitmask (state-holders, multi-slot
  composables that toggle bits per call). Otherwise omit it and the
  generator builds the mask automatically: one bit per nullable /
  optional C# param the caller passed `null` for.
- Kotlin extension receivers: declare as `IntPtr` with a name ending
  in `Scope` (e.g. `IntPtr rowScope`); the generator places it at
  `args[0]` and excludes it from the `$default` count.
- `IModifier?` is special-cased to call `ComposeBridges.ModifierHandle`
  (handles `null` → `IntPtr.Zero`).
- String params are hoisted into a `IntPtr __ref_<name>` and freed in
  the generated `finally`.
- Non-void return (state holders): the generator emits
  `return CallStaticObjectMethod(...)` inside the `try`/`finally`.

### What still lives hand-written

`ModifierHandle` and the modifier-chain helpers (`PaddingAll`,
`FillMaxWidth`, etc.) — these don't follow the
`@Composable + $default` shape, so they remain plain JNI calls.

### Generator diagnostics

| ID      | Meaning                                                     |
|---------|-------------------------------------------------------------|
| CN2001  | `[ComposeBridge]` is missing/unmatched `Defaults` enum.     |
| CN2002  | Bridge signature/`Defaults` parameter count disagree.       |
| CN2003  | Bridge partial-method param doesn't match any Kotlin name.  |
| CN2004  | `[ComposeBridge]` has a malformed JNI signature.            |
| CN2005  | `Defaults` disagrees with the JNI `$default` slot.          |

**When you add a new generator diagnostic, also update this table in
`.github/copilot-instructions.md` (and the matching table for the
`ComposeDefaultsGenerator` above if it's a CN1xxx code). Source of
truth is `src/ComposeNet.SourceGenerators/Diagnostics.cs`.**

**Do not add a `[ComposeBridge]` if the binding already exposes the
method**; call the generated C# entry point instead (see
`Column.cs::Column.Render` for the canonical example).

## Facade conventions (`Composables.cs`)

- All public types derive from `ComposableNode` (a single `internal
  abstract void Render(IComposer)`).
- Container composables derive from `ComposableContainer`, which
  implements `IEnumerable` + `Add(ComposableNode?)` so callers can use
  C# collection-initializer syntax:
  ```csharp
  new Column { new Text("Hi"), new Button(onClick: …) { new Text("Tap") } }
  ```
- Wrap Kotlin lambdas with `ComposableLambda0/1/2/3` (existing
  helpers — don't hand-roll new lambda adapters).
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

## Bindings policy

The repo used to ship its own `*.Compose.*` binding projects. **Don't
bring those back.** Reference the official NuGets only:
`Xamarin.AndroidX.Compose.*` and `Xamarin.AndroidX.Compose.Material3`.
If a needed Compose API isn't bound, the workflow is:

1. File / link a tracking issue against `dotnet/android-libraries` (see
   the existing #1415–#1418 references in `README.md`).
2. Add a `[ComposeBridge]` partial method in `ComposeBridges.cs` (see
   above) — the generator handles all the JNI plumbing.
3. Add the matching `[ComposeDefaults]` declaration.
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
