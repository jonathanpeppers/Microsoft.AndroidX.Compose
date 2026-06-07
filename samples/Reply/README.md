# Reply (C# port)

A simplified C# port of Google's
[`Reply`](https://github.com/android/compose-samples/tree/main/Reply)
Material 3 adaptive design study, rebuilt on the `ComposeNet.Compose`
facade. Upstream Reply is a polished email client demonstrating
adaptive layouts (compact / medium / expanded), foldable awareness,
multi-pane list-detail, a docked search bar, multi-select, and a
fully animated bottom-nav / nav-rail / nav-drawer switchover.

This port keeps the **data layer faithful** (12 emails with thread
replies, 13 accounts, identical strings) and renders a **single-pane
phone layout** built from the same Material 3 building blocks.

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

## What's missing (and why)

Upstream Reply is, before anything else, an **adaptive layouts
showcase**. The C# facade doesn't yet bind the primitives that
adaptation relies on, so the port consciously omits them — with
links back to the tracking issues.

| Upstream feature | Status | Tracking issue |
|------------------|--------|----------------|
| `NavigationSuiteScaffold` + `WindowSizeClass` (compact → medium → expanded switchover) | dropped — pinned to bottom nav | [#143](https://github.com/jonathanpeppers/compose-net/issues/143) |
| `NavigationRail` / `PermanentNavigationDrawer` / `ModalNavigationDrawer` content for medium and expanded sizes | dropped — bottom nav only | [#143](https://github.com/jonathanpeppers/compose-net/issues/143) |
| `accompanist.adaptive.TwoPane` + `WindowLayoutInfo`/`FoldingFeature` (list-detail with fold avoidance) | dropped — single-pane | [#168](https://github.com/jonathanpeppers/compose-net/issues/168) |
| `NavigationDrawerItem` rows inside `ModalDrawerSheet` | not used (no drawer in single-pane port) | [#163](https://github.com/jonathanpeppers/compose-net/issues/163) |
| `BackHandler {}` to collapse multi-select / detail | dropped — system back falls through to the navigator | [#166](https://github.com/jonathanpeppers/compose-net/issues/166) |
| `NavOptions` with `popUpTo` / `launchSingleTop` / `restoreState` for bottom-nav tab semantics | dropped — re-tapping a tab re-navigates | [#169](https://github.com/jonathanpeppers/compose-net/issues/169) |
| `DockedSearchBar` (overlay + autocomplete + query state) | replaced with a static "search-shaped" row | [#167](https://github.com/jonathanpeppers/compose-net/issues/167) |
| `Modifier.nestedScroll(scrollBehavior)` + `TopAppBarDefaults.exitUntilCollapsedScrollBehavior()` (top-bar collapse on scroll) | dropped | [#142](https://github.com/jonathanpeppers/compose-net/issues/142) |
| `LazyListState.lastScrolledBackward` / `canScrollBackward` (drives search-bar lift animation) | dropped | [#164](https://github.com/jonathanpeppers/compose-net/issues/164) |
| `semantics { selected = isSelected }` on email cards (screen reader announces multi-select state) | dropped | [#165](https://github.com/jonathanpeppers/compose-net/issues/165) |
| `Modifier.windowInsetsPadding(WindowInsets.statusBars)` on bars | dropped | [#69](https://github.com/jonathanpeppers/compose-net/issues/69) |
| `MaterialTheme.typography.*` per-style reads (titleLarge / bodyMedium / labelMedium / …) | dropped — `FontSize` literals inline | [#61](https://github.com/jonathanpeppers/compose-net/issues/61) |
| `stringResource(R.string.…)` lookups | dropped — strings inlined | [#146](https://github.com/jonathanpeppers/compose-net/issues/146) |
| Per-tab back-stack state retention (each tab keeps its scroll position when switched) | dropped (needs NavOptions) | [#169](https://github.com/jonathanpeppers/compose-net/issues/169) |
| `ReplyHomeViewModel` + `StateFlow` + `collectAsStateWithLifecycle` (one source of truth) | replaced with `MutableState` / `MutableStateList` remembered at `MainActivity` scope | [#160](https://github.com/jonathanpeppers/compose-net/issues/160) |

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
`ComposeNet.Compose` facade.
