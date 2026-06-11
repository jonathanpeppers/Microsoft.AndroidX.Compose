# Microsoft.AndroidX.Compose.Maui sample

The smallest possible .NET MAUI app wired up to the experimental
**`Microsoft.AndroidX.Compose.Maui`** handler backend. It mirrors the
shape of `dotnet new maui` (Shell + `MainPage.xaml` + Resources/Styles
+ OpenSans fonts + splash + appicon) so the rendered output can be
compared to the stock AppCompat-backed template one-for-one.

The interesting line is in
[`MauiProgram.cs`](MauiProgram.cs):

```csharp
builder
    .UseMauiApp<App>()
    .UseAndroidXCompose();   // <-- swap stock handlers for Compose ones
```

`UseAndroidXCompose()` overwrites the Android registrations for the
controls the backend currently owns (Label, Button). Everything else
falls back to MAUI's stock handler, so the rest of the page renders
through normal AppCompat views.

## Side-by-side with `dotnet new maui`

Same APK shape, same fonts, same status bar, same splash. The only
intentional visible delta in Phase 1 is the **Button container color** —
Compose Material 3 ships `#6750A4` and we don't yet wire up MAUI's
`Primary` resource (`#512BD4`). That's tracked as a Phase 2 follow-up
in [`docs/maui-backend.md`](../../docs/maui-backend.md).

| Compose backend (this sample)                                 | Stock MAUI template (`dotnet new maui`)                              |
| :-----------------------------------------------------------: | :-------------------------------------------------------------------: |
| <img src="../../docs/maui-sample.png" width="320" alt="Microsoft.AndroidX.Compose.Maui sample running on Android" /> | <img src="../../docs/maui-sample-stock.png" width="320" alt="dotnet new maui template running on Android" /> |

## Run

```pwsh
dotnet build src/Microsoft.AndroidX.Compose.Maui.Sample -t:Run
```

Requires the `android` workload and a connected device or emulator.

## What's verified

- Splash → Shell handoff matches the template (`Maui.SplashTheme`,
  `colorPrimaryDark` status bar).
- **One `ComposeView` per page** for fully-converted layouts (Page →
  VSL/HSL/ScrollView → Label/Button/Entry/Image). Verify with
  `adb shell uiautomator dump` — pages whose root is one of our
  converted containers (e.g. `CounterPage`'s `ScrollView` + VSL) show
  exactly one `androidx.compose.ui.platform.ComposeView` node.
- `Label` renders through Compose `Text` — TextColor, FontSize,
  FontWeight, HorizontalTextAlignment, HorizontalLayoutAlignment
  (→ `Modifier.FillMaxWidth()`).
- `Button` renders through Compose Material 3 `Button` — Click event,
  Text, HorizontalLayoutAlignment, `IView.Background` routed into
  `ButtonColors.containerColor`.
- `Entry`, `Image` follow the same `IComposeHandler` pattern.
- `VerticalStackLayout` / `HorizontalStackLayout` render as Compose
  `Column` / `Row`; `ScrollView` wraps content in
  `Modifier.verticalScroll` / `horizontalScroll`.
- **Unknown / unconverted controls** (`CollectionView`, `BoxView`,
  `Grid`, third-party renderers) are hosted via Compose's
  `AndroidView { factory = child.ToPlatform(MauiContext) }` interop
  from inside the page composition — MAUI's normal handler resolution
  still runs them, and Compose just hosts the resulting Android
  `View`. `HomePage` exercises this path (root `Grid` + `CollectionView`).

## What's intentionally deferred

See [`docs/maui-backend.md`](../../docs/maui-backend.md) for the full
phased plan. Notable gaps:

- All other leaf controls: Editor, CheckBox, Switch, Slider,
  ProgressBar, ActivityIndicator, …
- Layouts that need the generic
  `androidx.compose.ui.layout.Layout {}` adapter forwarding to MAUI's
  `ILayoutManager`: Grid, AbsoluteLayout, FlexLayout, StackLayout.
  Today these stay on the stock `LayoutHandler` and are hosted via
  `AndroidView` from inside the page composition.
- Compose-backed leaves inside a stock container (e.g. `CollectionView`
  item template) currently fall back to one `ComposeView` per leaf
  because they can't fold into a parent composer that doesn't exist.
  The "one ComposeView per page" invariant only holds when the entire
  path from `PageHandler` down uses our handlers.
- Compose `ComposeView` consumes pointer events; a Compose-backed leaf
  inside a stock `CollectionView` item template swallows taps that
  would otherwise trigger MAUI `TapGestureRecognizer`s on parent
  containers. Workaround: put a stock leaf (e.g. `Switch`) inside the
  item template, or convert the container.
- BackgroundColor, Border, Shadow on the `ContentPage` itself —
  `PageHandler` doesn't inherit MAUI's `ContentViewHandler`, so page-
  level cross-platform properties don't flow. Apply via Compose
  modifiers inside the composition instead.
