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
| Medium                | JetNews     | ⬜️     | Needs image loading and navigation. |
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
| [#51](https://github.com/jonathanpeppers/compose-net/issues/51)  | `Pager`, `FlowRow`/`FlowColumn`, `BoxWithConstraints`, remaining Lazy variants | **JetNews** carousel, **Jetsnack** chip wrapping. |
| [#53](https://github.com/jonathanpeppers/compose-net/issues/53)  | `PullToRefreshBox` | News-feed refresh in **JetNews** / **Reply**. |
| [#58](https://github.com/jonathanpeppers/compose-net/issues/58)  | `Text` styling — `TextStyle`, `FontWeight`, `AnnotatedString`, `KeyboardOptions`, `supportingText`, leading/trailing icons | **Jetchat** bold author names; every sample's typography. |
| [#61](https://github.com/jonathanpeppers/compose-net/issues/61)  | Theming — parameterize `MaterialTheme`, `ColorScheme` / `Typography` / `Shapes`, `MaterialTheme.colorScheme.*` reads, Material Icons | **Jetchat** primary-tinted "me" bubble, every sample's app-bar icons. |
| [#62](https://github.com/jonathanpeppers/compose-net/issues/62)  | State primitives — `rememberSaveable`, `mutableStateListOf`/`MapOf`, `derivedStateOf` | Rotation-stable state in every sample. |
| [#63](https://github.com/jonathanpeppers/compose-net/issues/63)  | `Modifier` surface — gestures, focus, semantics, `weight` / `align` extras (`verticalScroll` / `horizontalScroll` ✅ landed in #94 and exercised by **Jetchat**'s drawer panel) | Drag/drop everywhere. |
| [#64](https://github.com/jonathanpeppers/compose-net/issues/64)  | Drawing primitives — `Canvas`, `drawBehind`, `Brush`, `Path`, `Shape` factories | Custom visuals in **JetLagged**. |
| [#65](https://github.com/jonathanpeppers/compose-net/issues/65)  | C# value types for inline classes (`Color`, `Dp`, `Sp`, `FontWeight`, `TextAlign`, `Shape`) | Asymmetric `RoundedCornerShape(topStart, topEnd, …)` on **Jetchat** bubbles; ergonomic API everywhere. |
| [#69](https://github.com/jonathanpeppers/compose-net/issues/69)  | WindowInsets padding modifiers (`imePadding`, `navigationBarsPadding`, `statusBarsPadding`, …) | IME-synced input row in **Jetchat**. |
| [#70](https://github.com/jonathanpeppers/compose-net/issues/70)  | `Row` / `Column` `Arrangement` parameter (`Start`, `End`, `Center`, `SpaceBetween`, `SpaceAround`, `spacedBy`) | Right-aligned "me" bubbles in **Jetchat** (currently faked with `Spacer().Weight(1f)`). |

## Attribution

These samples are C# ports inspired by Google's [android/compose-samples](https://github.com/android/compose-samples), which is licensed under the [Apache License 2.0](https://github.com/android/compose-samples/blob/main/LICENSE). No upstream Kotlin source code is copied into this repo — the ports re-implement the same UI in C# against this repo's `ComposeNet.Compose` facade.

The four per-author avatar PNGs in [`samples/Jetchat/Resources/drawable-nodpi/`](Jetchat/Resources/drawable-nodpi/) (`avatar_ali.png`, `avatar_aubrey.png`, `avatar_taylor.png`, `avatar_jordan.png`) were generated with [DiceBear](https://www.dicebear.com)'s `lorelei` style and are released under [CC0 1.0](https://creativecommons.org/publicdomain/zero/1.0/) (no attribution required, but credit appreciated). All other sample drawables and string content under each `samples/<Name>/` folder are original to this repo.
| [#20](https://github.com/jonathanpeppers/compose-net/issues/20)  | Edge-to-edge bootstrapping | Status/nav-bar overlap on every sample. |

## Conventions

- Each sample is its own `net10.0-android` Exe project under
  `samples/<Name>/`.
- `<ProjectReference Include="..\..\src\ComposeNet.Compose\ComposeNet.Compose.csproj" />`.
- Reuse the existing root `Directory.Build.targets` for AndroidX
  version management — do not pin versions per sample.
- Omit `android:icon=` from `AndroidManifest.xml` so the framework
  default ships (matches `.github/copilot-instructions.md` simplification).
- One class per `.cs` file (facade convention).
