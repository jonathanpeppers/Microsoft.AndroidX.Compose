# Sample parity baseline

**Recorded baseline: 2026-09-15.** Jetchat and Reply were compared on an
isolated Android emulator in light/dark mode and compact, short, medium
and expanded windows for
[#349](https://github.com/jonathanpeppers/Microsoft.AndroidX.Compose/issues/349).
This report distinguishes **verified flow matches**, **observed differences**
and **not established** behavior. It does not claim whole-app or pixel parity.
JetNews received source-confirmed documentation corrections, not a device comparison.

## References and artifact boundaries

| Input | Identity |
| --- | --- |
| Kotlin application source | [android/compose-samples `4c1fe7586e2fbf1c934925ef8ab64d3803361423`](https://github.com/android/compose-samples/tree/4c1fe7586e2fbf1c934925ef8ab64d3803361423) |
| C# application/runtime source | `b8c93a68579a2430a54cbe3271730047516edec9`; subsequent commits on this branch change documentation only |
| Kotlin reference adaptation | User-authorized Gradle target SDK **33 to 36**, plus debug-only capture instrumentation/manifest. Application code/resources unchanged. Not pristine target-33 behavior. |
| Builds | Debug capture APKs, not Release profiling. C# min/target/compile SDK 24/36/36; Kotlin 23/36/37. All candidates include x86_64, install normally and are not `testOnly`. |
| Dependency difference | C# Compose runtime 1.11.3; Kotlin APK metadata reports 1.12.0. Both use Material 3 1.4.0. Binding revisions remain in `Directory.Build.targets`. |
| Performance | Separate [#346](https://github.com/jonathanpeppers/Microsoft.AndroidX.Compose/issues/346). Debug/software-rendered captures are not language-overhead, frame-rate or memory comparisons. |

Raw PNGs, native hierarchies, action transcripts, APKs and hash manifests
are **session artifacts, not repository files**. `comparison-evidence-index.json`
indexes 213 source-specific attempts and 3,711 hashed files, including failed
and limited attempts. Those numbers are **not pass counts**. The index and
this report belong to the #389/#349 comparison session; selected evidence
can be attached for shared review without committing raw run output.

The earlier ten Reply state pairs from #348/#382 remain separate historical
evidence at C# `8da2c4f3d74b072516e47ddc733cb6974249cbfb`. They are not
relabeled as the new runs.

### Capture APKs

Full hashes, signatures, source/driver identities and embedded managed-PE
hashes are retained in the corresponding session manifests. Prefixes below
identify the accepted evidence groups, not interchangeable builds.

| Evidence group | C# APK SHA-256 prefix | Kotlin APK SHA-256 prefix |
| --- | --- | --- |
| Jetchat conversation/drawer/Send | `9754a7f5dec7` | `c9431136d650` |
| Jetchat emoji/recording/profile/recreation/synthetic drag | `8d21dfa31448` | `19cdd856935a` |
| Jetchat size matrix | `10ddad31e63a` | `0c8414f256df` |
| Reply search and size matrix | `e1809ee40df8` | `1ed20227e3e7` |
| Reply scrolling/recreation and Kotlin selection/navigation | `f5e6ec30f1e1` | `a6594db41d9b` |
| Final C# avatar/navigation/Back observations | `7da0f5f28db0` | Use the preceding Kotlin evidence, not an invented uniform-harness run |

Driver corrections stayed outside application code. They addressed
AppCompat theme admission, standard native Back dispatch, mistaking the
long-clickable search editor for an email row, implementation-specific
`DM`/`Direct Messages` labels, and observing actual activity recreation
rather than requiring an optional `ActivityMonitor` notification.
Original failures remain indexed.

## Environment and size matrix

| Condition | Recorded value |
| --- | --- |
| Device | New, session-owned `parity349` AVD; serial `emulator-5580`; no physical Pixel input |
| OS / API / ABI | Android 16 / API 36 / native x86_64 |
| System image | Google Play image revision 7, extension 17; fingerprint `google/sdk_gphone64_x86_64/emu64xa:16/BE2A.250530.026.D1/13818094:user/release-keys` |
| Emulator / host | Android Emulator 37.1.11, build 15917651; Windows/WHPX; two virtual cores, 4096 MB; `swiftshader_indirect`; headless, no snapshots |
| Locale / font / contrast | `en-US`; font-weight adjustment 0; system contrast 0; font scale recorded per configuration below |
| Theme | AVD-local system night mode set to the requested theme and restored after each case; actual activity night mode checked. A per-activity override alone did not override Kotlin Jetchat's AppCompat policy. |
| Dynamic color | Jetchat follows its sample theme/dynamic-color policy. C# Reply uses default dynamic MaterialTheme; Kotlin Reply uses its custom contrast-aware theme with dynamic color off by default. Palette/provider-font differences are not normalized away. |
| Input / bars | Native actions and bounded gestures on positively owned windows; active IME/navigation settings, window/inset data and action timestamps retained. IME Search/Send uses the editor's advertised action, not hardware Enter. |
| Cleanup | Original AVD size/density overrides, font scale and night mode restored; owned packages stopped and emulator processes/ports verified absent. No verifier bypass or shared-device setting change. |

Every size-matrix record checked the **actual activity** width/height in dp,
density and font scale, not merely successful `wm` commands.

| Configuration | Pixels / density | Actual activity viewport | Font scale | Records |
| --- | --- | --- | --- | --- |
| Compact | 1080 x 2400 / 420 dpi | 411 x 914 dp | 1.0 | Initial and interaction runs, separately indexed |
| Short compact | 1080 x 1920 / 480 dpi | 360 x 640 dp | 1.3 | 36 |
| Medium | 1400 x 1800 / 320 dpi | 700 x 900 dp | 1.0 | 16 |
| Expanded | 2000 x 1600 / 320 dpi | 1000 x 800 dp | 1.0 | 16 |

All **68 geometry/configuration checks** passed. This is not 68 functional
parity passes. Short-window captures cover Jetchat conversation, emoji/input,
nonblank-input mic and profile states plus Reply's five search/detail states.
Larger windows cover Jetchat conversation/profile and Reply inbox/detail.
Reply detail was reached through the verified R05 search-result route where
the original R11 row locator was limited; that does not pass R11 navigation.
No fold posture, arbitrary locale or every possible font size is claimed.

## Finite cases and results

The [Jetchat checklist](Jetchat/README.md#finite-comparison-checklist) defines
J01-J13 with initial state, actions and expected outcomes. Fresh independent
cases start at the seeded conversation or Inbox; state is retained inside
each editing/navigation/recreation case.

`EXECUTED` and `observations-completed` are driver execution statuses, not
automatic parity verdicts. **Not established** means insufficient evidence
for that behavior, not an absent product feature or a successful comparison.

### Jetchat

| Cases | Result and boundary |
| --- | --- |
| J01-J02 conversation/drawer | Both themes captured and both channel selections exercised. Both apps retain the single `#composers` conversation despite changing the drawer highlight. Seed text, avatars, video and geometry differ. |
| J03 visible Send / IME Send | Verified matching behavior in both themes: ` hello ` and ` Ime349 ` are sent untrimmed; the editor clears and remains focused. Whitespace-only IME Send retains the spaces. Native text evidence, not a screenshot-only assertion. |
| J04 emoji/focus/Stickers/Back | Both implementations produce `ab😀cd` from the controlled caret, hand focus to the selector, refocus the editor and dismiss the selector/dialog. Repeated in the short font-scale-1.3 configuration. Layout differs. |
| J05 selectors | @/photo/location observations retained; C# unavailable panels differ from Kotlin dialog/animated panels. Full paired selector sequence is **not established** because the video-picker boundary did not satisfy the strict resolved-component/root-owner capture gate. |
| J06-J07 recording | Long-press UI timer and normal release observed; Kotlin's short-tap tooltip is visually present. C# hides the mic with nonblank input. Cancel/corridor and pulse-timing equivalence are **not established**: injected cancel sequences left ambiguous UI in both implementations. No recorded audio is claimed. |
| J08 jump control | C# jump action exercised. Kotlin's labeled control is visible in captured pixels but absent from the returned accessibility tree; its tap outcome is **not established**. Do not call the product control missing. |
| J09-J10 profiles/history | Profile layouts/actions captured in both themes and all window classes. C# Up was exercised; Kotlin's different drawer/scrolling toolbar prevented that selector from finding an Up control. One Kotlin history sequence completed; another was blocked by an ambiguous matching label. No full navigation-equivalence claim. |
| J11 synthetic drag | Both insert test-owned plain text `ParityDrop349`; only C# accepts the nonresolving image URI as text. Temporary test-source overlay is identified in artifacts. This is not real image loading/preview. Live mention/URL interaction equivalence is **not established** by these runs. |
| J12 activity recreation | Verified difference in both themes: C# loses unsent `Retain349` and closes the emoji selector; Kotlin preserves both. This is activity recreation, not process-death proof. |
| J13 video | C# has no corresponding attachment/player surface. Kotlin's platform-picker ownership mismatch stopped capture before foreign UI inspection. Selection, preview, playback and sending are **not established**; no media was accessed. |

### Reply

Post-baseline integration evidence for #383 was recorded separately on
2026-09-17, with the final compact reviewer-fix validation at executable
source `0edae7729af64010ac5a361e99d6030ab8d80207`.
On a physical Pixel 10 (Android 16 / API 36), original 1080 x 2424 at
420 dpi (411 x 923.4 dp), font scale 1.0, all seven linked
all nine `ReplyNavigationTests` and all three `ReplySearchTests` passed from exact
fresh-installed, embedded-assembly APKs. The run exercised avatar selection,
strict Android checked-state semantics, forward/back FAB behavior and detail
presence, scrolling/centered detail toolbar, navigation/restoration and
search ownership. Fresh compact light/dark screenshots and accessibility
hierarchies are session artifacts. This is source-specific C# acceptance,
not a new matched Kotlin comparison; the R01-R12 observations below remain
the fixed #349 baseline.

A separate source-specific matrix then passed the exact adaptive test at
600 x 900 dp and 840 x 800 dp, with native hierarchy evidence for the
start-side rail control and light/dark Inbox/detail captures at each width.
The pinned policy uses bottom navigation below 600 dp width or 480 dp height,
a rail through 1199 dp, and a drawer from 1200 dp; the 1200 dp boundary is
covered by a focused policy regression. Those matrix dimensions were
temporary window/density overrides on the same physical phone. They do not
prove physical tablet/foldable behavior, fold posture handling, or dual-pane
content; #168 remains the list/detail boundary.

| ID | Initial state / action | Result |
| --- | --- | --- |
| R01 | Fresh Inbox, collapsed empty search | Paired light/dark states, including short window. Visual theme/card/FAB differences remain. |
| R02 | Open empty search | Both show "No search history"; inline Kotlin surface and C# focusable popup differ. |
| R03 | Enter `Bonjour` | Both show "Bonjour from Paris" / Allison Trabucco. Matching prefix-query/result observations repeated across sizes. |
| R04 | Enter `no-such-email` | Both show "No item found". |
| R05 | Recover `Bonjour`, select result | Both open email ID 2's "Bonjour from Paris" / "7 Messages" detail. Repeated in compact, short, medium and expanded windows. |
| R06 | Independently exercise leading Back, IME Search and system Back | Leading Back clears/collapses; IME/native dismissal preserve query and collapse. Outside-tap comparison is **not established** because the driver could not prove a bounded inert tap target. |
| R07 | Query, depart to a tab, return | Query ownership observations retained. One C# re-entry snapshot was limited during navigation; universal re-entry equivalence is **not established** by this driver. |
| R08 | Long-press email ID 0, then tap its proven avatar bounds | Verified visual difference: Kotlin toggles checkmark/avatar in place; C# opens detail. The second C# avatar toggle was deliberately not attempted after leaving Inbox. Raw `isSelected` on one wrapper is not the effective selection state. |
| R09 | Scroll, switch/re-tap tabs, Back | Kotlin sequence completed. C# captures include overlapping outgoing placeholder and incoming Inbox during transition; stable return/offset equivalence is **not established**, not a proven navigation regression. Earlier #347 evidence retains its own source/build boundary. |
| R10 | Scroll forward/backward, open and scroll detail | Both sequences captured. C# keeps bottom navigation and omits the Compose FAB in detail; Kotlin retains it in single-pane detail. Timing/expansion cannot be inferred solely from a missing accessibility label. |
| R11 | Initial/detail at larger widths | Native geometry verified. Kotlin shows adaptive rail and dual pane at the recorded expanded window; C# stays bottom-nav/single-pane. Detail-size evidence uses R05, not a passed R11 locator. |
| R11 recreation | Recreate scrolled Inbox, then detail | Both themes/implementations completed with a distinct resumed activity and destroyed old instance; before/after native state retained. No process-death or private exact-offset claim. |
| R12 | Selection, detail Back/Up, root Back, placeholder tabs | Both implementations' corrected-driver sequences completed. Selection/root-Back observations remain distinct from the unresolved stable tab-return comparison in R09. |

## Differences and follow-ups

The categories are **sample integration**, **missing reusable API**,
**intentional deviation**, and **not yet investigated**. A missing facade
control is not automatically a missing official binding; inspect the
runtime `.Android.dll` before adding JNI.

| Difference | Classification / tracking |
| --- | --- |
| Reply avatar behavior, styling, inset ownership, adaptive navigation, FAB/detail presentation | Sample integration: [#383](https://github.com/jonathanpeppers/Microsoft.AndroidX.Compose/issues/383). Preserve the demonstrated search behavior. |
| Reply fold/list-detail APIs and exact Card/style parameter coverage | Not yet investigated at member level: [#168](https://github.com/jonathanpeppers/Microsoft.AndroidX.Compose/issues/168), #383. The size screenshots prove the layout difference, not package absence. |
| Reply state-based popup versus legacy inline search; separate detail route/activity state | Intentional API/architecture adaptations documented in [Reply](Reply/README.md). |
| Jetchat selector, author baseline, drawer, jump and profile presentation | Sample integration: [#384](https://github.com/jonathanpeppers/Microsoft.AndroidX.Compose/issues/384); delivered #333-#344 APIs are not reopened. |
| Jetchat programmatic Tooltip / native infinite pulse | Missing reusable facade controls: [#388](https://github.com/jonathanpeppers/Microsoft.AndroidX.Compose/issues/388) / [#385](https://github.com/jonathanpeppers/Microsoft.AndroidX.Compose/issues/385). Binding availability still requires inspection. |
| Jetchat draft/selector recreation | Observed sample difference: [#386](https://github.com/jonathanpeppers/Microsoft.AndroidX.Compose/issues/386). General saved-state APIs already exist. |
| Jetchat video flow | Sample integration and dependency audit: [#387](https://github.com/jonathanpeppers/Microsoft.AndroidX.Compose/issues/387); no full media acceptance. |
| Jetchat rewritten fixture/local fonts, image-URI text acceptance and normalized profile history | Intentional deviations; see [Jetchat](Jetchat/README.md#remaining-differences-and-classification). |
| Stable Reply tab-return observation, outside taps, Jetchat cancel/timing/link/picker segments | Not yet investigated to a conclusive behavioral result. Retain the exact limits above with #383/#384/#387; do not turn discovery/driver limits into product defects or passes. |
| JetNews adaptive/theme/localization/search/link work | [#159](https://github.com/jonathanpeppers/Microsoft.AndroidX.Compose/issues/159); source-only reconciliation, not a full new port. |

### Data and rendering caveats

Jetchat has nine rewritten C# messages versus ten pinned Kotlin messages,
including video. Reply shares 12 inbox emails and 13 accounts, but Kotlin
shuffles most thread lists while C# uses a fixed seven-item sequence.
JetNews uses six original articles with upstream photos. Different body
content/order cannot be scored as a pure renderer mismatch.

System bars, IME, animation transitions, theme palettes and provider-font
resolution are not silently normalized. C# R09 transition-overlap images
are specifically excluded from claims about settled route/scroll parity.
No screenshot similarity percentage is used.

## Repetition, reconciliation and acceptance

The selected editor/search flows were repeated after the sprint deliveries:
Jetchat's native input/focus behavior in compact and short windows; Reply's
query/recovery/selected-detail behavior across all recorded window classes;
and both implementations' activity recreation on frozen candidates.
There is no matched pre-sprint image set, so no invented "before" result.

| #349 acceptance area | Evidence / disposition |
| --- | --- |
| Finite screens/interactions with initial states | J01-J13 and R01-R12 above and in the Jetchat checklist. |
| Matched captures and interaction differences, light/dark and sizes | Recorded states and 68 actual geometry checks; every behavioral limitation remains explicit, not a pass. |
| Classify differences and link actionable gaps | Categories/follow-ups above, including the issue's allowed **not yet investigated** category. |
| Correct sample READMEs and links | Jetchat/Reply integrations and JetNews PNG/markup/refresh/share implementations reconciled against code. |
| Reconcile partial trackers | #340 completed; #120 remains open for search/SnackbarHost/BottomSheetScaffold migrations. TimeInput and ModalBottomSheet already migrated. #159/#149 historical claims annotated. |
| Repeat selected comparisons and report remaining differences | Frozen-source repeat evidence above; no blanket exact parity, full accessibility, fold or process-death claim. |

This completes the bounded comparison record and documentation work for
#349, not the implementation of every feature follow-up. The **not
established** rows remain explicit verification work under the linked
follow-ups and the issue's allowed "not yet investigated" category.
Accepting this baseline must not turn those rows into passes: neither raw
capture counts nor source-symbol coverage establish functional parity.

## Attribution

The fixed [upstream source](https://github.com/android/compose-samples/tree/4c1fe7586e2fbf1c934925ef8ab64d3803361423)
and its [Apache 2.0 license](https://github.com/android/compose-samples/blob/4c1fe7586e2fbf1c934925ef8ab64d3803361423/LICENSE)
identify the Kotlin oracle and sample assets. See [sample attribution](README.md#attribution).
Screenshots are unmodified session evidence, not replacement artwork or a
claim of ownership over system keyboard/chrome.
