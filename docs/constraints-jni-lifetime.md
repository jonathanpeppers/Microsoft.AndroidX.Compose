# Constraints JNI class-reference lifetime

Investigation for [#357](https://github.com/jonathanpeppers/Microsoft.AndroidX.Compose/issues/357),
starting at `4241672b7d965b9725bc277b9667599188c73a9a`.

## Original observation and limits

The September 11 Pixel 7 abort belonged to `net.compose.devicetests`, PID
16414. ART reported deleted global `jclass 0x5866` in `CallStaticIntMethodA`
from `composenet.compose.MeasurePolicyLambda.n_invoke`, immediately after
explicit Java GC. The recovered native backtrace and process context agree.
The existing symbolication resolves all five Mono frames to interpreter
dispatch (`do_icall`, `L00`, `mono_interp_exec_method`, `interp_entry`,
`interp_entry_static_ret_5`), not to a particular managed accessor.

The original crashing APK was overwritten. Its recorded SHA256 is
`C45637BE733293F4F0399693B30063CC5D5679FC7EAA2583E740F74C4E73CED0`.
The later fixed-pixel identity fixture's passing TRX excluded the suspect
getters and is **not** evidence of a runtime fix.

Recovered original evidence, SHA256:

| Artifact | SHA256 |
| --- | --- |
| `identity-native-crash.txt` | `94FC19B9F071D2BA59119E86A4DE64185E6F33416D9F3C48222BAABAFD177D14` |
| `identity-crash-process-context.txt` | `872CAB80B2693CFAD5CBD08455CA9D2AE6CFA109E2658B2B5F36523C0130EB22` |
| `identity-symbolicated.txt` | `F7E538DE1D3421807F4425BEF4BEB21EE093711A8DFA3ECBE638BAAB6F45D0D8` |

These files do not trace allocation/deletion of `0x5866`. A newly reproduced
failure can establish the current cache defect without retrospectively
identifying the exact getter or reference owner in the original run.

## Current implementation and ownership

The audited base still had the historical implementation: six accessors
shared `s_constraintsClass`, assigned the raw
`Class.FromType(typeof(AndroidX.Compose.UI.Unit.Constraints)).Handle`.
Four `(J)I` methods and two `(J)Z` methods reused that class for static JNI
dispatch. `Constraints.ConstrainWidth/ConstrainHeight` call the facade's
min/max getters; they are managed `Math.Clamp` helpers, not independent
bound-getter calls.

Inspected with `ilspycmd`, including the actual runtime companion DLL:

| Binary | Version | SHA256 |
| --- | --- | --- |
| `Xamarin.AndroidX.Compose.UI.Unit.Android.dll` | 1.11.3.1, net10.0-android36.0 | `B3CB81DDC5B16D7A1F34E0198D62C49BD841CE3C242C054A88DB331B88594EA5` |
| `Mono.Android.dll` | Android runtime pack 36.1.69 | `703B750E48A5A5592567FAB9020CBEC7FC27D7F463733F7FA096F3060F194315` |

The binding contains `Constraints`, its companion factories, `Copy`, and
`CopyMaxDimensions`, but none of the six synthetic accessors. Inspecting the
small facade DLL alone would not establish that absence.

In this runtime, `Class.FromType` passes `JNIEnv.FindClass(type)` to
`Object.GetObject<Class>(..., TransferGlobalRef)`. The Class is a Java peer;
the peer registry contains weak references, not a permanent managed root.
`Object.SetHandle` constructs the peer through the runtime value manager
and processes ownership transfer. `JavaObject` finalization delegates to
that value manager; the Mono GC bridge also participates in peer-reference
retirement. Saving the numeric `.Handle` does not retain that owner.

By contrast, `JNIEnv.FindClass(string)` promotes its lookup result to a
global reference and releases the intermediate local. A bridge can retain
that global independently of any temporary Class peer. It must not apply
another `NewGlobalRef`/`DeleteLocalRef` layer to that result.

The fix declares all six accessors with `[ComposeBridge]`, using the existing
static primitive-return generator path. No new generator behavior, managed
packing algorithm, public API, or default mask is needed. Generated caches
own their class globals and preserve the same JVM names, `(J)I`/`(J)Z`
signatures and calls. Six host cases pin the primitive dispatch and prohibit
temporary Class peers or extra reference layers in the generated output.

The bounded audit found no other persistent Class-derived cache used for
repeated static dispatch. `MeasurableMeasure` has a borrowed class cache
used only for its initial method-ID lookup, then invokes the measurable
instance; its current `.Android` binding exposes `IMeasurable.Measure(long)`.
`BrushCompanion` uses a temporary class for its initial field lookup, then
retains the companion peer. These lookup-only paths are not evidence of the
observed repeated static-call failure and are not migrated in this change.
The ViewModel and saveable-array helpers also do not keep a Class-derived
raw handle across calls. This is not a general JNI audit or a claim that
all lookup-only lifetime handling is safe.

## Measurement regression

`ConstraintsMeasurementTests` launches a real Compose activity. Its outer
custom Layout passes a known packed envelope to a child custom Layout via
`Measurable.Measure`. That child's native `MeasurePolicyLambda` callback:

- Checks the received envelope and all six getters against independent
  expected bounds, primes the production caches, performs three managed
  GC/finalizer and Java GC/finalizer cycles, and repeats the reads.
- Checks both clamp helpers below, within, and above the bounds, then
  uses the results to measure and place a real child.
- Logs the run GUID, PID, first post-GC accessor, cached class value, and
  (on the unfixed implementation only) a weak observation of the original
  Class peer. The observation does not keep that peer alive across GC.

Each case requires 24 completed composition and measurement/placement phases,
cycling bounded, fixed, unbounded-width, and unbounded-height envelopes.
The outer Layout's phase-dependent padding changes its native constraints
to request remeasurement; updating a remembered policy's managed delegate
alone does not invalidate unchanged native geometry.
Six data rows rotate the first post-GC accessor. A separate control runs the
same layout, composition, GC, child-measurement and placement path without
any Constraints getter/clamp call. Its expected dimensions use the independent
bounds, rather than avoiding native measurement.

The conditional identity fixture also uses real max getters and clamps again,
with GC before structural updates, while retaining its node-order and
state-identity assertions.

## Reproduction protocol

Build both source variants from the recorded base with identical fixture
sources. The unfixed variant changes only the fixtures; the candidate also
replaces the six bridge implementations. Freeze the APKs before running.

```powershell
dotnet build src\Microsoft.AndroidX.Compose.DeviceTests -c Debug `
  -p:RuntimeIdentifier=android-arm64 -p:ApplicationId=net.compose.constraints357 `
  -p:EmbedAssembliesIntoApk=true -p:AndroidUseAssemblyStore=false
```

This produces a targetSdk 36 Debug APK with managed PE payloads embedded in
ELF `payload` sections under `lib/arm64-v8a`. Verify the installed base APK's
SHA256 and these embedded payloads, not merely a nearby build output.
Start each instrumentation run in a fresh process, record its PID and fixture
GUID, confirm CheckJNI in that process, and retain source-specific results
and owned crash evidence. Do not disable CheckJNI or GC.

After obtaining an exclusive device lease, run these filters with:

```powershell
adb -s <serial> shell am instrument -w -e filter <filter> `
  net.compose.constraints357/net.compose.devicetests.TestInstrumentation
```

| Variant | Filter | Purpose |
| --- | --- | --- |
| Unfixed | `FullyQualifiedName~ConstraintsMeasurementTests.MeasurementControl` | No-getter measurement/GC control |
| Unfixed | `FullyQualifiedName~ConstraintsMeasurementTests.MeasurementAccessors` | Reproduce, retain failure and identify first post-GC call |
| Candidate | `FullyQualifiedName~ConstraintsMeasurementTests` | Seven cases, repeated fresh-process runs |
| Candidate | `FullyQualifiedName~CompositionIdentityTests.ConditionalCalls` | Five identity/node-order cases with real getters |
| Candidate | `FullyQualifiedName~CompositionIdentityTests.SaveableState` | Two recreation/save-state cases |

Only the newly installed isolated package is eligible for cleanup. Preserve
the frozen APKs, hashes, TRXs and PID-scoped logs; do not reinterpret a
nonreproduction as confirmation of the historical failure's cause.

## Validation status (September 16)

**Accepted:** the current unfixed implementation reproduces the deleted-class
CheckJNI abort; the generated-bridge candidate passes 21 device test
executions (14 distinct cases) with CheckJNI confirmed in every process.
The exact historical getter and numeric peer allocation/deletion trace
remain unproven. The following separates the initial fixture failure,
unfixed reproduction and candidate results.

Offline validation: 457 generator tests passed (451 existing plus six
primitive-accessor cases); facade and Gallery builds passed without warnings.
The self-contained unfixed and candidate device APKs built successfully with
the two existing XA0101 testhost Content warnings. Extracted Mono.Android
and UI.Unit.Android payload hashes match the audited binaries above. The
decompiled measurement test and activity are identical between the two APKs.

| Frozen artifact | SHA256 |
| --- | --- |
| `constraints357-unfixed.apk` | `DD106F095D742A55CA76A5658D7923A9A18DDBC4FFA1E191A1C60EF02F5FB128` |
| `constraints357-candidate.apk` | `3649EFBD668A82E2387039609C250009724B14D46C5CF3947463100CA5C9A55E` |
| Unfixed embedded facade PE | `0BE0673156FB0D72780EB14BF5DE19F942986D04F5A233D2E760DAC873F6DD06` |
| Candidate embedded facade PE | `D1A510767702C6959EE41FEFE236CCB556D0793FD38189EB15CA2AB214FFF0FA` |

The first exclusive lease's preflight found
the Pixel 7 keyguard showing and input restricted. The lease was released
at 08:40:23 -05:00 before any installation or instrumentation, with the
isolated package and PID absent. No settings or UI bypass was attempted.

After owner unlock, a fresh lease installed the unfixed APK and verified the
pulled installed base and embedded PE hashes. Its no-getter control (PID 22069,
14:05:28-14:05:59 UTC) failed at phase 1 because composition did not cause
another measure/placement. This is a fixture failure, not the reported
CheckJNI bug. The fresh `unfixed-control-0905.trx` has SHA256
`E4B9CCB1CD6E79042BEA14E65ADC7D00D17B38FC953BB5658625D6D403353580`.
The post-run PID-filtered log was empty; the first collector then failed on
null input. CheckJNI was not observed, which does not establish that it was
disabled. No accessor or candidate run followed. The newly installed isolated
package was removed and its PID absence verified before release at
09:06:27 -05:00.

The revised fixture adds the geometry invalidation above and early TRX
run-ID/PID diagnostics. The revised evidence collector starts live immediately
after instrumentation publishes its PID, specifies `*:V`, preserves log
stdout/stderr/exit and fails explicitly on absent evidence. Both variants
were rebuilt with the same corrected fixture and frozen separately:

| Revised artifact | SHA256 |
| --- | --- |
| `constraints357-unfixed-r2.apk` | `E4E128D9F7FA68E677910E68785D2136891A737C8D5846D837AC75B070ED784D` |
| `constraints357-candidate-r2.apk` | `9FD091E8258C2FE4897C667D7ABABD20B90F35EF3F7886BB5621D8E8BEF123C9` |
| Unfixed r2 embedded facade PE | `ED9EF86FE7CEDCBFE7AC33E4C88BEC4D7E80582DD58FD62F34CC3E80A2AFF032` |
| Candidate r2 embedded facade PE | `8362C967D033D63D272AB7F35D35FC19E8953C99C0FC7DC53952D77C926E7E8C` |

The revised unfixed control passed in PID 29993: its TRX records 24
compositions, 24 native measurements and `placedPhase=23`. The same process's
startup log records `Late-enabling -Xcheck:jni`.

The fresh accessor process, PID 30128, then crashed. Its live and post-run
app-PID logs were empty, so instrumentation's `Process crashed` message was
initially classified as unknown and no candidate test ran. After cleanup and
lease release, a separate evidence-only lease recovered the 49-line owned
crash report by **crashed PID, package and invocation time**, not logger PID.
The logger was PID 30193; the report names crashed PID 30128,
`net.compose.constraints357`, and the verified unfixed r2 installation path.

At 11:04:11.941 -05:00 it reports deleted global `jclass 0x52c6` (index 662,
table size 661) in `CallStaticIntMethodA`, from
`composenet.compose.MeasurePolicyLambda.n_invoke`. Its native stack explicitly
contains `CheckJNI::CallStaticIntMethodA`; the Mono BuildId and five offsets
match the September 11 report. This establishes a new reproduction of the
reported failure on the current unfixed implementation with the functional
no-getter control passing. It does not supply the missing app-PID log's
precise getter/weak-peer trace or identify the historical `0x5866` owner.

| r2 native evidence | SHA256 |
| --- | --- |
| `unfixed-r2-control-1102.trx` | `E342125C67745FDC3BD1E73E82FB5E590CE69A5C11D5E75E111D3968CA110527` |
| `unfixed-r2-control-1102-pid-29993-logcat.txt` | `6A31D4836766C040ADE5CB855B4EF7438A31E76A359A13687063A784FFDCFB2A` |
| `unfixed-r2-accessors-30128-owned-crash.txt` | `C7E3A1C8CC3CB4C33F0D2BC9505581FCC3DD88C315211257C7CF750681646B80` |

The recovered report contains only the time- and package-matched owned crash
block. No unrelated raw logs were retained.

### Candidate acceptance

The frozen candidate r2 was installed under a new exclusive lease. Its
pulled installed base and embedded facade, test, Mono.Android and
UI.Unit.Android PE hashes matched the frozen/audited copies.

| Run | Fresh PID | Passed | TRX SHA256 |
| --- | --- | --- | --- |
| Measurement/GC A | 30712 | 7 | `217AD0A00EF2F6A2DEA0182A4A0A2EEC5313AEDF0623FADEEE25B473BAAF0094` |
| Measurement/GC B | 30875 | 7 | `405B5EDCF27D725133AA76180E4CFE595FCF4E6A64420C53FCACA1BBE50FAEA2` |
| Conditional identity/node order | 31043 | 5 | `2BBE36DEF7F73BD075574AD2B4835E3370FD9F44F21465C986352DD879DDAB65` |
| Saveable identity restoration controls | 31138 | 2 | `9F62BED1D4A26295CF56E4C137859AF2D8AE47CAC8F70475B1ED8D691C09DB2A` |

Every measurement case records exactly 24 compositions, 24 native
measurements and final placed phase 23 in its TRX. The two processes total
336 native phases: 288 getter-bearing phases plus 48 no-getter controls.
Each process log contains 864 post-GC accessor calls (1,728 total), with a
stable nonzero generated class handle for each of the six accessors
throughout that process. All clamp and native child-size assertions remain
enabled. The five conditional identity cases exercise the restored getter
and clamp measurement path; the two saveable cases retain their existing
Column-based restoration-control role.

The first three candidate processes directly logged
`Late-enabling -Xcheck:jni`. The fast saveable run's original PID-filtered
collector was empty despite its fresh passing TRX, so acceptance stopped
pending evidence rather than inferring CheckJNI from another process.
An approved bounded main-buffer read then retained only exact log-entry
PID 31138 within 11:17:18-11:17:30. It records both
`Process 31138 created for net.compose.constraints357` and
`Late-enabling -Xcheck:jni` at 11:17:20.926, plus the verified candidate
installation path. The retained startup file SHA256 is
`E9930CE0E098138A318C041CA4DDF7BE5249119C5489666E2C0D004EFA8C9E8B`.
No test was rerun or assertion relaxed to fill that evidence gap.

Thus 21 executions passed, representing 14 distinct cases, with actual
per-process CheckJNI evidence and no candidate invalid-class abort. The
newly installed isolated package was force-stopped and uninstalled; its
PID/package absence and termination of all owned collectors were verified
before releasing the device at 11:19:47.855 -05:00.
