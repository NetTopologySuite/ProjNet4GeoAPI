# Milestone 17 — CA1510 Warning Reduction

## Scope
Reduce `CA1510` warnings by normalizing null-argument guard patterns to target-aware throw helper usage.

## Changes
- Added `src/ProjNet/ArgumentGuard.cs` with a multi-target-safe `ThrowIfNull` helper:
  - `net8.0`: uses `ArgumentNullException.ThrowIfNull`.
  - `netstandard2.0` / `netstandard2.1`: preserves explicit `ArgumentNullException`.
- Replaced explicit `throw new ArgumentNullException(nameof(...))` guard blocks in affected files with `ArgumentGuard.ThrowIfNull(...)`.
- Replaced remaining null-coalescing throw expressions (`?? throw new ArgumentNullException(...)`) with explicit `ArgumentGuard.ThrowIfNull(...)` + assignment in constructors/factory paths.
- Kept span/whitespace parser semantics unchanged where `ReadOnlySpan<char>` validation is expected by tests.

## Validation
- Full rebuild with warning log:
  - `dotnet build ProjNet4GeoAPI.sln -c Release -t:Rebuild --nologo -v minimal -flp:"logfile=artifacts\m17-ca1510-rebuild-warnings.log;warningsonly;verbosity=normal"`
- Full test suite:
  - `dotnet test ProjNet4GeoAPI.sln -c Release --no-build --nologo`

## Results
- Build succeeded with `276` warnings and `0` errors.
- Tests succeeded (`3768 total`, `3250 passed`, `0 failed`, `518 skipped`).

## Warning delta
- Before CA1510 wave: `301` warnings; `CA1510=37`.
- After CA1510 wave: `276` warnings; `CA1510=0`.
- Net delta: `-25` total warnings, `-37` CA1510 occurrences.
- `CA1859` remains at `0` in the validated output.
