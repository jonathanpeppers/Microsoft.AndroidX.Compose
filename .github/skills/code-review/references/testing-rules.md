# Testing Review Rules

## Required coverage

| Change | Expected evidence |
|--------|-------------------|
| Source generator or `ComposeDefaults.cs` | Focused synthetic compilation tests plus `dotnet test src/Microsoft.AndroidX.Compose.SourceGenerators.Tests`. |
| Runtime facade or bridge | Focused tests where possible and `dotnet build src/Microsoft.AndroidX.Compose`. |
| Public UI surface | A focused gallery demo registered in `Catalog.Demos`; build the gallery and use device verification for runtime-only behavior. |
| Composition identity, saved state, JNI, or recomposition | A regression that exercises the real runtime path, often in device tests or a purpose-built gallery demo. |
| MAUI handler | Mapper/state behavior coverage and sample/device validation appropriate to the handler path, including fallback when relevant. |
| Bug fix | A regression that fails without the fix and passes with it. |

## Test quality

| Check | What to look for |
|-------|------------------|
| Assert the contract directly | Verify exact generated output, diagnostics, masks, identity, ownership, or visible behavior rather than a loose proxy. |
| Cover invalid shapes | Generator features need negative tests for misuse and the expected diagnostic ID/location. |
| Cover boundaries | Include bit 31/wide masks, null versus omitted arguments, receiver shapes, repeated siblings, first/recomposition paths, and cancellation when relevant. |
| Keep tests deterministic | Do not depend on current time, locale, filesystem order, randomized hashes, device-global state, or unstable call-site ordering. |
| Avoid snapshot laundering | Do not update expected output merely to make a regression pass; explain and verify intentional generated-output changes. |
| Match the smallest sufficient suite | Do not require full Android/device validation for documentation-only changes, but do not accept generator-only tests for a runtime lifecycle defect. |

Review CI's actual commands in `.github/workflows/build.yml`; ensure the pull
request's changed subsystem is exercised rather than assuming a green,
unrelated job proves correctness.

