# Test Parity Matrix (.NET vs PROJ Reference)

## Scope and rule context
- Scope: `test\ProjNet.Tests` (current .NET test evidence) mapped to `spec\PROJ\test\*` and related PROJ docs references.
- Non-breaking rule: Public API compatibility remains mandatory during modernization.

## Parity matrix

| .NET test area/file | PROJ reference theme/source location | Status | Priority | Planned action |
| --- | --- | --- | --- | --- |
| `test\ProjNet.Tests\PublicApiBaselineTests.cs` | C/API contract and usage stability checks in `spec\PROJ\test\unit\test_c_api.cpp`, `spec\PROJ\test\unit\include_proj_h_from_c.c` | partial | P0 | Keep baseline gate; add modernization checks ensuring new internals do not alter shipped public API signatures or behavior contracts. |
| `test\ProjNet.Tests\CoordinateSystemServicesTest.cs` + `SRIDReader.cs` | Authority/database-driven CRS lookup in `spec\PROJ\test\unit\test_factory.cpp`, `spec\PROJ\test\cli\test_projinfo.yaml` | partial | P0 | Add deterministic authority-resolution tests (code-space + code, failure paths, aliasing, caching behavior) aligned with PROJ factory patterns. |
| `test\ProjNet.Tests\WKT\WKTCoordSysParserTests.cs` | WKT parser robustness/normalization in `spec\PROJ\test\unit\test_io.cpp`, CRS metadata consistency in `spec\PROJ\test\unit\test_crs.cpp` | covered | P1 | Keep broad WKT parse/roundtrip corpus; add negative grammar/error-class assertions mirroring PROJ parser strictness patterns. |
| `test\ProjNet.Tests\WKT\WKTMathTransformParserTests.cs` + affine checks in `CoordinateTransformTests.cs` | Math transform parsing and expression handling in `spec\PROJ\test\unit\test_io.cpp`; operation chaining concept in `spec\PROJ\docs\source\operations\pipeline.rst` | partial | P1 | Add matrix of malformed/exponent/extreme-value affine inputs and explicit inverse-stability checks on chained transforms. |
| `test\ProjNet.Tests\CoordinateTransformTests.cs` (projection coverage + expected numeric assertions) | Projection/transform numerical verification in `spec\PROJ\test\cli\test_proj.yaml`, `spec\PROJ\test\cli\test_invproj.yaml`, `spec\PROJ\test\gie\builtins.gie` | partial | P0 | Build a small imported canonical vector set (from PROJ CLI/GIE cases) for reproducible forward/inverse parity on high-value projections. |
| `test\ProjNet.Tests\CoordinateTransformTests.cs` (`TestDatumTransform`, `TestGeocentric`, prime meridian/unit tests) | Datum/geocentric/unit/PM behavior in `spec\PROJ\test\cli\test_cs2cs_various.yaml`, `spec\PROJ\test\gie\unitconvert.gie`, `spec\PROJ\test\unit\test_operationfactory.cpp` | partial | P0 | Expand to table-driven datum/PM/unit cases with explicit forward+inverse tolerances and coordinate-order assertions. |
| `test\ProjNet.Tests\GitHub\Issues.cs`, `ProjNetIssues.cs`, `SharpMapIssues.cs` | Regression-style operation/inverse integrity and self-tests in `spec\PROJ\test\unit\gie_self_tests.cpp`, operation tests in `spec\PROJ\test\unit\test_operation.cpp` | covered | P1 | Keep issue-regression suite; tag by capability and link each case to a parity theme for traceability. |
| `test\ProjNet.Tests\WKT\PostGisSpatialRefSysTableParserTest.cs` | Large CRS catalog parsing and authority consistency in `spec\PROJ\test\unit\test_factory.cpp`, `spec\PROJ\test\cli\test_projinfo.yaml` | partial | P2 | Replace DB-dependent variability with pinned fixture snapshots; keep optional live PostGIS check as non-blocking. |
| `(no equivalent in test\ProjNet.Tests)` for grid shifts/network grids/TIN/deformation | Grid/network/TIN/deformation coverage in `spec\PROJ\test\unit\test_grids.cpp`, `spec\PROJ\test\unit\test_network.cpp`, `spec\PROJ\test\unit\test_tinshift.cpp`, `spec\PROJ\test\gie\gridshift.gie`, `spec\PROJ\test\gie\deformation.gie`, `spec\PROJ\test\gie\tinshift.gie` | missing | P0 | Add phased tests: (1) local grid fixtures, (2) deterministic no-network mode, (3) optional network-enabled integration lane, (4) deformation/TIN parity vectors. |
| `(no equivalent in test\ProjNet.Tests)` for ranked operation selection and area-of-use candidate ordering | Candidate ranking/context behavior in `spec\PROJ\test\unit\test_operationfactory.cpp` | missing | P0 | Introduce operation-selection tests asserting candidate count/order/accuracy metadata where available, not only single transform success. |
| `(no equivalent in test\ProjNet.Tests)` for PROJJSON interoperability | PROJJSON spec and schema reference `spec\PROJ\docs\source\specifications\projjson.rst` (+ IO tests in `spec\PROJ\test\unit\test_io.cpp`) | missing | P1 | Add roundtrip/import-export tests for PROJJSON equivalents of high-value CRS/operations, with compatibility-safe API surface additions only. |

## Tolerance strategy (explicit)

### Forward checks
- Use authoritative expected outputs from pinned parity vectors (seeded from PROJ CLI/GIE cases).
- Apply metric-specific absolute tolerances:
  - angular outputs (deg): strict small epsilon,
  - projected outputs (m/ft): scenario-based thresholds,
  - vertical components: separate threshold from horizontal.
- Keep tolerance constants centralized (single helper) and referenced by test category.

### Inverse checks
- For every forward assertion, require inverse/round-trip assertion unless mathematically undefined for the case.
- Evaluate `source -> target -> source` with independently defined reverse tolerance (can be looser than forward when justified).
- For 3D tests, validate XY and Z independently to avoid masked failures.

## Compatibility constraints (non-breaking API)

- Public API surface must remain non-breaking; baseline gate stays mandatory (`PublicApiBaselineTests.cs` + `src\ProjNet\PublicAPI.Shipped.txt`).
- New parity tests must prefer existing public entry points (`CoordinateSystemFactory`, `CoordinateTransformationFactory`, `CoordinateSystemServices`) over internal-only hooks.
- Behavior changes that tighten correctness must be introduced without removing existing public members; compatibility-impacting changes require explicit opt-in/versioned path.
- Optional external dependencies (network/database) must not destabilize default CI lanes; provide deterministic offline path first.
