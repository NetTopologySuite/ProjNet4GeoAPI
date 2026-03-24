# Migration and Release Notes Baseline

## Compatibility contract
- Existing public API remains available and functional.
- New capabilities are additive.
- Breaking removals are out of scope for this modernization phase.

## User-facing migration guidance
- Existing `CoordinateSystemServices`, `CoordinateSystemFactory`, and `CoordinateTransformationFactory` usage remains valid.
- New optional APIs added during modernization:
  - `CoordinateSystemServices.TryGetCoordinateSystem(int, out CoordinateSystem)`
  - `CoordinateSystemServices.TryGetCoordinateSystem(string, long, out CoordinateSystem)`
  - `CoordinateSystemServices.GetAvailableSridValues()`
- Recommended migration pattern:
  - prefer `TryGet...` over null/exception-driven lookup logic in new code.

## Versioning and release communication
- Use SemVer-compatible release notes:
  - **Added**: additive APIs/behaviors,
  - **Changed**: compatible behavior refinements,
  - **Deprecated**: obsolete annotations and replacements,
  - **Removed**: only with explicit major-version decision record.
- For this baseline, there are no newly obsolete public members.

## Reproducible update operations
- Test baseline:
  - `dotnet build .\\ProjNet4GeoAPI.sln -c Release -v minimal`
  - `dotnet test .\\ProjNet4GeoAPI.sln -c Release --no-build -v minimal`
- Mutation baseline:
  - `dotnet tool restore`
  - `dotnet dotnet-stryker --config-file stryker-config.json --output artifacts/stryker`
- Benchmark baseline:
  - `dotnet build .\\src\\ProjNet.Benchmark\\ProjNet.Benchmark.csproj -c Release`
  - `dotnet run -c Release --project src\\ProjNet.Benchmark\\ProjNet.Benchmark.csproj -- --filter *ProjParityBenchmarks*`

## Current release hardening snapshot
- Version line is prepared for `3.0.0` (`src/Directory.Build.props`).
- `PackageValidationBaselineVersion` is pinned to `2.1.0` until `3.0.0` is published.
- Current validation profile (latest milestone closure):
  - `Build`: success with warning baseline
  - `Tests`: `3734 total`, `3216 passed`, `0 failed`, `518 skipped`
