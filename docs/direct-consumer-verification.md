# Standalone C# package consumer verification

**Status (2026-09-27 UTC):** The isolated `net11.0-android` NuGet consumer passes
Debug and Release/R8 on an API-36 emulator, including accessible controls,
text input, navigation, and Activity recreation. The final R8 DEX contains
252/252 unique `[ComposeBridge]` class/name/signature triples and both
synthetic Material 3 Text compatibility overloads. The local `.9` package
awaits Kaylee's review before pinning it in SentenceStudio; no SentenceStudio
code has been changed.

`samples/DirectConsumer` references only the `Microsoft.AndroidX.Compose`
NuGet. It has no MAUI, Comet, facade project reference, or app-specific asset.
The image is an Android system drawable. Its `TextFieldValue` state retains
text and selection; a small boolean, not the 60-item dataset, saves list order.

## Source and upstream status

- Source: `jonathanpeppers/Microsoft.AndroidX.Compose`, upstream `main` at
  `8be2f82fa16abb2bb1180b04a38e4de943b749e9` (fetched 2026-09-27 UTC).
  The local `sentencestudio-foundation` branch was created from that commit.
- The upstream `Build` workflow succeeded for that SHA on 2026-09-22 (run
  `35745616243`, run number `1098`); its `Publish packages to GitHub Packages`
  step also succeeded. The workflow calculates `0.1.0-beta.1098` from that
  run number, but registry version listing returned HTTP 403 because the
  current GitHub credential lacks `read:packages`, so availability there
  was not independently confirmed. `gh release list --repo
  jonathanpeppers/Microsoft.AndroidX.Compose` returned no releases. The
  NuGet.org flat-container indexes for `microsoft.androidx.compose` and
  `microsoft.androidx.compose.sourcegenerators` both returned HTTP 404.
  This validation instead uses the upstream `dotnet pack` path and a local feed.
- The Comet vendor note names `29017064e3f3775d808d0b301ca994ac9eea7e9e`
  as its starting revision. Read-only inspection of
  `maui-labs` branch `davidortinau/baristanotes-rewrite` at
  `0bce1594b4d113160274e6e9c4dd27d27405519c` found:

  | Vendor change | Upstream at `8be2f82` | Direct-consumer decision |
  | --- | --- | --- |
  | AndroidX Compose 1.11.3.1 pins, fractional `Sp`, stable lazy item keys | Present in `Directory.Build.targets`, `Sp.cs`, and `LazyColumn.cs` | Do not duplicate. |
  | ART profile targets and profile | At the upstream base, the targets SHA-256 was `25b9d24e7cdc555e89f67b71c0e6e94f9a606f26d499afb3637f62fb76666cc3` and the profile SHA-256 was `39e93121f7a1cf6613505dfad28b2c05c78bcb62a11790f9db81ea20472dbcf4`, matching the vendor note | Keep the upstream profile. This branch modifies the targets to import generated package-transitive R8 rules, so the current targets are no longer byte-identical to that base. |
  | Vendored facade targeting `net11.0-android` | Upstream library targets `net10.0-android`; its `net10.0-android36.0` package restores and runs in this `net11.0-android` consumer | Do not retarget the library solely for consumption. |
  | Annotated-string JNI type and NativeAOT style-peer annotations | Upstream has its own `AnnotatedString` registration and trim-annotated `SpanStyle`/`TextStyle` casts | This CoreCLR plain-text slice does not prove NativeAOT equivalence; defer a source-level AOT comparison until needed. |
  | Center-snap fling, `LastVisibleItemIndex`, `RetainItem`/`LazyItemLease` | Not present | Not needed by this first direct-consumer proof; do not copy speculatively. |
  | LocalCafe icon, Comet MVU/backend and `Comet.targets` | Not standalone facade features | Do not import. |

The upstream source generator is unchanged. Generic additions are
`SemanticsScope` selectable-group, toggle-state, and progress-action helpers
with a JNI callback; `GC.KeepAlive` prevents collection of the semantics
receiver mid-callback. The pinned Foundation AAR has a nondefault Brush
`border-ziNgDLE(Modifier,float,Brush,Shape)` overload, not the previously
declared `$default` overload; the corrected bridge supplies a rectangle when
shape is omitted. The sample exercises that Brush border. It saves primitive
state and `TextFieldValue` through upstream `RememberSaveable`. Separate
Roslyn tooling generates exact R8 retention rules from **all**
`[ComposeBridge]` declarations; the package imports them via `buildTransitive`.
CI builds an external NuGet consumer and compares its final R8 DEX with the
rules extracted from the packed NuGet. No Comet code or generator replacement
was copied.

## SDK diagnostics, package, and provenance

Run from the clone or one of its subdirectories. Set `repo` once; no
workstation-specific paths are required. Run all four diagnostics before
invoking a .NET build:

```sh
repo="$(git rev-parse --show-toplevel)"
cd "$repo"
find . -maxdepth 4 -name global.json
dotnet --list-sdks
dotnet --info | head -20
dotnet workload list

base=8be2f82fa16abb2bb1180b04a38e4de943b749e9
version=0.1.0-foundation.8be2f82.9
feed="$repo/artifacts/local-feed"
package="$feed/Microsoft.AndroidX.Compose.$version.nupkg"
test "$(git rev-parse HEAD)" = "$base"
dotnet run scripts/sync-compose-bridge-r8.cs -- --check
if test ! -e "$package"; then
  dotnet pack src/Microsoft.AndroidX.Compose/Microsoft.AndroidX.Compose.csproj \
    -c Release -o "$feed" -p:PackageVersion="$version" \
    -p:RepositoryUrl="$(git remote get-url origin)" \
    -p:RepositoryCommit="$base" -p:RepositoryType=git -v:minimal
fi
test -s "$package"
shasum -a 256 "$package"
expected=8d309c5da8fb19640edf4a1ca036c5ec86ae98a769a0446bfc01093562bb38ea
actual="$(shasum -a 256 "$package" | awk '{print $1}')"
test "$actual" = "$expected" || { echo "Local package hash differs; do not substitute it for this pin" >&2; exit 1; }
```

`global.json` specifies only `Microsoft.Build.NoTargets` and does **not**
pin the .NET SDK. This machine selected SDK `11.0.100-rc.1.26425.128` and
workload version `11.0.100-rc.1.26458.5`. Installed workloads listed were
`macos` and `maui`; none were installed or changed.

The versioned, gitignored local package is
`artifacts/local-feed/Microsoft.AndroidX.Compose.0.1.0-foundation.8be2f82.9.nupkg`,
SHA-256
`8d309c5da8fb19640edf4a1ca036c5ec86ae98a769a0446bfc01093562bb38ea`.
It contains `lib/net10.0-android36.0`, the source-generator analyzer, and
packed `buildTransitive` props, targets, ART profile, and both hand-written
and generated R8 rules. The generated bridge-rules file has SHA-256
`ffea4198b68c519dadcbd11500ca8a45d274058d438b0df7a9834bd86dfbbbd7`.
**The `.nuspec` repository commit is the upstream base, not the complete
source of this uncommitted local package.** The tracked facade patch had
SHA-256 `6e9d34d33ff84e451f7a1db0ef0fca85448da12294f220ac7f14ca05f07cab20`
under `git diff --binary -- src/Microsoft.AndroidX.Compose | shasum -a 256`;
the untracked `ProgressSemanticsCallback.cs` had SHA-256
`9c585b2c230ff4b5ce071f4fe0793497bee058a68cb469429a440430982e1254`.
Keep the patch and package hash together; do not present the base SHA as
provenance for byte-for-byte identical upstream binaries. The commands refuse
to overwrite an existing package; a fresh archive with a different checksum
must be evaluated as a different artifact even if its version string matches.

## Isolated NuGet consumer and build checks

The following mirrors `.github/workflows/build.yml`: the consumer directory
is outside the clone, so it cannot import repository `Directory.Build.targets`
or its direct source-tree profile import.

```sh
dotnet test src/Microsoft.AndroidX.Compose.SourceGenerators.Tests \
  -c Release -v:minimal
dotnet build tests/Microsoft.AndroidX.Compose.PackageSmoke \
  -c Release -p:PackageSmokeVersion="$version" \
  -p:PackageSmokeSource="$feed" -v:minimal
dotnet run scripts/check-art-baseline-profile.cs -- "$package"

smoke="$(mktemp -d "${TMPDIR:-/tmp/}compose-package-smoke.XXXXXX")"
mkdir -p "$smoke/app" "$smoke/feed"
cp "$package" "$smoke/feed/"
cp "$repo/samples/DirectConsumer/DirectConsumer.csproj" \
  "$repo/samples/DirectConsumer/"*.cs \
  "$repo/tests/Microsoft.AndroidX.Compose.PackageSmoke/PackageConsumer.cs" \
  "$smoke/app/"
dotnet build "$smoke/app/DirectConsumer.csproj" -c Release -f net11.0-android \
  -p:ComposePackageVersion="$version" \
  -p:RestoreAdditionalProjectSources="$smoke/feed" \
  -p:AndroidPackageFormat=apk -p:AndroidLinkTool=r8 \
  -p:AndroidBuildApplicationPackage=true -p:TreatWarningsAsErrors=true \
  -p:EmitCompilerGeneratedFiles=true \
  -p:CompilerGeneratedFilesOutputPath="$smoke/app/obj/generated"
dotnet msbuild "$smoke/app/DirectConsumer.csproj" \
  -p:TargetFramework=net11.0-android -p:ComposePackageVersion="$version" \
  -preprocess:"$smoke/app/obj/consumer.pp.xml"
! grep -Fq "$repo/Directory.Build.targets" "$smoke/app/obj/consumer.pp.xml"
! grep -Fq "$repo/Directory.Build.props" "$smoke/app/obj/consumer.pp.xml"
nuget_root="$(dotnet msbuild "$smoke/app/DirectConsumer.csproj" \
  -p:TargetFramework=net11.0-android \
  -p:ComposePackageVersion="$version" -getProperty:NuGetPackageRoot)"
cmp "$package" "${nuget_root%/}/microsoft.androidx.compose/$version/microsoft.androidx.compose.$version.nupkg"
for ext in props targets; do
  grep -Fq "${nuget_root%/}/microsoft.androidx.compose/$version/buildTransitive/Microsoft.AndroidX.Compose.$ext" \
    "$smoke/app/obj/consumer.pp.xml"
done
generated="$(find "$smoke/app/obj/generated" -name '*Interceptors.g.cs' -print | sed -n '1p')"
test -n "$generated"
grep -Fq 'PackageConsumer.Greeting(' "$generated"
grep -Fq 'Composables.Text(' "$generated"
dotnet run scripts/check-art-baseline-profile.cs -- \
  "$smoke/app/bin/Release/net11.0-android/net.compose.samples.directconsumer-Signed.apk"
unzip -p "$package" buildTransitive/Microsoft.AndroidX.Compose.bridges.pro > \
  "$smoke/app/packed-bridges.pro"
test -s "$smoke/app/packed-bridges.pro"
cmp "$smoke/app/packed-bridges.pro" \
  src/Microsoft.AndroidX.Compose/buildTransitive/Microsoft.AndroidX.Compose.bridges.pro
sdk_root="${ANDROID_SDK_ROOT:-${ANDROID_HOME:-}}"
: "${sdk_root:?Set ANDROID_SDK_ROOT or ANDROID_HOME to the installed SDK}"
ANDROID_SDK_ROOT="$sdk_root" python3 scripts/check-compose-bridge-dex.py \
  "$smoke/app/bin/Release/net11.0-android/net.compose.samples.directconsumer-Signed.apk" \
  --bridge-rules "$smoke/app/packed-bridges.pro"
```

Final results: **557/557** generator tests passed; in-repo package smoke
and external Release/R8 consumer compiled with zero warnings/errors. The
isolated project restored the just-packed NuGet; its preprocessed MSBuild
output imported the package's `.props` and `.targets`, did not import either
checkout `Directory.Build` file, and generated the `Greeting`/`Text`
interceptors. Both the `.nupkg` and Release APK passed the ART-profile check.
The packed `.bridges.pro` matched the generated source, and the final APK
retained **252/252** unique bridge signatures plus both `Text--4IGK_g`
overloads. The installed Release APK also passed the runtime test below.

## API-36 Debug runtime and Activity recreation

Use a fresh application ID for each run. Do not uninstall, clear data, or
reuse build outputs while changing `ApplicationId`: an incremental build in
the same project `obj` directory produced an APK whose **filename** had the
new ID but whose embedded manifest retained an earlier ID. Verify the
manifest *before* installation. A direct `adb install` also needs
`AndroidEnableFastDeployment=false` for Debug, or the thin APK omits
`System.Private.CoreLib.dll`; see the
[.NET for Android Fast Deployment documentation](https://learn.microsoft.com/dotnet/android/building-apps/build-process#fast-deployment).

```sh
serial="${ANDROID_SERIAL:?Set ANDROID_SERIAL to an API-36 emulator from adb devices -l}"
test "$(adb -s "$serial" shell getprop ro.build.version.sdk | tr -d '\r')" = 36
sdk_root="${ANDROID_SDK_ROOT:-${ANDROID_HOME:-}}"
: "${sdk_root:?Set ANDROID_SDK_ROOT or ANDROID_HOME to the installed SDK}"
aapt="$(find "$sdk_root/build-tools" -type f -name aapt -print | sed -n '1p')"
test -x "$aapt"
app="net.compose.samples.directconsumer.proof$(date -u +%Y%m%d%H%M%S)"
consumer="$(mktemp -d "${TMPDIR:-/tmp/}compose-runtime.XXXXXX")"
cp "$repo/samples/DirectConsumer/DirectConsumer.csproj" \
  "$repo/samples/DirectConsumer/"*.cs "$consumer/"
dotnet build "$consumer/DirectConsumer.csproj" -c Debug -f net11.0-android \
  -p:ComposePackageVersion="$version" \
  -p:RestoreAdditionalProjectSources="$feed" \
  -p:AndroidEnableFastDeployment=false -p:AndroidBuildApplicationPackage=true \
  -p:ApplicationId="$app"
apk="$consumer/bin/Debug/net11.0-android/$app-Signed.apk"
test -s "$apk"
manifest_id="$("$aapt" dump badging "$apk" | awk -F "'" '/^package: name=/{print $2; exit}')"
test "$manifest_id" = "$app" || { echo "Unexpected APK package: $manifest_id" >&2; exit 1; }
test -z "$(adb -s "$serial" shell pm path "$app")" ||
  { echo "Refusing to overwrite installed app $app" >&2; exit 1; }
adb -s "$serial" install "$apk"
activity="$(adb -s "$serial" shell cmd package resolve-activity --brief "$app" |
  tail -n 1 | tr -d '\r')"
case "$activity" in "$app/"*) echo "Resolved $activity";; *) echo "Unexpected activity" >&2; exit 1;; esac
python3 "$repo/scripts/verify-direct-consumer-android.py" \
  --serial "$serial" --package "$app" \
  --evidence-root "$repo/artifacts/evidence"
```

The verifier discovers the activity, asserts every named actionable
accessibility node (`toggle`, `level-slider`, `option-a`, `option-b`) and its
checked/selected state, exercises text/IME, a button, slider, dialog,
navigation and keyed list reorder, then rotates the emulator. It checks that
the **same process** logs a new `MainActivity.OnCreate` instance with
`restored=True`, that input `ABC`, caret `2..2`, count `1`, switch `On`,
slider `0.84`, radio `B`, dialog confirmation and first list item `60`
survive, and restores the original orientation. It rejects physical devices,
disabled auto-rotation, and nonfresh app state rather than altering settings
or deleting data.

Debug evidence (an earlier revision with the same saveable-state behavior,
before the Brush-border exercise was added) is in the gitignored
`artifacts/evidence/direct-consumer-20260927T042526Z-6beae2ff/` directory:
`result.json`, lifecycle log, accessibility XML, and before/after screenshots.
Its run was **2026-09-27T04:25:26Z–04:26:37Z**, and reported
`instance=1 restored=False` then `instance=2 restored=True` under PID `9073`.
No MAUI/Comet packages were in the restored consumer.

## Release/R8 root cause and verified runtime

Earlier Release APKs failed because R8 cannot see Java methods reached only
through managed `[ComposeBridge]` JNI lookups. Debug DEX contained both
synthetic `androidx.compose.material3.TextKt.Text--4IGK_g` compatibility
overloads (String and AnnotatedString), while the `.6` R8 DEX retained only
the newer `Text-Nvy7gAk`. The managed String bridge descriptor agreed with
the pinned Material 3 AAR. This established a missing R8 reachability edge,
**not** a bad Text JNI signature. The initial three failures were:

| Package revision | API-36 Release result |
| --- | --- |
| `.4` | `ClassNotFoundException: androidx.navigation.compose.NavHostControllerKt` from `RememberNavController`. |
| `.5` | After narrow navigation keep rules, `ClassNotFoundException: androidx.compose.foundation.layout.WindowInsetsPadding_androidKt` from `SafeDrawingPadding`. |
| `.6` | After an exact inset keep rule, `NoSuchMethodError: androidx.compose.material3.TextKt.Text--4IGK_g(...)` from `ComposeBridges.TextExplicitDefaults`. |

The narrow Text rule retained the String bridge method and its additional
AnnotatedString compatibility overload, but the next Release run exposed
another JNI-only missing method, `TestTagKt.testTag`. Instead of adding an
ad-hoc rule, `scripts/sync-compose-bridge-r8.cs` now derives precise
class/member rules and contract triples from all `[ComposeBridge]`
declarations. `scripts/check-compose-bridge-dex.py` checks those triples
against **final APK DEX** and separately asserts both compatibility Text
overloads. A stale Brush-border `$default` declaration failed this gate in
`.8` (**251/252** triples): inspection of the pinned Foundation AAR showed
only the nondefault Shape overload. After correcting the bridge, `.9`
retained **252/252**, plus both Text overloads.

To reproduce the **Release runtime** without reusing an app ID or installing
over an existing app, run the SDK checks and package commands above, set
`ANDROID_SERIAL` to an API-36 emulator, and use a fresh scratch project:

```sh
serial="${ANDROID_SERIAL:?Set ANDROID_SERIAL to an API-36 emulator from adb devices -l}"
test "$(adb -s "$serial" shell getprop ro.build.version.sdk | tr -d '\r')" = 36
sdk_root="${ANDROID_SDK_ROOT:-${ANDROID_HOME:-}}"
: "${sdk_root:?Set ANDROID_SDK_ROOT or ANDROID_HOME to the installed SDK}"
aapt="$(find "$sdk_root/build-tools" -type f -name aapt -print | sed -n '1p')"
test -x "$aapt"
app="net.compose.samples.directconsumer.releaseproof$(date -u +%Y%m%d%H%M%S)"
release="$(mktemp -d "${TMPDIR:-/tmp/}compose-release.XXXXXX")"
cp "$repo/samples/DirectConsumer/DirectConsumer.csproj" \
  "$repo/samples/DirectConsumer/"*.cs "$release/"
dotnet build "$release/DirectConsumer.csproj" -c Release -f net11.0-android \
  -p:ComposePackageVersion="$version" \
  -p:RestoreAdditionalProjectSources="$feed" \
  -p:AndroidPackageFormat=apk -p:AndroidLinkTool=r8 \
  -p:AndroidBuildApplicationPackage=true -p:TreatWarningsAsErrors=true \
  -p:ApplicationId="$app"
apk="$release/bin/Release/net11.0-android/$app-Signed.apk"
test -s "$apk"
manifest_id="$("$aapt" dump badging "$apk" | awk -F "'" '/^package: name=/{print $2; exit}')"
test "$manifest_id" = "$app" ||
  { echo "Unexpected APK package: $manifest_id" >&2; exit 1; }
unzip -p "$package" buildTransitive/Microsoft.AndroidX.Compose.bridges.pro > \
  "$release/packed-bridges.pro"
cmp "$release/packed-bridges.pro" \
  "$repo/src/Microsoft.AndroidX.Compose/buildTransitive/Microsoft.AndroidX.Compose.bridges.pro"
ANDROID_SDK_ROOT="$sdk_root" python3 "$repo/scripts/check-compose-bridge-dex.py" \
  "$apk" --bridge-rules "$release/packed-bridges.pro"
test -z "$(adb -s "$serial" shell pm path "$app")" ||
  { echo "Refusing to overwrite installed app $app" >&2; exit 1; }
adb -s "$serial" install "$apk"
python3 "$repo/scripts/verify-direct-consumer-android.py" \
  --serial "$serial" --package "$app" \
  --evidence-root "$repo/artifacts/evidence"
```

The `.9` isolated Release APK was installed on an API-36 emulator with
16-KB pages, then passed **all** verifier interactions at
**2026-09-27T04:53:44Z–04:54:55Z**. The same process (PID `1642`) logged
`MainActivity.OnCreate` instance `1` (`restored=False`) followed by instance
`2` (`restored=True`); `ABC` and caret `2..2`, count `1`, switch `On`,
slider `0.84`, radio `B`, dialog confirmation and reversed first item `60`
survived. UIAutomator dumps contain a named, checked switch (`toggle`),
named SeekBar (`level-slider`), and grouped, named RadioButtons (`option-a`
and `option-b`) with the correct selected option. Screenshots, XML, lifecycle
log, and `result.json` are in the gitignored
`artifacts/evidence/direct-consumer-20260927T045344Z-b83124f8/` directory.
The preserved `artifacts/evidence/release-r8-v9-verified.apk` has SHA-256
`4819af30c86d5987039c44ee846a0524e2ea164e3f0650992e7886ca651adfc2`.
The packed bridge rules have SHA-256
`ffea4198b68c519dadcbd11500ca8a45d274058d438b0df7a9834bd86dfbbbd7`.
The post-run `adb logcat -d --pid=1642` query returned no tagged
AndroidRuntime, MonoDroid, or .NET errors. The historical `.6` signed APK
and crash trace
remain in gitignored
`artifacts/evidence/release-r8-v6-runtime-blocked.apk` (SHA-256
`8e0f01ec64a7cc4e0ef086f32b794bc0bf76d8660f8de9eb624b7c1b91f0af68`)
and `artifacts/evidence/release-r8-v6-runtime-blocked.log`.

## Pending dependency gate

The [official Ailoha Android instructions](https://ailoha.dev/docs/android.html)
require both
`com.ailoha:ailoha-android-agent` and
`com.ailoha:ailoha-android-agent-compose` in debug builds. A CLI binary is
installed, but the pinned Comet branch and the checked local Maven/Gradle/
NuGet/Ailoha caches did not provide both versioned AARs with verifiable official
origin and checksums. Ailoha is **not integrated**. The blocked step is to
obtain and verify the official core **and** Compose artifacts for one version;
do not substitute an unverified build or vendor Comet's agent.

**Kaylee, before SentenceStudio slice work:** review the `.9` package hash,
generated R8 keep-rule contract, corrected Brush-border ABI, isolated
Release/R8 consumer, DEX gate, and saved API-36 E2E evidence above; reproduce
the Release run with an external package consumer and sign off on the
MAUI-free package pin. Include a real-keyboard check of `TextFieldValue`
input at sign-off; investigate the separate string-overload character loss
if that path becomes necessary. Only after that review should the first
SentenceStudio native Android slice depend on the pinned local package.
Ailoha remains independently blocked pending official provenance for both
artifacts. No commits, pushes, forks, or PRs were created for this snapshot.
