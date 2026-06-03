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

## `ComposeBridges.cs` — raw JNI bridges

When an overload is stripped from the binding we call it via
`JNIEnv.FindClass` + `GetStaticMethodID` + `CallStaticVoidMethod`.
Pattern, copied throughout the file:

1. Cache the JNI class + method handles in `static IntPtr` fields
   (initialise lazily on first call — Android's class loader is slow).
2. Build the full JNI signature string as a `const string` next to the
   bridge method, with a comment showing the Kotlin parameter list in
   source order.
3. Allocate `JValue* args = stackalloc JValue[N]`; fill positionally.
4. The `$default` bitmask is the **last** `int` arg (after any
   `$changed`/`$changed1`/… and `composer`). Compute it from the
   matching `XxxDefault.All` constant.
5. For string params: `IntPtr ref = JNIEnv.NewString(s);` inside a
   `try`/`finally` that calls `DeleteLocalRef`.
6. Pass managed objects as `((Java.Lang.Object)obj).Handle`. Use
   `IntPtr.Zero` for `null`.
7. **Always wrap `JNIEnv.CallStatic*Method` in `try { … } finally { … }`
   and call `GC.KeepAlive(...)` on every managed parameter whose
   `.Handle` was read into a `JValue` (lambdas, the composer, optional
   slot wrappers — `null` is a no-op so unconditional `KeepAlive` on
   nullable params is fine).** This matches what
   `dotnet/java-interop` generates for bound members. Without it, the
   JIT considers each managed wrapper dead the instant `.Handle` is
   read, and a GC during the JNI call can finalize the wrapper and
   invalidate the underlying handle. Combine with the `DeleteLocalRef`
   `finally` for string args — one `try`/`finally` doing both is the
   normal shape.

Pattern:
```csharp
JValue* args = stackalloc JValue[N];
// fill args ...
try
{
    JNIEnv.CallStaticVoidMethod(s_cls, s_method, args);
}
finally
{
    GC.KeepAlive(onClick);
    GC.KeepAlive(content);
    GC.KeepAlive(composer);
}
```

Keep these methods `internal` — user code never touches JNI directly.
**Do not add new JNI bridges if the binding already exposes the
method**; call the generated C# entry point instead (see
`Composables.cs::Column.Render` for the canonical example).

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
2. Add a JNI bridge in `ComposeBridges.cs` as a temporary measure.
3. Generate the matching `$default` enum via the declarative attribute
   form above.
4. When the upstream binding fix ships, delete the bridge and switch
   to the generic attribute form.

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
