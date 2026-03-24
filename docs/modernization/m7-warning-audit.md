# Milestone 7 Warning Audit

## Scope and command
- Command: `dotnet build ProjNet4GeoAPI.sln -c Release --tl:off -v minimal -clp:WarningsOnly`
- Source log: `%TEMP%\m7-warning-audit.log`

## Baseline snapshot
- Total warnings: `685`
- By area:
  - `src`: `441`
  - `test`: `242`
  - `other`: `2`

## Top warning codes in `src`
- `SA1117`: `144`
- `CA1859`: `39`
- `CA1510`: `38`
- `SA1204`: `30`
- `SA1201`: `28`
- `SA1407`: `21`
- `SA1611`: `18`
- `SA1602`: `18`
- `SA1101`: `15`
- `SA1202`: `10`
- `SA1312`: `9`
- `SA1615`: `9`
- `SA1600`: `9`
- `CA2235`: `6`

## Top warning codes in `test`
- `SA1611`: `149`
- `SA1518`: `49`
- `SA1210`: `10`
- `CA1062`: `8`
- `CA1308`: `6`

## Hotspot files (`src`)
- `CoordinateSystems\Projections\AiroceanProjection.cs`: `150`
- `CoordinateSystems\Transformations\DefModelMathTransform.cs`: `35`
- `CoordinateSystems\Transformations\GeoTiffXyzGridShiftMathTransform.cs`: `32`
- `CoordinateSystems\Projections\IseaProjection.cs`: `24`
- `CoordinateSystems\Projections\SpaceObliqueMercatorProjection.cs`: `22`

## Categorization

### Fixable now (M7.2 target)
- Style/order/formatting warnings: `SA1117`, `SA1201`, `SA1202`, `SA1204`, `SA1312`, `SA1514`, `SA1515`, `SA1516`, `SA1508`, `SA1518`.
- Readability/safety warnings with mechanical fixes: `CA1510`, `SA1407`, `SA1101`.
- Documentation completeness in touched files: `SA1600`, `SA1602`, `SA1611`, `SA1615`.
- Low-risk analyzer fixes: `CA1861`, selected `CA1062`, selected xUnit analyzer warnings.

### Legacy-acceptable / needs targeted suppression or deferred refactor
- `CA1859` (performance preference that can conflict with abstraction boundaries and API design intent).
- `CA2235` / `SYSLIB0051` (legacy serialization compatibility paths).
- `CA1031`, `CA2214`, `CS7095` (behavior-sensitive refactors; not safe for bulk mechanical edits).
- Globalization analyzers where behavior could change without domain review: `CA1305`, `CA1307`, `CA1308`.

## Execution plan for M7.2
1. Reduce hotspot style warnings first (`AiroceanProjection`, `DefModelMathTransform`, `GeoTiffXyzGridShiftMathTransform`, `IseaProjection`, `SpaceObliqueMercatorProjection`).
2. Apply mechanical safety/readability fixes (`CA1510`, `SA1407`, `SA1101`) in source files.
3. Resolve documentation warnings in touched files.
4. Rebuild and re-audit; only then add targeted suppressions for warnings classified as legacy-acceptable.
