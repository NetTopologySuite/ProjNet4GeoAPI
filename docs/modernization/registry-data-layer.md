# Registry Data Layer Baseline

## Implemented in this step
- Extended `CoordinateSystemServices` with explicit registry lookup APIs:
  - `TryGetCoordinateSystem(int srid, out CoordinateSystem coordinateSystem)`
  - `TryGetCoordinateSystem(string authority, long code, out CoordinateSystem coordinateSystem)`
  - `GetAvailableSridValues()`
- Added tests in `CoordinateSystemServicesTest` for:
  - lookup by SRID,
  - lookup by authority/code,
  - available SRID list coverage for default managed definitions.

## Why this matters
- Moves registry access from implicit exception/null patterns to explicit `TryGet` APIs.
- Provides a clear managed lookup layer for future resolver/grid integration steps.
- Keeps existing public API behavior intact while adding non-breaking capabilities.
