# Jetchat (compose-net port)

A simplified C# port of the official Compose sample
[android/compose-samples ▸ Jetchat](https://github.com/android/compose-samples/tree/main/Jetchat).
The upstream sample is labeled **Low complexity** and is the smallest
of the six showcase apps, which makes it the natural first target for
`ComposeNet.Compose`.

![Jetchat running on an Android emulator](../docs/jetchat.png)

Run with:

```pwsh
dotnet build samples/Jetchat -t:Run
```

## What's faithful

- `MaterialTheme` wrapping the whole tree — picks up the device
  wallpaper-derived dynamic color scheme on Android 12+ (Material You).
- `Scaffold` + `CenterAlignedTopAppBar` with a two-line title showing
  the channel name (`#composers`) and member count (`42 members`),
  matching upstream.
- A "Today" day-separator row (`HorizontalDivider` + `Text` +
  `HorizontalDivider`) above the message list.
- Message bubbles with author name + relative timestamp on one row,
  a 40 dp rounded emoji avatar tile, and a rounded coloured bubble for
  the message body.
- **Streak-aware avatars** — when a sender posts multiple messages in a
  row, only the first shows the avatar tile; subsequent messages indent
  with a `Spacer(Size(40))` so the bubbles still align.
- **`isUserMe` right-alignment** for the local user — distinct blue
  bubble, no avatar tile, pushed to the right with `Spacer().Weight(1f)`.
  (See issue #70 for the proper `Arrangement.End` fix.)
- A pinned input row at the bottom: a `TextField` that grows to fill
  available width via `Modifier.Weight(1f)`, plus an `IconButton` send
  control.
- Reactive message list via `ObservableList<Message>` — tapping send
  appends a message and the UI recomposes.

## What's omitted

Each row links to the upstream issue tracking the missing facade
feature:

| Upstream feature                          | Tracking issue |
|-------------------------------------------|----------------|
| `verticalScroll` for the message list (now uses `LazyColumn`) | [#63](https://github.com/jonathanpeppers/compose-net/issues/63) |
| Navigation drawer + multi-channel nav     | requires `androidx.navigation.compose` binding (no issue yet) |
| Image / sticker / file attachments        | requires `painterResource` + an image loader |
| User profile screen                       | depends on navigation |
| IME-synchronized scroll-to-bottom         | [#69](https://github.com/jonathanpeppers/compose-net/issues/69) (`imePadding`) |
| Bold author names / typography variants   | [#58](https://github.com/jonathanpeppers/compose-net/issues/58) (`Text` styling, `FontWeight`) |
| `MaterialTheme.colorScheme.primary` reads for the "me" bubble | [#61](https://github.com/jonathanpeppers/compose-net/issues/61) |
| `Row(horizontalArrangement = Arrangement.End)` for "me" alignment | [#70](https://github.com/jonathanpeppers/compose-net/issues/70) (workaround: `Spacer().Weight(1f)`) |
| Asymmetric `RoundedCornerShape(topStart, …)` on bubbles | [#65](https://github.com/jonathanpeppers/compose-net/issues/65) |
| Vector / painter resources for the app-bar icons | [#61](https://github.com/jonathanpeppers/compose-net/issues/61) (Material Icons) |

## Facade features added for this port

Phase 1 of this branch landed the facade gaps Jetchat needed:

- **Sizing modifiers**: `Modifier.Size(int)`, `Size(int, int)`,
  `Width(int)`, `Height(int)`.
- **Background / Border / Clickable**: surfaced existing bridges as
  public `Modifier` methods (`Background(long)` / `Border(int, long, int = 0)`
  taking a packed Compose `Color` long built via
  `AndroidX.Compose.UI.Graphics.ColorKt.Color(...)`, and
  `Clickable(Action)`).
- **Shape clipping**: `Modifier.Clip(int cornerDp)` — two-step JNI
  helper that constructs a `RoundedCornerShape` and applies
  `ClipKt.clip` in one fluent op.
- **Weight modifier**: `Modifier.Weight(float, bool fill = true)` —
  dispatches to `RowScope` / `ColumnScope` `weight$default` based on
  the enclosing `RenderContext.CurrentScopeKind`. The input row in
  this sample is the primary consumer.
- **`ObservableList<T>`**: a managed `IList<T>` that participates in
  Compose's snapshot system without needing a `SnapshotStateList`
  binding. Backs the message log.
