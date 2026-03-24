# Milestone 1b Checkpoint: Generator Consolidation

Milestone 1b is complete.

## Outcome

- EPSG catalog generation now uses only the EPSG WKT ZIP source (no SQLite `proj.db` dependency).
- Large generated runtime arrays were replaced with on-demand switch-based accessors for:
  - CRS records
  - Conversions and conversion parameters
  - Explicit operation parameters
- Generated catalog output is now split into focused partial files:
  - `EpsgGeneratedCatalog.g.cs` (core)
  - `EpsgGeneratedCatalog.Types.g.cs`
  - `EpsgGeneratedCatalog.Projected.g.cs`
  - `EpsgGeneratedCatalog.Conversions.g.cs`
  - `EpsgGeneratedCatalog.Operations.g.cs`

## Validation

Final validation on this checkpoint:

- `dotnet build ProjNet4GeoAPI.sln -v minimal`
  - succeeded with existing baseline warnings (`571`)
- `dotnet test test\ProjNet.Tests\ProjNET.Tests.csproj -v minimal --no-build`
  - total: `3734`
  - passed: `3216`
  - failed: `0`
  - skipped: `518`

Additional generator/runtime checks were added in `StructuredEpsgCatalogTests` to verify:

- removed eager arrays (`Conversions`, `ConversionParameters`, `ExplicitOperations`) stay removed
- switch-based conversion lookup paths remain functional
- projected CRS conversion lookups are consistent across the full generated catalog
