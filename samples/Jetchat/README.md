# Jetchat (Microsoft.AndroidX.Compose port)

The profile FAB reads `tertiaryContainer` from the live color scheme, matching
Google's pinned `4c1fe7586e2fbf1c934925ef8ab64d3803361423` profile role. Its icon
and label inherit Material 3's corresponding content color; they do not capture
the primary-container foreground or a static theme value. The existing extended
FAB animation and layout approximations are unchanged. (Microsoft.AndroidX.Compose port)

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

Debug builds accept `--es test-palette light` or `--es test-palette dark`
on the activity launch intent for bounded comparisons. This overrides only
that activity's Compose palette; it does not change Android resource
configuration, system-bar appearance, or device settings. An omitted extra
continues to follow the system theme.

## Surface styling validation

A bounded Pixel 7 smoke on 2026-09-14 passed against the embedded, merged
`b0d9f97` Jetchat APK (SHA-256
`69956B98CF1759093F843FA2CA22BECB97058BC55E07C4D4320D36FABA0E1A67`).
The installed hash matched, private override files were absent, and the
external app directory did not exist. Light, dark, and no-override cold
launches reached the owned, focused/resumed activity. Native-idle captures
showed distinct input and nested-selector tonal surfaces, readable content,
and selected/unselected icon contrast. Emoji selection followed by editor
focus and typing retained the emoji plus `Surface`, closed the selector,
showed the IME, and enabled Send. Dark emoji and location-placeholder panels
also rendered correctly; normal launch followed the system palette.

This smoke includes the merged inset and baseline changes, but is not
whole-app pixel/font parity or acceptance for other feature work. The
later focus-target integration from #375 is not in that frozen APK. The
separate Gallery styling cycle passed on `8a71bb6` (`080E7771...`), and the
15 strict native Surface transition/identity/pixel/restoration cases passed
on `9ba2c04` (`582282EE...`), as recorded in
[`docs/compose-internals.md`](../../docs/compose-internals.md). Those earlier
APKs are not presented as reruns against this merged Jetchat revision.

## Scroll observation

The jump-to-bottom button observes a remembered `DerivedState<bool>`, matching
the pinned Kotlin sample's `derivedStateOf` predicate. Pixel-by-pixel scroll
updates can change the predicate's inputs without invalidating the surrounding
Box content when visibility stays the same. The threshold uses the current
Compose density, and the remembered calculation is keyed by the scroll-state
wrapper and pixel threshold. List keys, message content and jump-button actions
are unchanged.

A bounded Pixel 7 check on 2026-09-16 compared uninstrumented Mono Release
builds with default Profiled AOT, partial trimming and D8. For three scripted
scroll/input/send runs each, median HWUI jank changed from 6.62% (6.31-6.72%)
to 3.12% (2.56-3.19%); the per-run histogram p90 changed from 25 ms (24-25 ms)
to 10 ms (10-10 ms). Two return-baseline runs measured 6.36% (6.19-6.53%) and
p90 25.5 ms (24-27 ms). Native jump show/tap/return-hide checks passed for
baseline and candidate.

This is initial evidence, not a statistical guarantee or an isolated density
lookup benchmark. The return-baseline block followed an installation
interruption; its third planned run was not started because of the unchanged
time-budget guard. The scripted windows had no in-window UiAutomator query and
included a three-second post-Back settling delay. Earlier assisted-window
numbers and diagnostic-trace timings are not pooled with this comparison.

## Message identity

Identity was checked against upstream commit
`4c1fe7586e2fbf1c934925ef8ab64d3803361423`:
[`ConversationUiState.kt`](https://github.com/android/compose-samples/blob/4c1fe7586e2fbf1c934925ef8ab64d3803361423/Jetchat/app/src/main/java/com/example/compose/jetchat/conversation/ConversationUiState.kt)
has no message identifier, and
[`Conversation.kt`](https://github.com/android/compose-samples/blob/4c1fe7586e2fbf1c934925ef8ab64d3803361423/Jetchat/app/src/main/java/com/example/compose/jetchat/conversation/Conversation.kt)
uses unkeyed `item` calls for messages and day headers.

The port therefore keeps positional keys rather than pretending that
author/content/timestamp identifies a message. Sending the same text twice
or dropping the same attachment twice creates distinct messages with possibly
identical values; a content hash or concatenation would produce duplicate
keys. In particular, the sample's send timestamp is fixed and cannot serve
as an identifier. The flattened row objects are rebuilt during composition,
so their object identity is not stable either.

Before opting this list into stable keys, a persistent message model must
provide an immutable, unique ID assigned once when each message is created.
It must survive insertions, deletions, reordering, and reconstruction of the
display rows; day-header keys must occupy a separate namespace.
`LazyColumn<T>.Key` accepts non-null `string`, `int`, or `long` values.
Until such business identity exists, message-local state and viewport
anchoring still follow positions, as in the upstream sample.

## Conversation inset ownership

The conversation follows `ConversationContent` and `UserInput` at pinned upstream
commit `4c1fe7586e2fbf1c934925ef8ab64d3803361423`:
`Scaffold.ContentWindowInsets` is the live Material 3 default with navigation-bar
and IME insets excluded. Scaffold's remaining padding is forwarded once to the
body column; the top app bar still owns its status-bar edge. The input's **inner
column**, not its enclosing `Surface`, applies
`NavigationBarsPadding().ImePadding()`. These consuming modifiers use the larger
bottom inset rather than adding the navigation bar to the full keyboard height,
and the Surface remains behind the navigation bar.

Opening the emoji selector moves focus to its remembered `FocusRequester` and
low-level `FocusTarget`, ending the editor's IME session without making the input
container an extra accessibility focus stop. Exactly one selector-keyed effect
requests focus only for the attached emoji panel. Focusing the message field
closes the selector again; Back dismisses the selector. The Foundation
`BasicTextField` shares its focused state and keyboard callbacks with this handoff.
Input/selector tonal styling is also separate from this ownership change.

Omitting `ContentWindowInsets` (including in the profile screen) still uses
Kotlin's regular default. Null means default; `new WindowInsets()` means a supplied
all-zero value, not omission. The composable `Scaffold(..., contentWindowInsets: ...)`
adapter supports the same contract and keeps its original CLR signature for
compiled consumers and method groups.

The Gallery's **Scaffold content insets** demo opens a dedicated edge-to-edge
activity so the catalog's enclosing Scaffold cannot mask the result. Compare
Default, Zero, and Input-owned modes, focus the editor, switch to the selector
and back, dismiss the keyboard, and recreate the activity. Content must stay
above the navigation bar/IME with no extra bottom strip. Device regressions
also measure the actual body bounds and forwarded padding for both tree paths
and both composable adapters. The demo's ordinary body identity must stay the
same while switching inset modes; its saved tap count must also survive
**Recreate activity** (a new ordinary identity after recreation is expected).

## What's faithful

- **Input and selector Surface roles** — pinned
  [`UserInput.kt`](https://github.com/android/compose-samples/blob/4c1fe7586e2fbf1c934925ef8ab64d3803361423/Jetchat/app/src/main/java/com/example/compose/jetchat/conversation/UserInput.kt)
  uses 2 dp tonal elevation and `secondary` content color for the input
  Surface, with a nested 8 dp selector Surface. The selector uses the
  theme's surface/on-surface pair and cumulative tonal elevation (10 dp);
  its children no longer paint over that tint with `surfaceVariant`.
  Unselected input icons use `secondary`; selected icons use `onSecondary`.
  These changes leave editor focus, fonts, metrics, and inset ownership intact.
  The merged inset ownership from
  [#369](https://github.com/jonathanpeppers/Microsoft.AndroidX.Compose/pull/369)
  is preserved: the Surface extends behind the bars, and its inner Column
  owns navigation/IME padding once.
- **Bundled Karla / Montserrat families** — the six unmodified fallback TTFs
  from upstream revision `4c1fe7586e2fbf1c934925ef8ab64d3803361423` are wired
  into `JetchatFonts`, theme typography and the conversation/drawer/profile
  text roles. Font assignment preserves the pinned numeric metrics described below.
  These are upstream's local fallbacks, not its Google Fonts provider downloads.
  Jetchat has no bundled italic face, so italic emphasis uses Compose synthesis.
  See [font sources, weights, copyright and SHA-256](Assets/FONT_SOURCES.txt)
  and the bundled [SIL Open Font License](Assets/FONT_LICENSE.txt).
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
- **Pinned text metrics, including fractional spacing** — `Theme/Typography.cs`
  supplies the upstream font sizes, line heights, weights, and unrounded
  letter spacing to the theme and the conversation, drawer, profile, and
  emoji-selector labels. `Sp(float)` / `0.5f.Sp()` preserve the Kotlin
  `TextUnit` float payload. See the bounded metric comparison below;
  metric equality alone is not whole-app typography or font parity.
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
- **Foundation `BasicTextField` input** — the bound `TextFieldValue` editor
  preserves selection and IME composition without Material field chrome.
  Its native inner-editor decoration supplies the unfocused, empty
  "Message #composers" hint, 32 dp start padding, centered 64 dp input row,
  Karla body-large metrics, secondary-colored text/cursor and `maxLines = 1`.
  IME Send and the visible Send control use the same callback: ignore blank
  text, preserve surrounding whitespace on nonblank messages, clear the
  value/selection/composition, reset the message list and close the selector.
  Neither action clears editor focus, matching the pinned rapid-entry behavior.
  Emoji insertion replaces the current selection (including reversed
  selections), retains composition as upstream's `copy` does, and moves the
  cursor to the end of the resulting buffer. Opening the emoji panel transfers
  focus: Foundation 1.11.3's `CoreTextField` calls `deselect()` on focus loss,
  collapsing a nonempty selection to its maximum offset before insertion.
  Selecting `b` in `abcd` then opening the panel therefore inserts at `ab|cd`;
  the facade does not restore the earlier focused selection. Native diagnostics
  confirm the live value is `2..2` immediately before insertion, even though the
  closed IME session can still report its cached `1..2` range. Focused-selection
  replacement remains covered separately. The Send button's filled/outlined
  enabled and disabled treatment follows upstream.
- **5 input-selector icons** — emoji, @ mention, image, location,
  video call — same row upstream's `UserInputSelector` provides.
  Each is a toggleable `IconButton` whose background fills with
  `secondary` and whose tint flips to `onSecondary`
  when selected, matching upstream's selection visual. Selecting the
  emoji button opens the upstream-style pill selector with a vertically
  scrollable 10-column tappable grid. Selecting Stickers opens the
  upstream unavailable-feature dialog and resets to Emojis; selecting @ /
  image / location / video opens a `FunctionalityNotAvailable` panel —
  the same fallback upstream uses for the unbound selector pages.
- **IME + navigation-bar safe insets** owned by the input's inner column via
  `Modifier.NavigationBarsPadding().ImePadding()`, excluded from Scaffold's
  content insets, plus
  `WindowSoftInputMode = SoftInput.AdjustResize` on the activity, so
  the keyboard pushes the input row up without obscuring it (and
  without the system's default `adjustUnspecified` behaviour
  double-shifting the content under edge-to-edge).
- **Voice record mic + recording indicator** — when the text field
  is empty the trailing send affordance is joined by a mic
  `IconButton` that swaps the `TextField` for an animated
  recording overlay (pulsing red dot + MM:SS timer + "Swipe to
  cancel" hint). A native long press starts the UI-only recording;
  release finishes it. Per-event X/Y pixel movement accumulates, and a left
  swipe of at least 200 dp cancels only while the vertical displacement
  is at most 80 dp in either direction. Returning inside that vertical
  corridor after crossing the horizontal threshold also cancels, matching
  pinned `voiceRecordingGesture`. A cancelled gesture cannot later commit.
  Native coroutine cancellation is forwarded to the recording callback only
  while a recording gesture is active: disposing an idle detector or an
  already-cancelled gesture must not emit another recording cancellation.
  The overlay swap rides on the new generic `AnimatedContent<T>`
  facade. Callback updates during recomposition retain the active native
  handler; removing the control cancels its Kotlin pointer-input job.
  The surrounding tooltip disables automatic input so it cannot compete
  for the same long press. See *What's still omitted* for the short-tap tooltip gap.
  The button background scale (spring with medium-bouncy damping and low
  stiffness), alpha (2000 ms tween), and icon tint (200 ms tween) now share one
  native `Transition<bool>`, matching pinned `RecordButton.kt` at
  `4c1fe7586e2fbf1c934925ef8ab64d3803361423`. Background and foreground derive
  from the live `LocalContentColor`/`contentColorFor` roles, with circle clipping.
  Row alignment belongs to the enclosing Tooltip, not its recording anchor:
  animation-driven anchor recomposition can run without the parent Row scope.
  The independent recording-indicator timer and repeating pulse remain unchanged.
  No audio recording or permissions are added.
- **Expanded-input dismissal** — `BackHandler` collapses any open
  selector before system back reaches navigation. A remembered requester
  targets the emoji column with `FocusTarget`, not the editor or its parent.
  A selector-keyed effect requests focus only when the emoji target is
  attached; unrelated recomposition does not steal focus from its children.
  Editor focus gain closes the panel and resets message scroll. The panel
  keeps its accessibility description without adding `Focusable` semantics.
  The pinned `UserInput.kt` at `4c1fe7586e2fbf1c934925ef8ab64d3803361423`
  does not call `clearFocus` after Send (despite the original #342 motivation).
  Send therefore retains upstream keyboard behavior; the explicit
  `LocalFocusManager` clear/force-clear APIs are demonstrated in Gallery.
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
  the selected chat row's colors.
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

## Bounded typography comparison (#334)

The numeric reference is Google's
[`theme/Typography.kt`](https://github.com/android/compose-samples/blob/4c1fe7586e2fbf1c934925ef8ab64d3803361423/Jetchat/app/src/main/java/com/example/compose/jetchat/theme/Typography.kt)
at **`4c1fe7586e2fbf1c934925ef8ab64d3803361423`**, not moving `main`.
`Theme/Typography.cs` mirrors the upstream file organization. Its
`Typography.CreateJetchatTypography()` factory constructs the 15 theme slots
corresponding to Kotlin's top-level `JetchatTypography` value, retaining
per-composition caching in `JetchatTheme`. `JetchatFonts.WithFonts(...)` applies
the bundled Karla/Montserrat families to that baseline without changing its metrics.
The following screen text is explicitly assigned the corresponding metrics;
buttons and the message editor also consume the theme's type slots.

| Text / bounded screen | Upstream slot | Font size (sp) | Line height (sp) | Letter spacing (sp) | Weight |
|---|---|---:|---:|---:|---|
| Conversation channel and author names | titleMedium | 16 | 24 | 0.15 | SemiBold |
| Member count, timestamps; drawer section headings; profile field labels | bodySmall | 12 | 16 | 0.4 | Bold |
| Conversation date separator ("Today", "20 Aug") | labelSmall | 11 | 16 | 0.5 | SemiBold |
| Message body; profile position and field values | bodyLarge | 16 | 24 | 0.15 | Normal |
| Drawer channel and profile rows (selected and unselected) | bodyMedium | 14 | 20 | 0.25 | Medium |
| Profile name | headlineSmall | 24 | 32 | 0 | SemiBold |
| Emoji / sticker selector labels | titleSmall | 14 | 20 | 0.1 | Bold |
| Send button (theme label) | labelLarge | 14 | 20 | 0.1 | SemiBold |

The pinned sample uses **integer font sizes** in these styles; only letter
spacing is fractional. The Gallery's **Fractional typography** demo separately
exercises a 16.25 sp font, 24.75 sp line height, and positive/zero/negative
tracking. No arbitrary fractional font-size adjustments are applied to Jetchat.

`JetchatTypographyTests` compiles the actual sample metric definitions and
font-copy helper into the device test app and checks every native `Typography`
slot's packed font size, line height, letter spacing, and weight, both before
and after applying the resource families. It also checks family assignments
and that explicit families survive `WithTypography`. `ComposeValueTypeTests` checks
the float bit payload through bound `GetSp(float)`, `TextStyle`, and `SpanStyle`.
These are source/interop comparisons, not pixel-equality tests.

The visual checklist is bounded to the initial composers conversation
(channel/member labels, a visible author/timestamp/message and date separator),
the open drawer, the colleague profile's name/fields, and the emoji-selector
labels. Karla/Montserrat resource families from **#335** are now combined with
these metrics. Provider-downloaded font differences, baseline layout, avatars/sample data, dynamic
colors, and other recorded layout differences prevent a whole-screen parity
claim. A matched Kotlin/C# screenshot comparison against this exact revision
remains pending; no verified pinned Kotlin APK is available in this worktree.

### Device evidence

On 2026-09-14, the embedded Debug build from `a25a8e3` passed **16/16**
focused device cases (value-type and actual Jetchat typography interop),
alongside **4/4** host compatibility/bridge-lowering tests. The Gallery
fractional demo and all four Jetchat checklist states above were captured
and inspected: text was readable, fractional tracking was visible in the
Gallery comparison, and conversation/drawer/profile/emoji navigation completed.
This is C# rendering evidence, not a matched Kotlin visual comparison.

Capture configuration: **Pixel 7**, Android **API 37**,
**1080 x 2400**, **420 dpi**, font scale **1.0**, system **dark** theme with
dynamic colors. No device-wide settings were changed. Screenshots, UI
hierarchies, the TRX, and installed-APK SHA-256 verification were retained
in the issue execution artifacts (`gallery-fractional-typography.png`,
`jetchat-conversation.png`, `jetchat-drawer.png`, `jetchat-profile.png`,
`jetchat-emoji.png`, `sp-device-tests.trx`, `device-evidence.json`).
Light theme, other display/font scales, and resource-font integration
are not established by these captures.

After integrating the resource-font changes from `5b321e4`, **18/18** focused
native cases passed on the Pixel 7, including all 15 typography slots with
and without the bundled families and explicit family retention on `Text`
and `AnnotatedText`. The embedded test APK's installed SHA-256 matched the
host artifact; results are retained in `merge-device-tests.trx`.
This additional interop check does not add combined-font screenshots or
establish pixel parity.

## Baseline alignment and clipping

The recording timer and cancellation viewport now use `Modifier.AlignByBaseline()`.
The moving cancellation content is inside `ClipToBounds()`, rather than moving
the clipping viewport itself. The unavailable-panel subtitle uses
`PaddingFrom(AlignmentLineKt.FirstBaseline, before: 32)` instead of a fixed
8 dp gap. Existing font sizes, family assignments, text metrics and accessibility
labels are unchanged.

These uses follow
[`UserInput.kt` at the pinned revision](https://github.com/android/compose-samples/blob/4c1fe7586e2fbf1c934925ef8ab64d3803361423/Jetchat/app/src/main/java/com/example/compose/jetchat/conversation/UserInput.kt).
The ordinary input editor uses Box center-start alignment upstream, not baseline
alignment; the baseline group belongs to the recording indicator.

The issue's profile-clipping motivation was broader than that pinned source:
[`Profile.kt`](https://github.com/android/compose-samples/blob/4c1fe7586e2fbf1c934925ef8ab64d3803361423/Jetchat/app/src/main/java/com/example/compose/jetchat/profile/Profile.kt)
uses half-scroll top padding and `clip(CircleShape)`, **not** `clipToBounds`.
This change therefore leaves profile geometry untouched. The port's rounded
header, host/collapsing-container behavior, and baseline-height helpers still
differ; adding a rectangular clip would not establish profile parallax parity.

## What's still omitted

Remaining differences include missing reusable APIs, sample-specific
layout work, and unavailable official bindings:

| Upstream feature                          | Why it's not here |
|-------------------------------------------|--------------------|
| Short-tap recording tooltip | The recording wrapper sets `Tooltip.EnableUserInput=false`, as pinned upstream does, to avoid stealing the native long press. Programmatically showing "Touch and hold to record" on a short tap still needs tooltip-state control; this is not full recording-UX parity. Short taps do not start or finish recording. |
| Recording-indicator infinite pulse | Still timer-driven; distinct from the record button's native finite scale/alpha/color transition. No audio-recording subsystem is implemented. |
| Google Fonts provider typography | The exact pinned Karla / Montserrat resource fallbacks are bundled. Provider-backed downloads remain outside the resource-font API; no downloaded-font parity is claimed. |
| Exact profile baseline-height and parallax geometry | Reusable baseline alignment/padding and `ClipToBounds` are available. Profile's existing rounded clip, padding-based motion and host layout remain unchanged; the pinned upstream uses `CircleShape` and separate baseline-height helpers, not a rectangular clip. |
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

The `Button` facade exposes `Enabled`. The sample disables
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
  scheme's secondary/on-secondary pair directly, matching the input
  Surface's inherited secondary content role.
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
