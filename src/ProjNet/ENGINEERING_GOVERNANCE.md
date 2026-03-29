# Engineering governance

This document defines the active engineering and quality gates for `ProjNET`.

## Public API baseline policy

`src/ProjNet/PublicAPI.Shipped.txt` is the canonical public API baseline for the main `ProjNET` library.

- Verification runs in `test/ProjNet.Tests/PublicApiBaselineTests.cs`.
- The baseline gate must stay green in regular validation.
- Intentional API surface changes must update shipped/unshipped baselines in a reviewed commit.

### Approved baseline update flow

Use this only when a public API change is intentional and approved:

1. Run baseline update:
   - PowerShell:  
     `$env:PROJNET_UPDATE_PUBLIC_API_BASELINE='1'; dotnet test .\test\ProjNet.Tests\ProjNET.Tests.csproj --filter PublicApiBaselineTests`
2. Inspect and review changes in:
   - `src/ProjNet/PublicAPI.Shipped.txt`
   - `src/ProjNet/PublicAPI.Unshipped.txt`
3. Re-run without update variable:
   - `Remove-Item Env:PROJNET_UPDATE_PUBLIC_API_BASELINE -ErrorAction Ignore`
   - `dotnet test .\test\ProjNet.Tests\ProjNET.Tests.csproj --filter PublicApiBaselineTests`

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
  - `dotnet test .\test\ProjNet.Tests\ProjNET.Tests.csproj --tl:off -v minimal`

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
- Modernization and milestone records are maintained in `docs/modernization/`.

## Licensing and attribution policy

- Project-level licensing and attribution references are maintained in:
  - `LICENSES/`
  - `NOTICE.md`
- Source files use SPDX-style headers with provenance-specific attribution.

## Deprecation and compatibility policy

- Compatibility-first is the default: avoid breaking removals in active modernization waves.
- Obsolete APIs are acceptable when:
  - replacement guidance is explicit,
  - behavior remains functional during deprecation window,
  - removals are deferred to a major-version decision.
