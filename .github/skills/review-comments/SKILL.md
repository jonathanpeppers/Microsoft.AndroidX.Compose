---
name: review-comments
description: Address unresolved review comments on the current branch's PR — classify each, apply code changes when warranted, and reply to every thread with what was done and why. Use whenever the user wants to review, address, respond to, triage, or handle PR reviewer feedback.
---

# Review Comments

Address reviewer feedback on the current branch's pull request end-to-end:
discover the PR, read every **unresolved** comment thread, decide whether
each is actionable, make the code changes (if any), and post a reply on
each thread explaining what changed and **why** (or why no change was
needed). The reasoning is the point — a reply that just says "done" is a
failure mode.

## Workflow

### 1. Locate the PR for this branch

```pwsh
$branch = git rev-parse --abbrev-ref HEAD
gh pr list --head $branch --state open --json number,url,headRefName,baseRefName,title
```

If zero matches, stop and tell the user the branch has no open PR. If
multiple, ask the user which one. Cache the PR number in `$pr` for the
rest of the run.

### 2. Pull *unresolved* review threads via GraphQL

`gh pr view --json comments,reviews` does **not** expose `isResolved` on
review threads — you have to use GraphQL. Save the query to a temp
file so PowerShell quoting doesn't mangle it:

```pwsh
$owner, $repo = (gh repo view --json nameWithOwner -q .nameWithOwner) -split '/'
$query = @'
query($owner:String!, $repo:String!, $pr:Int!) {
  repository(owner:$owner, name:$repo) {
    pullRequest(number:$pr) {
      reviewThreads(first:100) {
        nodes {
          id
          isResolved
          isOutdated
          path
          line
          comments(first:50) {
            nodes {
              id
              databaseId
              author { login }
              body
              url
              diffHunk
            }
          }
        }
      }
    }
  }
}
'@
$tmp = New-TemporaryFile
Set-Content -Path $tmp -Value $query -Encoding utf8
gh api graphql -F owner=$owner -F repo=$repo -F pr=$pr --field query="@$tmp"
Remove-Item $tmp
```

Filter to `isResolved == false`. Also pull standalone issue comments
(`gh pr view $pr --json comments`) — those have no resolved state, so
treat any since the last commit you pushed as fair game.

### 3. Classify each comment

For each unresolved thread, pick one of:

| Category | Action |
|----------|--------|
| **Actionable code change** | Make the edit in this worktree |
| **Question** | Answer it in the reply (no code change) |
| **Suggestion you disagree with** | Reply with reasoning; don't change code |
| **Already addressed** | Reply pointing at the commit/line that addressed it |
| **Out of scope / follow-up** | Reply acknowledging, suggest a follow-up issue |
| **Praise / "LGTM"** | Skip — no reply needed |

Stay inside the scope of the PR. If a reviewer asks for a big
refactor that isn't what this PR is about, push back politely and
propose a follow-up issue rather than expanding scope.

### 4. Make the code changes

Group related comments and apply edits together. Run whatever the repo's
build/test commands are after the edits — for this repo specifically:

```pwsh
dotnet test  src/Microsoft.AndroidX.Compose.SourceGenerators.Tests
dotnet build src/Microsoft.AndroidX.Compose
dotnet build src/Microsoft.AndroidX.Compose.Gallery
```

Only run the suites whose inputs you actually changed. See
`.github/copilot-instructions.md` for the full build matrix.

Commit with a focused message that references the PR; include the
standard trailer:

```
Address review feedback

Co-authored-by: Copilot <223556219+Copilot@users.noreply.github.com>
```

Push to the PR branch (`git push`). Do **not** force-push unless the
user explicitly asked.

### 5. Reply to every thread (except skipped praise/off-topic)

The reply is the visible artifact of this skill — invest in it.

**Reply structure** (keep it tight, 2–5 sentences):

1. **What was done** — one line. e.g. "Renamed `Foo` → `Bar` in `X.cs`
   and updated callers."
2. **Why** — one or two lines of reasoning. Reference the relevant
   convention, doc, or prior art when possible.
3. **Pointer** — link the commit SHA or file:line when helpful.

Use the right reply API depending on the thread type:

**Inline review-comment thread** (has `path` and `line`) — reply to
the existing thread so it stays threaded, then resolve it:

```pwsh
# Reply to the thread (uses the first comment's databaseId as the parent)
gh api -X POST "/repos/$owner/$repo/pulls/$pr/comments/$parentCommentId/replies" `
       -f body="$replyBody"

# Resolve the thread (GraphQL — needs the thread node id, not databaseId)
$resolveQuery = 'mutation($id:ID!) { resolveReviewThread(input:{threadId:$id}) { thread { isResolved } } }'
gh api graphql -F id=$threadId -f query=$resolveQuery
```

**Standalone PR comment** (no `path`/`line`):

```pwsh
gh pr comment $pr --body $replyBody
```

Only resolve threads where you either (a) made the change the reviewer
asked for, or (b) the reviewer's concern is genuinely answered by your
reply. Leave threads open when you've declined to act and want the
reviewer to weigh in — that's an explicit handoff, not avoidance.

### 6. Summarize for the user

End with a compact summary in the chat — not on GitHub:

- How many threads were unresolved going in
- How many you addressed with code (with commit SHA)
- How many you replied-and-resolved without code
- How many you replied to but left open for the reviewer
- Any threads you intentionally skipped (praise, off-topic) and why

This lets the user verify your judgment before the reviewer sees the
replies.

## Tone for replies

Write as the PR author talking to a colleague — direct, specific, no
filler. Skip "Thanks for the great feedback!" preambles. Reasoning
beats deference: a reply that says *why* you did or didn't do something
is more useful than one that just agrees.

**Bad:**
> Done!

**Bad:**
> Thanks so much for catching this! You're absolutely right — I've gone
> ahead and made that change as you suggested.

**Good:**
> Renamed to `ButtonDefault.All` for consistency with the other
> generated `$default` enums in `ComposeDefaults.cs` (`ColumnDefault.All`,
> `RowDefault.All`). Fixed in 3f1c8a2.

**Good (declining):**
> Leaving as-is. `IntPtr.Zero` is the documented sentinel for "Kotlin
> default" throughout `ComposeBridges.cs` — switching just this one
> bridge to `null` would break the pattern. Happy to do a sweep across
> all bridges in a follow-up if you'd prefer that direction.

## Common failure modes to avoid

- **Replying "done" with no reasoning.** The reasoning is the entire
  point of this skill.
- **Silently expanding scope** to satisfy a reviewer's tangential
  comment. Propose a follow-up issue instead.
- **Resolving threads you didn't actually address.** Resolution is the
  reviewer's call when you push back — leave it open.
- **Force-pushing** without being asked. This repo uses normal pushes
  to PR branches.
- **Editing files in `main_checkout_path`** (the main checkout) instead
  of `workspace_path` (the worktree). Always work in the worktree.
- **Skipping the build/test commands.** A reply that says "fixed" on a
  change that doesn't compile is worse than no reply.
