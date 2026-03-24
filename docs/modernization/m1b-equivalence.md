# Milestone 1b Equivalence Verification (`gen-3-verify-output-equivalence`)

## Scope

Compared the legacy generator path (SQLite `proj.db` + WKT operations) with the new WKT-only path for structural catalog output.

The legacy baseline was evaluated from commit `20ffb124f4db3ebcda19acc2fe3f9f0ca34f9ea3`.

## Validation commands

```powershell
python tools\generate_epsg_catalog.py --zip <EPSG_ZIP> --output <tmp_output>
powershell -ExecutionPolicy Bypass -File tools\Generate-EpsgManagedData.ps1 -ZipPath <EPSG_ZIP> -OutputPath <generated_file>
dotnet build ProjNet4GeoAPI.sln -c Release -nologo
dotnet test test\ProjNet.Tests\ProjNET.Tests.csproj -c Release --no-build -nologo
```

## Key result

The operation extraction path is **fully equivalent**:

- `operations_equal = True`
- `operation_parameters_equal = True`
- `explicit_operations_equal = True`
- Counts remain unchanged: `2909` operations, `13034` operation parameters, `1313` explicit operations.

## Structural CRS catalog comparison

### Source dataset counts (pre-filter)

- Units: old `85`, new `20`
- Coordinate systems: old `139`, new `96`
- Axes-by-CS: old `139`, new `96`
- Ellipsoids: old `55`, new `44`
- Prime meridians: old `15`, new `14`
- Geodetic datums: old `697`, new `624`
- Vertical datums: old `267`, new `262`
- Conversions: old `2741`, new `2450`
- Geodetic CRS: old `1306`, new `1152`
- Projected CRS: old `5762`, new `5398`
- Vertical CRS: old `301`, new `263`
- Compound CRS: old `493`, new `483`

### Built catalog counts (post-filter)

- Reference records: old `7563`, new `7181`
- Geographic records: old `714`, new `900`
- Geocentric records: old `294`, new `252`
- Projected records: old `5761`, new `5398`
- Vertical records: old `301`, new `263`
- Compound records: old `493`, new `368`

### SRID set delta

- Old-only SRIDs: `648`
- New-only SRIDs: `266`

Classification:

- **Old-only absent from new source (`535`)**: these SRIDs are not present as supported CRS WKT entries in the EPSG archive and therefore cannot be reconstructed from the selected WKT-only source.
- **Old-only present in new source but filtered (`113`)**: all are `COMPOUNDCRS` where the vertical component is not in the new supported reference set (`missing_vertical` only).
- **New-only (`266`)**: all already exist in old source data but were previously filtered; they are predominantly geodetic entries previously typed as `geographic 3d` and now admitted as geographic records through WKT-based normalization.

## Interpreted differences

1. The new WKT-only path intentionally tracks the EPSG WKT publication more strictly, which changes coverage composition.
2. Legacy SQLite-derived metadata included additional records that do not map to parseable CRS WKT definitions in the archive.
3. WKT-only classification admits additional geodetic entries that were previously excluded under the old `geographic 2d`/`geocentric` gate.

## Data quality guard added

The parser now skips non-WKT placeholder files (example files such as `EPSG-CRS-5614.wkt` contain plain text: "WKT is not supported..."), avoiding false parse failures.

## Follow-up actions

- Continue with generator optimization milestones (`gen-4+`) to reduce runtime initialization cost and generated code size.
- Evaluate introducing generated `record struct` output (with `IsExternalInit` polyfill where required for older TFMs) as part of upcoming codegen modernization.
