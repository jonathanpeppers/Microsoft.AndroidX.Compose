# Reply (C# port)

A simplified C# port of Google's
[`Reply`](https://github.com/android/compose-samples/tree/main/Reply)
Material 3 adaptive design study, rebuilt on the `Microsoft.AndroidX.Compose`
facade. Upstream Reply is a polished email client demonstrating
adaptive layouts (compact / medium / expanded), foldable awareness,
multi-pane list-detail, a docked search bar, multi-select, and a
fully animated bottom-nav / nav-rail / nav-drawer switchover.

This port keeps the **data layer faithful** (12 emails with thread
replies, 13 accounts, identical strings) and renders a **single-pane
phone layout** built from the same Material 3 building blocks.

<img src="../docs/reply.png" alt="Reply running on an Android device" width="320" />

## What's here

- Faithful port of upstream `data/local/*`:
  - `Email`, `Account`, `MailboxType`, `EmailAttachment`
  - `LocalAccountsDataProvider` — default account + 10 contacts
  - `LocalEmailsDataProvider` — 12 emails with thread replies,
    matching upstream string content
- Routes: `Inbox`, `Articles`, `DirectMessages`, `Groups`,
  `EmailDetail/{emailId}` (the four top-level routes mirror upstream's
  `Route` sealed interface; `EmailDetail` is added for the single-pane
  port since upstream uses pane navigation, not a `NavHost` route, for
  the detail view)
- Top-level destinations + `ReplyNavigationActions` wrapper using native
  pop-to-Inbox, save/restore-state and single-top navigation
- `ReplyBottomNavigationBar` — 4 `NavigationBarItem`s
- `ReplyInboxScreen` — `LazyColumn` of `ReplyEmailListItem`s with a
  simplified "search bar"-shaped row pinned to the top and an
  `ExtendedFloatingActionButton` ("Compose") anchored to the
  bottom-right
- `ReplyEmailListItem` — `Card` with `Modifier.CombinedClickable`
  (tap → open, long-press → toggle multi-select), `AnimatedContent`
  swapping a checkmark avatar in/out when selected
- `ReplyEmailDetail` — `Scaffold` + `EmailDetailAppBar` over a
  `LazyColumn` of `ReplyEmailThreadItem`s with Reply / Reply All
  buttons
- `EmptyComingSoon` — placeholder for the Articles / DMs / Groups
  tabs

## Stable list identity

The inbox and email-thread `LazyColumn<Email>` instances use
`Key = static email => email.Id`, matching both upstream `items(...,
key = { it.id })` calls in
[`ReplyListContent.kt`](https://github.com/android/compose-samples/blob/4c1fe7586e2fbf1c934925ef8ab64d3803361423/Reply/app/src/main/java/com/example/reply/ui/ReplyListContent.kt).
The identity reference is pinned to upstream commit
`4c1fe7586e2fbf1c934925ef8ab64d3803361423`.

`Email.Id` is the existing `long` business identifier also used for selection
and navigation, not the email's position, subject, sender, or timestamp.
Keep it unchanged when updating/reordering an email and unique within each
list. IDs may repeat between the inbox and a separate thread list; the seed
data has no repeated IDs within either list. This lets Compose retain an
email's item state and scroll anchor when other emails move around it.
The key API accepts non-null `string`, `int`, or `long` values; omitted keys
retain positional behavior.

## Navigation and Back contract

Behavior is compared with **android/compose-samples
`4c1fe7586e2fbf1c934925ef8ab64d3803361423`**, not a moving `main`:
[`ReplyNavigationActions.kt`](https://github.com/android/compose-samples/blob/4c1fe7586e2fbf1c934925ef8ab64d3803361423/Reply/app/src/main/java/com/example/reply/ui/navigation/ReplyNavigationActions.kt),
[`ReplyListContent.kt`](https://github.com/android/compose-samples/blob/4c1fe7586e2fbf1c934925ef8ab64d3803361423/Reply/app/src/main/java/com/example/reply/ui/ReplyListContent.kt),
and [`ReplyHomeViewModel.kt`](https://github.com/android/compose-samples/blob/4c1fe7586e2fbf1c934925ef8ab64d3803361423/Reply/app/src/main/java/com/example/reply/ui/ReplyHomeViewModel.kt).

| Flow | Pinned Kotlin behavior / C# contract |
|------|------------------------------------|
| Tap the current top-level tab repeatedly | Pop to the graph start without removing it, save popped entries, launch single-top, restore saved state. No duplicate current-tab entries. C# uses `PopUpToRoute = Route.Inbox` because its graph has a flat, fixed start route. |
| Switch tabs and return to Inbox | Restore the destination's native saved state, including the inbox's default Kotlin `rememberLazyListState()`. Do not replace it with a new managed `LazyListState`. |
| Open an email; system Back or app-bar Up | Return to the retained inbox context and reset the opened-email highlight to the first email. The same close action handles both paths. |
| Multi-selection | Long-press toggles a selected email. Selection persists through detail/tab navigation; **selection alone does not consume Back**. The pinned source has no selection Back handler. |
| Selected bottom-navigation item | Derived from the rendered destination, including Inbox for the port's detail route, rather than a separate click-updated route variable. Native Back and restoration cannot leave a stale selected tab. |
| Activity recreation | Compose saves the navigation stack and each destination's list state. `ReplyState` saves opened-email and selected IDs in the activity Bundle. Kotlin's ViewModel retains these on configuration changes; this port also saves them for Android saved-task restoration. |

The port retains its existing `EmailDetail/{emailId}` route instead of rewriting
the app to use Kotlin's in-Inbox detail pane. Switching from detail to another
tab and **tapping Inbox or pressing system Back** restores that saved detail
route. Navigation 2.9.8's non-inclusive saved pop associates the saved stack
with the `popUpTo` destination (Inbox) as well as the popped destination
(EmailDetail); restoring Inbox follows that association. A Back handler
exists only inside the non-Inbox top-level destinations
and uses the same Inbox restore action. This preserves Kotlin's still-open
in-Inbox detail context without changing the existing route architecture.
Inbox-root Back remains unhandled by the sample and exits normally; detail
owns its close action, and selection alone never intercepts.

### Search integration (planned)

Interactive search is **not implemented by this change**: `ReplySearchBar`
remains a static placeholder. #348 owns that separate implementation. The
agreed integration contract is to use the inbox's same `Action<long>` for
row/result opening. Search will own only its local expanded-state dismissal:
native Back will collapse it without clearing the query; leading Back will
clear and collapse. Result selection will invoke the detail callback, then
clear/collapse search, without mutating multi-selection or installing an
app-wide Back handler.
The pinned search uses ordinary `remember`, not saved state; leaving its
composition resets its query/expansion. The search implementation and paired
Kotlin search artifacts are tracked separately from navigation.

### Regression coverage

`ReplyNavigationTests` in `Microsoft.AndroidX.Compose.DeviceTests` links the
actual sample source and resources, rather than a second navigation model.
The focused native cases cover repeated tab taps and Back, exact visible email
IDs/pixel offsets after tabs and detail Back/Up, retained selection, root Back
fallthrough, saved detail, and activity recreation on a tab, detail, and Inbox.
Assertions run after native lifecycle/transition, recomposer, snapshot, and
layout readiness; they do not poll for expected route/scroll values.

```pwsh
dotnet build samples\Reply
dotnet build src\Microsoft.AndroidX.Compose.DeviceTests
# On a separately authorized device:
adb shell am instrument -w -e filter FullyQualifiedName~ReplyNavigationTests net.compose.devicetests/net.compose.devicetests.TestInstrumentation
```

Physical-device verification on 2026-09-15 used the linked Reply UI at commit
`259f694` on a Pixel 7: all three navigation tests passed in separate native
instrumentation invocations, as did the padding-overload regression control.
Visible inbox email IDs and pixel offsets matched after tab switches, detail
Back/Up, and activity recreation; saved-detail restoration also passed.
Host builds alone are not native proof. These runs do not establish
process-death restoration or matched Kotlin/C# visual parity, and the separate
search implementation remains outside this navigation test suite.

## What's missing (and why)

Upstream Reply is, before anything else, an **adaptive layouts
showcase**. This port remains single-pane; adaptive/fold-aware integration is
separate from the completed top-level navigation work. Entries below describe
sample omissions, not proof that the current library lacks the corresponding API.

| Upstream feature | Status | Tracking issue |
|------------------|--------|----------------|
| `NavigationSuiteScaffold` (compact → medium → expanded switchover) | dropped — pinned to bottom nav | `Xamarin.AndroidX.Compose.Material3.Adaptive.NavigationSuite` not yet referenced; would also need [#163](https://github.com/jonathanpeppers/Microsoft.AndroidX.Compose/issues/163). The size-class read itself ([#143](https://github.com/jonathanpeppers/Microsoft.AndroidX.Compose/issues/143)) shipped — see `composer.CurrentWindowAdaptiveInfo()`. |
| `NavigationRail` / `PermanentNavigationDrawer` / `ModalNavigationDrawer` content for medium and expanded sizes | dropped — bottom nav only | [#163](https://github.com/jonathanpeppers/Microsoft.AndroidX.Compose/issues/163) (drawer-row facade) — branching on `WindowSizeClass` is unblocked by [#143](https://github.com/jonathanpeppers/Microsoft.AndroidX.Compose/issues/143). |
| `accompanist.adaptive.TwoPane` + `WindowLayoutInfo`/`FoldingFeature` (list-detail with fold avoidance) | dropped — single-pane | [#168](https://github.com/jonathanpeppers/Microsoft.AndroidX.Compose/issues/168) |
| `NavigationDrawerItem` rows inside `ModalDrawerSheet` | not used (no drawer in single-pane port) | [#163](https://github.com/jonathanpeppers/Microsoft.AndroidX.Compose/issues/163) |
| Search overlay + autocomplete + query state | replaced with a static "search-shaped" row | [#165](https://github.com/jonathanpeppers/Microsoft.AndroidX.Compose/issues/165) |
| `Modifier.nestedScroll(scrollBehavior)` + `TopAppBarDefaults.exitUntilCollapsedScrollBehavior()` (top-bar collapse on scroll) | dropped | [#142](https://github.com/jonathanpeppers/Microsoft.AndroidX.Compose/issues/142) |
| `LazyListState.lastScrolledBackward` / `canScrollBackward` (drives search-bar lift animation) | dropped | [#164](https://github.com/jonathanpeppers/Microsoft.AndroidX.Compose/issues/164) |
| `semantics { selected = isSelected }` on email cards (screen reader announces multi-select state) | dropped | [#167](https://github.com/jonathanpeppers/Microsoft.AndroidX.Compose/issues/167) |
| `Modifier.windowInsetsPadding(WindowInsets.statusBars)` on bars | dropped | [#69](https://github.com/jonathanpeppers/Microsoft.AndroidX.Compose/issues/69) |
| `MaterialTheme.typography.*` per-style reads (titleLarge / bodyMedium / labelMedium / …) | dropped — `FontSize` literals inline | [#61](https://github.com/jonathanpeppers/Microsoft.AndroidX.Compose/issues/61) |
| `stringResource(R.string.…)` lookups | dropped — strings inlined | [#146](https://github.com/jonathanpeppers/Microsoft.AndroidX.Compose/issues/146) |
| `ReplyHomeViewModel` + `StateFlow` + `collectAsStateWithLifecycle` (one source of truth) | replaced with activity-owned `ReplyState` (`MutableState` / `MutableStateList`) and native navigation state | [#160](https://github.com/jonathanpeppers/Microsoft.AndroidX.Compose/issues/160) |

The reply / reply-all buttons, star icon, more-options menu, and
account avatar in the search-shaped row are all wired as no-ops —
upstream Reply doesn't ship implementations either; they're
intentionally-stubbed UI affordances.

## Assets

- 12 icon vector drawables under
  `Resources/drawable/` (Apache 2.0, copied verbatim from upstream
  `Reply/app/src/main/res/drawable/`).
- 12 avatar JPGs + 4 paris photos under
  `Resources/drawable-nodpi/` (Apache 2.0, copied verbatim from
  upstream `Reply/app/src/main/res/drawable-nodpi/`).

## Run

```pwsh
dotnet build samples/Reply           # build
dotnet build samples/Reply -t:Run    # deploy + run on connected device
```

## Attribution

The data-layer string content (email subjects/bodies/account names),
icon vectors, and avatar / photo bitmaps are copied from
[android/compose-samples](https://github.com/android/compose-samples/tree/main/Reply)
under the
[Apache License 2.0](https://github.com/android/compose-samples/blob/main/LICENSE).
The C# UI code is original to this repo and built against the
`Microsoft.AndroidX.Compose` facade.
