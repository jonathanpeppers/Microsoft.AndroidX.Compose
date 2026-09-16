# Microsoft.AndroidX.Compose samples

C# ports of selected apps from
[android/compose-samples](https://github.com/android/compose-samples)
to demonstrate the `Microsoft.AndroidX.Compose` facade running on
.NET-for-Android. Each port is **simplified** to fit the current facade
surface — the per-sample `README.md` lists what was kept, what was cut,
and which facade features had to land first.

The [sample parity baseline](parity-baseline.md) pins the Kotlin reference,
defines finite Jetchat/Reply flows and separates source findings from paired
device evidence. A runnable port is not a claim of whole-app parity.

Build any sample with:

```pwsh
dotnet build samples/<Name> -c Release
dotnet build samples/<Name> -t:Run        # deploy + run on device
```

## Checklist

Port status legend: ✅ runnable port · 🚧 in progress · ⬜️ not started · ❌ blocked
(needs binding work upstream).

| Complexity (upstream) | Sample      | Status | Notes |
|----------------------:|-------------|:------:|-------|
| Low                   | **Jetchat** | ✅      | Channel drawer UI over one chat log, profile routes, Foundation `BasicTextField` with working IME Send, emoji/focus handoff, native long-press recording gestures, placeholder attachment panels, drag/drop feedback, custom fonts and Surface/FAB styling. Remaining differences include sample integration, data/deviation and uninvestigated behavior, not just missing reusable APIs. See [Jetchat](Jetchat/README.md). |
| Medium                | JetNews     | ✅      | Phone-only Home / Article / Interests, drawer, bookmarks, bundled hero PNGs, styled paragraph runs, refresh/retry, snackbar feedback and share chooser. Six original seed articles; adaptive layouts, theme/localization polish, clickable links and search filtering remain. See [JetNews](JetNews/README.md). |
| Medium                | Reply       | ✅      | Phone-only inbox/detail and four tabs, interactive docked prefix search, single-top/save-restore navigation, stable email keys and long-press selection. Search/navigation are integrated, not placeholders. Adaptive navigation, FAB response, selected semantics and visual styling remain sample work; fold-aware list/detail has separate scope. See [Reply](Reply/README.md). |
| Medium-High           | Jetsnack    | ⬜️     | Heavy custom layouts and animation. |
| High                  | Jetcaster   | ⬜️     | Coroutines, DataStore, Hilt, media playback. |
| High                  | JetLagged   | ⬜️     | Custom drawing + heavy animation. |

When adding a new sample, append a row above and create
`samples/<Name>/README.md` describing the omissions.

## Tracked differences

Reconciled against live issue titles/states and C# source
`b8c93a68579a2430a54cbe3271730047516edec9` on 2026-09-15.
Do not reopen a completed binding issue merely because a sample still
omits a parameter or interaction. Check the exact member and classify the
difference first.

| Issue | Current scope |
| --- | --- |
| [#349](https://github.com/jonathanpeppers/Microsoft.AndroidX.Compose/issues/349) | Finite Jetchat/Reply parity baseline, remaining matched captures and documented limitations. Source-level Jetchat differences are listed in its README; not all have device evidence. |
| [#384](https://github.com/jonathanpeppers/Microsoft.AndroidX.Compose/issues/384) | **Jetchat presentation integration** using delivered APIs. Its README separately links Tooltip control (#388), infinite pulse (#385), video (#387) and draft restoration (#386). |
| [#383](https://github.com/jonathanpeppers/Microsoft.AndroidX.Compose/issues/383) | **Reply sample integration**: delivered adaptive-navigation, FAB, selection, inset and theme APIs; exact remaining styling slots still require an audit. |
| [#168](https://github.com/jonathanpeppers/Microsoft.AndroidX.Compose/issues/168) | **Reply fold/list-detail**: reusable API and integration scope, separate from already-delivered NavigationSuite and size reads. Verify current binding members before repeating the tracker's historical package-availability claims. |
| [#159](https://github.com/jonathanpeppers/Microsoft.AndroidX.Compose/issues/159) | **JetNews** remaining adaptive, navigation and sample polish; existing hero images, styled runs, refresh and share behavior are not missing features. |
| [#120](https://github.com/jonathanpeppers/Microsoft.AndroidX.Compose/issues/120) | Remaining generator migrations: search family, SnackbarHost and BottomSheetScaffold. TimeInput and ModalBottomSheet already migrated. Not a reason to call existing sample controls unavailable. |
| [#346](https://github.com/jonathanpeppers/Microsoft.AndroidX.Compose/issues/346) | Separate frozen-build performance work. Screenshot or symbol parity does not establish performance parity. |

Closed gaps that previously appeared here (now usable in samples):
**#51** Pager / FlowRow / FlowColumn / BoxWithConstraints / LazyStaggeredGrid,
**#53** PullToRefreshBox,
**#58** Text styling + TextField config,
**#59** `CompositionLocal` + built-in `LocalContext` / `LocalDensity` / `LocalContentColor` / `LocalTextStyle`,
**#61** Theming reads + `Color` value type + parameterized `MaterialTheme`,
**#62** State primitives (`RememberSaveable` / `mutableStateListOf` / `mutableStateMapOf` / `derivedStateOf`),
**#63** Modifier surface (Background/Border/Clickable/Size/Width/Height/AspectRatio/Offset/Alpha/Clip/Weight + scroll + focus + semantics + Draggable),
**#64** Drawing primitives (the specific advanced follow-ups #292/#293 remain separate),
**#65** Compose value types (`Color`/`Dp`/`Sp`/`FontWeight`/`TextAlign`),
**#69** WindowInsets padding modifiers (`imePadding` / `navigationBarsPadding` / `statusBarsPadding` / `displayCutoutPadding` / …),
**#70** Row/Column `Arrangement`,
**#140** `DrawerState.open()` / `close()` suspend bridges,
**#141** `AnnotatedString` + `SpanStyle` for inline-run text styling,
**#142** `Modifier.nestedScroll` + `TopAppBarDefaults` scroll behaviors,
**#143** `WindowSizeClass` predicates + `currentWindowAdaptiveInfo()` extension (NavigationSuiteScaffold is also available; fold-aware list/detail remains separate in #168),
**#144** Custom `Layout` measure/place primitive,
**#145** `ContentScale` + `Alignment` slots on the `Image` facade,
**#146** `stringResource(id)` lookup,
**#163** `NavigationDrawerItem` facade,
**#164** `LazyListState` scroll-direction / visible-item properties,
**#165** state-based search-bar pair,
**#166** `BackHandler {}` from `androidx.activity.compose`,
**#167** Typed `semantics` properties (`Selected`, `Role`, `OnClick` label, …),
**#169** `NavOptions` (`popUpTo` + `launchSingleTop` + `restoreState`),
**#333** Child enter/exit transition modifiers,
**#334** Fractional `Sp`,
**#335** Resource-backed `Font` / custom `FontFamily` (bundled Karla/Montserrat in **Jetchat**),
**#336** Transition float/color animations,
**#337** Long-press drag gestures,
**#339** Scaffold content-inset customization,
**#340** BasicTextField and Jetchat IME Send,
**#341** Baseline alignment and clip-to-bounds,
**#342** Focus target/manager APIs,
**#343** Surface styling slots,
**#344** FAB color/elevation slots,
**#347** Reply tab navigation/state integration,
**#348** Reply docked-search integration.

Per-sample READMEs may still note these features as deferred — closing
the facade gap unblocks the sample, but each port has to be updated
separately to actually consume the new binding.
In particular, #336 is transition animation, #337 is long-press drag,
and #339-#344 are respectively insets, editor/IME, baseline/clip, focus,
Surface and FAB. These links identify delivered work, not open API gaps.

## Attribution

These samples are C# ports inspired by Google's [android/compose-samples](https://github.com/android/compose-samples), which is licensed under the [Apache License 2.0](https://github.com/android/compose-samples/blob/main/LICENSE). No upstream Kotlin source code is copied into this repo — the ports re-implement the same UI in C# against this repo's `Microsoft.AndroidX.Compose` facade.

The four per-author avatar PNGs in [`samples/Jetchat/Resources/drawable-nodpi/`](Jetchat/Resources/drawable-nodpi/) (`avatar_ali.png`, `avatar_aubrey.png`, `avatar_taylor.png`, `avatar_jordan.png`) were generated with [DiceBear](https://www.dicebear.com)'s `lorelei` style and are released under [CC0 1.0](https://creativecommons.org/publicdomain/zero/1.0/) (no attribution required, but credit appreciated). `profile_ali.png`, `profile_someone_else.jpg`, and `sticker.png` are copied from, and the light/dark `jetchat_logo.xml` resources are adapted from, [android/compose-samples](https://github.com/android/compose-samples/tree/main/Jetchat/app/src/main/res) under the [Apache License 2.0](https://github.com/android/compose-samples/blob/main/LICENSE).

The 12 article photos in [`samples/JetNews/Resources/drawable-nodpi/`](JetNews/Resources/drawable-nodpi/) (`post_1.png`/`post_1_thumb.png` through `post_6.png`/`post_6_thumb.png`) and the wordmark vector [`samples/JetNews/Resources/drawable/ic_jetnews_wordmark.xml`](JetNews/Resources/drawable/ic_jetnews_wordmark.xml) are copied verbatim from [android/compose-samples](https://github.com/android/compose-samples/tree/main/JetNews/app/src/main/res) under the [Apache License 2.0](https://github.com/android/compose-samples/blob/main/LICENSE).

The 12 icon vector drawables in [`samples/Reply/Resources/drawable/`](Reply/Resources/drawable/) and the 12 avatar JPGs + 4 paris photos in [`samples/Reply/Resources/drawable-nodpi/`](Reply/Resources/drawable-nodpi/) are copied verbatim from [android/compose-samples](https://github.com/android/compose-samples/tree/main/Reply/app/src/main/res) under the [Apache License 2.0](https://github.com/android/compose-samples/blob/main/LICENSE). The data-layer string content for **Reply** (email subjects/bodies, account names, attachment descriptions) is also copied verbatim from upstream's `LocalEmailsDataProvider.kt` / `LocalAccountsDataProvider.kt` under the same license.

All other sample drawables and string content under each `samples/<Name>/` folder are original to this repo.

## Conventions

- Each sample is its own `net11.0-android` Exe project under
  `samples/<Name>/`.
- `<ProjectReference Include="..\..\src\Microsoft.AndroidX.Compose\Microsoft.AndroidX.Compose.csproj" />`.
- Reuse the existing root `Directory.Build.targets` for AndroidX
  version management — do not pin versions per sample.
- Omit `android:icon=` from `AndroidManifest.xml` so the framework
  default ships (matches `.github/copilot-instructions.md` simplification).
- One class per `.cs` file (facade convention).
