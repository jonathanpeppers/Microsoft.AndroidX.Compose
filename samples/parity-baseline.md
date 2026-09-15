# Sample parity baseline

**Status: partial baseline, not whole-app parity.** This is the bounded
comparison record for [#349](https://github.com/jonathanpeppers/Microsoft.AndroidX.Compose/issues/349),
reconciled on 2026-09-15. Source comparisons and documentation corrections
cover Jetchat and Reply; JetNews received only confirmed documentation fixes.
Ten historical Reply light/dark screenshot pairs are retained as session
artifacts; their results and provenance are summarized below.
Matched Jetchat captures, additional sizes, complete capture-time environment
metadata and a repeat on the final integrated candidate remain outstanding.
Keep #349 open until those gates are recorded.

## Fixed references and evidence boundaries

| Evidence | Revision / scope |
| --- | --- |
| Kotlin application source | [android/compose-samples `4c1fe7586e2fbf1c934925ef8ab64d3803361423`](https://github.com/android/compose-samples/tree/4c1fe7586e2fbf1c934925ef8ab64d3803361423). No moving `main` is used as the comparison oracle. |
| C# source audit | `b8c93a68579a2430a54cbe3271730047516edec9`, after the search/navigation and Jetchat feature deliveries. |
| Preserved Reply captures | C# `8da2c4f3d74b072516e47ddc733cb6974249cbfb`, from [#382](https://github.com/jonathanpeppers/Microsoft.AndroidX.Compose/pull/382) / #348. The application source is unchanged at the audit revision, but runtime/generator changes exist between these revisions. These are not new captures of `b8c93a6`. |
| Kotlin capture variant | The pinned Reply source with the user-authorized Gradle `targetSdk` change **33 to 36**, plus debug-only capture instrumentation. Original application code/resources unchanged; original signer retained. Not pristine target-33 behavior. |
| Jetchat historical acceptance | #372 / #340 editor, Gallery and real-IME evidence; separate Surface and animation deliveries. Each result belongs to its original source/APK, not the current branch or a Kotlin comparison. |
| Excluded claims | No new performance measurement (#346), JetNews rewrite, foldable acceptance, process-death guarantee, package publication, or exact whole-app visual match. |

The [API coverage report](../docs/api-coverage.md) was regenerated during
reconciliation; only its timestamp changed. Symbol matches do not establish
overload, parameter, ownership, accessibility or behavior parity. A closed
binding issue is a delivered prerequisite, not proof its sample integration
is complete.

## Capture conditions

The accepted Reply PNGs and native hierarchy files were checked against
#348's final `paired-reference-target36.json` manifest. Older `light-failure`
files were excluded. The raw captures and manifests are session artifacts,
not repository assets; native hierarchy whitespace is retained verbatim
for hash verification. The historical run is summarized in
[#382](https://github.com/jonathanpeppers/Microsoft.AndroidX.Compose/pull/382).

| Condition | Preserved Reply run |
| --- | --- |
| Date / device | 2026-09-15; physical Pixel 7. C# light/dark around 13:20-13:21 and Kotlin around 13:49, UTC-05:00. Not simultaneous captures. |
| Android OS / API / build | Not recorded in the imported capture manifest. Must be recovered from a contemporaneous record or recaptured; APK target/compile SDK below is **not** the device API level. |
| Display | All 20 PNGs are 1080 x 2400 pixels, portrait. Actual window dp dimensions and density overrides were not recorded in this manifest. Do not substitute assumed Pixel defaults. |
| Font / locale / contrast | Font scale, locale tag, system contrast and font-weight adjustment were not recorded in the imported manifest. English labels and a Gboard keyboard are visible; that does not establish the settings/version. |
| Theme | Debug harness applied a per-activity night override before creation and checked actual `nightMask`: light 16, dark 32. No global theme setting was changed. Gboard remains dark in both app themes. |
| Dynamic color | Source-confirmed difference: C# uses default `MaterialTheme` (`UseDynamicColor = true` on API 31+); pinned Kotlin `ContrastAwareReplyTheme` defaults to `dynamicColor = false`, custom typography/shapes and contrast-aware schemes. Exact wallpaper-derived colors/system contrast were not recorded. |
| C# build | Debug capture APK; min/target/compile SDK 24/36/36. SHA-256 `d66c0a92efe88f7638ab2090c468c2783d4836c71b43897a3c3d715dee860741`. |
| Kotlin build | Debug target-36 capture APK; min/target/compile SDK 23/36/37. SHA-256 `3a28f98c14084f4b652569905bcc7a078126f4d052636a0a55733d3c104b5779`. Gradle wrapper 9.5.0, AGP 9.3.1, Java 17.0.16. |
| Installation boundary | Original target-33 capture APK was rejected by normal verification. The later authorized target-36 variant installed normally. No verifier bypass, package rename or signing-identity change was used. Target modernization can change edge-to-edge behavior. |
| Capture method | Debug instrumentation drives each real app activity and reads the owned native hierarchy. Query edits use fresh text/bounds checks. Screenshots retain system bars/IME; no pixel normalization or similarity score is claimed. |

### Required conditions for the next run

Freeze both source revisions, build configuration, exact dependency/build
changes and APK hashes before device use. Record OS release/API/build,
display and app-window px/dp, density, font scale/weight adjustment, locale,
orientation, navigation mode, IME/version, actual activity night mode,
dynamic-color flags, wallpaper/palette and system contrast. Never fill
missing historical values from a later device query.

Use a freshly coordinated exclusive device window: no concurrent installs,
input or profiling. Prefer per-activity/debug configuration for comparison
settings; do not change a shared physical device's global display/theme
settings without authorization. Keep any reference patches, capture harness
hash, interaction transcript, PNG/hierarchy hashes and failures with the run.
The source apps' different theme choices should first be reported as-is,
not silently patched to improve a screenshot.

## Finite screen and interaction checklist

Each case has a defined starting state. Reset only the sample-owned state
between independent cases; retain it within a navigation/editing case.
Screenshots alone do not pass input, restoration, accessibility or animation
checks. `Paired` below means a preserved state comparison, not a claim of
pixel identity. `Historical C#` means one implementation was exercised.

### Jetchat

The [Jetchat checklist](Jetchat/README.md#finite-comparison-checklist) defines 13 cases, J01-J13, covering
conversation, channels, input/Send, focus/emoji, attachments, recording,
profile and restoration. They are source-audited expectations, not new
device passes. The final current-candidate run must execute those cases
against the fixed Kotlin reference and attach paired captures for the
screen states, plus interaction evidence for the behavior-only cases.

Historical #340 evidence establishes Foundation editing and real software
IME Send on its exact candidate: nonblank input is sent **untrimmed**,
input clears and focus remains; whitespace-only Send leaves the spaces
unchanged and the visible Send control disabled. Native focus loss may
collapse a selection before emoji insertion; do not compare a stale
pre-handoff selection against the post-handoff caret.

### Reply

| ID | Initial state and action | Expected outcome / current evidence |
| --- | --- | --- |
| R01 | Fresh Inbox, first email visible, query empty, search collapsed, no multi-selection. Capture. | Inbox/search/navigation/FAB visible. Light + dark **paired** (`collapsed`); palette, typography, card shape and FAB differences remain. |
| R02 | R01; tap search. Capture before typing. | Expanded empty search says "No search history"; focused input and IME. Light + dark **paired** (`empty`); inline-surface versus popup geometry differs. |
| R03 | R02; type `Bonjour`. Capture. | One result: email ID 2, "Bonjour from Paris", Allison Trabucco. Light + dark **paired** (`match`). Prefix matching, not substring/body search. |
| R04 | R03; replace with `no-such-email`, then restore `Bonjour`. | "No item found", then the same ID-2 result returns. Light + dark **paired** no-result states; the original capture transcripts record exact query recovery. |
| R05 | Matching `Bonjour` result; select it. Capture; use Up, repeat and use system Back. | Opens ID 2 detail with "7 Messages", no search editor. Light + dark **paired** (`selected`). Up/Back/list-offset behavior has separate **historical C#** #347 evidence, not paired navigation proof. |
| R06 | Expanded search containing `Bonjour`; exercise leading arrow, IME Search, system Back and outside-tap dismissal independently. | Leading arrow clears/collapses; IME Search and native dismissal retain query/collapse. The C# popup consumes outside taps rather than activating Inbox. **Historical C#** #348; paired interactive repeat pending. |
| R07 | Search query entered; trigger ordinary recomposition, then leave for a tab/detail and return. | Recomposition retains query/expansion; leaving search composition resets them. **Historical C#** #348. Native navigation/list state is distinct from unsaved search state. |
| R08 | Inbox, no selection; long-press ID 1, tap its avatar, and inspect selected semantics. | Kotlin long-press and avatar each toggle selection. C# long-press is wired; avatar toggle and selected semantics are missing sample integration. Capture/semantics repeat pending (#383). |
| R09 | Inbox scrolled to a recorded email ID and pixel offset; switch Articles/DMs/Groups, re-tap current tab, return and recreate activity. | Single-top tabs and retained native destination/list state; selection preserved. **Historical C#** #347. Exact paired visible IDs/offsets and final-candidate recreation repeat pending. |
| R10 | Inbox at top; scroll forward, then backward; open ID 2 detail and scroll it. | Kotlin FAB shrinks/expands by scroll direction and remains in single-pane detail; detail app bar scrolls with the list. C# FAB stays expanded in Inbox and is absent in detail; toolbar is pinned. Source-confirmed differences (#383); paired motion pending. |
| R11 | Fresh medium and expanded windows; repeat Inbox and ID-2 detail. | Kotlin navigation adapts; expanded width selects dual pane. C# remains bottom-nav/single-pane. Capture pending; adaptive nav #383, fold/list-detail work #168. |
| R12 | Inbox with selected IDs, then detail, tab and root Back; separately invoke Compose/star/reply affordances. | Selection alone does not consume Back. Detail closes before root exit; tab return preserves still-open detail per the port's route contract. Stub actions remain stubs in both apps. Historical C# navigation evidence only; paired repeat pending. |

### Size/theme execution matrix

These are finite follow-up targets, **not measurements already taken**.
The added dimensions describe an app window in dp, not a request to resize
the shared physical Pixel. Use an authorized emulator/window fixture.

| Configuration | Cases | Result |
| --- | --- | --- |
| Compact portrait, normal font, light + dark | J01-J13; R01-R10, R12 | Only historical R01-R05 state pairs available; all Jetchat pairing and remaining Reply interactions pending. |
| Short compact window, 360 x 640 dp, font scale 1.3, light + dark | J01, J04, J05, J07, J09; R01-R05 | Pending. Check editor/IME/selector reachability and text clipping; exact environment must be recorded. |
| Medium window, 700 x 900 dp, font scale 1.0, light + dark | J01, J09; R11 | Pending. Distinguish navigation adaptation from fold/list-detail support. |
| Expanded window, 1000 x 800 dp, font scale 1.0, light + dark | J01, J09; R11 | Pending. No fold posture is simulated by width alone. |

Fold postures, every locale, every font scale and every possible input
sequence are outside this finite baseline. #168 requires separate hinge/
posture evidence before claiming fold-aware parity.

## Historical Reply comparison results

The ten light/dark pairs cover five scripted states in both apps. The
full-resolution PNGs, native hierarchy dumps and hash manifest remain
with the session evidence. This documentation records the findings, not
a checked-in capture bundle. For shared review, attach selected screenshots
to the issue or PR rather than adding raw run output to Git.

| State | Result in both light and dark |
| --- | --- |
| R01 collapsed | Same initial inbox/search state; palette, typography, card shape and FAB styling differ. |
| R02 empty | "No search history"; matching editor bounds, different expanded-surface height/modality. |
| R03 match | `Bonjour` finds "Bonjour from Paris" by Allison Trabucco; input bounds agree, surrounding visuals differ. |
| R04 no result | `no-such-email` shows "No item found"; transcripts also record recovery to `Bonjour`. |
| R05 selected email | Same "Bonjour from Paris" / "7 Messages" header, no editor; toolbar placement, styling and thread order differ. |

### Observed differences and classification

Use exactly these meanings: **sample integration** (port does not use an
available behavior/API), **missing reusable API** (specific unsupported
surface established), **intentional deviation** (explicit adaptation), or
**not yet investigated** (insufficient behavioral/member-level evidence).
A single screen may contain differences in several categories.

| Difference | Classification | Evidence / action |
| --- | --- | --- |
| Reply dynamic/default theme versus custom contrast-aware theme/typography | Sample integration | `ReplyApp.cs`, `MaterialTheme.cs`, pinned `theme/Theme.kt`; visible in both themes. #383. Preserve this cause instead of attributing all pixel differences to bindings. |
| Reply avatar click, selected semantics, FAB colors/scroll response/detail presence, toolbar layout and bar spacing | Sample integration | Source paths in the [Reply README](Reply/README.md#whats-missing-and-why), paired screenshots and pinned `ReplyListContent.kt` / `ReplyEmailListItem.kt`. #383. |
| Reply Card color/shape route and all missing per-component style slots | Not yet investigated | Rectangular C# card backgrounds versus rounded Kotlin cards are visible. Audit exact Card/other parameter support before splitting any library gap from #383. A `Card` symbol match is not parameter parity. |
| Reply focusable search popup instead of the legacy inline expanded surface | Intentional deviation | #348's existing state-based API integration. Input bounds are `[42,178,1038,326]` px in all eight editor pairs, but expanded minimum height/modality differ. Outside tap is not equivalent. |
| Reply separate detail route and activity-owned state instead of in-Inbox detail/ViewModel | Intentional deviation | #347; native stack/list restoration is preserved by the explicit Back contract. C# also saves selected/opened IDs in the activity Bundle. |
| Reply medium/expanded navigation | Sample integration | NavigationSuite, rail/drawer and size-read APIs already exist; #383. |
| Reply fold-aware dual pane and exact replacement for Accompanist TwoPane | Not yet investigated | Current port is single-pane. #168 is the existing reusable-API/integration tracker; its historical "neither package bound" claim is not a member audit of today's bindings. |
| Jetchat selector/@ dialog, nonblank-input mic presence, author baselines, drawer, jump and profile presentation | Sample integration | [Detailed source comparison](Jetchat/README.md#remaining-differences-and-classification); [#384](https://github.com/jonathanpeppers/Microsoft.AndroidX.Compose/issues/384). Reuse the completed sprint APIs, not duplicate binding requests. |
| Jetchat programmatic short-tap tooltip and native infinite pulse | Missing reusable API | Missing **facade control** established; official-binding availability not established. [#388](https://github.com/jonathanpeppers/Microsoft.AndroidX.Compose/issues/388) and [#385](https://github.com/jonathanpeppers/Microsoft.AndroidX.Compose/issues/385) require binding inspection first. #336 covers finite, not infinite, animations. |
| Jetchat video flow and draft/selector restoration | Sample integration | [#387](https://github.com/jonathanpeppers/Microsoft.AndroidX.Compose/issues/387) and [#386](https://github.com/jonathanpeppers/Microsoft.AndroidX.Compose/issues/386). Video dependencies and the exact TextFieldValue saver adapter remain **not yet investigated**; do not claim generic saved state is missing. |
| Jetchat fixture/local fonts, broader drop acceptance and normalized profile history | Intentional deviation | Exact boundaries in its README. Neither app implements a second conversation log when the channel highlight changes. |
| Jetchat widget availability, provider fonts, full accessibility and matched timing | Not yet investigated | #349 bounded follow-up; #149's closed historical acceptance described a different drawer shape and has been annotated, not reopened. No blanket missing-package claim. |
| JetNews remaining adaptive/theme/localization/search/link behavior | Sample integration or not yet investigated, as listed in [JetNews](JetNews/README.md#whats-omitted) | #159. Hero PNGs, styled runs, refresh/retry, bookmark feedback and share chooser already exist. JetNews is not device-compared here. |

### Sample data that prevents naive pixel comparison

Jetchat's pinned fixture contains a video message absent from the C# fixture
(10 versus 9 initial messages). Both channel selectors retain the single
`#composers` log. Different message/avatar/time choices and C# drop extensions
are documented in its README. Normalize
the chosen logical message/scroll anchor in a new fixture **only as an
explicitly identified derived reference**, or retain the differences.

Reply has 12 inbox emails and 13 accounts (3 user accounts, 10 contacts).
The #348 source comparison checked inbox IDs/subjects/sender IDs and account
names. C# keeps the seven thread entries in fixed order; Kotlin uses
`threads.shuffled()` for most emails, including ID 2. Thus the selected
email/header can match while the first thread card differs, even across
Kotlin light/dark runs. Text whitespace, line wrapping and displayed
timestamp styling are not covered by the search-data equality assertion.

JetNews has six original articles about this library with upstream article
photos. Its prose is intentionally not the Kotlin sample's article corpus.

## Delivery and issue reconciliation

| Planning-time claim in #349 | Current result / remaining boundary |
| --- | --- |
| Jetchat only sets IME Send options and uses Material TextField | #340 is closed; both BasicTextField routes, generated authoring/decoration and sample keyboard actions shipped. Historical C# native/IME evidence is retained in #372. Updated tracker distinguishes completion from unperformed whole-app pairing. |
| Reply search is a static row | #348 / #382 delivered prefix search and native dismissal. This report summarizes the final target-36 reference pairs retained in session artifacts, not the earlier failed/overflow candidates. |
| Reply top-level navigation is plain Navigate | #347 / #380 delivered pop-to-Inbox, save/restore and single-top behavior. Focused C# navigation tests are documented; they are not paired visual or process-death evidence. |
| JetNews hero PNGs and inline spans are absent | Corrected from `HomeCards`, `PostScreen`, `PostBody`: present. Underlined links remain non-clickable, and search does not filter. |
| #120 still needs shared-state and veto generation invented | Corrected tracker remains **open**: `TimeInput` and `ModalBottomSheet` are generated; current search-family ownership/migration, SnackbarData forwarding and BottomSheetScaffold's two-stage state/hybrid lowering remain. #354's closure is not blanket migration completion. |
| Closed issue links represent missing reusable APIs | Sample index and READMEs separate delivered prerequisites, concrete port work and uninvestigated APIs. #333-#337 and #339-#344 are delivered prerequisites, not open Jetchat blockers. |

There is no matched pre-sprint capture set for these cases. The earlier
claims above are a source/backlog history, not invented "before" screenshots.
The only available after-change paired states are R01-R05 at the recorded
Reply candidate. **No new final-candidate device repeat was performed for
this documentation change.** Publish a separate run record with the same
case IDs after the remaining changes; do not overwrite historical hashes or
promote pending cells to passes.

## Reference map and attribution

Under the fixed Kotlin revision, the principal Reply oracles are
[`ReplyApp.kt`](https://github.com/android/compose-samples/blob/4c1fe7586e2fbf1c934925ef8ab64d3803361423/Reply/app/src/main/java/com/example/reply/ui/ReplyApp.kt),
[`ReplyListContent.kt`](https://github.com/android/compose-samples/blob/4c1fe7586e2fbf1c934925ef8ab64d3803361423/Reply/app/src/main/java/com/example/reply/ui/ReplyListContent.kt),
[`ReplyAppBars.kt`](https://github.com/android/compose-samples/blob/4c1fe7586e2fbf1c934925ef8ab64d3803361423/Reply/app/src/main/java/com/example/reply/ui/components/ReplyAppBars.kt),
[`ReplyEmailListItem.kt`](https://github.com/android/compose-samples/blob/4c1fe7586e2fbf1c934925ef8ab64d3803361423/Reply/app/src/main/java/com/example/reply/ui/components/ReplyEmailListItem.kt)
and [`ReplyNavigationActions.kt`](https://github.com/android/compose-samples/blob/4c1fe7586e2fbf1c934925ef8ab64d3803361423/Reply/app/src/main/java/com/example/reply/ui/navigation/ReplyNavigationActions.kt).
Jetchat's README links its corresponding pinned screen/input/profile files.

The captured sample imagery includes Google's Apache-2.0-licensed Reply
assets and sample text; see [sample attribution](README.md#attribution)
and the [pinned upstream license](https://github.com/android/compose-samples/blob/4c1fe7586e2fbf1c934925ef8ab64d3803361423/LICENSE).
Screenshots are unmodified evidence, not replacement artwork or a claim
that system keyboard/chrome belongs to this project.
