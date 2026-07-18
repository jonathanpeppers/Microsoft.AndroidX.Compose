# Jetchat (Microsoft.AndroidX.Compose port)

A C# port of the official Compose sample
[android/compose-samples ▸ Jetchat](https://github.com/android/compose-samples/tree/main/Jetchat).
The upstream sample is labeled **Low complexity** and is the smallest
of the six showcase apps, which makes it the natural first target for
`Microsoft.AndroidX.Compose`.

<img src="../docs/jetchat.png" alt="Jetchat running on an Android emulator" width="320" />

Run with:

```pwsh
dotnet build samples/Jetchat -t:Run
```

## What's faithful

- **Jetchat-branded light/dark theme** — `JetchatTheme` selects the
  upstream blue/yellow palette from `isSystemInDarkTheme()` and supplies
  it through `MaterialTheme`; on Android 12+ it follows upstream by using
  the system dynamic light/dark color scheme.
- **Live `MaterialTheme.colorScheme.*` reads** via the new `Composed`
  composer-aware wrapper. Bubble, drawer-selection, divider, top-bar
  subtitle, timestamp and member-count colors all flow from the active
  scheme (`Primary`, `PrimaryContainer`, `SurfaceVariant`,
  `OnSurfaceVariant`, `Surface`, `OnSurface`) instead of hardcoded hex.
- **Hamburger nav** — `IconButton` in the top app bar fires
  `DrawerStateHolder.OpenAsync()`; tapping a channel in the drawer
  fires `CloseAsync()`. Both go through new `SuspendBridge` plumbing
  around `DrawerState.open()` / `close()`.
- **Navigation drawer** — `ModalNavigationDrawer` + `ModalDrawerSheet`
  with the upstream vector wordmark, divider, "Chats" section, divider, "Recent
  Profiles" section. The drawer column is wrapped in
  `Modifier.VerticalScroll(rememberedScrollState)` so it scrolls when
  it overflows on small heights.
- **Multi-channel state** — `ConversationUiState` holds a
  `Dictionary<string, ChannelState>` keyed by channel name. Tapping a
  drawer row swaps the active channel; the title, member count, and
  message list all recompose against the newly selected channel's
  `MutableStateList<Message>`. Two channels are seeded
  (`composers`, `droidcon-nyc`) with distinct message logs.
- `Scaffold` + `CenterAlignedTopAppBar` with a two-line title showing
  the current channel name and member count, plus trailing **search**
  and **info** action icons. Search / info open a
  **FunctionalityNotAvailable `AlertDialog`** — the same affordance
  upstream's `FunctionalityNotAvailablePopup` provides for unbound
  features.
- Drawable-resource avatars rendered via the Phase 7
  `Image(int drawableResourceId, …)` facade, the same shape as
  upstream's `painterResource(R.drawable.someone_else)` calls. Where
  upstream reuses a single `someone_else.jpg` photo for every non-`me`
  author, this port ships a **distinct portrait per author** generated
  with [DiceBear](https://www.dicebear.com)'s `lorelei` style
  ([CC0 1.0](https://creativecommons.org/publicdomain/zero/1.0/) —
  deterministic from the author's name).
- **`LazyColumn(reverseLayout = true)`** — newest message at index 0
  sits at the bottom of the viewport, matching upstream's scroll-from-
  bottom behaviour. The new `ReverseLayout` property landed in this
  port.
- **Asymmetric chat bubbles** via the new
  `Shape.RoundedCorners(Dp, Dp, Dp, Dp)` factory: outgoing messages
  use `(20, 4, 20, 20)` to flatten the top-right corner; incoming
  messages use `(4, 20, 20, 20)` to flatten the top-left corner
  pointing at the avatar — same shape upstream's `ChatItemBubble`
  draws.
- **Multiple dated separators** — "Today" and "20 Aug" rows are emitted
  between the same message groups as upstream. Their divider color uses
  `onSurface` at 12% alpha.
- Message bubbles with a 40 dp circular avatar tile (16 dp horizontal
  padding around it, mirroring upstream's 74 dp avatar+padding
  reservation) and a rounded coloured bubble for the message body.
- **Typography parity via the `Text` styling surface** — author names
  at 16 sp / `FontWeight.Medium` (M3 `titleMedium`), timestamps at
  12 sp (`bodySmall`), the "Today" separator at 11 sp / Medium /
  1 sp letter-spacing (`labelSmall`, rounded from 0.5 sp because
  `Sp` only takes integers), top-bar channel name at 16 sp / Medium
  with a 12 sp member-count subtitle, and drawer brand / section /
  chat labels at the matching M3 sizes.
- **Streak-aware avatars + per-author spacing** — when a sender
  posts multiple messages in a row, only the chronologically-last
  one shows the avatar tile; subsequent messages indent with a 72 dp
  `Spacer` so the bubbles still align. Author boundaries get an
  extra 4 dp of top padding (8 dp first-in-chain vs 4 dp within a
  streak). Because we use `reverseLayout = true` the streak walk
  mirrors upstream's `isLastMessageByAuthor` directly.
- **`isUserMe` differentiation** for the local user via a primary-
  colored bubble. Layout structure (avatar+spacer + author+text
  column) is identical for me vs others — same as upstream's
  `Message`/`AuthorAndTextMessage` row, no right-alignment.
- A pinned input row at the bottom with a single-line message field,
  "Type a message" placeholder, Send-labeled IME action, and a `Send`
  `TextButton` that is genuinely disabled while the input is empty.
  Its filled/outlined enabled and disabled treatment follows upstream.
  The remaining `BasicTextField` and elevated-`Surface` differences are
  tracked below.
- **5 input-selector icons** — emoji, @ mention, image, location,
  video call — same row upstream's `UserInputSelector` provides.
  Each is a toggleable `IconButton` whose background fills with
  `secondaryContainer` and whose tint flips to `onSecondaryContainer`
  when selected, matching upstream's selection visual. Selecting the
  emoji button opens the upstream-style pill selector with a vertically
  scrollable 10-column tappable grid. Selecting Stickers opens the
  upstream unavailable-feature dialog and resets to Emojis; selecting @ /
  image / location / video opens a `FunctionalityNotAvailable` panel —
  the same fallback upstream uses for the unbound selector pages.
- **IME + navigation-bar safe insets** on the input area via
  `Modifier.NavigationBarsPadding().ImePadding()` plus
  `WindowSoftInputMode = SoftInput.AdjustResize` on the activity, so
  the keyboard pushes the input row up without obscuring it (and
  without the system's default `adjustUnspecified` behaviour
  double-shifting the content under edge-to-edge).
- **Voice record mic + recording indicator** — when the text field
  is empty the trailing send affordance is joined by a mic
  `IconButton` that swaps the `TextField` for an animated
  recording overlay (pulsing red dot + MM:SS timer + "Swipe to
  cancel" hint). Tap-to-toggle starts and finishes the recording;
  dragging the mic horizontally past a 200 dp threshold cancels.
  The overlay swap rides on the new generic `AnimatedContent<T>`
  facade. Long-pressing the mic also shows the upstream "Touch and hold
  to record" tooltip. See *What's still omitted* for the exact gesture
  and transition-animation gaps.
- **Expanded-input dismissal** — `BackHandler` collapses any open
  selector before system back reaches navigation. Exact upstream focus
  transfer from the editor to the emoji panel requires the focus-target
  APIs tracked below.
- **Image attachment bubbles** — the upstream sticker drawable is seeded
  on the second message and rendered in its own 160 dp rounded bubble
  through the existing resource-backed `Image` facade.
- **Jump-to-bottom FAB** — appears after the list moves beyond the
  upstream 56 dp threshold and calls
  `LazyListState.AnimateScrollToItemAsync(0)`.
- **Pinned top-bar scroll behavior** — the conversation scaffold installs
  `Modifier.NestedScroll(...)` and passes the same remembered behavior to
  `CenterAlignedTopAppBar`, matching upstream elevation-on-scroll.
- **Drag-and-drop feedback** — accepted text/image drags add the first
  payload as a new message; the conversation gets the same red border
  and translucent red hover background during the drag lifecycle.
- **Clickable message annotations** — URLs use Compose's platform URI
  handler and `@aliconors` navigates to the matching profile route.
- **Accessibility grouping** — author and timestamp share merged
  semantics so screen readers announce them as one row.
- Reactive message list via `MutableStateList<Message>` — tapping
  send appends to the active channel and the UI recomposes.
- Reactive channel selection via `MutableState<string>` — drawer
  taps flow into the title, the member count, the message list, and
  the bolded selected chat row.
- Newly sent messages stamp `"now"` (matching upstream's
  `R.string.now` resource value).
- **`NavController` / `NavHost` routing** between two destinations:
  a `home` route hosting the conversation and a
  `profile/{userId}` route hosting `Profile`. Drawer profile rows
  and message-avatar taps both navigate to the profile route; the
  topbar's back arrow / system back returns. The drawer lives
  above the `NavHost` so it stays available on both screens.
- **Profile screen** (`Profile.cs`) — `Scaffold` with a
  `CenterAlignedTopAppBar` (back + more-options), a vertically
  scrolling body wrapped in `BoxWithConstraints` so the hero
  portrait caps at half the available height and moves at half scroll
  speed for the upstream parallax effect, name / status /
  display-name / position / twitter / timezone / channels rows,
  and an `ExtendedFloatingActionButton` aligned `BottomEnd` that
  expands / collapses based on `scrollState.Value == 0` (the M3
  equivalent of upstream's custom `AnimatingFabContent`). FAB
  icon and label switch on `ProfileScreenState.IsMe()`:
  `ic_create` + "Edit profile" for the local user; `ic_chat` +
  "Message" for a colleague.
- **`ProfileViewModel`** tracks the active user id in a
  `MutableState<string>` and resolves it to a
  `ProfileScreenState` via `Profiles.GetById(...)`. The route
  content reads the user id directly from
  `NavBackStackEntry.Arguments` so the route's display state is
  driven entirely by the back-stack argument; nothing is mutated
  during composition. (Upstream uses a `MutableLiveData` here; the
  port keeps the ID in `MutableState<string>` because Compose's
  snapshot system in this binding only safely shuttles
  JVM-convertible values, and the ID is a plain `string`.)
- **Stack normalization on profile navigation.** When the drawer
  fires `onProfileClicked` (or `onChatClicked`) while the profile
  screen is on top of the back stack, `JetchatApp` first calls
  `nav.PopBackStack(Routes.Home, inclusive: false)` and then
  navigates. Without that pop, opening the drawer from one
  profile and tapping a different profile row would push a second
  `profile/{userId}` entry, so back would return to the previous
  profile rather than the conversation.

## What's still omitted

Everything that can be completed with the current facade is wired. The
remaining differences require missing reusable APIs or an unavailable
official binding:

| Upstream feature                          | Why it's not here |
|-------------------------------------------|--------------------|
| Press-and-hold record gesture (`pointerInput` / `detectDragGesturesAfterLongPress`) | Missing Compose pointer-input surface; tracked by [#334](https://github.com/jonathanpeppers/Microsoft.AndroidX.Compose/issues/334). Until it lands, recording remains tap-to-start / tap-to-finish with draggable swipe cancellation. |
| Record-button `updateTransition` + `animateFloat` / `animateColor` | Missing transition value-animation surface; tracked by [#335](https://github.com/jonathanpeppers/Microsoft.AndroidX.Compose/issues/335). The port retains its visually equivalent timer-driven pulse. |
| Fractional `Sp` letter spacing (`0.5.sp`, `0.1.sp`) | `Sp` is integer-only; tracked by [#336](https://github.com/jonathanpeppers/Microsoft.AndroidX.Compose/issues/336). |
| Karla / Montserrat resource-backed typography | Custom `Font(resourceId)` / `FontFamily(fonts)` construction is missing; tracked by [#337](https://github.com/jonathanpeppers/Microsoft.AndroidX.Compose/issues/337). |
| Foundation text-input structure and IME Send callback | `BasicTextField` and keyboard-action support are missing; tracked by [#339](https://github.com/jonathanpeppers/Microsoft.AndroidX.Compose/issues/339). The current Material `TextField` preserves editing, placeholder, line, and IME-option behavior. |
| Emoji-panel focus transfer and IME dismissal | `Modifier.focusTarget`, focus observation, and ambient focus-manager access are missing; tracked by [#340](https://github.com/jonathanpeppers/Microsoft.AndroidX.Compose/issues/340). |
| Input/selector tonal elevation and content color | The current `Surface` facade omits color, content-color, elevation, and border slots; tracked by [#341](https://github.com/jonathanpeppers/Microsoft.AndroidX.Compose/issues/341). |
| Scaffold inset exclusion | `Scaffold.contentWindowInsets` cannot yet be customized, so the port applies IME/navigation padding directly to the input surface; tracked by [#342](https://github.com/jonathanpeppers/Microsoft.AndroidX.Compose/issues/342). |
| Exact baseline spacing and clipped profile parallax | Baseline-relative alignment/padding and `clipToBounds` modifiers are missing; tracked by [#343](https://github.com/jonathanpeppers/Microsoft.AndroidX.Compose/issues/343). |
| Profile FAB tertiary container | Material 3 FAB color/elevation slots are omitted by the current facades; tracked by [#344](https://github.com/jonathanpeppers/Microsoft.AndroidX.Compose/issues/344). The port uses the default primary-container/content pair to preserve contrast. |
| Glance home-screen widget + `requestPinAppWidget(...)` | No official .NET binding for `androidx.glance:glance-appwidget` is currently published. Upstream only shows the drawer entry when a compatible widget provider can be pinned, so the port omits it until that binding exists. |

## Facade features added for this port

In addition to the earlier round that landed during the first
iteration (sizing modifiers, `Background` / `Border` / `Clickable` /
`Clip` / `Weight`, `MutableStateList<T>`, drawable-resource
`Image` / `Icon`, `Modifier.VerticalScroll`, `ModalNavigationDrawer`),
this completion round added:

- **`Composed`** — a `ComposableNode` wrapper around
  `Func<IComposer, ComposableNode>` so sample code can read
  `c.ColorScheme()` /
  `c.Typography()` from inside a tree builder
  without needing a `partial class : ComposableNode` subclass.
- **`LazyColumn<T>.ReverseLayout`** — surfaces the
  `reverseLayout` parameter that's been on the underlying
  `LazyColumn` Kotlin composable from day one.
- **`DrawerStateHolder.OpenAsync()` / `CloseAsync()`** — `Task`-
  returning helpers backed by new raw-JNI suspend bridges over
  `DrawerState.open()` / `close()`, wired through the existing
  `SuspendBridge` continuation infrastructure.
- **`Shape.RoundedCorners(Dp, Dp, Dp, Dp)`** — asymmetric corner
  factory that calls
  `RoundedCornerShapeKt.RoundedCornerShape(float, float, float, float)`
  directly. (The 4-arg `(Dp, Dp, Dp, Dp)` overload is bindable;
  only the single-radius `(Dp)` overload is mangled.)
- **`Modifier.DragAndDropTarget(...)` + `DragAndDropEvent` +
  `DragAndDropTarget` facades** — wraps
  `androidx.compose.ui.draganddrop.dragAndDropTarget`; the sample uses
  the start/enter/exit/end callbacks for drop-zone feedback and appends the first
  dropped text or URI as a message.
- **`AnnotatedString` / `AnnotatedStringBuilder` / `SpanStyle` /
  `LinkAnnotation` / `AnnotatedText`** — facade primitives for
  Compose's rich-text type. `AnnotatedText` is a sibling of the
  source-generated `Text` facade rather than an extra ctor — the
  `AnnotatedString` overload's mangled JVM name (`Text-IbK3jfQ`)
  carries an extra `Map` slot for inline content, and the source-
  generator path emits one `Render` per facade. Same precedent as
  `Icon` exposing both vector-asset and resource-id paths.

## Implementation notes

### Why `Composed` instead of a `ComposableNode` subclass

As of #132, `ComposableNode.Render(IComposer)` is `public abstract`
and `ComposableContainer.Children` / `RenderChildren` are
`protected`, so subclassing from outside the facade assembly is
fully supported. `Composed(Func<IComposer, ComposableNode>)` is
the more concise alternative when all you want is to read
`c.ColorScheme()` /
`c.Typography()` from inside an existing builder
without writing a whole new class — the body lambda runs every
composition pass with the live `IComposer`, computes whatever
scheme / typography slots it needs, and returns the subtree built
against them. The Jetchat sample uses it at the top of `Build` so
the entire tree gets recomputed against the active scheme.

### Why hamburger nav fires `_ = drawerState.OpenAsync()`

`DrawerStateHolder.OpenAsync()` returns `Task`, and `IconButton`'s
`onClick` is `Action` (not `Func<Task>`). The fire-and-forget
discard is intentional — the suspend bridge runs on
`AndroidUiDispatcher.Main` and any exception inside the suspend
faults the returned task synchronously, but the click handler has
no way to surface that. `OpenAsync` throws
`InvalidOperationException` only if `Jvm` is null, which is
impossible by the time a click can fire (the field is populated on
the first render of `ModalNavigationDrawer`, which is unavoidable
before any user input).

### `reverseLayout = true` streak walk

With `reverseLayout = true`, item index 0 sits at the bottom of
the viewport. The sample reverses the message list before passing
it to `LazyColumn` so the newest message ends up at index 0. The
streak walk runs back-to-front
(`for (int i = src.Count - 1; i >= 0; i--)`) and emits messages in
chronological order with `IsStreak = prev?.Author == m.Author`
meaning "this message is followed in time by another from the same
author" — so the avatar appears on the chronologically-last
message of each chain, matching the Slack / iMessage convention
upstream uses.

### Send is disabled on empty input

The `TextButton` facade now exposes `Enabled`. The sample disables
the action for whitespace input, uses the upstream transparent
disabled container and outline treatment, and retains the
`IsNullOrWhiteSpace` guard in the Send handler.

### Drag-and-drop target hoisting

The `DragAndDropTarget` instance for the conversation surface is
hoisted into `composer.Remember` so the underlying
`DragAndDropTargetElement` keeps a stable identity across
recompositions; otherwise Compose rebuilds the modifier element
every frame and its internal hover/started/ended bookkeeping
resets. `OnDrop` reads the first `ClipData` item's text or URI and appends it
through `ui.AddMessage`. `OnStarted` / `OnEntered` / `OnExited` /
`OnEnded` drive the same border and hover background as upstream.

### `MessageFormatter` regex behaviour

`MessageFormatter.Format` runs the same alternation regex as
upstream — `(https?://[^\s\t\n]+)|(`[^`]+`)|(@\w+)|(\*[\w]+\*)|(_[\w]+_)|(~[\w]+~)`
— so a URL containing an `@` is consumed greedily as a single URL
match (the `[^\s\t\n]+` URL run reaches the next whitespace), and
the `@mention` branch only fires for bare tokens. This matches
upstream's behaviour even though regex alternation itself isn't
"longest-first" — the URL pattern simply wins because it's listed
first and its character class is greedy.

### `@mention` and URL taps use Compose links

`MessageFormatter` emits a `LinkAnnotation.Url` for web links, so
Compose opens them through the platform URI handler. Clickable person
annotations resolve the handle through `Profiles.GetById(...)` and use
the same profile-navigation callback as avatar taps.

### Layout and styling decisions vs upstream

Where the port intentionally takes a different path from the Kotlin
original:

- **Search / Info top-bar icons.** Upstream uses bare `Icon`
  composables with `.clickable` (not `IconButton`) so the touch
  target hugs the icon's 24 dp height. The port uses the same
  shape — `new Icon(... ) { Modifier.Clickable(...).Padding(...).Height(24) }`
  — to keep the visuals identical.
- **Avatar double border.** Upstream's `.border(1.5dp, accent,
  CircleShape).border(3dp, surface, CircleShape).clip(CircleShape)`
  composes outside-in: the 3 dp surface ring sits between the
  1.5 dp accent ring and any surrounding background. The port
  uses the same modifier chain.
- **Same chat-bubble silhouette** — upstream's
  `RoundedCornerShape(4, 20, 20, 20)` is the single source of
  truth and applies to both `me` and other-author bubbles. Only
  the fill flips (`primary` ↔ `surfaceVariant`).
- **Inside-streak gap is 4 dp; between-author gap is 8 dp** —
  matches upstream's per-author `Spacer` heights.
- **Selector icon highlight.** Upstream paints a rounded selected
  background in the current content color. The port uses the active
  scheme's secondary/on-secondary pair directly until `Surface`
  content-color slots are available.
- **`FunctionalityNotAvailable` collapse.** Upstream has two
  variants — an `AlertDialog` (DM selector) and a full panel
  ("Functionality currently not available / Grab a beverage and
  check back later!"). The port collapses both into one panel for
  the expanded-selector case; the dialog variant still fires from
  the search/info icons.
- **`ModalDrawerSheet` background.** Upstream defaults to
  `surfaceContainerLow`; this port's facade defaults to
  `secondaryContainer`, which in Jetchat's dark palette is a
  saturated blue. The drawer pins to `surface` to match upstream.
- **Drawer divider alpha.** Upstream tints with
  `onSurface.copy(alpha = 0.12f)`; the port uses
  `Color.WithAlpha(31)` for the equivalent ARGB value.
