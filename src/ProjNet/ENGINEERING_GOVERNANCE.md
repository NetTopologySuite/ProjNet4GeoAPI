# Engineering governance

This document defines the active engineering and quality gates for `ProjNET`.

## Public API baseline policy

`src/ProjNet/PublicAPI.Shipped.txt` is the canonical public API baseline for the main `ProjNET` library.

- Verification runs in `test/ProjNet.Tests/CodeQuality/PublicApiBaselineTests.cs`.
- The baseline gate must stay green in regular validation.
- Intentional API surface changes must update the shipped baseline in a reviewed commit.

### Approved baseline update flow

Use this only when a public API change is intentional and approved:

1. Run baseline update:
   - PowerShell:  
      `$env:PROJNET_UPDATE_PUBLIC_API_BASELINE='1'; dotnet test --project .\test\ProjNet.Tests\ProjNET.Tests.csproj --filter-class ProjNet.Tests.PublicApiBaselineTests`
2. Inspect and review changes in:
   - `src/ProjNet/PublicAPI.Shipped.txt`
   - ProjNET uses `PublicApiBaselineTests` with `PublicApiGenerator`; there is no `PublicAPI.Unshipped.txt` file in this repository.
3. Re-run without update variable:
   - `Remove-Item Env:PROJNET_UPDATE_PUBLIC_API_BASELINE -ErrorAction Ignore`
   - `dotnet test --project .\test\ProjNet.Tests\ProjNET.Tests.csproj --filter-class ProjNet.Tests.PublicApiBaselineTests`

## Target framework policy

`ProjNET` must continue to ship `netstandard2.0`.

Approved target frameworks:

- `netstandard2.0` (required shipping target)
- `netstandard2.1`
- `net8.0`

Build policy is enforced in `src/ProjNet/ProjNET.csproj` via `ValidateTargetFrameworkPolicy`.

## Testing policy

- Unit and integration tests run on xUnit v3.
- Default validation command:
  - `dotnet test --project .\test\ProjNet.Tests\ProjNET.Tests.csproj`

## CI/CD pipeline

- `/.github/workflows/full-ci.yml` is the main validation pipeline for push, pull request, and manual runs. It builds the solution, runs pull-request dependency review, collects Cobertura coverage, executes the API/parity/benchmark smoke gates, packs artifacts, publishes to MyGet from `develop` and `master`, and publishes to NuGet from `master`.
- `/.github/workflows/benchmarks.yml` runs the curated BenchmarkDotNet suite on a weekly schedule and on manual demand. The default branch persists benchmark history to the `benchmark-data` branch; non-default refs run compare-only checks against that stored baseline.
- `/.github/workflows/codeql.yml` runs the dedicated C# CodeQL security analysis workflow on push, pull request, and weekly schedule.
- `/.github/workflows/mutation-tests.yml` runs the Stryker mutation suite for manual runs and for `develop` pushes that touch the configured source, test, tooling, or workflow paths.
- `/.github/dependabot.yml` manages weekly NuGet and GitHub Actions dependency updates.

### Coverage policy

- Coverage is collected in CI via `dotnet-coverage` and published as Cobertura output plus markdown summaries.
- Same-repository non-bot pull requests receive the current coverage summary directly in the PR discussion.
- There is no hard fail threshold at this stage; coverage is tracked for visibility and regression monitoring.

### Benchmark policy

- The benchmark pipeline uses a curated benchmark set defined in `/.github/scripts/Get-CuratedBenchmarkConfiguration.ps1`.
- `full-ci.yml` runs the curated smoke path so benchmark execution, report conversion, and curated dataset completeness fail fast during normal CI.
- `benchmarks.yml` runs the full curated suite, converts BenchmarkDotNet reports into the regression dataset consumed by `benchmark-action`, and raises alerts at a `150%` regression threshold without failing the workflow.

## Code style and analyzers

- `.editorconfig` is the primary style source of truth.
- StyleCop analyzers are enabled repository-wide.
- File-header diagnostics `SA1633`-`SA1638` are intentionally disabled to allow provenance-specific SPDX headers.
- `stylecop.json` keeps XML header enforcement disabled (`xmlHeader: false`) for variable attribution scenarios.
- Null checks should use pattern-style comparisons (`is null` / `is not null`) in new and touched code.
- `StringSyntaxAttribute` annotations should be added only when APIs accept caller-supplied regex or format strings; current handwritten code primarily uses inline patterns and `GeneratedRegex`.
- Prefer nullable flow attributes (`NotNull`, `NotNullWhen`, `MemberNotNull`, etc.) and throw-flow attributes (`DoesNotReturn`, `DoesNotReturnIf`) on shared guard helpers where applicable.
- Reflection/AOT-related attributes (`DynamicallyAccessedMembers`, `RequiresUnreferencedCode`, `RequiresDynamicCode`) are audited together with trim warnings in the dedicated AOT milestone.

## Documentation policy

- Public API changes should include XML documentation updates when applicable.
- External web URLs should not be embedded in XML API docs unless required for legal/provenance context.

## Licensing and attribution policy

- Project-level licensing and attribution references are maintained in:
  - `LICENSES/`
  - `NOTICE.md`
- Source files use SPDX-style headers with provenance-specific attribution.

## Deprecation and compatibility policy

- Compatibility-first is the default: avoid breaking removals in active release lines.
- Obsolete APIs are acceptable when:
  - replacement guidance is explicit,
  - behavior remains functional during deprecation window,
  - removals are deferred to a major-version decision.
