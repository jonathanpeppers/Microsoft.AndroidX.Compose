# JetNews (Microsoft.AndroidX.Compose port)

A simplified C# port of
[android/compose-samples ▸ JetNews](https://github.com/android/compose-samples/tree/4c1fe7586e2fbf1c934925ef8ab64d3803361423/JetNews).
Upstream is labelled **Medium complexity**; this port targets the
phone-only single-pane flow and leans on the same data-shape ideas
without copying any Kotlin source.

Documentation reconciled against C# revision
`b8c93a68579a2430a54cbe3271730047516edec9` for
[#349](https://github.com/jonathanpeppers/Microsoft.AndroidX.Compose/issues/349).
JetNews is a source/documentation check only in the
[sample parity baseline](../parity-baseline.md), not a matched-device comparison.

<img src="../docs/jetnews.png" alt="JetNews running on an Android device" width="320" />

Run with:

```pwsh
dotnet build samples/JetNews -t:Run
```

## What's faithful

- Three top-level screens: **Home**, **Article**, **Interests**, all
  wired through `NavHost` + `NavController` with a typed `Routes`
  helper for the route strings.
- **Navigation drawer** (`ModalNavigationDrawer` + `ModalDrawerSheet`)
  with a JetNews logo header, divider, and two destination rows
  (Home, Interests) that highlight whichever route is currently
  active and navigate via the shared `NavController`. Tapping a row
  fires `DrawerStateHolder.CloseAsync()` so the drawer slides shut
  behind the navigation; tapping the top-bar hamburger fires
  `OpenAsync()` to slide it back open. Both go through the
  `SuspendBridge` plumbing around `DrawerState.open()` / `close()`.
- **Home feed**: `Scaffold` + `CenterAlignedTopAppBar` (hamburger +
  title + search icon action) with a `LazyColumn` that flattens the
  highlighted post, a "Recommended for you" section, a "Popular on
  JetNews" section, and a "Based on your history" section into a
  single lazy list — same structure upstream's `PostList` produces
  with `LazyColumn` and section headers.
- **Article reader**: `Scaffold` + `TopAppBar` with a back arrow that
  pops the back stack, a hero card with title / subtitle / metadata,
  and per-paragraph rendering keyed off `ParagraphType` (Title /
  Caption / Header / Subhead / Text / CodeBlock / Quote / Bullet) —
  the same eight styles upstream's `Paragraph` enum defines.
  `BottomAppBar` hosts the bookmark toggle and a share button.
- **Article assets and markup**: home cards and the article reader use
  bundled hero/thumbnail PNGs. Paragraphs with markup use `AnnotatedText`
  and `SpanStyle` for bold, italic, underlined link text, and monospace
  code; quotes are italic. Link styling does not make a URL clickable.
- **Interests** screen: `PrimaryTabRow` with Topics / People /
  Publications tabs (driven by a remembered `MutableState<int>`),
  per-tab toggleable lists backed by three `MutableStateList<string>`
  selections, with check / add leading icons reflecting current
  membership.
- **Bookmarks**: per-post `IconToggleButton` (Phase 2
  `[Callback(typeof(bool))]`) that flips membership in a shared
  `MutableStateList<string>` of post ids — both the home cards and
  the article bottom bar use the same component.
- **Theme**: `MaterialTheme` wraps the whole tree, so the device's
  Material You dynamic color scheme drives surface / primary / etc.
- **Feed state and feedback**: destination-scoped `HomeViewModel` exposes
  loading, content, error/retry, and refresh state. `PullToRefreshBox` and
  the toolbar refresh action invoke `RefreshAsync`; bookmark actions show
  sample-managed snackbar feedback.
- **Resource-backed icons** via the Phase 7 `[PainterResource]`
  `Icon` facade (menu, search, back, bookmark, bookmark-filled,
  share, home, interests, check, add, close, refresh, JetNews logo,
  and JetNews wordmark) — fourteen vector drawables under
  `Resources/drawable/`.

## What's omitted

These are remaining sample differences, not a list of missing bindings.
Closed API issues identify delivered prerequisites; sample-side follow-up
is tracked in [#159](https://github.com/jonathanpeppers/Microsoft.AndroidX.Compose/issues/159).

| Upstream feature                                                | Tracking issue |
|-----------------------------------------------------------------|----------------|
| Adaptive list-detail two-pane layout on wider devices | Not investigated in this documentation pass; #159 tracks the navigation/shared-transition design and exact remaining APIs. The size-class read shipped in [#143](https://github.com/jonathanpeppers/Microsoft.AndroidX.Compose/issues/143). |
| Top-bar elevation / collapse on scroll (`pinnedScrollBehavior`, `enterAlwaysScrollBehavior`) | Sample integration: [#142](https://github.com/jonathanpeppers/Microsoft.AndroidX.Compose/issues/142) delivered `Modifier.nestedScroll` and scroll behaviors; this sample does not use them. Follow-up: #159. |
| Adaptive Topics two-column layout (`InterestsAdaptiveContentLayout`) — port renders one column | Sample integration: the custom `Layout` primitive shipped in [#144](https://github.com/jonathanpeppers/Microsoft.AndroidX.Compose/issues/144). Follow-up: #159. |
| Resource-localized strings, theme typography and replacement of fixed paragraph colors | Sample integration: [#146](https://github.com/jonathanpeppers/Microsoft.AndroidX.Compose/issues/146), [#59](https://github.com/jonathanpeppers/Microsoft.AndroidX.Compose/issues/59), [#58](https://github.com/jonathanpeppers/Microsoft.AndroidX.Compose/issues/58), and [#61](https://github.com/jonathanpeppers/Microsoft.AndroidX.Compose/issues/61) are delivered prerequisites, not open binding blockers. Follow-up: #159. |
| Search filtering, clickable article links, deep links and upstream error/snackbar behavior | Remaining behavior to investigate/integrate in #159. Search accepts text but does not filter; link spans are decorative. Refresh, retry, bookmark feedback and a share chooser already exist. |
| Hilt / Kotlin Flow / serialization-backed repository | Intentional C# adaptation: `HomeViewModel`, `BookmarksViewModel`, `IPostsRepository` and `PostsRepository` over six original seed articles; not an absence of ViewModel support. |

## Implementation notes

### Seed data is original content about Microsoft.AndroidX.Compose itself

`PostsRepo.cs` ships six articles whose bodies discuss the
Microsoft.AndroidX.Compose project (binding strategy, Material 3 facade,
navigation, state holders, …). This sidesteps reproducing
upstream's Android-team Kotlin blog posts and keeps the sample
self-documenting.

### Hero images use bundled bitmaps

`HomeCards.cs` and `PostScreen.cs` pass `Post.HeroId` / `Post.ThumbId`
to `Image`. Hero regions use the source image's `992 / 296` aspect
ratio; thumbnails are sized separately. The twelve PNGs come from the
upstream sample under Apache 2.0; see the [asset attribution](../README.md#attribution).
Image support from [#145](https://github.com/jonathanpeppers/Microsoft.AndroidX.Compose/issues/145)
is no longer an implementation blocker. This is not a claim that every
crop, inset or color matches upstream.

### Paragraphs preserve styled runs, not link actions

`Paragraph.Markups` contains ranges passed by `PostBody.BuildAnnotated`
to `AnnotatedStringBuilder.AddStyle`. Bold, italic, underlined links and
monospace code are rendered using the API delivered in
[#141](https://github.com/jonathanpeppers/Microsoft.AndroidX.Compose/issues/141).
Code has fixed light-background/dark-foreground colors; quote paragraphs
use `FontStyle.Italic`. `Markup.Href` is not consumed by this renderer:
an underlined URL is not an implemented navigation action.

### Drawer items auto-close on tap

`JetnewsDrawer.Build` navigates on tap, updates a
`MutableState<string>` mirror of the active route, and fires
`DrawerStateHolder.CloseAsync()` so the drawer slides shut behind
the navigation. Re-tapping the already-active row also closes the
drawer (no navigation, just dismiss). The top-bar hamburger on
Home / Interests fires `OpenAsync()` to slide it back open. Both
go through the `SuspendBridge` plumbing wired up in
[#140](https://github.com/jonathanpeppers/Microsoft.AndroidX.Compose/issues/140).

### Search accepts input; share opens a chooser

The home search icon toggles an `OutlinedTextField` with a remembered
query and a close/reset action, but does not filter the feed.
The article share button opens a confirmation dialog; its "Share anyway"
action invokes `MainActivity.SharePost`, which launches an Android
`ACTION_SEND` chooser with the article title and a synthetic URL.
No share target produces a snackbar, not a silent no-op. The synthetic
URL is not evidence of working inbound deep links.

### Interests is a single column

Upstream's `InterestsAdaptiveContentLayout` is a custom `Layout {}`
that splits topics into multiple columns based on available width.
The port still renders the Topics tab as a flat single-column list with
section headers. The `Layout` primitive is available
([#144](https://github.com/jonathanpeppers/Microsoft.AndroidX.Compose/issues/144));
the adaptive algorithm has not been integrated.

### Static builders, not `ComposableNode` subclasses

Each screen uses a static `Build(…)` method returning `ComposableNode`.
This is a sample authoring choice: `ComposableNode.Render(IComposer)` is
public and can be overridden by a sample-side subclass. The builders
construct trees when their composable paths execute; that observation is
not a measured allocation or performance result. NavHost remembers the navigation graph,
not the first per-destination tree (see `NavHost.cs`). Each successful
host render publishes the current destination content and invalidates
visible destinations; inactive screens receive the latest content when
revisited. The first render still defines the registered route topology;
later content updates do not replace the graph or back stack.

### State lives at MainActivity scope

Most `MutableState` / `MutableStateList` instances (current route,
three interests selections, interests-tab index) live in
`MainActivity.OnCreate` via `Remember(...)`. They flow as
parameters through `JetnewsApp.Content` → per-screen builders. This
keeps the screen builders pure functions of their inputs and lets
the same `MutableState` reference survive recompositions and
navigation transitions.

Bookmarks are an exception: they live in a
`BookmarksViewModel` acquired via `composer.ViewModel<T>(…)` at the
activity scope, so the set survives configuration change (the
activity's `ViewModelStore` retains the instance across
`Activity.OnCreate` calls). The home feed and the article screen
both receive the same `BookmarksViewModel` reference, so toggling a
bookmark from either screen is observed by the other.

### Per-screen state lives in `HomeViewModel`

The Home feed state is owned by a `HomeViewModel : Microsoft.AndroidX.Compose.ViewModel`
acquired in `HomeScreen.Build` via `composer.ViewModel(...)`. The VM
is rooted in the current `NavBackStackEntry`'s `ViewModelStore`, so
it survives recomposition and configuration change, and clears
when the user pops the destination off the back stack.
