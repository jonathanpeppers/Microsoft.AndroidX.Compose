---
applyTo: "**/PublicAPI.*.txt"
---

# CRITICAL: PUBLIC API BASELINES MUST ALWAYS BE SORTED

> [!CAUTION]
> **NON-NEGOTIABLE:** An edit to any `PublicAPI.*.txt` file is incomplete
> until the entire API entry list is sorted. Never append an entry and leave
> it out of order.

1. `#nullable enable` MUST remain the first line.
2. Every subsequent non-empty line MUST be alphabetically sorted using
   ordinal string ordering.
3. After adding, removing, or updating any entry, re-sort and verify the
   entire file before finishing the task.
