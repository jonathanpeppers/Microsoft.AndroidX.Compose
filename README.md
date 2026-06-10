# Microsoft.AndroidX.Compose

Build Android UI with **Jetpack Compose** from a .NET for Android app — pure C#, no Kotlin in the project, on top of the existing `Xamarin.AndroidX.Compose.*` bindings.

<p align="center">
  <img src="docs/images/hello-compose-csharp.png" alt="Hello from .NET running Jetpack Compose UI on Android" width="380" />
</p>

*Material 3 sample inside the gallery's "Hello from .NET" demo: `Text`, a `Button`, and a counter driven by `mutableStateOf` — all authored from C#.*

## Why

[*Android UI Development is Compose First*](https://android-developers.googleblog.com/2026/05/android-ui-development-is-compose-first.html) (Nick Butcher, May 2026) puts Views, Fragments, RecyclerView, and the View-based tooling into **maintenance mode**. All new Android UI APIs target Compose. .NET for Android needs a story — analogous to UIKit→SwiftUI in 2019.

This repo is **Tier 1**: a C#-only .NET-for-Android app that hosts Compose UI by calling the existing `androidx.compose.*` runtime through the existing Xamarin bindings. No new compiler, no new runtime, no Kotlin source. A potential Tier 2 (a Roslyn source generator that lets you author `[Composable]`-attributed C# methods directly) is out of scope here — see [docs/compose-internals.md](docs/compose-internals.md) for that analysis.

## Build & run

Requires the .NET 10 SDK with the `android` workload and an Android API 34+ emulator or device.

```pwsh
dotnet workload restore
dotnet build src/Microsoft.AndroidX.Compose.Gallery -t:Run    # deploys to the connected device/emulator
```

Generator unit tests run without an Android SDK:

```pwsh
dotnet test src/Microsoft.AndroidX.Compose.SourceGenerators.Tests
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
public class MainActivity : ComponentActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        this.SetContent(c =>
        {
            var count = c.MutableStateOf(0);
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

The translation is mechanical — `new` instead of bare calls, commas instead of newlines, `c => ` lambdas thread the `IComposer` explicitly (the equivalent of Kotlin's IR-injected `$composer`):

| Kotlin                                        | C# (this repo)                                                  |
| --------------------------------------------- | --------------------------------------------------------------- |
| `setContent { … }`                            | `this.SetContent(c => { … })` on `ComponentActivity`            |
| `Text("Hi")`                                  | `new Text("Hi")`                                                |
| `Column { … }`                                | `new Column { … }` (collection-initializer)                     |
| `Button(onClick = { x++ }) { … }`             | `new Button(onClick: () => x++) { … }`                          |
| `MaterialTheme { … }`                         | `new MaterialTheme { … }`                                       |
| `var count by remember { mutableStateOf(0) }` | `var count = c.MutableStateOf(0)`                               |
| `count++`                                     | `count++` (operator on `MutableNumberState<T>`, picked by overload resolution for `int`/`long`/`float`/`double`) |
| `"Count: $count"`                             | `$"Count: {count}"` (via `MutableState<T>.ToString`)            |
| `if (count > 0) …`                            | `if (count > 0) …` (implicit `MutableState<T>` → `T`)           |

That's an end-to-end Material 3 counter app in ~13 lines of composition — start from this shape when adding a new screen. The actual [`src/Microsoft.AndroidX.Compose.Gallery/MainActivity.cs`](src/Microsoft.AndroidX.Compose.Gallery/MainActivity.cs) in the repo is a much larger **gallery app** that exercises every facade across a navigable catalog with search; for a single-screen real-app example see [`samples/Jetchat`](samples/Jetchat).

## What's wrapped today

The facade [`Microsoft.AndroidX.Compose`](src/Microsoft.AndroidX.Compose) covers the common Material 3 + Foundation surface:

| Category                | Composables |
| ----------------------- | ----------- |
| Theme & layout          | `MaterialTheme` (parameterizable `ColorScheme`/`Typography`/`Shapes`/`Dark`/`UseDynamicColor`, plus `CurrentColorScheme`/`CurrentTypography`/`CurrentShapes` reads), `Column`, `Row` (`Arrangement`), `Box`, `Spacer`, `Scaffold`, `HorizontalDivider`, `VerticalDivider`, `BoxWithConstraints` |
| Lazy lists & paging     | `LazyColumn<T>`, `LazyRow<T>`, `LazyVerticalGrid<T>`, `LazyHorizontalGrid<T>`, `LazyVerticalStaggeredGrid<T>`, `LazyHorizontalStaggeredGrid<T>` (+ `GridCells`/`StaggeredGridCells`), `HorizontalPager`, `VerticalPager` (+ `PagerState`), `FlowRow`, `FlowColumn` |
| Carousels & pull        | `HorizontalMultiBrowseCarousel`, `HorizontalCenteredHeroCarousel`, `HorizontalUncontainedCarousel`, `PullToRefreshBox` (+ `PullToRefreshState`) |
| Surfaces                | `Surface`, `Card`, `ElevatedCard`, `OutlinedCard` |
| App bars                | `TopAppBar` family (Center/Medium/Large/Flexible — with optional subtitles via Phase 9 branching), `BottomAppBar`, `FlexibleBottomAppBar` |
| Tabs                    | `TabRow` family (Primary/Secondary, scrollable variants), `Tab`, `LeadingIconTab`, `CustomTab` |
| Buttons                 | `Button`, `OutlinedButton`, `TextButton`, `ElevatedButton`, `FilledTonalButton`, `IconButton`, `OutlinedIconButton`, `FilledIconButton`, `FilledTonalIconButton`, full `IconToggleButton` family, `FloatingActionButton` (+ `Small`/`Large`/`Extended` variants) |
| Text & input            | `Text` (`TextStyle`/`FontWeight`/`FontStyle`/`FontFamily`/`TextDecoration`/`TextAlign`/`TextOverflow`), `TextField`, `OutlinedTextField`, `SecureTextField`, `OutlinedSecureTextField` |
| Media                   | `Image`, `Icon` (drawable-resource and `ImageVector` overloads), `Icons` (Filled/Outlined/Rounded/Sharp/TwoTone + AutoMirrored) |
| Chips                   | `AssistChip`, `FilterChip`, `InputChip`, `SuggestionChip` (+ `Elevated*` variants) |
| Selection               | `Checkbox`, `TriStateCheckbox`, `RadioButton`, `Switch`, `Slider`, `RangeSlider`, `SegmentedButton`, `SingleChoiceSegmentedButtonRow`, `MultiChoiceSegmentedButtonRow` |
| Progress & feedback     | `CircularProgressIndicator`, `LinearProgressIndicator`, `ListItem`, `Badge`, `BadgedBox` |
| Menus & search          | `DropdownMenu` + `DropdownMenuItem`, `ExposedDropdownMenuBox` + `ExposedDropdownMenu`, `SearchBar` family (Top, ExpandedDocked, ExpandedFullScreen, `DockedSearchBar`) |
| Navigation              | `NavHost`, `NavController`, `NavBackStackEntry`, `NavigationBar`, `NavigationRail`, `WideNavigationRail`, `ModalWideNavigationRail` (+ items) |
| Drawers                 | `ModalNavigationDrawer`, `DismissibleNavigationDrawer`, `PermanentNavigationDrawer` (+ matching sheets, generated via Phase 10 `[ConfirmStateChange]`) |
| Sheets & pickers        | `ModalBottomSheet`, `BottomSheetScaffold`, `DatePicker`/`DatePickerDialog`, `DateRangePicker`/`DateRangePickerDialog`, `TimePicker`/`TimeInput`/`TimePickerDialog` |
| Overlays                | `AlertDialog`, `Snackbar` + `SnackbarHost`, `Tooltip` |
| Animation               | `AnimatedVisibility`, `AnimatedContent`, `Crossfade` |
| Effects                 | `composer.LaunchedEffect`, `composer.DisposableEffect`, `composer.SideEffect` |
| Modifier chains         | `Padding`, `FillMaxWidth/Height/Size`, `Width`, `Height`, `Size`, `AspectRatio`, `Offset`, `Alpha`, `Background`, `Border`, `Clip`, `Clickable`, `Weight`, `VerticalScroll`/`HorizontalScroll` (+ `ScrollState`), `Draggable` (+ `DraggableState`), focus/semantics/gestures, `SafeDrawingPadding`, `SystemBarsPadding`, plus per-inset `ImePadding`, `NavigationBarsPadding`, `StatusBarsPadding`, `CaptionBarPadding`, `DisplayCutoutPadding`, `WaterfallPadding`, `SystemGesturesPadding`, `MandatorySystemGesturesPadding`, `SafeContentPadding`, `SafeGesturesPadding` |
| Value types             | `Color` (+ `FromRgb`/`FromArgb`/`FromHex` and theme reads), `Dp`, `Sp`, `FontWeight`, `TextAlign`, `Shape` |
| State                   | `Remember` (+ keyed `Remember(factory, key1, …)`, `RememberKeyed`), `RememberSaveable` (+ keyed), `MutableState<T>`, `MutableNumberState<T>`, `MutableStateList<T>`, `MutableStateMap<K,V>`, `DerivedStateOf`, `ProduceState`, plus `DatePickerState`, `DateRangePickerState`, `TimePickerState`, `SearchBarState`, `SnackbarHostState`, `ScrollState`, `PagerState`, `PullToRefreshState`, `DraggableState`, `DrawerStateHolder`, `WideNavigationRailState`, `FocusRequester`/`FocusState` |
| Async                   | `SuspendBridge` — Kotlin `suspend` functions surfaced as C# `Task` / `Task<T>` (drives `ScrollState.ScrollToAsync`, `LazyListState.AnimateScrollToItemAsync`, `SnackbarHostState.ShowSnackbarAsync`, etc.) |

## Samples

[`samples/`](samples) mirrors the official [`android/compose-samples`](https://github.com/android/compose-samples) repo in C#. See [`samples/README.md`](samples/README.md) for the scoreboard of which samples are ported and what was simplified.

## Status

The gallery builds, deploys to an Android 16 (API 36) emulator, and renders a real Material 3 UI end-to-end: dynamic Material You colors via parameterizable `MaterialTheme`, edge-to-edge layout, an interactive `Button` that increments `MutableNumberState<int>` and recomposes the count. The catalog app in [`src/Microsoft.AndroidX.Compose.Gallery`](src/Microsoft.AndroidX.Compose.Gallery) exercises the full facade across a category-organized, navigable, searchable surface (text styling, lists, pickers, dialogs, sheets, navigation, animation, effects, search, dropdowns, draggable modifiers, …).

The facade and sample reference the official `Xamarin.AndroidX.Compose.*` 1.11.2.x and `Xamarin.AndroidX.Compose.Material3` 1.4.0.x NuGets directly — the per-binding projects this repo originally needed have been deleted.

## Docs

- [docs/architecture.md](docs/architecture.md) — how the facade works, JNI bridges, the `$default` source generator, what's missing on the C# side.
- [docs/compose-internals.md](docs/compose-internals.md) — how Jetpack Compose actually works (Kotlin compiler plugin, IR pipeline), why we can't just port it, and the Maven/NuGet artifact map.
- [docs/NOTES.md](docs/NOTES.md) — historical notes from the original Tier 1 experiment, including the in-repo binding projects that have since been deleted.
