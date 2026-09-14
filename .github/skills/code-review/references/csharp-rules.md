# C# Review Rules

## Compatibility and nullability

| Check | What to look for |
|-------|------------------|
| Respect the target framework | Runtime and app projects target modern .NET Android; the source generator targets `netstandard2.0`. Do not use APIs unavailable to the changed project. |
| Preserve nullable contracts | Avoid success-shaped null fallbacks and postfix `!`. Use parameter guards for arguments and actionable invalid-state exceptions for lifecycle-owned properties. |
| Preserve cancellation | Async APIs must accept and forward `CancellationToken` consistently. Catch-all handlers must not turn cancellation into success or an unrelated failure. |
| Keep type safety | Prefer proper types and guards over `as any`-style equivalents, broad casts, or reflection when symbols are available. |

## Error handling and lifetime

| Check | What to look for |
|-------|------------------|
| No silent failures | Do not add empty catches, ignored JNI failures, hidden build errors, or default return values that look successful. |
| Actionable exceptions | Name the broken contract and include unexpected values or types when useful. |
| Dispose deterministically | Java peers, streams, local JNI references, registrations, and other resources need correct success and exception paths. |
| Publish initialized state | Shared caches and lazily initialized peers must not expose partially initialized values under concurrent access. |

## Performance and organization

| Check | What to look for |
|-------|------------------|
| Avoid recomposition allocations | Hot `Render` and generated lowering paths should preserve remembered callbacks, stable lambdas, and cached peers rather than allocating each pass. |
| Avoid repeated JNI work | Class, method, field, and stable peer lookups should follow existing cache patterns. |
| Avoid accidental quadratic work | Repeated linear searches in generators, catalogs, handlers, or render loops need scrutiny. |
| Keep helpers internal | New public members require a real external contract, XML docs, and API baseline entries. |
| One class per file | Do not group unrelated public or internal types in one file. |

