# Milestone 7.3 API Baseline

## Scope
- Re-validate the public API baseline after the M7 warning-remediation changes.
- Regenerate baseline files only if the public API test detects drift.

## Commands
- `dotnet test test\ProjNet.Tests\ProjNET.Tests.csproj -c Release --filter "FullyQualifiedName~PublicApiBaselineTests" --nologo`
- `PROJNET_UPDATE_PUBLIC_API_BASELINE=1 dotnet test test\ProjNet.Tests\ProjNET.Tests.csproj -c Release --filter "FullyQualifiedName~PublicApiBaselineTests" --nologo`

## Results
- Baseline validation run passed (`1` test, `0` failed).
- Baseline update-mode run passed (`1` test, `0` failed).
- No changes were produced in:
  - `src\ProjNet\PublicAPI.Shipped.txt`
  - `src\ProjNet\PublicAPI.Unshipped.txt`

## Notes
- Public API remained stable through M7.2; no baseline file updates were required.
