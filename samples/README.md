# Microsoft.AndroidX.Compose samples

C# ports of selected apps from
[android/compose-samples](https://github.com/android/compose-samples)
to demonstrate the `Microsoft.AndroidX.Compose` facade running on
.NET-for-Android. Each port is **simplified** to fit the current facade
surface — the per-sample `README.md` lists what was kept, what was cut,
and which facade features had to land first.

Build any sample with:

```pwsh
dotnet build samples/<Name> -c Release
dotnet build samples/<Name> -t:Run        # deploy + run on device
```

## Checklist

Status legend: ✅ done · 🚧 in progress · ⬜️ not started · ❌ blocked
(needs binding work upstream).

| Complexity (upstream) | Sample      | Status | Notes |
|----------------------:|-------------|:------:|-------|
| Low                   | **Jetchat** | ✅      | Multi-channel chat with programmatic navigation drawer, profile routes, reverse-layout message log, emoji selector, recording approximation, attachments, drag/drop feedback, dynamic color, and upstream wordmark. Remaining differences are linked reusable API gaps in `Jetchat/README.md`. |
| Medium                | JetNews     | ✅      | Simplified phone-only single-pane port — three screens (Home / Article / Interests), navigation drawer with two destinations and auto-close on item tap, hamburger top-bar toggle, `PrimaryTabRow` on Interests, per-post bookmark toggle, six condensed seed posts with solid-color hero panels. Adaptive list-detail layout, inline paragraph spans, `nestedScroll` top-bar elevation, and real hero PNGs are all pending. See `JetNews/README.md`. |
| Medium                | Reply       | ✅      | Simplified phone-only single-pane port — bottom-nav scaffold with 4 destinations, inbox `LazyColumn` of cards with `AnimatedContent`-swapped selected avatar and `CombinedClickable` (tap/long-press) multi-select, email-detail `Scaffold` with thread cards, Articles/DMs/Groups stub screens. The only outstanding facade gap is TwoPane / fold-aware list-detail ([#168](https://github.com/jonathanpeppers/Microsoft.AndroidX.Compose/issues/168)); `NavigationDrawerItem` (#163), `BackHandler` (#166), `NavOptions` (#169), the state-based search-bar pair (#165), `LazyListState` scroll-direction reads (#164), `semantics { selected }` (#167), `Modifier.nestedScroll` (#142), and adaptive `WindowSizeClass` reads (#143) all shipped — those are deferred port wiring, not facade gaps. See `Reply/README.md`. |
| Medium-High           | Jetsnack    | ⬜️     | Heavy custom layouts and animation. |
| High                  | Jetcaster   | ⬜️     | Coroutines, DataStore, Hilt, media playback. |
| High                  | JetLagged   | ⬜️     | Custom drawing + heavy animation. |

When adding a new sample, append a row above and create
`samples/<Name>/README.md` describing the omissions.

## Tracked facade gaps

Sample fidelity is currently bounded by what the facade can express. Each
omission points to a real issue — work the sample, find a gap, file an
issue, link it back here. Closing one of these unblocks every sample
that needs the same primitive.

| Issue | Area                        | Blocks (in samples) |
|------:|-----------------------------|--------------------|
| [#64](https://github.com/jonathanpeppers/Microsoft.AndroidX.Compose/issues/64)  | Drawing primitives — `Canvas`, `drawBehind`, `Brush`, `Path`, `Shape` factories | Custom visuals in **JetLagged**; asymmetric `RoundedCornerShape(topStart, topEnd, …)` on **Jetchat** bubbles. |
| [#144](https://github.com/jonathanpeppers/Microsoft.AndroidX.Compose/issues/144) | Custom `Layout {}` primitive — Measurable / Placeable / MeasureScope | `InterestsAdaptiveContentLayout` in **JetNews**, custom carousels in **Jetsnack**, asymmetric chat bubbles in **Jetchat**. |
| [#168](https://github.com/jonathanpeppers/Microsoft.AndroidX.Compose/issues/168) | `TwoPane` / `NavigableListDetailPaneScaffold` + Jetpack `WindowManager` (`WindowLayoutInfo`/`FoldingFeature`) | Adaptive list-detail with fold avoidance in **Reply**. |
| [#334](https://github.com/jonathanpeppers/Microsoft.AndroidX.Compose/issues/334) | Long-press pointer-input drag gestures | Exact push-to-talk gesture in **Jetchat**. |
| [#335](https://github.com/jonathanpeppers/Microsoft.AndroidX.Compose/issues/335) | Transition float/color value animations | Record-button transitions in **Jetchat**. |
| [#336](https://github.com/jonathanpeppers/Microsoft.AndroidX.Compose/issues/336) | Fractional `Sp` | Exact Jetchat typography metrics. |
| [#337](https://github.com/jonathanpeppers/Microsoft.AndroidX.Compose/issues/337) | Resource-backed `Font` / custom `FontFamily` | Karla/Montserrat typography in **Jetchat**. |
| [#339](https://github.com/jonathanpeppers/Microsoft.AndroidX.Compose/issues/339) | `BasicTextField` + keyboard actions | Exact message-editor structure and IME Send in **Jetchat**. |
| [#340](https://github.com/jonathanpeppers/Microsoft.AndroidX.Compose/issues/340) | Focus target/observation/manager APIs | Emoji-panel focus transfer in **Jetchat**. |
| [#341](https://github.com/jonathanpeppers/Microsoft.AndroidX.Compose/issues/341) | Complete Material 3 `Surface` styling slots | Input and selector elevation/content color in **Jetchat**. |
| [#342](https://github.com/jonathanpeppers/Microsoft.AndroidX.Compose/issues/342) | `Scaffold.contentWindowInsets` customization | Exact inset ownership in **Jetchat**. |
| [#343](https://github.com/jonathanpeppers/Microsoft.AndroidX.Compose/issues/343) | Baseline layout + `clipToBounds` modifiers | Exact input alignment and clipped profile parallax in **Jetchat**. |
| [#344](https://github.com/jonathanpeppers/Microsoft.AndroidX.Compose/issues/344) | Material 3 FAB color/elevation slots | Tertiary profile FAB styling in **Jetchat**. |

Closed gaps that previously appeared here (now usable in samples):
**#51** Pager / FlowRow / FlowColumn / BoxWithConstraints / LazyStaggeredGrid,
**#53** PullToRefreshBox,
**#58** Text styling + TextField config,
**#59** `CompositionLocal` + built-in `LocalContext` / `LocalDensity` / `LocalContentColor` / `LocalTextStyle`,
**#61** Theming reads + `Color` value type + parameterized `MaterialTheme`,
**#62** State primitives (`RememberSaveable` / `mutableStateListOf` / `mutableStateMapOf` / `derivedStateOf`),
**#63** Modifier surface (Background/Border/Clickable/Size/Width/Height/AspectRatio/Offset/Alpha/Clip/Weight + scroll + focus + semantics + Draggable),
**#65** Compose value types (`Color`/`Dp`/`Sp`/`FontWeight`/`TextAlign`),
**#69** WindowInsets padding modifiers (`imePadding` / `navigationBarsPadding` / `statusBarsPadding` / `displayCutoutPadding` / …),
**#70** Row/Column `Arrangement`,
**#140** `DrawerState.open()` / `close()` suspend bridges,
**#141** `AnnotatedString` + `SpanStyle` for inline-run text styling,
**#142** `Modifier.nestedScroll` + `TopAppBarDefaults` scroll behaviors,
**#143** `WindowSizeClass` predicates + `currentWindowAdaptiveInfo()` extension (NavigationSuiteScaffold, SharedTransitionLayout, and ListDetailScene bindings still missing — see [#168](https://github.com/jonathanpeppers/Microsoft.AndroidX.Compose/issues/168) for the fold-aware TwoPane piece),
**#145** `ContentScale` + `Alignment` slots on the `Image` facade,
**#146** `stringResource(id)` lookup,
**#163** `NavigationDrawerItem` facade,
**#164** `LazyListState` scroll-direction / visible-item properties,
**#165** state-based search-bar pair,
**#166** `BackHandler {}` from `androidx.activity.compose`,
**#167** Typed `semantics` properties (`Selected`, `Role`, `OnClick` label, …),
**#169** `NavOptions` (`popUpTo` + `launchSingleTop` + `restoreState`).

Per-sample READMEs may still note these features as deferred — closing
the facade gap unblocks the sample, but each port has to be updated
separately to actually consume the new binding.

## Attribution

These samples are C# ports inspired by Google's [android/compose-samples](https://github.com/android/compose-samples), which is licensed under the [Apache License 2.0](https://github.com/android/compose-samples/blob/main/LICENSE). No upstream Kotlin source code is copied into this repo — the ports re-implement the same UI in C# against this repo's `Microsoft.AndroidX.Compose` facade.

The four per-author avatar PNGs in [`samples/Jetchat/Resources/drawable-nodpi/`](Jetchat/Resources/drawable-nodpi/) (`avatar_ali.png`, `avatar_aubrey.png`, `avatar_taylor.png`, `avatar_jordan.png`) were generated with [DiceBear](https://www.dicebear.com)'s `lorelei` style and are released under [CC0 1.0](https://creativecommons.org/publicdomain/zero/1.0/) (no attribution required, but credit appreciated). `profile_ali.png`, `profile_someone_else.jpg`, and `sticker.png` are copied from, and the light/dark `jetchat_logo.xml` resources are adapted from, [android/compose-samples](https://github.com/android/compose-samples/tree/main/Jetchat/app/src/main/res) under the [Apache License 2.0](https://github.com/android/compose-samples/blob/main/LICENSE).

The 12 article photos in [`samples/JetNews/Resources/drawable-nodpi/`](JetNews/Resources/drawable-nodpi/) (`post_1.png`/`post_1_thumb.png` through `post_6.png`/`post_6_thumb.png`) and the wordmark vector [`samples/JetNews/Resources/drawable/ic_jetnews_wordmark.xml`](JetNews/Resources/drawable/ic_jetnews_wordmark.xml) are copied verbatim from [android/compose-samples](https://github.com/android/compose-samples/tree/main/JetNews/app/src/main/res) under the [Apache License 2.0](https://github.com/android/compose-samples/blob/main/LICENSE).

The 12 icon vector drawables in [`samples/Reply/Resources/drawable/`](Reply/Resources/drawable/) and the 12 avatar JPGs + 4 paris photos in [`samples/Reply/Resources/drawable-nodpi/`](Reply/Resources/drawable-nodpi/) are copied verbatim from [android/compose-samples](https://github.com/android/compose-samples/tree/main/Reply/app/src/main/res) under the [Apache License 2.0](https://github.com/android/compose-samples/blob/main/LICENSE). The data-layer string content for **Reply** (email subjects/bodies, account names, attachment descriptions) is also copied verbatim from upstream's `LocalEmailsDataProvider.kt` / `LocalAccountsDataProvider.kt` under the same license.

All other sample drawables and string content under each `samples/<Name>/` folder are original to this repo.

## Conventions

- Each sample is its own `net10.0-android` Exe project under
  `samples/<Name>/`.
- `<ProjectReference Include="..\..\src\Microsoft.AndroidX.Compose\Microsoft.AndroidX.Compose.csproj" />`.
- Reuse the existing root `Directory.Build.targets` for AndroidX
  version management — do not pin versions per sample.
- Omit `android:icon=` from `AndroidManifest.xml` so the framework
  default ships (matches `.github/copilot-instructions.md` simplification).
- One class per `.cs` file (facade convention).
