# Build, Package, and Public API Review Rules

## Package and project files

| Check | What to look for |
|-------|------------------|
| Central package versions | Project files use versionless `PackageReference Include`; versions belong in matching `PackageReference Update` entries in `Directory.Build.targets`. |
| Keep binding versions aligned | Compose runtime/facade `.Android` package pairs and Kotlin/AndroidX dependencies must remain compatible as a matrix. |
| Preserve MAUI pins | MAUI and its load-bearing Android parent-package pins have documented runtime compatibility reasons; require evidence before changing them. |
| Ship analyzers correctly | Runtime NuGet packaging and local project references must continue exposing source generators and interceptor configuration to consumers. |
| Keep templates synchronized | Public setup, package, TFM, or authoring changes may require template metadata and generated project updates. |
| Pin workflow dependencies | GitHub Actions and containers in compiled workflows should be compiler-generated or pinned to immutable SHAs/digests. |

## Public API baselines

| Check | What to look for |
|-------|------------------|
| Track every public symbol | New APIs belong in `PublicAPI.Unshipped.txt`; removals or signature changes are breaking unless intentionally managed. |
| Keep the baseline sorted | `#nullable enable` stays first and all non-empty entries use ordinal sorting. |
| Account for generated APIs | `dotnet format` may skip source-generated symbols; compare build warnings and add exact remaining entries manually. |
| Document public API | Baseline entries do not replace XML documentation or gallery/sample coverage. |

## Versioning

When `eng/Versions.props` or template version metadata changes, load
`.github/instructions/versioning.instructions.md` and verify every coupled
location. Do not assume a package version update is isolated.

