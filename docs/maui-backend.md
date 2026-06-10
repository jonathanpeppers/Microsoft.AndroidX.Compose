# Compose backend for .NET MAUI

## Problem

.NET MAUI already runs on Android via an **AppCompat / Android View** backend
that lives inside `Microsoft.Maui.Controls`. Google's "Android UI Development
is Compose First" announcement (May 2026) puts the View system into
maintenance mode — all new Android UI APIs target Jetpack Compose.

`Microsoft.AndroidX.Compose` (this repo) already exposes Material 3 + Foundation
Compose as a C# facade. The opportunity is to plug that facade into MAUI as a
**custom platform backend**, the way `maui-labs` already ships custom
backends for desktops:

| Backend | TFM | UI tech | Repo |
| --- | --- | --- | --- |
| Windows.WPF | `net10.0-windows` | WPF | `dotnet/maui-labs/platforms/Windows.WPF` |
| MacOS (AppKit) | `net10.0-macos` | AppKit | `dotnet/maui-labs/platforms/MacOS` |
| Linux.Gtk4 | `net10.0` + GTK4 | GTK4 | `dotnet/maui-labs/platforms/Linux.Gtk4` |
| **Compose (this)** | `net10.0-android` | **Jetpack Compose** | (new) |

This would be the first **mobile-replacement** backend (the maui-labs ones
are all desktop) — directly competing with MAUI's stock AppCompat Android
backend and offering Material 3 / dynamic color / edge-to-edge out of the
box.

## How a MAUI backend is built (from `maui-labs/platforms/Windows.WPF`)

Each backend is a single `net10.0-<platform>` library project under
`platforms/<Name>/src/<Name>/`. The shape is identical across the three
existing ones:

```text
src/<Name>/
  Handlers/           # one pair of files per MAUI control
    ButtonHandler.cs              # cross-platform PropertyMapper + ctor
    ButtonHandler.<Platform>.cs   # partial: CreatePlatformView, Connect/DisconnectHandler, Map*
    LabelHandler.cs / .Windows.cs
    LayoutHandler.cs / .Windows.cs
    ...
  Hosting/
    AppHostBuilderExtensions.cs   # UseMauiAppXxx<TApp>() + SetupDefaults() + ViewMapper.ModifyMapping fixups
  Platform/
    MauiXxxApplication.cs         # WPF Application / NSApplication / Gtk.Application base class
    MauiXxxWindow.cs              # native window wrapper
    XxxViewHandler.cs             # generic base ViewHandler<TVirtual, TPlatform>
    LayoutPanel.cs                # ViewGroup-equivalent hosting MAUI layouts
    ContentPanel.cs               # ContentControl-equivalent hosting pages
    DispatcherProvider.cs         # IDispatcherProvider impl
    XxxFontManager.cs             # IFontManager / IFontRegistrar
    XxxTicker.cs                  # Microsoft.Maui.Animations.Ticker on the UI thread
    XxxAlertManagerSubscription.cs # DispatchProxy hack for the internal AlertManager
    ModalNavigationManager.cs     # push/pop modal pages
    GestureManager.cs             # MAUI gesture recognizers → native input
    ThemeManager.cs               # dark / light detection + AppTheme changed event
```

### The hand-off contract

1. Consumer's `App.xaml.cs` (or equivalent) calls
   `MauiApp.CreateBuilder().UseAndroidXCompose<App>().Build()`.
2. `UseAndroidXCompose<TApp>()` calls `SetupDefaults()` which:
   - Registers every handler via `builder.ConfigureMauiHandlers(...)`
     mapping MAUI control types to platform handler types
     (`handlers.AddHandler<Button, ButtonHandler>()`).
   - Registers `IDispatcherProvider`, `Ticker`, `IFontManager`,
     `IEmbeddedFontLoader`, `IFontRegistrar`, alert subscription, theme
     manager, lifecycle.
   - Calls `RemapForControls()` which uses
     `ViewHandler.ViewMapper.ModifyMapping(nameof(IView.Width), ...)`
     to bridge cross-platform properties (`IView.Width`/`Height`/
     `Background`/`Margin`/`Visibility`/`Opacity`/`IsEnabled`/
     `Translation`/`Scale`/`Rotation`/`Shadow`/`Clip`) onto the actual
     platform widget type. The base MAUI mapper targets WinUI types;
     backends override every entry.
3. Each handler is a `partial class XxxHandler : <Backend>ViewHandler<IXxx, TPlatform>` with:
   - **`XxxHandler.cs`** — pure cross-platform: the
     `PropertyMapper<IXxx, XxxHandler>` listing
     `[nameof(IButton.Text)] = MapText`, the `CommandMapper`, and two
     `()`/`(mapper, commandMapper)` ctors. Only references MAUI
     abstractions.
   - **`XxxHandler.<Platform>.cs`** — `CreatePlatformView()`,
     `ConnectHandler`/`DisconnectHandler` (wire native events), and the
     `static MapText(...)` etc. methods that mutate the platform
     widget. Uses `using` aliases to disambiguate (e.g.
     `using WButton = System.Windows.Controls.Button`) because MAUI and
     the platform share type names.
4. `<Backend>ViewHandler<TVirtual, TPlatform>` is the **critical
   adapter**. Cross-platform `ViewHandler` has no-op `PlatformArrange`
   and returns `Size.Zero` from `GetDesiredSize`. The backend overrides
   them to call native measure/arrange:
   - On WPF: `platformView.Measure(...)` / `platformView.Arrange(...)`.
   - On Android (today, MAUI stock): `View.Measure(widthSpec, heightSpec)`
     / `View.Layout(l, t, r, b)`.
5. **Layouts** reuse MAUI's cross-platform `ILayoutManager` (Grid / Stack /
   Flex / Absolute). The only platform-specific piece is a `LayoutPanel`:
   a native container (WPF `Panel`, AppKit `NSView`, GTK `Widget`,
   Android `ViewGroup`) whose `MeasureOverride` / `ArrangeOverride`
   delegates to MAUI's `LayoutManager` which in turn measures/arranges
   each child native control.
6. **Modal navigation, alerts, font registration, animation ticking,
   gestures, theme** are each implemented as small platform glue classes
   talking to MAUI's published interfaces (`IModalNavigationManager`,
   `IFontManager`, `Microsoft.Maui.Animations.Ticker`, etc.).

`WPFFontManager`, `WPFTicker`, `WPFAlertManagerSubscription` (a
`DispatchProxy` over the internal `AlertManager`), `ModalNavigationManager`,
`GestureManager`, `ThemeManager`, `LifecycleManager` are all 60–250 lines
each — small, focused, single responsibility.

## Why Compose maps cleanly onto the same shape

The MAUI handler protocol is **imperative + mutation** (`MapText` reaches
into `handler.PlatformView.Content = ...`). Compose is **declarative +
recomposition** — you don't mutate a `Button`, you call the
`Button(text=...)` function every recomposition.

The bridge is `ComposeView` (already used throughout this library —
`samples/Jetchat`, every gallery demo). `ComposeView` is an **Android
View** (`ViewGroup` subclass) that owns a private composition. Set its
content once with `view.SetContent(c => ...)` and the composition lambda
re-runs whenever any `MutableState<T>` read inside it changes.

So per-handler:

```csharp
// ButtonHandler.Android.cs
sealed partial class ButtonHandler : ComposeViewHandler<IButton, ComposeView>
{
    readonly MutableState<string>  _text     = ...;
    readonly MutableState<bool>    _enabled  = ...;
    readonly MutableState<Modifier?> _modifier = ...;
    Action? _click;

    protected override ComposeView CreatePlatformView()
    {
        var view = new ComposeView(Context);
        view.SetContent(c => new AndroidX.Compose.Button(
            onClick: () => _click?.Invoke())
        {
            Modifier = _modifier.Value,
            new Text(_text.Value),
        });
        return view;
    }

    public static void MapText(ButtonHandler h, IButton b)  => h._text.Value = b.Text ?? "";
    public static void MapIsEnabled(ButtonHandler h, IView v) => h._enabled.Value = v.IsEnabled;
    // ...
}
```

The handler's `PlatformView` is `Android.Views.View` from MAUI's
perspective — fits MAUI's layout/measure/arrange exactly the same as the
stock backend. Inside, Compose owns the rendering. Mappers write to
`MutableState<T>` slots; Compose recomposes the island automatically.

### One-ComposeView-per-handler vs. single-root composition

| Strategy | Pros | Cons |
| --- | --- | --- |
| **Option 1 — per-handler `ComposeView`** ✅ adopted Phase 1 | Trivially maps to existing handler shape; works with MAUI layout system unchanged; matches WPF/AppKit/GTK precedent; ships fast | Each leaf has its own composer + recomposer + snapshot subscription; no cross-sibling animations; M3 theming has to be re-installed per island |
| Option 2 — single root composition | Idiomatic Compose; one theme, semantics tree, lazy lists work across siblings; shared snapshot graph; better perf at scale | Need a `RenderContext`/`ComposableNode` graph mirroring MAUI's VisualTree; reimplement MAUI layout in Compose `Layout {}`; much bigger lift |

**Plan**: shipped the per-handler model in Phase 1 (matches maui-labs
precedent); the single-root composition stays as an opt-in optimisation
in a later phase. The handler API doesn't change between the two modes —
only the bootstrapping does.

#### Phase 1 Option 1 mapper rules (lessons learned)

Per-leaf `ComposeView` rendering only matches stock MAUI's defaults
once a handful of fix-ups are applied in the mappers. Document these
so subsequent handlers don't re-discover them:

- **Suppress MAUI background painting on Compose-skinned widgets.**
  Material 3 composables like `Button`, `Card`, `Surface` paint their
  own pill/card surface. If `IView.Background` runs as well, MAUI
  drops a `SolidColorBrush` drawable onto the outer `ComposeView` and
  you see a wide rectangle behind the small Material pill. Override
  the entry with a no-op:

  ```csharp
  [nameof(IView.Background)] = (h, v) => { /* Compose owns the surface */ }
  ```

  Apply to any handler whose composable owns its visual surface
  (`Button`, future `Border`, `Card`-backed controls). Leaves with no
  intrinsic surface (`Label`) should let the default `ViewMapper`
  mapping run.

- **Map `HorizontalLayoutAlignment` → `Modifier.fillMaxWidth()`** when
  the caller asks to fill or centre. Compose's `Text` only honours
  `textAlign` when its measured width spans the available space, so
  `HorizontalTextAlignment="Center"` on a stock MAUI `Label` (e.g. a
  `Headline`/`SubHeadline` style) renders left-aligned until the
  Compose `Text` also fills its slot.

- **`MutableState<T>` only handles primitives, `string`, and
  `Java.Lang.Object` subclasses.** A `MutableState<TextAlignment>`
  (MAUI enum) throws `NotSupportedException` at field-initializer
  time — crashes the app before any frame paints. Store the enum's
  backing primitive (`MutableState<int>`) and cast back inside
  `SetContent`. This is now codified in
  [`compose-maui.instructions.md`][instructions].

- **Centralise MAUI `Color` → Compose packed `long` conversion** in
  [`ColorMapping.ToPackedLong`][colormapping] so every mapper goes
  through `AndroidX.Compose.Color`'s own packing (round-to-nearest,
  no truncation off-by-one) instead of hand-rolling the bit twiddling
  per handler.

[instructions]: ../.github/instructions/compose-maui.instructions.md
[colormapping]: ../src/Microsoft.AndroidX.Compose.Maui/ColorMapping.cs

## Proposed repo layout

Add a new project alongside the existing facade. Mirrors the maui-labs
layout 1:1:

```text
src/
  Microsoft.AndroidX.Compose/                        # existing facade  (no changes)
  Microsoft.AndroidX.Compose.SourceGenerators/       # existing         (no changes)
  Microsoft.AndroidX.Compose.Maui/                   # NEW — the backend
    Handlers/
      ButtonHandler.cs            ButtonHandler.Android.cs
      LabelHandler.cs             LabelHandler.Android.cs
      EntryHandler.cs             EntryHandler.Android.cs
      EditorHandler.cs            EditorHandler.Android.cs
      ImageHandler.cs             ImageHandler.Android.cs
      CheckBoxHandler.cs / SwitchHandler / SliderHandler / ProgressBarHandler / ...
      LayoutHandler.cs            LayoutHandler.Android.cs        # Grid/Stack/Flex/Absolute
      ScrollViewHandler.cs        ScrollViewHandler.Android.cs    # Compose VerticalScroll/HorizontalScroll
      CollectionViewHandler.cs    CollectionViewHandler.Android.cs # LazyColumn<T>/LazyRow<T>
      PageHandler.cs              PageHandler.Android.cs          # Scaffold + ContentPanel
      NavigationViewHandler.cs    TabbedViewHandler.cs            # NavHost + NavigationBar / TabRow
      FlyoutViewHandler.cs        ShellHandler.cs                 # ModalNavigationDrawer + Shell
      ApplicationHandler.cs       WindowHandler.cs
      ImageButtonHandler / RadioButtonHandler / PickerHandler / DatePickerHandler /
      TimePickerHandler / StepperHandler / SearchBarHandler / RefreshViewHandler /
      ActivityIndicatorHandler / BorderHandler / ContentViewHandler / WebViewHandler /
      GraphicsViewHandler / IndicatorViewHandler / SwipeViewHandler / ...
    Hosting/
      AppHostBuilderExtensions.cs       # UseAndroidXCompose<TApp>() + SetupDefaults() + RemapForControls()
    Platform/
      ComposeViewHandler.cs             # ViewHandler<TVirtual, TPlatform : Android.Views.View> bridge
      MauiComposeActivity.cs            # AppCompatActivity replacement (extends ComponentActivity, EnableEdgeToEdge)
      ComposeLayoutViewGroup.cs         # Android.Views.ViewGroup hosting MAUI ILayoutManager (analogue of LayoutPanel)
      ComposeContentViewGroup.cs        # single-child content panel for pages (analogue of ContentPanel)
      ModifierBridge.cs                 # IView.Width/Height/Margin/Background/Transforms → AndroidX.Compose.Modifier
      DispatcherProvider.cs             # IDispatcherProvider via Android Handler/Looper.MainLooper
      ComposeTicker.cs                  # Microsoft.Maui.Animations.Ticker via Choreographer
      ComposeFontManager.cs             # IFontManager / IFontRegistrar (forwards to MAUI's existing Android font loader)
      ComposeAlertManagerSubscription.cs # DispatchProxy intercepting AlertManager → AndroidX.Compose.AlertDialog
      ModalNavigationManager.cs         # push/pop modal pages via overlay ComposeView
      ThemeManager.cs                   # Configuration.UiMode change → AppTheme
      MauiContext.cs / ApplicationExtensions.cs / ResourceProvider.cs
    Microsoft.AndroidX.Compose.Maui.csproj  # net10.0-android, references ../Microsoft.AndroidX.Compose
  Microsoft.AndroidX.Compose.Maui.Sample/   # NEW — a tiny MAUI app proving the bootstrap
    MauiProgram.cs                          # builder.UseAndroidXCompose<App>()
    App.cs / MainPage.cs                    # ContentPage with Button/Entry/Slider/CollectionView
    MainActivity.cs                         # MauiComposeActivity
```

`Microsoft.AndroidX.Compose.Maui` references `Microsoft.AndroidX.Compose` as a
project reference. The existing facade does the heavy lifting (JNI bridges,
`$default` enums, value-type packing, suspend bridges); the backend project
is **only** glue.

## Handler scope — phased plan

### Phase 1 — bootstrap + smallest possible working set ✅ shipped

Goal: render a single-page MAUI app with a button and a label using the
Compose backend.

**Delivered:**

- `Microsoft.AndroidX.Compose.Maui.csproj` (net10.0-android), project ref
  to the facade.
- `Hosting/AppHostBuilderExtensions.UseAndroidXCompose()` overlay over
  `UseMauiApp<TApp>()` that overwrites stock Android handler
  registrations for the controls we own.
- `Handlers/LabelHandler.cs`, `Handlers/ButtonHandler.cs` —
  per-leaf `ComposeView` (Option 1) backed by `MutableState<T>` slots
  for text/colour/font/alignment/fill-width. Mappers documented in the
  [Option 1 mapper rules](#phase-1-option-1-mapper-rules-lessons-learned)
  section above.
- `ColorMapping` helper — centralises every MAUI → Compose colour
  conversion through `AndroidX.Compose.Color`'s ARGB ctor.
- `Microsoft.AndroidX.Compose.Maui.Sample` — `dotnet new maui` shape
  (Shell + `Styles.xaml` + `Resources/Fonts/Images`) with
  `.UseAndroidXCompose()` flipped on, deployable via
  `dotnet build src/Microsoft.AndroidX.Compose.Maui.Sample -t:Run`.
  Visually matches stock-MAUI defaults.

**Deferred to a future phase** (not blocking Phase 1):

- `ComposeViewHandler<TVirtual, TPlatform>` base class with a custom
  `PlatformArrange` / `GetDesiredSize`. Stock MAUI's `ViewHandler<,>`
  on `net10.0-android` is good enough today because `ComposeView`
  measures itself like any `Android.Views.View`.
- `MauiComposeActivity` / `ComponentActivity` host. The sample still
  uses MAUI's stock `MauiAppCompatActivity` because per-leaf
  `ComposeView` works inside it.
- `PageHandler`, `LayoutHandler`, `ApplicationHandler`,
  `WindowHandler`, `ComposeLayoutViewGroup` / `ComposeContentViewGroup`,
  `DispatcherProvider`, `ComposeTicker`. All stock-MAUI handlers; we
  only swap leaves where Compose provides a clear win
  (Material 3 surfaces).
- `BackgroundColor` mapping on `Button` — needs a `ButtonColors`
  bridge in the facade (current `Button` facade only exposes
  `Shape`/`ContentPadding`). Accept M3 primary-purple default for now.
- Custom `FontFamily`, italic, `FontAttributes.Italic`, decoration,
  line-height passthrough.
- Image handler — stock MAUI handler keeps rendering
  `dotnet_bot.png`; replacing with Compose's `Image` lands later.

### Phase 2 — input + visual breadth (target: "Control Gallery parity for leaves")

The list mirrors the WPF backend's leaf controls:

`Entry`, `Editor`, `Image` (file/uri/stream/font sources), `ImageButton`,
`CheckBox`, `Switch`, `Slider`, `ProgressBar`, `ActivityIndicator`,
`Stepper`, `RadioButton`, `Picker`, `DatePicker`, `TimePicker`,
`SearchBar`, `Border`, `BoxView`, `ContentView`, `ScrollView`,
`RefreshView`, `IndicatorView`.

Each is a ~150-line handler pair backed by the existing facade
(`Button`, `Text`, `TextField`, `OutlinedTextField`, `Switch`,
`Slider`, `CircularProgressIndicator`, `LinearProgressIndicator`,
`Checkbox`, `RadioButton`, `Image`, `Icon`, `BoxView`-equivalent,
`Card`, `Border`, `LazyColumn`-with-PullToRefreshBox, `IndicatorView`-as-`Row`-of-dots).

Also in Phase 2: `ComposeAlertManagerSubscription` (intercept
`AlertManager` → render via `AlertDialog`), `ComposeFontManager`,
`ThemeManager`, `ModifierBridge` for visibility/opacity/transforms/clip/shadow.

### Phase 3 — collection + container (target: list-driven apps)

`CollectionView` → `LazyColumn<T>` / `LazyRow<T>` / `LazyVerticalGrid<T>`
chosen by `ItemsLayout`. `ListView` → same. `CarouselView` →
`HorizontalPager` (+ PagerState). `TableView` → grouped `LazyColumn`.
`SwipeView` → `Modifier.Swipeable` (or `SwipeToDismissBox` if/when
wrapped).

This phase is also where it gets clear whether per-handler ComposeView
scales — lazy lists with 10000 items shouldn't allocate 10000 composers.
Likely the `CollectionViewHandler` becomes a single `LazyColumn<T>`
whose items render *managed* `ComposableNode`s built from MAUI's
`DataTemplate`, sidestepping per-cell `ComposeView` islands.

### Phase 4 — navigation (target: real apps)

- `NavigationViewHandler` → Compose `NavHost` + `NavController` (already
  wrapped). Push/pop maps to `navController.Navigate` / `PopBackStack`.
- `TabbedViewHandler` → `NavigationBar` (bottom) or `TabRow` (top
  depending on `BarPosition`).
- `FlyoutViewHandler` → `ModalNavigationDrawer`.
- `ShellHandler` → `Scaffold` + `ModalNavigationDrawer` + `NavigationBar`
    - `NavHost` (URI-routed). This is the biggest single handler — keep
  scope tight, target Shell's tabbed shape first, flyout second, routing
  third.
- `ModalNavigationManager` — overlay `ComposeView` on the activity's
  decor view; animate with `AnimatedVisibility`.

### Phase 5 — graphics, gestures, shapes, BlazorWebView, infra

- `GraphicsViewHandler` → reuse MAUI's `PlatformGraphicsView` (Android
  `View` subclass) inside an `AndroidView { ... }` composable, OR map
  `ICanvas` to Compose's `Canvas` composable.
- `WebViewHandler` / `HybridWebViewHandler` → Android `WebView` inside
  `AndroidView { ... }`. `BlazorWebViewHandler` already has an
  Android-specific implementation in
  `Microsoft.AspNetCore.Components.WebView.Maui` — reuse, just plug into
  Compose's interop.
- Shapes (`Rectangle`/`Ellipse`/`Path`/...) → Compose `Canvas` drawing
  via `DrawScope`.
- Gestures: MAUI's gesture recognizers already work on any Android
  `View`. The challenge is `Tap`/`Pan`/`Pinch` competing with Compose's
  own gesture detectors — `Modifier.PointerInput { }` may need to
  forward gestures to MAUI's `GestureManager`.

### Phase 6 — Essentials (parallel work, optional)

A `Microsoft.AndroidX.Compose.Maui.Essentials` project that maps MAUI's
`IFilePicker`, `IShare`, `IBattery`, `IConnectivity`, `IPreferences`,
`ISecureStorage`, `IGeolocation` to Android's stock APIs (mostly
identical to what MAUI's stock Essentials already does — likely just
delegate, but some essentials might need Compose-aware UIs like
`FilePicker` using a Compose `BottomSheet`).

## Open design questions (resolve before implementation starts)

1. **`AppCompatActivity` vs `ComponentActivity`** — MAUI's stock
   Android backend assumes `AppCompatActivity`. This backend would
   require apps to use `ComponentActivity` (Compose's host). Document
   the migration; provide `MauiComposeActivity` base class so consumers
   just inherit.
2. **Does `Microsoft.Maui.Controls` even resolve under
   `net10.0-android` without dragging in the stock AppCompat
   handlers?** The maui-labs WPF/MacOS/GTK backends explicitly use the
   **platform-agnostic** `net10.0` MAUI assemblies and rely on the fact
   that `ToPlatform()` returns `object` on net10.0 (handlers register
   themselves). On `net10.0-android`, MAUI's `Controls` package will
   resolve to the Android-specific build and try to register its own
   handlers. The hosting extension needs to either:
   - Run `SetupDefaults` *after* the stock `UseMauiApp` and overwrite
     every handler registration (verified pattern in WPF backend);
   - Or short-circuit `UseMauiApp` and skip the stock platform's
     `ConfigureMauiHandlers`.

   **Action:** prove this with a Phase 1 smoke test before designing
   anything else.

3. **One ComposeView per handler — is the per-island theme acceptable
   for v1?** Each island needs its own `MaterialTheme` wrapper around
   the facade. Either every handler wraps in `MaterialTheme { ... }`
   (heavy; theme can't be customized per-app), or the host
   `MauiComposeActivity` installs a single root `MaterialTheme` and
   each `ComposeView` is wrapped in a thin reader composable that
   inherits the host theme via `CompositionLocal` (need to verify this
   works across separate ComposeView instances — it may NOT, in which
   case we ship per-island theming for v1 with single-root as Phase 2).

4. **Layout sizing** — Compose composables size themselves; MAUI wants
   you to honor an explicit `widthSpec`/`heightSpec`. Need to
   translate MAUI's `MeasureFlags` to Compose constraints, probably
   inside the `ComposeView` host: `Modifier.Width(constraint.maxWidth)`
   when the spec is `Exactly`, no width modifier when `AtMost`. This
   is the same problem AndroidView/ComposeView interop already solves
   in the Android team's docs — copy that.

5. **AOT/Trimming** — every handler reference cascades into JNI
   bridge usage. The existing facade is already AOT-friendly (no
   reflection on JNI types). The MAUI handler registration uses
   `DynamicallyAccessedMembers` annotations — keep those consistent
   with the WPF backend's pattern.

## Out of scope

- iOS / catalyst — Jetpack Compose is Android-only. Compose
  Multiplatform exists but isn't what this library wraps.
- The proposed Roslyn source generator that lets you author
  `[Composable]`-attributed C# methods directly (Tier 2 from
  [`architecture.md`](architecture.md)). The Compose MAUI backend uses
  the existing Tier 1 tree facade — not blocked on that.
- Replacing MAUI's `BindingContext` / XAML loader / hot reload — those
  all live above the handler boundary and just work.