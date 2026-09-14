---
name: code-review
description: >-
  Review Microsoft.AndroidX.Compose pull requests against the repository's
  Compose runtime, JNI, source-generator, MAUI, Android, build, testing, and
  public API rules. Use whenever the user asks for a code review, says
  "review this PR", provides a pull request URL or number, asks whether a
  change is ready to merge, or invokes the repository's /review workflow.
---

# Microsoft.AndroidX.Compose Code Review

Review pull requests for a C#-only .NET-for-Android facade over Jetpack
Compose. Correctness depends on Kotlin Compose ABI details, JNI ownership,
source-generated code, stable composition identity, and official
`Xamarin.AndroidX.Compose.*` bindings.

Adapted from
[`jonathanpeppers/dotnes/.github/skills/code-review`](https://github.com/jonathanpeppers/dotnes/tree/bb4fee4138598073e545a71f3eb01cbaa044640b/.github/skills/code-review).

## Review mindset

Be polite but skeptical. Prioritize runtime correctness, JNI safety,
composition identity, generated-code consistency, and regressions over style.
Three verified findings are more useful than fifteen speculative comments.

Flag severity in every inline comment:

- ❌ **error** — Must fix before merge: crashes, incorrect JNI/Compose ABI,
  composition corruption, broken public API, security defects, or regressions.
- ⚠️ **warning** — Should fix: missing regression coverage, incomplete wiring,
  meaningful performance problems, or repository-rule violations.
- 💡 **suggestion** — Optional improvement: focused readability, design, or
  documentation advice that is concrete enough to act on.

For substantive pull requests, prefer useful inline findings over hiding
issues in the summary. Do not invent a nit to create a comment.

## Workflow

### 1. Identify the pull request

When an agentic workflow was triggered by a pull request comment, use the pull
request from the event context. Otherwise parse the URL, `owner/repo#number`,
or number supplied by the user. A bare number defaults to
`jonathanpeppers/Microsoft.AndroidX.Compose`.

### 2. Gather context before reading the narrative

Read the diff and changed-file list first:

```bash
gh pr diff "$pr" --repo "$repo"
gh pr view "$pr" --repo "$repo" --json files
```

For each changed file, read the full file, not only the diff. Read callers,
sibling implementations, tests, and generated inputs when the change affects
an API or shared helper. Trace changed values to their final binding, JNI, or
Compose runtime consumer.

Form an independent assessment before reading the pull request description.
Names, comments, and generated output are not proof that behavior is correct.

### 3. Reconcile the pull request narrative

```bash
gh pr view "$pr" --repo "$repo" --json title,body,author
```

Treat claims as things to verify. Confirm that bug fixes address the root
cause, new public surfaces are fully wired, and performance claims have
evidence appropriate to the claim.

### 4. Check CI

```bash
gh pr checks "$pr" --repo "$repo"
```

Inspect failed GitHub Actions job logs. Distinguish pull-request regressions
from infrastructure failures or known flakes. Do not report a clean result
while required checks fail.

### 5. Load repository rules

Always read:

- `.github/copilot-instructions.md` — canonical repository architecture and
  implementation rules.
- `references/repo-conventions.md`
- `references/ai-pitfalls.md`

Then load only the relevant focused checklists:

- `references/csharp-rules.md` for C# changes.
- `references/compose-rules.md` for
  `src/Microsoft.AndroidX.Compose/`, templates, samples, or gallery changes.
- `references/generator-rules.md` for source-generator, generator-test,
  `ComposeDefaults.cs`, `ComposeBridges.cs`, or generated-facade changes.
- `references/maui-rules.md` for the MAUI backend or MAUI sample.
- `references/testing-rules.md` for test changes or behavior changes that need
  tests or gallery/device coverage.
- `references/build-rules.md` for project, package, workflow, template,
  props, targets, or public API baseline changes.
- `references/security-rules.md` for code, build, workflow, dependency, JNI,
  process, or file-system changes.

Also read matching scoped instruction files:

- `.github/instructions/dotnet-android.instructions.md`
- `.github/instructions/compose-maui.instructions.md`
- `.github/instructions/public-api.instructions.md`
- `.github/instructions/versioning.instructions.md`

### 6. Analyze the diff

Review in this priority order:

1. Compose ABI, composition identity, and recomposition correctness
2. JNI ownership, lifetime, signatures, and binding use
3. Source-generator output, diagnostics, determinism, and compatibility
4. Runtime and MAUI behavior
5. Public API compatibility and complete feature wiring
6. Tests, gallery demos, and device regressions
7. Build, package, template, and dependency consistency
8. Security and supply-chain safety
9. Performance, duplication, and documentation

Constraints:

- Comment only on added or modified lines in the diff.
- Use the new-file line number and verify it belongs to the changed hunk.
- Put one issue in each inline comment.
- If one issue repeats, comment once and list other affected locations.
- Do not flag compiler, analyzer, or formatting failures CI already reports
  unless the finding explains a meaningful underlying defect.
- Verify concerns against the full context. Ask a focused question when
  evidence is incomplete instead of asserting a false positive.
- Do not demand broad cleanup unrelated to the pull request.

### 7. Submit the review

Use this inline format:

```text
🤖 {severity} **{Category}** — {What is wrong, why it matters, and what to do instead.}
```

Useful categories include: Compose ABI · Composition identity · Default mask ·
Changed mask · JNI signature · JNI lifetime · Binding usage · Facade
generation · Source generator · Diagnostics · Suspend bridge · MAUI handler ·
Public API · Android compatibility · Build · Dependencies · Testing · Gallery ·
Performance · Error handling · Security · Documentation.

Submit:

- inline comments for verified, actionable findings; and
- one summary with the verdict, issue counts by severity, CI state, and
  concise positive callouts.

Never submit an `APPROVE` review. Use `COMMENT` when no blocking issue exists
and `REQUEST_CHANGES` when an ❌ error must be fixed. If the author is Copilot
and the verdict requires changes, prefix the summary with `@copilot ` so the
coding agent can act on it.

