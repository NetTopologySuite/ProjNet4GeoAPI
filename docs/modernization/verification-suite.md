# Verification Suite Baseline

## Implemented in this step
- Added `VerificationSuiteTests` with deterministic reference-point coverage:
  - EPSG:4326 -> EPSG:3857 reference vectors,
  - EPSG:3857 -> EPSG:4326 inverse vectors.
- Added compatibility regression test for legacy `CoordinateSystemServices` lookup APIs:
  - `GetCoordinateSystem(int)`,
  - `GetCoordinateSystem(string,long)`,
  - `TryGetCoordinateSystem(string,long,...)`,
  - `GetSRID(string,long)`.
- Added direct `proj2proj` parity theory suite backed by committed PROJ-derived fixtures:
  - `Proj2ProjParityTheoryTests` (all parity cases run unconditionally).

## Why this matters
- Provides a stable, low-noise regression anchor for core transform correctness.
- Adds explicit guardrails for legacy API behavior continuity during modernization.

## Next increment
- Continue expanding fixture breadth with additional EPSG projected pairs and grid-backed scenarios as grid assets become available.
