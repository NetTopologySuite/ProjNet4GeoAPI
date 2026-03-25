# Milestone 17 — CA1859 Warning Reduction

## Scope
Targeted reduction of high-volume `CA1859` warnings in transformation runtime helpers using behavior-safe type narrowing on private methods.

## Changes
- Updated selected private helper method signatures from `IReadOnlyDictionary<string, string>` to `Dictionary<string, string>` where all call sites already pass concrete dictionaries.
- Updated one private grid-search helper in deformation runtime from `IReadOnlyList<T>` to `ReadOnlyCollection<T>` where the owning field type is already `ReadOnlyCollection<T>`.
- No public API changes.

## Validation
- Build + focused tests:
  - `dotnet build ProjNet4GeoAPI.sln -c Release --nologo`
  - `dotnet test test\ProjNet.Tests\ProjNET.Tests.csproj -c Release --no-build --nologo --filter "FullyQualifiedName~HornerMathTransform|FullyQualifiedName~Molodensky|FullyQualifiedName~Topocentric|FullyQualifiedName~VertOffset|FullyQualifiedName~Deformation|FullyQualifiedName~DefModel"`
  - Result: build succeeded (`245` warnings, `0` errors in focused build); filtered tests passed (`72/72`).
- Full regression validation:
  - `dotnet build ProjNet4GeoAPI.sln -c Release -t:Rebuild --nologo -v minimal -flp:"logfile=artifacts\m17-ca1859-rebuild-warnings.log;warningsonly;verbosity=normal"`
  - `dotnet test ProjNet4GeoAPI.sln -c Release --no-build --nologo`
  - Result: build succeeded with `301` warnings, `0` errors; tests succeeded (`3768 total`, `3250 passed`, `0 failed`, `518 skipped`).

## Warning delta
- Prior full-build warning baseline: `340`.
- Post-change full-build warning baseline: `301`.
- Net delta: `-39` warnings.
- `CA1859` in validated full-build output: `0`.

## Notes
- This step intentionally limits changes to private helper signatures and preserves runtime behavior.
- Remaining top warning families now include `CA1510`, `SA1201`, `SA1204`, `SA1515`, and `SA1407` for follow-up steps.
