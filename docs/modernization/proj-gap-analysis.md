# PROJ Gap Analysis (Wave A)

## Scope
This matrix compares current `ProjNet4GeoAPI` implementation evidence with `PROJ` reference capabilities to drive modernization work.  
Legacy public API compatibility is mandatory: existing public members must stay present and functional.

## Feature matrix

| ID | Capability area | Current `ProjNet4GeoAPI` evidence | `PROJ` reference evidence | Gap summary | Priority | Release |
| --- | --- | --- | --- | --- | --- | --- |
| GA-01 | Public API compatibility governance | Compatibility and API retention are required by plan policy | N/A | Missing hard verification artifacts and regression checks in implementation flow | Must | R1 |
| GA-02 | Authority/registry-backed CRS discovery | Current flow is mostly in-memory and static | `proj.db` model and database query APIs in `spec\PROJ\src\proj.h` | Missing managed, data-driven authority model | Must | R1 |
| GA-03 | CRS to CRS operation resolution | Static/type-based transformation wiring in factory paths | `proj_create_crs_to_crs` style resolution in `PROJ` | Missing ranked candidate resolution model | Must | R1 |
| GA-04 | Pipeline/chained operations | Limited composition in current code paths | PROJ transformation pipeline model | Missing generalized operation pipeline abstraction | Must | R1 |
| GA-05 | Projection breadth | Limited registry compared to PROJ surface | PROJ supports many projection methods | Coverage gap for projection kernel breadth | Should | R2 |
| GA-06 | Modern WKT (WKT2) | Legacy-heavy WKT paths | Modern WKT support in PROJ APIs and docs | Missing incremental WKT2 parsing/export strategy | Must | R1/R2 |
| GA-07 | PROJJSON support | No clear PROJJSON path | `proj_as_projjson` and PROJJSON spec | Missing modern JSON exchange support | Should | R2 |
| GA-08 | Grid/resource management | No dedicated managed resolver subsystem | PROJ resource model (local data + optional network) | Missing deterministic managed resource/grid strategy | Must | R1/R2 |

## Initial release split

### Release 1 (foundation)
- GA-01: API compatibility baseline + regression coverage.
- GA-02: Managed authority/registry abstraction without runtime SQLite/native dependency.
- GA-03: Internal resolver behind existing public API.
- GA-04: Pipeline support for prioritized scenarios.
- GA-06 (partial): high-value WKT2 read support.
- GA-08 (partial): deterministic local grid/resource behavior.

### Release 2 (expansion)
- GA-05: broader projection coverage.
- GA-06 (expanded): broader WKT2 parse/export fidelity.
- GA-07: PROJJSON support.
- GA-08 (advanced): optional network acquisition/caching model.

## Notes
- Priorities are aligned with non-breaking modernization.
- Any compatibility exception must be approved and traceable.
