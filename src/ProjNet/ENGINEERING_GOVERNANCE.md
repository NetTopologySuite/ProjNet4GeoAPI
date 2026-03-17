# Engineering governance (Wave A)

## Public API baseline policy

`src/ProjNet/PublicAPI.Shipped.txt` is the canonical public API baseline for the main `ProjNET` library.

- Verification runs in `test/ProjNet.Tests/PublicApiBaselineTests.cs`.
- The test fails when public API changes are detected.
- This check is non-breaking for consumers and guards accidental API drift.

## Unit test framework policy

All tests are executed on xUnit v3.2.2.

- Test discovery/execution uses xUnit attributes (`[Fact]`, `[Theory]`, `[InlineData]`).
- `dotnet test` is supported via `xunit.runner.visualstudio` (v3 adapter).
- Existing test intent is preserved; compatibility/environment-dependent tests are skipped explicitly with reason.

### Approved update flow

Use this only when a public API change is intentional and reviewed:

1. Run baseline update:
   - PowerShell: `$env:PROJNET_UPDATE_PUBLIC_API_BASELINE='1'; dotnet test .\test\ProjNet.Tests\ProjNET.Tests.csproj --filter PublicApiBaselineTests`
2. Inspect and review changes in `src/ProjNet/PublicAPI.Shipped.txt`.
3. Run full validation without the update variable:
   - `Remove-Item Env:PROJNET_UPDATE_PUBLIC_API_BASELINE -ErrorAction Ignore`
   - `dotnet test .\test\ProjNet.Tests\ProjNET.Tests.csproj --filter PublicApiBaselineTests`

## Target framework policy

`ProjNET` must continue to ship `netstandard2.0`.

Current approved targets are:

- `netstandard2.0` (required shipping target)
- `netstandard2.1` (additional target justified by runtime/API improvements while preserving broad compatibility through `netstandard2.0`)

Build policy is enforced in `src/ProjNet/ProjNET.csproj` via `ValidateTargetFrameworkPolicy`.

## Obsolete usage compatibility guidance

Obsolete APIs are allowed when all of the following are true:

- A replacement API is clearly identified in the obsolete message.
- Existing obsolete behavior remains functional for the deprecation window.
- Removal is deferred to a major version change.

This keeps migration paths explicit and non-breaking for existing consumers.

## Analyzer and StyleCop quality gates

- Style policy baseline follows StyleCop 6.2.0 conventions; SDK-style build enforcement is implemented via `StyleCop.Analyzers`.
- Roslyn analyzers are enabled solution-wide with maximum build strictness (`AnalysisLevel=latest`, `AnalysisMode=AllEnabledByDefault`, `WarningLevel=9999`, `EnforceCodeStyleInBuild=true`).
- Style rules are enforced with `StyleCop.Analyzers`.
- CI build does not downgrade warning level.
- Warning suppression changes require explicit approval.
