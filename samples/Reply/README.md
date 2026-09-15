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
- Top-level destinations + `ReplyNavigationActions` wrapper
- `ReplyBottomNavigationBar` — 4 `NavigationBarItem`s
- `ReplyInboxScreen` — `LazyColumn` of `ReplyEmailListItem`s with a
  functioning docked search bar pinned to the top and an
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

## Docked search

Search follows `ReplyDockedSearchBar` in
[`ReplyAppBars.kt`](https://github.com/android/compose-samples/blob/4c1fe7586e2fbf1c934925ef8ab64d3803361423/Reply/app/src/main/java/com/example/reply/ui/components/ReplyAppBars.kt),
with selection/navigation behavior from the same revision's
[`ReplyListContent.kt`](https://github.com/android/compose-samples/blob/4c1fe7586e2fbf1c934925ef8ab64d3803361423/Reply/app/src/main/java/com/example/reply/ui/ReplyListContent.kt)
and English labels from `Reply/app/src/main/res/values/strings.xml`.

Typing filters the existing 12 emails immediately by **case-insensitive
subject or sender full-name prefix**. It does not trim whitespace or search
bodies/substrings. Results retain source order and `Email.Id` keys, showing
the subject, sender full name, and 32 dp avatar in Material 3 list items.
An empty query shows "No search history"; unmatched input shows "No item
found". Selecting a result calls the same email-ID navigation callback as an
inbox row, then clears the query and collapses search without changing the
multi-selection set.

| Action | Query | Search |
|--------|-------|--------|
| Tap the input | retained | expands and focuses the editor |
| Leading Back arrow | cleared | collapses |
| Keyboard Search action | retained | collapses |
| System Back while search is expanded | retained | collapses without navigating |
| Tap outside the state-based popup | retained | dismisses the popup |
| Leave the inbox search composition, then return | reset to empty | collapsed |

The query and expansion wrappers are remembered **inside the search
composition**, not on the reconstructed tree node or in app-wide navigation
state. Ordinary recomposition preserves them; tab/detail departure and
activity recreation reset them, matching upstream's `remember` (not
`rememberSaveable`) ownership. The native expanded popup handles dismissal;
there is no competing app-wide search `BackHandler`.
On the tested Pixel, one system Back dismissed both the expanded popup and
its IME. A second Back would reach the underlying activity/navigation; the
test must not assume an extra IME-only Back is always required.

API adaptation: upstream uses the older query/expanded `DockedSearchBar`;
this port uses the existing state-based `SearchBar`, `SearchBarInputField`,
and `ExpandedDockedSearchBar` pair. Both resolve native Material 3 1.4.0,
but the new API uses a **focusable popup**, whereas upstream's older API
expands an inline surface. Outside taps therefore dismiss this popup rather
than directly activating the underlying inbox. Popup animation, sizing, and
IME focus details must not be described as pixel-identical without a matched
device comparison. No JNI or binding
changes are part of this integration. Search uses native theme typography;
the surrounding sample's existing theme/adaptive-layout differences remain.

`ReplySearchTests` in the device-test project covers prefix matching,
empty/no-match results, recomposition, native Back, leading-arrow clearing,
result selection, and removal/re-entry using the real search component and
linked sample data.

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

## What's missing (and why)

Upstream Reply is, before anything else, an **adaptive layouts
showcase**. The C# facade doesn't yet bind the primitives that
adaptation relies on, so the port consciously omits them — with
links back to the tracking issues.

| Upstream feature | Status | Tracking issue |
|------------------|--------|----------------|
| `NavigationSuiteScaffold` (compact → medium → expanded switchover) | dropped — pinned to bottom nav | `Xamarin.AndroidX.Compose.Material3.Adaptive.NavigationSuite` not yet referenced; would also need [#163](https://github.com/jonathanpeppers/Microsoft.AndroidX.Compose/issues/163). The size-class read itself ([#143](https://github.com/jonathanpeppers/Microsoft.AndroidX.Compose/issues/143)) shipped — see `composer.CurrentWindowAdaptiveInfo()`. |
| `NavigationRail` / `PermanentNavigationDrawer` / `ModalNavigationDrawer` content for medium and expanded sizes | dropped — bottom nav only | [#163](https://github.com/jonathanpeppers/Microsoft.AndroidX.Compose/issues/163) (drawer-row facade) — branching on `WindowSizeClass` is unblocked by [#143](https://github.com/jonathanpeppers/Microsoft.AndroidX.Compose/issues/143). |
| `accompanist.adaptive.TwoPane` + `WindowLayoutInfo`/`FoldingFeature` (list-detail with fold avoidance) | dropped — single-pane | [#168](https://github.com/jonathanpeppers/Microsoft.AndroidX.Compose/issues/168) |
| `NavigationDrawerItem` rows inside `ModalDrawerSheet` | not used (no drawer in single-pane port) | [#163](https://github.com/jonathanpeppers/Microsoft.AndroidX.Compose/issues/163) |
| `BackHandler {}` to collapse multi-select / detail | dropped — system back falls through to the navigator | [#166](https://github.com/jonathanpeppers/Microsoft.AndroidX.Compose/issues/166) |
| `NavOptions` with `popUpTo` / `launchSingleTop` / `restoreState` for bottom-nav tab semantics | dropped — re-tapping a tab re-navigates | [#169](https://github.com/jonathanpeppers/Microsoft.AndroidX.Compose/issues/169) |
| `Modifier.nestedScroll(scrollBehavior)` + `TopAppBarDefaults.exitUntilCollapsedScrollBehavior()` (top-bar collapse on scroll) | dropped | [#142](https://github.com/jonathanpeppers/Microsoft.AndroidX.Compose/issues/142) |
| `LazyListState.lastScrolledBackward` / `canScrollBackward` (drives search-bar lift animation) | dropped | [#164](https://github.com/jonathanpeppers/Microsoft.AndroidX.Compose/issues/164) |
| `semantics { selected = isSelected }` on email cards (screen reader announces multi-select state) | dropped | [#167](https://github.com/jonathanpeppers/Microsoft.AndroidX.Compose/issues/167) |
| `Modifier.windowInsetsPadding(WindowInsets.statusBars)` on bars | dropped | [#69](https://github.com/jonathanpeppers/Microsoft.AndroidX.Compose/issues/69) |
| `MaterialTheme.typography.*` per-style reads (titleLarge / bodyMedium / labelMedium / …) | dropped — `FontSize` literals inline | [#61](https://github.com/jonathanpeppers/Microsoft.AndroidX.Compose/issues/61) |
| `stringResource(R.string.…)` lookups | dropped — strings inlined | [#146](https://github.com/jonathanpeppers/Microsoft.AndroidX.Compose/issues/146) |
| Per-tab back-stack state retention (each tab keeps its scroll position when switched) | dropped (needs NavOptions) | [#169](https://github.com/jonathanpeppers/Microsoft.AndroidX.Compose/issues/169) |
| `ReplyHomeViewModel` + `StateFlow` + `collectAsStateWithLifecycle` (one source of truth) | replaced with `MutableState` / `MutableStateList` remembered at `MainActivity` scope | [#160](https://github.com/jonathanpeppers/Microsoft.AndroidX.Compose/issues/160) |

The reply / reply-all buttons, star icon, more-options menu, and
account avatar in the search bar are all wired as no-ops —
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
