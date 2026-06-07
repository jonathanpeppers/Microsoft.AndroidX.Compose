# compose-net

Build Android UI with **Jetpack Compose** from a .NET for Android app — pure C#, no Kotlin in the project, on top of the existing `Xamarin.AndroidX.Compose.*` bindings.

<p align="center">
  <img src="docs/images/hello-compose-csharp.png" alt="Hello from .NET running Jetpack Compose UI on Android" width="380" />
</p>

*Material 3 sample on an Android emulator: `Text`, a `Button`, and a counter driven by `mutableStateOf` — all authored from C#.*

## Why

[*Android UI Development is Compose First*](https://android-developers.googleblog.com/2026/05/android-ui-development-is-compose-first.html) (Nick Butcher, May 2026) puts Views, Fragments, RecyclerView, and the View-based tooling into **maintenance mode**. All new Android UI APIs target Compose. .NET for Android needs a story — analogous to UIKit→SwiftUI in 2019.

This repo is **Tier 1**: a C#-only .NET-for-Android app that hosts Compose UI by calling the existing `androidx.compose.*` runtime through the existing Xamarin bindings. No new compiler, no new runtime, no Kotlin source. A potential Tier 2 (a Roslyn source generator that lets you author `[Composable]`-attributed C# methods directly) is out of scope here — see [docs/compose-internals.md](docs/compose-internals.md) for that analysis.

## Build & run

Requires the .NET 10 SDK with the `android` workload and an Android API 34+ emulator or device.

```pwsh
dotnet workload restore
dotnet build src/ComposeNet.Sample -t:Run    # deploys to the connected device/emulator
```

Generator unit tests run without an Android SDK:

```pwsh
dotnet test src/ComposeNet.SourceGenerators.Tests
```

## What it looks like

Side-by-side Kotlin vs C#. Both render the same screen: a Material 3 themed counter app.

### Kotlin

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

### C# (this repo)

```csharp
[Activity(Label = "@string/app_name", MainLauncher = true,
          Theme = "@android:style/Theme.Material.Light.NoActionBar")]
public class MainActivity : ComposeActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        SetContent(() =>
        {
            var count = Remember(() => new MutableNumberState<int>(0));
            return new MaterialTheme
            {
                new Column
                {
                    Modifier.Companion.SafeDrawingPadding(),
                    new Text("Hello from .NET"),
                    new Text($"Count: {count}"),
                    new Button(onClick: () => count++)
                    {
                        new Text("Tap to increment"),
                    },
                },
            };
        });
    }
}
```

The translation is mechanical — `new` instead of bare calls, commas instead of newlines, `() =>` instead of `{ … }` lambdas:

| Kotlin                                        | C# (this repo)                                                |
| --------------------------------------------- | ------------------------------------------------------------- |
| `setContent { … }`                            | `SetContent(() => { … })` on `ComposeActivity`                |
| `Text("Hi")`                                  | `new Text("Hi")`                                              |
| `Column { … }`                                | `new Column { … }` (collection-initializer)                   |
| `Button(onClick = { x++ }) { … }`             | `new Button(onClick: () => x++) { … }`                        |
| `MaterialTheme { … }`                         | `new MaterialTheme { … }`                                     |
| `var count by remember { mutableStateOf(0) }` | `var count = Remember(() => new MutableNumberState<int>(0))`  |
| `count++`                                     | `count++` (operator on `MutableNumberState<T>`)               |
| `"Count: $count"`                             | `$"Count: {count}"` (via `MutableState<T>.ToString`)          |

That's the entire [`MainActivity.cs`](src/ComposeNet.Sample/MainActivity.cs) — ~27 lines including ceremony, 13 for the composition itself.

## What's wrapped today

The facade [`ComposeNet.Compose`](src/ComposeNet.Compose) covers the common Material 3 + Foundation surface:

| Category                | Composables |
| ----------------------- | ----------- |
| Theme & layout          | `MaterialTheme`, `Column`, `Row`, `Box`, `Spacer`, `Scaffold`, `HorizontalDivider`, `VerticalDivider` |
| Lazy lists              | `LazyColumn<T>`, `LazyRow<T>`, `LazyVerticalGrid<T>`, `LazyHorizontalGrid<T>` (+ `GridCells`) |
| Surfaces                | `Surface`, `Card`, `ElevatedCard`, `OutlinedCard` |
| App bars                | `TopAppBar` family (Center/Medium/Large/Flexible), `BottomAppBar`, `FlexibleBottomAppBar` |
| Tabs                    | `TabRow` family (Primary/Secondary, scrollable variants), `Tab`, `LeadingIconTab`, `CustomTab` |
| Buttons                 | `Button`, `IconButton`, `FloatingActionButton` |
| Text & input            | `Text`, `TextField`, `OutlinedTextField` |
| Media                   | `Image`, `Icon` |
| Chips                   | `AssistChip`, `FilterChip`, `InputChip`, `SuggestionChip` (+ `Elevated*` variants) |
| Selection               | `Checkbox`, `TriStateCheckbox`, `RadioButton`, `Switch`, `Slider`, `RangeSlider`, `SegmentedButton` |
| Progress & feedback     | `CircularProgressIndicator`, `LinearProgressIndicator`, `ListItem`, `Badge`, `BadgedBox` |
| Menus & search          | `DropdownMenu` + `DropdownMenuItem`, `SearchBar` family (Top, ExpandedDocked, ExpandedFullScreen) |
| Navigation              | `NavigationBar`, `NavigationRail`, `WideNavigationRail`, `ModalWideNavigationRail` (+ items) |
| Drawers                 | `ModalNavigationDrawer`, `DismissibleNavigationDrawer`, `PermanentNavigationDrawer` |
| Sheets & pickers        | `ModalBottomSheet`, `BottomSheetScaffold`, `DatePicker`/`DatePickerDialog`, `TimePicker`/`TimePickerDialog` |
| Overlays                | `AlertDialog`, `Snackbar` + `SnackbarHost`, `Tooltip` |
| Modifier chains         | `Padding`, `FillMaxWidth/Height/Size`, `Width`, `Height`, `Size`, `SafeDrawingPadding`, `SystemBarsPadding`, plus per-inset `ImePadding`, `NavigationBarsPadding`, `StatusBarsPadding`, `CaptionBarPadding`, `DisplayCutoutPadding`, `WaterfallPadding`, `SystemGesturesPadding`, `MandatorySystemGesturesPadding`, `SafeContentPadding`, `SafeGesturesPadding` |
| State                   | `Remember` (+ keyed `Remember(factory, key1, …)`, `RememberKeyed`), `RememberSaveable` (+ keyed), `MutableState<T>`, `MutableNumberState<T>`, `MutableStateList<T>`, `MutableStateMap<K,V>`, `DerivedStateOf`, `ProduceState`, plus `DatePickerState`, `TimePickerState`, `SearchBarState`, `SnackbarHostState` |

## Samples

[`samples/`](samples) mirrors the official [`android/compose-samples`](https://github.com/android/compose-samples) repo in C#. See [`samples/README.md`](samples/README.md) for the scoreboard of which samples are ported and what was simplified.

## Status

The sample builds, deploys to an Android 16 (API 36) emulator, and renders a real Material 3 UI end-to-end: dynamic Material You colors, edge-to-edge layout, an interactive `Button` that increments `MutableNumberState<int>` and recomposes the count.

The facade and sample reference the official `Xamarin.AndroidX.Compose.*` 1.11.2.x and `Xamarin.AndroidX.Compose.Material3` 1.4.0.x NuGets directly — the per-binding projects this repo originally needed have been deleted.

## Docs

- [docs/architecture.md](docs/architecture.md) — how the facade works, JNI bridges, the `$default` source generator, what's missing on the C# side.
- [docs/compose-internals.md](docs/compose-internals.md) — how Jetpack Compose actually works (Kotlin compiler plugin, IR pipeline), why we can't just port it, and the Maven/NuGet artifact map.
- [docs/NOTES.md](docs/NOTES.md) — historical notes from the original Tier 1 experiment, including the in-repo binding projects that have since been deleted.
