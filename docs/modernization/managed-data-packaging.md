# Managed Data Packaging Baseline

## Implemented baseline
- Default CRS definitions are now sourced via a managed provider abstraction:
  - `src\ProjNet\Data\ICoordinateSystemDefinitionProvider.cs`
  - `src\ProjNet\Data\ManagedCoordinateSystemDefinitionProvider.cs`
- `CoordinateSystemServices` now supports provider-based initialization while preserving existing constructors.
- Runtime remains fully managed and has no SQLite/native dependency path.

## Current packaged definitions
- SRID `4326` from `GeographicCoordinateSystem.WGS84.WKT`
- SRID `3857` from `ProjectedCoordinateSystem.WebMercator.WKT`

## Next increment
- Replace baseline provider content with generated managed assets from a pinned source dataset.
- Add deterministic generation metadata (input version/checksum/tool version).
- Extend provider coverage based on parity-matrix priorities.
