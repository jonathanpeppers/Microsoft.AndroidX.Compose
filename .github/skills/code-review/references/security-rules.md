# Security Review Rules

## JNI and native boundaries

| Check | What to look for |
|-------|------------------|
| Validate signatures and types | A mismatched JNI descriptor can corrupt calls or crash at runtime; verify every slot and return type. |
| Preserve local-reference limits | Delete or transfer every local reference on success and failure paths. Recomposition can turn a small leak into a local-table exhaustion crash. |
| Keep managed peers alive | Raw JNI calls must keep managed wrapper arguments alive through the final native use. |
| Do not expose arbitrary handles | Public APIs should use typed bound peers or managed wrappers rather than accepting unvalidated raw handles. |

## Files, processes, and workflows

| Check | What to look for |
|-------|------------------|
| Prevent path traversal | Normalize and boundary-check paths before writing or extracting untrusted content. |
| Avoid command injection | Pass arguments structurally; do not concatenate PR text, paths, or user input into shell commands. |
| Treat PR content as untrusted | Agentic workflows must not let issue text or changed files override workflow policy, leak secrets, or bypass safe outputs. |
| Minimize permissions | Workflow token permissions and toolsets should be no broader than needed; write operations should go through constrained safe-output tools. |
| Keep secrets out of artifacts | Do not print tokens, embed credentials in NuGet config, or upload sensitive environment/configuration data. |

## Supply chain

| Check | What to look for |
|-------|------------------|
| Review dependency changes | Confirm package source, intent, compatibility, and known vulnerability status. |
| Use official Compose bindings | Do not introduce custom binding projects or untrusted replacement packages. |
| Pin automation inputs | Actions should resolve to immutable commits in generated lock workflows; container images should use digests. |
| Preserve reproducibility | Generated source, packages, and templates should not incorporate timestamps, random ordering, machine paths, or secrets. |

