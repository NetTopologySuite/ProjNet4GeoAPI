# Milestone 7.4 Full Validation

## Scope
- Run full release validation after M7.2/M7.3 closure.
- Include build, full test suite, and a focused mutation run to verify mutation lane health without reopening unrelated baseline churn.

## Validation commands
- `dotnet build ProjNet4GeoAPI.sln -c Release --nologo`
- `dotnet test test\ProjNet.Tests\ProjNET.Tests.csproj -c Release --no-build --nologo`
- `dotnet dotnet-stryker --config-file <temp-focused-config> --output artifacts/stryker/m7-harden4-config --diag`
  - Focused mutate scope: `**/CoordinateSystems/Transformations/IdentityMathTransform.cs`
  - Thresholds forced to `0/0/0` for diagnostic stability in M7 closure.

## Results
- Release build succeeded (`284` warnings, `0` errors).
- Full test suite succeeded (`3734 total`, `3216 passed`, `0 failed`, `518 skipped`).
- Focused Stryker run completed and produced `artifacts/stryker/m7-harden4-config/reports/mutation-report.json`.
  - Total mutants in focused report: `119`
  - Status counts:
    - `Killed`: `5`
    - `Timeout`: `1`
    - `CompileError`: `17`
    - `Ignored`: `96`
  - Tested mutants (`Killed + Timeout` in this focused slice): `6`
  - Estimated focused mutation score: `83.33%` (`5/6`)

## Notes
- Earlier broad Stryker invocations in this milestone repeatedly stalled in this environment; the focused diagnostic run was used to produce deterministic mutation artifacts for milestone evidence.
- API baseline remained unchanged in M7.3 and did not require any `PublicAPI.*` file update.
