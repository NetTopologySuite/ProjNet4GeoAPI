# Format Support Upgrade Baseline

## Implemented in this step
- Added incremental WKT2-oriented normalization in `CoordinateSystemWktReader`:
  - `PROJECTEDCRS` -> `PROJCS`
  - `GEODCRS` / `GEODETICCRS` -> `GEOGCS`
  - `BASEGEODCRS` / `BASEGEOGCRS` -> `GEOGCS`
  - `ELLIPSOID` -> `SPHEROID`
  - `ID["..."]` -> `AUTHORITY["..."]` (token-safe replacement)
- Added regression tests in `test\ProjNet.Tests\WKT\WKTCoordSysParserTests.cs` for WKT2-like roots and identifiers.

## Compatibility
- Existing WKT1 parsing behavior remains intact.
- Public API surface remains unchanged.
- Changes are internal parser normalization only (additive compatibility behavior).

## Next increment
- Add dedicated WKT2 conversion/mapping layer for `CONVERSION`, `METHOD`, and richer metadata nodes.
- Expand test corpus with pinned WKT2 cases from parity matrix priorities.
