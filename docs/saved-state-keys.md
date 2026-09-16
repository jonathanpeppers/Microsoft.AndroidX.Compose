# Composition keys and saved task state

Compose's default `rememberSaveable` registry key depends on its ancestor
composition groups, not just the immediate `RememberSaveable` call site.
Every C#-supplied ancestor group/lambda key must therefore be reproducible in
a new process running the same APK.

## Runtime key contract

`SourceLocationKey` uses FNV-1a 32-bit over the UTF-16 code units of the caller
file path, followed by one FNV step mixing the full 32-bit line number.
Navigation destination lambdas use this same helper as other composable
lambda factories.

`CompositionGroupKey` identifies a tree child by its sibling index and runtime
type. Its type component is `SourceLocationKey.Compute(0, type.AssemblyQualifiedName)`;
the final key is one more FNV step, XORing the full 32-bit sibling index before
multiplication. Arithmetic wraps modulo 2^32. The type component is cached per
`Type` so rendering does not repeatedly format or hash type names.

The assembly-qualified identity includes the namespace, nested type name,
assembly name/version/culture/public-key token, and recursively qualified
constructed generic arguments (also distinguishing array shapes). This avoids
conflating same-named types in different assemblies, or different closed
generic nodes. Open/unnamed types without an assembly-qualified identity are
rejected, not silently assigned a shared key. No `Type.GetHashCode()` value is
passed to Compose; ordinary dictionary hashing inside the cache is harmless.
These are 32-bit hashes, not collision-free identifiers.

Both container rendering paths and segmented-button label children use this
positional/type key. Both direct content overloads use index zero and
`ComposableContentNode`, matching a tree container with one content adapter.
Same-type siblings retain positional identity across recompositions; swapping
to a different type changes the group key instead of reusing incompatible
remembered slots.

The runtime audit for #353 found six randomized key producers, all in those
paths. Other runtime group producers use `SourceLocationKey`, constant root
lambda keys, or deterministic navigation child indices. Generated restart
keys use deterministic FNV. Ordinary `GetHashCode` implementations, modifier
equality hashes, item keys, navigation freshness, and compiler control-flow
grouping are not changed by this fix.

## Compatibility

**Saved state written under the previous randomized key scheme may be
discarded after upgrading.** There is no reliable migration from a key salted
with an old process's random seed. The new scheme promises determinism for the
same built APK, not saved-state compatibility across arbitrary code changes.
Changing source paths/line numbers, type or assembly identities, generic
arguments, child positions, or this hashing scheme can change saved keys.
Persist durable user data independently of Android's transient saved-task state.

## Regression coverage

Host tests compile the six actual runtime key expressions and their helper
sources into one executable, run that same executable in fresh OS processes,
and compare the outputs. They also check positional distinctions, type swaps,
same-named types across assemblies and versions, constructed generics, array
shapes, direct/tree content parity, and navigation call-site parity:

```powershell
dotnet test src\Microsoft.AndroidX.Compose.SourceGenerators.Tests --filter FullyQualifiedName~CompositionKeyTests
```

This proves key determinism, **not Android saved-state restoration**.
For the latter, the device-test APK includes `SaveableProcessTestActivity` and
a host-driven acceptance script. Acquire exclusive use of the selected device
and the `net.compose.devicetests` app before installing or running it.

```powershell
dotnet build src\Microsoft.AndroidX.Compose.DeviceTests -p:EmbedAssembliesIntoApk=true
adb -s DEVICE_SERIAL install -r src\Microsoft.AndroidX.Compose.DeviceTests\bin\Debug\net11.0-android\net.compose.devicetests-Signed.apk
.\scripts\test-saveable-process.ps1 -Serial DEVICE_SERIAL -Adb adb
```

The script changes ten independent saveable counters beneath nested
containers, tree content, both direct-content overloads (indexed and
non-indexed), segmented-button labels, and navigation content. It waits for
successful recomposition and Android's `OnSaveInstanceState`, backgrounds the
app, asks Android to kill only that background test package, and brings its
existing task forward. It requires a new PID, the original task ID, saved
Bundle provenance, and all ten distinct values to restore. JSON snapshots are
output-only observations; the activity never reads them to initialize state.
The host must not reinstall, clear data, force-stop the app, or launch a new
task between save and restore.

The script removes its own task after success. On failure, inspect the last
snapshot and logcat before removing the test task; a timeout, missing saved
Bundle, or unchanged PID is not a passing result. Same-process activity
recreation alone does not test process-randomized keys.

## Recorded acceptance

On 2026-09-11, the fixed Debug device-test APK passed on Pixel 7 / Android 17.
Android restored task 120 in PID 17322 after background PID 17203 was killed;
the saved Bundle identified PID 17203 as its writer. All ten distinct values
101 through 110 restored, including both nested siblings, both content
overloads with and without indexing, both segmented-label siblings, and
navigation content. The activity's own task was removed after success.

The APK was installed once before the run, with no rebuild, reinstall,
force-stop, or data clearing between saving and restoring. Its SHA-256 was
`F9E10A9EE79098D31520EA8EB43B60135B36C70D7FF10445885C398563EE24FB`.
The 25 existing `ChangedBitsTests`, `ModifierStructuralKeyTests`,
`MutableComposableLambdaTests`, and `MutableManagedStateTests` also passed.
The original randomized-key mismatch was reproduced by the host's
cross-process regression before the fix; the recorded Android run verifies
the fixed behavior, not a pre-fix Android baseline.
