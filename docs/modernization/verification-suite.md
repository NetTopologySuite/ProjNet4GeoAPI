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

## Why this matters
- Provides a stable, low-noise regression anchor for core transform correctness.
- Adds explicit guardrails for legacy API behavior continuity during modernization.

## Next increment
- Expand suite with additional EPSG operation classes and area-of-use-sensitive cases.
- Add golden reference datasets sourced from prioritized parity matrix scenarios.
