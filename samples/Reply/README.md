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
than directly activating the underlying inbox. Its width is constrained to
the measured, padded search host so the popup retains both 16 dp side margins
instead of overflowing the right edge of the window. Popup animation and
IME focus details must not be described as pixel-identical without a matched
device comparison. No JNI or binding
changes are part of this integration. Search uses native theme typography;
the surrounding sample's existing theme/adaptive-layout differences remain.

`ReplySearchTests` in the device-test project covers prefix matching,
empty/no-match results, recomposition, native Back, leading-arrow clearing,
result selection, and removal/re-entry using the real search component and
linked sample data.

### Device validation

On 2026-09-15, source `8da2c4f` passed all four focused checks on the attached
Pixel 7: the numeric-padding regression and all three `ReplySearchTests`
(zero failures or skips). This includes no-result-to-match recovery, exact
popup bounds, IME Search, leading/system Back, both Back owners, selected
email ID, and composition departure/re-entry.

The real C# Reply activity also completed collapsed, empty, matching,
no-result, and selected-email capture flows in both light and dark mode.
The debug-only capture harness verified the activity's actual night
configuration; it did not change global settings.

The same five states were then captured in both themes from a
**target-SDK-modernized Kotlin reference**. At the user's request, the
reference's `gradle/libs.versions.toml` target SDK changed from 33 to 36,
matching the target generated by `net10.0-android`. The Kotlin application
code, resources, package name, and signing identity stayed unchanged; the
pristine artifact was retained separately. The updated APK installed through
normal verification, without using "Install anyway" or changing security
settings. The built APKs declare min/target SDK 24/36 for C# and 23/36 for Kotlin.
No SDK numbers are maintained in the C# source `AndroidManifest.xml`.

The ten paired states confirm matching queries, result labels, input bounds,
and selected-email headers. They are **not pixel-identical or whole-app
parity**: the existing app themes differ in palette/typography, Kotlin's
legacy expanded surface includes an additional input-height region, and the
detail toolbar's insets/alignment and randomized thread order differ. The
reference's target-SDK update can also affect edge-to-edge/inset behavior;
these captures are not presented as pristine target-33 behavior.

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

### Search integration

The interactive `ReplySearchBar` uses the inbox's same `Action<long>` for
row/result opening. Search owns only its local expanded-state dismissal:
native Back collapses it without clearing the query; leading Back clears
and collapses. Result selection invokes the detail callback, then
clears/collapses search, without mutating multi-selection or installing an
app-wide Back handler. See [Docked search](#docked-search) for the interaction
and popup-width contracts.
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
| `Modifier.nestedScroll(scrollBehavior)` + `TopAppBarDefaults.exitUntilCollapsedScrollBehavior()` (top-bar collapse on scroll) | dropped | [#142](https://github.com/jonathanpeppers/Microsoft.AndroidX.Compose/issues/142) |
| `LazyListState.lastScrolledBackward` / `canScrollBackward` (drives search-bar lift animation) | dropped | [#164](https://github.com/jonathanpeppers/Microsoft.AndroidX.Compose/issues/164) |
| `semantics { selected = isSelected }` on email cards (screen reader announces multi-select state) | dropped | [#167](https://github.com/jonathanpeppers/Microsoft.AndroidX.Compose/issues/167) |
| `Modifier.windowInsetsPadding(WindowInsets.statusBars)` on bars | dropped | [#69](https://github.com/jonathanpeppers/Microsoft.AndroidX.Compose/issues/69) |
| `MaterialTheme.typography.*` per-style reads (titleLarge / bodyMedium / labelMedium / …) | dropped — `FontSize` literals inline | [#61](https://github.com/jonathanpeppers/Microsoft.AndroidX.Compose/issues/61) |
| `stringResource(R.string.…)` lookups | dropped — strings inlined | [#146](https://github.com/jonathanpeppers/Microsoft.AndroidX.Compose/issues/146) |
| `ReplyHomeViewModel` + `StateFlow` + `collectAsStateWithLifecycle` (one source of truth) | replaced with activity-owned `ReplyState` (`MutableState` / `MutableStateList`) and native navigation state | [#160](https://github.com/jonathanpeppers/Microsoft.AndroidX.Compose/issues/160) |

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
