# compose-net samples

C# ports of selected apps from
[android/compose-samples](https://github.com/android/compose-samples)
to demonstrate the `ComposeNet.Compose` facade running on
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
| Low                   | **Jetchat** | ✅      | Simplified single-channel chat with navigation drawer (`composers` / `droidcon-nyc`), top-bar search/info action icons, distinct-per-author avatars (DiceBear `lorelei`, CC0), and a `LazyColumn`-backed message log. Programmatic drawer open + multi-channel routing still pending. See `Jetchat/README.md`. |
| Medium                | JetNews     | ✅      | Simplified phone-only single-pane port — three screens (Home / Article / Interests), navigation drawer with two destinations, `PrimaryTabRow` on Interests, per-post bookmark toggle, six condensed seed posts with solid-color hero panels. Adaptive list-detail layout, inline paragraph spans, `nestedScroll` top-bar elevation, drawer auto-close, and real hero PNGs are all pending. See `JetNews/README.md`. |
| Medium                | Reply       | ⬜️     | Needs adaptive layouts (window-size classes). |
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
| [#64](https://github.com/jonathanpeppers/compose-net/issues/64)  | Drawing primitives — `Canvas`, `drawBehind`, `Brush`, `Path`, `Shape` factories | Custom visuals in **JetLagged**; asymmetric `RoundedCornerShape(topStart, topEnd, …)` on **Jetchat** bubbles. |
| [#69](https://github.com/jonathanpeppers/compose-net/issues/69)  | WindowInsets padding modifiers (`imePadding`, `navigationBarsPadding`, `statusBarsPadding`, …) | IME-synced input row in **Jetchat**. |
| [#59](https://github.com/jonathanpeppers/compose-net/issues/59)  | `CompositionLocal` / `CompositionLocalProvider` (`LocalContext`, `LocalDensity`, `LocalContentColor`, `LocalTextStyle`, …) | Idiomatic theming and density reads across every sample. |
| [#20](https://github.com/jonathanpeppers/compose-net/issues/20)  | Edge-to-edge bootstrapping | Status/nav-bar overlap on every sample. |
| [#140](https://github.com/jonathanpeppers/compose-net/issues/140) | `DrawerState.open()` / `close()` suspend bridges | Drawer auto-close on item tap in **Jetchat** + **JetNews**; programmatic hamburger button. |
| [#141](https://github.com/jonathanpeppers/compose-net/issues/141) | `AnnotatedString` + `SpanStyle` for inline-run text styling | Link / Bold / Italic / Code spans inside paragraphs in **JetNews** article reader. |
| [#142](https://github.com/jonathanpeppers/compose-net/issues/142) | `Modifier.nestedScroll` + `TopAppBarDefaults` scroll behaviors | Top-bar elevation / collapse on scroll in **JetNews**, **Reply**, **Jetcaster**. |
| [#143](https://github.com/jonathanpeppers/compose-net/issues/143) | WindowSizeClass / `currentWindowAdaptiveInfo` | Adaptive layouts; blocks **Reply** entirely and the **JetNews** list-detail screen. |
| [#144](https://github.com/jonathanpeppers/compose-net/issues/144) | Custom `Layout {}` primitive — Measurable / Placeable / MeasureScope | `InterestsAdaptiveContentLayout` in **JetNews**, custom carousels in **Jetsnack**, asymmetric chat bubbles in **Jetchat**. |
| [#145](https://github.com/jonathanpeppers/compose-net/issues/145) | `ContentScale` + `Alignment` slots on the `Image` facade | Hero images on cards in **JetNews** (currently solid-color `Box` placeholders). |
| [#146](https://github.com/jonathanpeppers/compose-net/issues/146) | `stringResource(id)` lookup | Localizable UI strings in every sample (all currently inline literals). |

Closed gaps that previously appeared here (now usable in samples):
**#51** Pager / FlowRow / FlowColumn / BoxWithConstraints / LazyStaggeredGrid,
**#53** PullToRefreshBox, **#58** Text styling + TextField config,
**#61** Theming reads + `Color` value type + parameterized `MaterialTheme`,
**#62** State primitives (`RememberSaveable` / `mutableStateListOf` / `mutableStateMapOf` / `derivedStateOf`),
**#63** Modifier surface (Background/Border/Clickable/Size/Width/Height/AspectRatio/Offset/Alpha/Clip/Weight + scroll + focus + semantics + Draggable),
**#65** Compose value types (`Color`/`Dp`/`Sp`/`FontWeight`/`TextAlign`),
**#70** Row/Column `Arrangement`.

## Attribution

These samples are C# ports inspired by Google's [android/compose-samples](https://github.com/android/compose-samples), which is licensed under the [Apache License 2.0](https://github.com/android/compose-samples/blob/main/LICENSE). No upstream Kotlin source code is copied into this repo — the ports re-implement the same UI in C# against this repo's `ComposeNet.Compose` facade.

The four per-author avatar PNGs in [`samples/Jetchat/Resources/drawable-nodpi/`](Jetchat/Resources/drawable-nodpi/) (`avatar_ali.png`, `avatar_aubrey.png`, `avatar_taylor.png`, `avatar_jordan.png`) were generated with [DiceBear](https://www.dicebear.com)'s `lorelei` style and are released under [CC0 1.0](https://creativecommons.org/publicdomain/zero/1.0/) (no attribution required, but credit appreciated). All other sample drawables and string content under each `samples/<Name>/` folder are original to this repo.

## Conventions

- Each sample is its own `net10.0-android` Exe project under
  `samples/<Name>/`.
- `<ProjectReference Include="..\..\src\ComposeNet.Compose\ComposeNet.Compose.csproj" />`.
- Reuse the existing root `Directory.Build.targets` for AndroidX
  version management — do not pin versions per sample.
- Omit `android:icon=` from `AndroidManifest.xml` so the framework
  default ships (matches `.github/copilot-instructions.md` simplification).
- One class per `.cs` file (facade convention).
