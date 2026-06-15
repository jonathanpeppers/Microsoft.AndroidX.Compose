---
name: maui-coverage
description: Regenerate the .NET MAUI ⇄ Microsoft.AndroidX.Compose.Maui backend coverage report via `scripts/maui-coverage.cs`. Use for any MAUI backend question — "what handlers do we wrap", "is `SearchBar` Compose-backed yet", "what % of MAUI is on Compose", "what properties does our `ButtonHandler` miss" — or after bumping the `Microsoft.Maui.*` package version, adding a new handler under `src/Microsoft.AndroidX.Compose.Maui/Handlers/`, or wiring one into `UseAndroidXCompose()`.
---

# MAUI backend coverage

Regenerate `docs/maui-coverage.md` — the .NET MAUI ⇄ Microsoft.AndroidX.Compose.Maui
backend completeness report. The report is produced by a single .NET 10
file-based program at `scripts/maui-coverage.cs` that:

1. Reads the pinned `Microsoft.Maui.Controls` version from
   `Directory.Build.targets`.
2. Finds the matching `Microsoft.Maui.dll` (Core) in the local NuGet
   cache (`$env:NUGET_PACKAGES` if set, else the user-profile default).
3. Decompiles every `Microsoft.Maui.Handlers.*Handler` type via
   `ilspycmd` into `obj/maui-coverage/maui/`.
4. Regex-parses each handler's `IPropertyMapper<TVirtual, THandler>`
   field initializer — including base-mapper chains
   (`ViewHandler.ViewMapper`, sibling sub-mappers like
   `TextButtonMapper`/`ImageButtonMapper`).
5. Does the same for our own handlers under
   `src/Microsoft.AndroidX.Compose.Maui/Handlers/*.cs`.
6. Cross-references the two by property-mapper key name and writes a
   markdown report with summary, per-category roll-up, status table,
   gap analysis, and per-handler detail.

## Workflow

### 1. Run the script

From the repo root (Windows / PowerShell):

```pwsh
dotnet run scripts/maui-coverage.cs
```

That's it — no `.csproj`, no package install, no MSBuild target. The
script handles its own caching. First run shells to `ilspycmd` for
every handler type (~40 entries, takes ~20 seconds); subsequent runs
are `[cached] …` and finish in a couple of seconds.

Expected tail output:

```
Stock virtual-view registrations: 53
Report written: docs/maui-coverage.md
```

### 2. Verify the diff

```pwsh
git diff --stat docs/maui-coverage.md
```

The headline numbers live in the **Summary** block near the top:

```pwsh
Select-String -Path docs/maui-coverage.md -Pattern '^- \*\*' | Select-Object -First 5
```

If the totals didn't move when you expected them to (e.g. you added a
new handler in `src/Microsoft.AndroidX.Compose.Maui/Handlers/` but the
"Handlers we override" line is unchanged), see **Troubleshooting**
below.

### 3. Commit the regenerated report alongside the change that prompted it

The report is checked in deliberately so reviewers can see coverage
movement in the PR diff. Don't commit the script run on its own —
pair it with the handler addition, the package bump, or whatever
change motivated the refresh.

```pwsh
git add docs/maui-coverage.md
git commit --amend --no-edit  # or a fresh commit, depending on context
```

## Updating the MAUI version

The script reads `Microsoft.Maui.Controls`'s version from
`Directory.Build.targets` via regex on the
`<PackageReference Update="Microsoft.Maui.Controls" Version="..." />`
line. When you bump MAUI:

1. Edit `Directory.Build.targets` to the new version (e.g.
   `10.0.20` → `10.0.21`).
2. Run a normal `dotnet build` to restore the new package into the
   NuGet cache.
3. **Wipe the decompile cache** — the script keys cache files by
   handler type name, not by MAUI version, so stale `.cs` files from
   the previous version would be silently reused:
   ```pwsh
   Remove-Item -Recurse -Force obj/maui-coverage
   ```
4. Re-run the script.

## Troubleshooting

**"microsoft.maui.core X.Y.Z not found under ..."** — the package
hasn't been restored yet. Run any `dotnet build` in the repo first
(easiest: `dotnet build src/Microsoft.AndroidX.Compose.Maui`). The
script honours `$env:NUGET_PACKAGES` for non-default cache locations.

**"ilspycmd: command not found"** — install the global tool:
```pwsh
dotnet tool install -g ilspycmd
```

**Headline % didn't move after adding a handler** — there are two
common causes:

- The handler isn't wired into `UseAndroidXCompose()` in
  `src/Microsoft.AndroidX.Compose.Maui/Hosting/AppHostBuilderExtensions.cs`.
  The script parses that file for `handlers.AddHandler<MauiX, YHandler>()`
  calls; an unregistered handler file under `Handlers/` is invisible.
- The mapper field uses a syntax the regex doesn't recognise. The
  parser expects:
  ```csharp
  public static IPropertyMapper<TVirtual, THandler> Mapper =
      new PropertyMapper<TVirtual, THandler>(ViewHandler.ViewMapper)
      {
          [nameof(IFoo.Bar)] = MapBar,
      };
  ```
  Variations: `public new static IPropertyMapper<...>` (used by
  `PageHandler`), empty `{ }` body, and base-mapper chains across
  sibling sub-mappers are all handled. Custom shapes (raw `Dictionary<>`
  constructions, runtime mutation) will not be picked up — keep the
  declarative initializer form for consistency.

**`LabelHandler` / `CheckBoxHandler` aren't listed under any virtual
view** — MAUI registers a handful of handlers reflectively rather than
via the regex-scannable `AddHandler<TVirtual, THandler>()` calls in
`Microsoft.Maui.Controls.Hosting.AppHostBuilderExtensions`. The script
falls back to deriving the virtual-view name from the handler class
name (`XxxHandler` → `Xxx`) for those. See `SupplementWithImpliedRegistrations`.

**Report looks empty / 0% across the board** — the decompile cache
contains a half-written file. Wipe it:

```pwsh
Remove-Item -Recurse -Force obj/maui-coverage
dotnet run scripts/maui-coverage.cs
```

## What the report contains

- **Summary** — overall percentages. Three headline numbers:
  - **Stock MAUI handlers in scope** — denominator for handler-level
    coverage (excludes the abstract `ViewHandler`/`ElementHandler`
    bases).
  - **Handlers we override** — how many of those have a Compose
    backend in `src/Microsoft.AndroidX.Compose.Maui/Handlers/`.
  - **Property-mapper keys covered** — finer-grained signal across
    every property key on every covered handler. This is the most
    useful single number for tracking "behavioural completeness" of
    individual handlers over time.

- **Per-category coverage** — handlers grouped into
  **Pages / Navigation**, **Containers**, **Leaves**, **Menus /
  Toolbar**, **Shapes**, **App / Window**, **Other / advanced**. Each
  row shows handler coverage and key coverage for the bucket.

- **Per-handler coverage** — single table, sorted by category, with a
  ✅ / 🟡 / ❌ status icon for every stock handler. ✅ = all keys
  covered, 🟡 = partial, ❌ = no Compose backend at all.

- **Missing handlers worth investigating** — ❌ handlers sorted by
  number of stock keys (biggest gaps first). These are the highest-
  leverage candidates for new handler work.

- **Partial handlers (gap analysis)** — 🟡 handlers sorted by
  coverage % (lowest first), listing the specific missing keys. This
  is the prioritised TODO list for tightening existing handlers.

- **Per-handler property detail** — one section per stock handler.
  Missing keys are listed inline; the full `[x]`/`[ ]` stock-key list
  is in a collapsed `<details>` block to keep the report skimmable.

- **Microsoft.AndroidX.Compose.Maui-only handlers** — internal plumbing
  handlers with no stock counterpart.

- **Caveats** — the matching heuristics in plain English. Always
  worth a re-read before drawing conclusions from a number — in
  particular, `ViewHandler.ViewMapper` is shared via `RemapForCompose()`
  so every Compose-backed handler gets `Opacity`/`Scale*`/`Rotation*`
  /etc. "for free", and `CommandMapper` keys (focus, scroll-to, …)
  aren't included.

## Don't do these things

- Don't hand-edit `docs/maui-coverage.md`. It's regenerated every run
  and your edit will be lost.
- Don't add `obj/maui-coverage/` to git — it's already covered by the
  root `obj/` gitignore entry and contains the full decompiled MAUI
  Core handler source.
- Don't run the script from inside a subdirectory; paths in the
  script (`obj/maui-coverage`, `docs/maui-coverage.md`,
  `src/Microsoft.AndroidX.Compose.Maui/Handlers/`, `Directory.Build.targets`)
  are relative to the repo root.
- Don't compare coverage % across MAUI version bumps without first
  wiping `obj/maui-coverage/` — the cache is keyed by handler class
  name, not by MAUI version.
