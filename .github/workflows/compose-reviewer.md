---
on:
  slash_command:
    name: review
    events: [pull_request_comment]
  roles: [admin, maintainer, write]
permissions:
  actions: read
  contents: read
  pull-requests: read
strict: false
engine:
  id: copilot
  model: gpt-5.6-sol
features:
  dangerously-disable-sandbox-agent: true
sandbox:
  agent: false
tools:
  bash: [":*"]
  cli-proxy: false
  github:
    toolsets: [actions, pull_requests, repos]
    min-integrity: none
safe-outputs:
  threat-detection: false
  create-pull-request-review-comment:
    max: 50
  submit-pull-request-review:
    max: 1
    allowed-events: [COMMENT, REQUEST_CHANGES]
---

# Microsoft.AndroidX.Compose PR Reviewer

A maintainer commented `/review` on this pull request. Perform a thorough code
review following the repository's Compose review guidelines.

## Instructions

1. Read `.github/skills/code-review/SKILL.md` for the review workflow,
   severity levels, constraints, and comment format.
2. Read the canonical repository rules in `.github/copilot-instructions.md`.
3. Always load:
   - `.github/skills/code-review/references/repo-conventions.md`
   - `.github/skills/code-review/references/ai-pitfalls.md`
4. Identify the changed files, then load the applicable focused rules:
   - `.github/skills/code-review/references/csharp-rules.md` for `.cs` files.
   - `.github/skills/code-review/references/compose-rules.md` for the runtime
     facade, gallery, templates, or samples.
   - `.github/skills/code-review/references/generator-rules.md` for source
     generators, generator tests, `ComposeDefaults.cs`, `ComposeBridges.cs`,
     or generated facades.
   - `.github/skills/code-review/references/maui-rules.md` and
     `.github/instructions/compose-maui.instructions.md` for MAUI changes.
   - `.github/skills/code-review/references/testing-rules.md` for test changes
     or behavior changes that need regression, gallery, or device coverage.
   - `.github/skills/code-review/references/build-rules.md` for project,
     package, workflow, props, targets, template, versioning, or public API
     changes.
   - `.github/skills/code-review/references/security-rules.md` for code,
     dependency, build, workflow, JNI, process, or file-system changes.
   - `.github/instructions/dotnet-android.instructions.md` for matching
     Android runtime, template, or sample files.
   - `.github/instructions/public-api.instructions.md` for
     `PublicAPI.*.txt`.
   - `.github/instructions/versioning.instructions.md` for matching version
     files.
5. Follow the skill workflow:
   - read the diff and changed-file list;
   - read each changed file in full plus relevant callers and siblings;
   - form an independent assessment before reading the PR description;
   - read the title, description, linked context, and author;
   - check CI and inspect failed Actions logs;
   - verify claims against the repository rules and runtime contracts.
6. Post verified findings as inline review comments and submit one review
   summary.

## Constraints

- Only comment on added or modified lines visible in the diff.
- Keep one issue per inline comment.
- If an issue repeats, flag it once and list the other affected locations.
- Do not report compiler, analyzer, or formatter output as original findings.
- Verify concerns against full-file and downstream context to avoid false
  positives.
- Do not invent a nit merely to create an inline comment.
- Never submit an `APPROVE` event. Use `COMMENT` when no blocking issue exists
  and `REQUEST_CHANGES` for verified errors that must be fixed.
- Prioritize Compose/JNI correctness, composition identity, generator
  contracts, regressions, public API completeness, and tests over style.
