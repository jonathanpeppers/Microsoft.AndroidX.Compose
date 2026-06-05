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
- **Navigation drawer** — `ModalNavigationDrawer` + `ModalDrawerSheet`
  hosting upstream's `JetchatDrawerContent` shape: header logo +
  "Jetchat", divider, "Chats" section with `composers` and
  `droidcon-nyc` rows, divider, "Recent Profiles" section. Tapping a
  channel updates the selected highlight and the top-bar title. The
  drawer column is wrapped in `Modifier.VerticalScroll(rememberedScrollState)`
  so the panel scrolls if it overflows on small heights — same pattern
  upstream uses.
- `Scaffold` + `CenterAlignedTopAppBar` with a two-line title showing
  the current channel name and member count, plus trailing **search**
  and **info** action icons — same `Actions` slot upstream's
  `ChannelNameBar` puts them in. Icons are real drawable resources
  rendered through the Phase 7 `[PainterResource]` `Icon` facade.
- Drawable-resource avatars rendered via the Phase 7
  `Image(int drawableResourceId, …)` facade, the same shape as
  upstream's `painterResource(R.drawable.someone_else)` calls. Where
  upstream reuses a single `someone_else.jpg` photo for every non-`me`
  author, this port ships a **distinct portrait per author** generated
  with [DiceBear](https://www.dicebear.com)'s `lorelei` style
  ([CC0 1.0](https://creativecommons.org/publicdomain/zero/1.0/) —
  deterministic from the author's name).
- A "Today" day-separator row (`HorizontalDivider` + `Text` +
  `HorizontalDivider`) above the message list.
- Message bubbles with author name + relative timestamp on one row,
  a 40 dp circular avatar tile, and a rounded coloured bubble for
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
- Reactive channel selection via `MutableState<string>` — drawer taps
  flow into the title.

## What's omitted

Each row links to the upstream issue tracking the missing facade
feature:

| Upstream feature                          | Tracking issue |
|-------------------------------------------|----------------|
| Hamburger nav icon that programmatically opens the drawer | requires Kotlin `suspend` `DrawerState.open()` (no issue yet) — edge-swipe still works |
| Multi-channel message lists (the drawer changes the title but not the messages — we only seed one conversation) | requires extending the sample's seed data, not blocked by facade |
| `LazyColumn(reverseLayout = true)` so newest messages sit at the bottom | not yet exposed on the `LazyColumn` facade |
| Image / sticker / file message attachments inside bubbles | requires composable image-loader plumbing |
| User profile screen                       | depends on `androidx.navigation.compose` binding |
| IME-synchronized scroll-to-bottom         | [#69](https://github.com/jonathanpeppers/compose-net/issues/69) (`imePadding`) |
| Bold author names / typography variants   | [#58](https://github.com/jonathanpeppers/compose-net/issues/58) (`Text` styling, `FontWeight`) |
| `MaterialTheme.colorScheme.primary` reads for the "me" bubble + drawer selection | [#61](https://github.com/jonathanpeppers/compose-net/issues/61) |
| `Row(horizontalArrangement = Arrangement.End)` for "me" alignment | [#70](https://github.com/jonathanpeppers/compose-net/issues/70) (workaround: `Spacer().Weight(1f)`) |
| Asymmetric `RoundedCornerShape(topStart, …)` on bubbles | [#65](https://github.com/jonathanpeppers/compose-net/issues/65) |
| Search / info popups behind the action icons (`FunctionalityNotAvailablePopup`) | requires popup APIs not yet bound; the icons are wired but the onClick is a no-op |

## Facade features added for this port

Phase 1 of this branch landed the facade gaps Jetchat needed, and a
later round (Jun 5) closed three more — all now in use here:

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
  the enclosing `RenderContext.CurrentScopeKind`. The input row, the
  message list's vertical fill, and the day-separator dividers all use
  it.
- **`ObservableList<T>`**: a managed `IList<T>` that participates in
  Compose's snapshot system without needing a `SnapshotStateList`
  binding. Backs the message log.
- **Drawable-resource `Image` / `Icon`** (PR #86, Phase 7
  `[PainterResource]` facade): `new Image(int)` and
  `new Icon(int, string?)` over `painterResource(id)`. Drives every
  drawable in the drawer + the search/info top-bar actions + the
  message-row avatars.
- **`Modifier.VerticalScroll(ScrollState)`** (PR #94): the drawer
  panel uses it so an overflowing list of channels / profiles scrolls
  the same way upstream's `verticalScroll(rememberScrollState())` does.
- **`ModalNavigationDrawer` + `ModalDrawerSheet`** (already bound
  pre-port; first wired up by this sample): swipe-from-left opens the
  drawer; the panel falls back to
  `MaterialTheme.colorScheme.secondaryContainer` for its container
  color via the Phase 6 `DefaultColorFromTheme` facade attribute.
