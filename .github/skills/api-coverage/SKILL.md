---
name: api-coverage
description: Regenerate the Compose ⇄ Microsoft.AndroidX.Compose API coverage report via `scripts/api-comparison.cs`. Use for any Compose coverage question — "how much do we wrap", "what's missing in module X", "is Foo wrapped yet" — or after bumping a `Xamarin.AndroidX.Compose.*` version or adding a facade.
---

# API coverage

Regenerate `docs/api-coverage.md` — the Jetpack Compose ⇄ Microsoft.AndroidX.Compose API
coverage report. The report is produced by a single .NET 10 file-based
program at `scripts/api-comparison.cs` that downloads upstream AndroidX
`-sources.jar` files from Google Maven, parses public Kotlin
declarations, cross-references them with
`src/Microsoft.AndroidX.Compose/PublicAPI.Unshipped.txt`, and writes a markdown
report with a coverage % summary, per-module checkbox lists,
Microsoft.AndroidX.Compose-only types, and a "differences worth investigating"
appendix.

## Workflow

### 1. Run the script

From the repo root (Windows / PowerShell):

```pwsh
dotnet run scripts/api-comparison.cs
```

That's it — no `.csproj`, no package install, no MSBuild target. The
script handles its own dependency download and caching. First run
takes ~30 seconds to fetch all 24 sources jars; subsequent runs are
"[cached] …" and finish in a couple of seconds.

Expected tail output:

```
Kotlin symbols scanned: 2282
C# public symbols:      1139
Report written: docs/api-coverage.md
```

### 2. Verify the diff

```pwsh
git diff --stat docs/api-coverage.md
```

The headline numbers live near the top of the file (lines ~9-16).
Quick sanity check:

```pwsh
Select-String -Path docs/api-coverage.md -Pattern '^- \*\*' | Select-Object -First 5
```

If the totals didn't move when you expected them to (e.g. you added a
new facade but `Covered by Microsoft.AndroidX.Compose` is unchanged), see
**Troubleshooting** below.

### 3. Commit the regenerated report alongside the change that prompted it

The report is checked in deliberately so reviewers can see coverage
movement in the PR diff. Don't commit the script run on its own —
pair it with the facade addition, the package bump, or whatever
change motivated the refresh.

```pwsh
git add docs/api-coverage.md
git commit --amend --no-edit  # or a fresh commit, depending on context
```

## Updating the artifact list

The script has a hardcoded list of `(group, artifact, version)`
tuples near the top (look for `var artifacts = new[]`). It mirrors
`Directory.Build.targets`. When central package versions change:

1. Open `Directory.Build.targets` and note the new versions.
2. Map Xamarin → upstream: `Xamarin.AndroidX.Compose.Foo` version
   `1.11.2.1` → upstream `1.11.2` (the trailing `.N` is the
   binding-wrapper revision, dropped when fetching upstream
   sources). Material 3 `1.4.0.2` → `1.4.0`.
3. Edit the tuple list in `scripts/api-comparison.cs` to match.
4. Delete the cache to force a clean re-fetch:
   ```pwsh
   Remove-Item -Recurse -Force obj/api-coverage
   ```
5. Re-run the script.

## Troubleshooting

**"Could not download from dl.google.com"** — Google Maven sometimes
404s for transitional artifact versions. Verify the version exists
by opening
`https://dl.google.com/dl/android/maven2/androidx/compose/<module>/<artifact>/<version>/`
in a browser. If you typo'd a version, fix the tuple list.

**Headline % didn't move after adding a facade** — the matcher is
short-name based and case-insensitive. Check that the public type
name in `PublicAPI.Unshipped.txt` matches the Kotlin function /
class name exactly. For lowercase Kotlin functions
(`derivedStateOf`, `mutableStateListOf`, …) the matcher falls back
to method-name lookup against C# methods on classes — usually a
static method on `Compose.cs`. If a Kotlin extension function isn't
matching, check that the C# method name PascalCases the same way.

**"static" / generic methods on `Compose.cs` missing from index** —
the `PublicAPI.Unshipped.txt` parser handles `static `, `abstract `,
`virtual `, `override `, `sealed ` line prefixes plus generics on
the member name like `Remember<T>`. If a regression appears, the fix
lives in `ScanPublicApi`.

**Report looks empty / 0% across the board** — the cache directory
might contain a half-extracted jar. Wipe it:

```pwsh
Remove-Item -Recurse -Force obj/api-coverage
dotnet run scripts/api-comparison.cs
```

## What the report contains

- **Summary** — overall % and per-module table. The single most
  useful number is "@Composable functions: X / Y (Z%)" — that's the
  user-facing surface most directly comparable to Kotlin's docs.
- **Per-module sections** — GitHub-renderable task lists
  (`- [x] Button → type match`, `- [ ] BackHandler`). Split into
  `@Composable functions` and `Other top-level functions`.
- **Microsoft.AndroidX.Compose-only types** — C# types with no Kotlin counterpart in
  scope. Most are intentional infrastructure (`ComposableLambda*`,
  `RenderContext`, state-holder wrappers); a handful (the
  `*Flexible*AppBar` family) wrap `internal fun` Kotlin that's real
  on the JVM but not part of Kotlin's public API.
- **Differences worth investigating** — composables where Kotlin has
  ≥ 8 params. Coarse signal for missing slots; `OutlinedTextField`
  / `TextField` / `SecureTextField` (26-param Kotlin) typically top
  the list and warrant a slot-by-slot audit before any release.
- **Caveats** — the matching heuristics in plain English. Always
  worth a re-read before drawing conclusions from a number.

## Don't do these things

- Don't hand-edit `docs/api-coverage.md`. It's regenerated every run
  and your edit will be lost.
- Don't add `obj/api-coverage/` to git — it's already covered by the
  root `obj/` gitignore entry and contains ~50 MB of extracted
  Kotlin source.
- Don't run the script from inside a subdirectory; paths in the
  script (`obj/api-coverage`, `docs/api-coverage.md`,
  `src/Microsoft.AndroidX.Compose/PublicAPI.Unshipped.txt`) are relative to
  the repo root.
