# Jetchat (compose-net port)

A simplified C# port of the official Compose sample
[android/compose-samples ▸ Jetchat](https://github.com/android/compose-samples/tree/main/Jetchat).
The upstream sample is labeled **Low complexity** and is the smallest
of the six showcase apps, which makes it the natural first target for
`ComposeNet.Compose`.

<img src="../docs/jetchat.png" alt="Jetchat running on an Android emulator" width="320" />

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
- Message bubbles with a 40 dp circular avatar tile (16 dp horizontal
  padding around it, mirroring upstream's 74 dp avatar+padding
  reservation) and a rounded coloured bubble for the message body.
- **Typography parity via the new `Text` styling surface** (#73 /
  #58) — author names ride at 16 sp / `FontWeight.Medium` (Material
  3's `titleMedium`), timestamps at 12 sp (`bodySmall`), the "Today"
  separator at 11 sp / Medium / 1 sp letter-spacing (`labelSmall`,
  rounded from M3's 0.5 sp because `Sp` only takes integers), the
  top-bar channel name at 16 sp / Medium with a 12 sp member-count
  subtitle, and drawer brand / section / chat labels at the
  matching M3 sizes. Theme-aware reads (`MaterialTheme.typography.*`
  + tonal `onSurfaceVariant` color) aren't bound yet — see
  [#58](https://github.com/jonathanpeppers/compose-net/issues/58)
  / [#61](https://github.com/jonathanpeppers/compose-net/issues/61).
- **Streak-aware avatars + per-author spacing** — when a sender
  posts multiple messages in a row, only the first shows the avatar
  tile; subsequent messages indent with a 72 dp `Spacer` so the
  bubbles still align. Author boundaries get an extra 4 dp of top
  padding (8 dp first-in-chain vs 4 dp within a streak) — mirrors
  upstream's `spaceBetweenAuthors` modifier, with the
  forward-vs-reverse-layout flag inverted (we use `!isStreak` where
  upstream uses `isLastMessageByAuthor`).
- **`isUserMe` right-alignment** for the local user via
  `new Row(Arrangement.End)` (#100) — distinct blue bubble, no
  avatar tile, pushed to the right edge with proper Compose
  arrangement instead of the previous `Spacer().Weight(1f)` hack.
- A pinned input row at the bottom: a `TextField` that grows to fill
  available width via `Modifier.Weight(1f)`, plus an `IconButton` send
  control. Newly sent messages are stamped `"now"` (matching
  upstream's `R.string.now`) instead of a wall-clock time.
- Reactive message list via `MutableStateList<Message>` — tapping send
  appends a message and the UI recomposes.
- Reactive channel selection via `MutableState<string>` — drawer taps
  flow into the title and bold the selected chat row in the drawer.

## What's omitted

Each row links to the upstream issue tracking the missing facade
feature:

| Upstream feature                          | Tracking issue |
|-------------------------------------------|----------------|
| Hamburger nav icon that programmatically opens the drawer | needs a `DrawerState` wrapper + suspend bridges (the `SuspendBridge` plumbing landed in #97 but the drawer-state type isn't exposed yet) — edge-swipe still works |
| Multi-channel message lists (the drawer changes the title but not the messages — we only seed one conversation) | requires extending the sample's seed data, not blocked by facade |
| `LazyColumn(reverseLayout = true)` so newest messages sit at the bottom | not yet exposed on the `LazyColumn` facade |
| Image / sticker / file message attachments inside bubbles | requires composable image-loader plumbing |
| User profile screen                       | depends on `androidx.navigation.compose` binding |
| IME-synchronized scroll-to-bottom         | [#69](https://github.com/jonathanpeppers/compose-net/issues/69) (`imePadding`) |
| `MaterialTheme.colorScheme.primary` reads for the "me" bubble + drawer selection + tonal text colors (`onSurfaceVariant`) | [#61](https://github.com/jonathanpeppers/compose-net/issues/61) |
| `MaterialTheme.typography.*` reads (we approximate the M3 sp/weight values directly until these land) | [#58](https://github.com/jonathanpeppers/compose-net/issues/58) |
| Asymmetric `RoundedCornerShape(topStart, …)` on bubbles | `Modifier.Clip(Dp)` only takes a single radius; full `Shape` API tracked under [#65](https://github.com/jonathanpeppers/compose-net/issues/65)'s follow-up surface |
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
- **`MutableStateList<T>`**: a managed `IList<T>` that participates in
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

## Implementation notes

These notes track the technical reasoning behind specific deviations
from upstream's `Conversation.kt`, and the facade gaps that would let
us close them. Filed here instead of inline in `Conversation.cs` so
the C# source stays close in shape to the Kotlin original.

### Hard-coded colors instead of `MaterialTheme.colorScheme.*` reads — [#61]

User code can't read `MaterialTheme.colorScheme.primary` /
`primaryContainer` / `surfaceVariant` / `onSurfaceVariant` etc. yet —
only the source generator's `[ComposeFacade(DefaultColorFromTheme=…)]`
path consumes them, and it's internal. Until #61 lands the sample
hard-codes:

| Slot                          | Hex          | Approximates upstream's |
|-------------------------------|--------------|--------------------------|
| `MeBubbleColor`               | `#D0E4FF`    | `primaryContainer`       |
| `OtherBubbleColor`            | `#EDEDED`    | `surfaceVariant`         |
| `DrawerSelectedColor`         | `#D0E4FF`    | `primaryContainer`       |

Timestamps and member-count subtitles also keep the default content
color instead of `onSurfaceVariant`.

### Hard-coded sp / weight values instead of `MaterialTheme.typography.*` reads — [#58]

Same shape as the color gap: typography reads aren't exposed yet.
The sample sets `FontSize` / `FontWeight` directly to the M3 spec
values for each role:

| Upstream role     | Where used                           | sp | Weight       | Letter-spacing |
|-------------------|--------------------------------------|----|--------------|----------------|
| `titleMedium`     | author names, channel title          | 16 | Medium       | (default)      |
| `bodySmall`       | timestamps, member-count subtitle    | 12 | Normal       | (default)      |
| `labelSmall`      | "Today" day separator                | 11 | Medium       | **1** (rounded; spec is 0.5) |
| `titleSmall`      | drawer section labels                | 14 | Medium       | **dropped** (spec is 0.1, would round to 0) |
| `bodyLarge`       | drawer chat / profile rows           | 16 | Normal       | (default)      |
| custom brand      | drawer "Jetchat" header              | 18 | SemiBold     | — (upstream is also hand-tuned)

`Sp` currently only takes an `int` constructor (`new Sp(11)` / implicit
`int → Sp`). Adding `Sp(float)` would let labelSmall hit 0.5 sp and
titleSmall hit 0.1 sp.

### Single-radius bubbles instead of `RoundedCornerShape(topStart, …)`

Upstream's chat bubbles use `RoundedCornerShape(4.dp, 20.dp, 20.dp, 20.dp)`
to flatten the corner that visually meets the avatar. `Modifier.Clip(Dp)`
only takes one radius, so all four corners are 12 dp here. Closing the
gap needs the asymmetric `RoundedCornerShape` factory exposed through a
public `Shape` API.

### `!isStreak` ↔ upstream's `isLastMessageByAuthor`

Upstream's `LazyColumn` is `reverseLayout = true`, so "last message
by this author" in chronological order is the *first* message rendered
visually. Our `LazyColumn` is forward-layout (no `reverseLayout` slot
exposed yet), and we pre-compute `isStreak = (m.Author == lastAuthor)`
during enumeration. The semantic in both cases is identical: "this is
the first message visually rendered for this author chain — apply the
8 dp `spaceBetweenAuthors` top padding and show the avatar tile."

### 72 dp streak `Spacer` (upstream uses 74 dp)

Upstream reserves `42.dp + 16.dp + 16.dp = 74.dp` for the avatar slot
(42 dp avatar + 16 dp horizontal padding on each side). This sample
uses a 40 dp avatar (matching `androidx.compose.material3.Icon`'s
default tap target), so the streak `Spacer` is `40 + 16 + 16 = 72.dp`.

### `"now"` literal instead of formatted clock time

Newly sent messages stamp `"now"` to match upstream's
`R.string.now` resource value. Earlier revisions used a wall-clock
`HH:mm` formatter — the literal is faithful to the original.

### Search / info icon onClicks are no-ops

Upstream wires the icons to a `FunctionalityNotAvailablePopup`. The
icons are real affordances (correct drawables, real ripple via
`IconButton`), but the `onClick` is `NoOp` until popup APIs are bound.

### No hamburger nav icon — drawer opens via edge-swipe only

Programmatic drawer open requires a `DrawerState` C# wrapper plus
suspend bridges around `DrawerState.open()` / `close()`. The
`SuspendBridge` plumbing landed in #97 and is already used by
`ScrollState.scrollTo` + `animateScrollToItem`, so this is the next
state-holder to expose. A no-op hamburger button would be a misleading
affordance, so it's omitted entirely until the wrapper lands.

### Static builder instead of `ComposableNode` subclass

`Conversation.Build` is a `static` method that allocates a fresh
`ComposableNode` tree per composition pass. As of #132,
`ComposableNode.Render(IComposer)` is `public abstract` and
`ComposableContainer.Children` / `RenderChildren` are `protected`, so
a sample-side subclass with its own `Render` override is now possible
— converting `Conversation` to a subclass would let it participate in
the slot table directly and avoid the per-composition tree allocation
(Tier 1.5 cost). Left as a follow-up; the static builder still
renders correctly.

### Single-channel seed data

Tapping `composers` / `droidcon-nyc` updates the title and selection
highlight but the message list itself is shared across channels.
Multi-channel state would refactor `MutableStateList<Message>` into a
`Dictionary<string, MutableStateList<Message>>` or similar — out of
scope for this port; the upstream sample's seed data lives in a
separate `JetchatViewModel` + Hilt-injected fake repository.

[#58]: https://github.com/jonathanpeppers/compose-net/issues/58
[#61]: https://github.com/jonathanpeppers/compose-net/issues/61
