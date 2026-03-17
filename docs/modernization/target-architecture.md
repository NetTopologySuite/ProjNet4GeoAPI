# Target Architecture (Compatibility-First)

## Goals
- Keep the existing public API fully present and functional.
- Move internal behavior to a data-driven architecture aligned with modern PROJ concepts.
- Keep implementation fully managed C# with no native/runtime SQLite dependency.

## Module boundaries

| Module | Responsibility | Public API impact |
| --- | --- | --- |
| `ProjNet.IO` | Parse/serialize WKT (and future PROJJSON) into internal models | None (existing APIs retained) |
| `ProjNet.Registry` (new internal area) | Authority data access, CRS/operation metadata lookup, versioned data assets | None |
| `ProjNet.Resolution` (new internal area) | CRS->CRS operation candidate building, ranking, and selection | None |
| `ProjNet.Transformations` | Math transform execution, composition, inverse behavior | Existing API remains; internals can be replaced |
| `ProjNet.Resources` (new internal area) | Grid/resource location, deterministic offline behavior, optional network lane | None by default |
| `ProjNet.Compatibility` (new internal area) | Legacy behavior guards and compatibility assertions | None |

## Compatibility strategy
- `CoordinateSystemFactory`, `CoordinateTransformationFactory`, and `CoordinateSystemServices` remain primary public entry points.
- Internal resolver and registry services are introduced behind existing public API surfaces.
- New API additions are additive only; legacy members may be marked obsolete with replacement guidance, but remain functional.

## Error and diagnostics model
- No silent fallback behavior in new internal services.
- Normalize failures into explicit categories:
  - `InputValidation` (invalid CRS/operation definitions),
  - `DataUnavailable` (missing authority/grid/resource data),
  - `ResolutionFailure` (no valid operation path),
  - `ExecutionFailure` (runtime transform failure).
- Keep externally observable behavior backward-compatible; stricter diagnostics are additive.

## Data and resource architecture
- Authority/reference data is generated at build time into managed assets.
- Runtime reads managed assets only.
- Resource subsystem supports:
  - deterministic local-only mode (default),
  - optional network-enabled mode for extended datasets.

## Migration sequence
1. Introduce registry abstraction and managed data asset loader.
2. Introduce operation resolver and route existing factory calls through it.
3. Introduce resource/grid provider abstraction with deterministic defaults.
4. Extend parser/serializer path for modern formats (incremental WKT2, then PROJJSON).
5. Expand parity and benchmark suites while preserving public API behavior.

## Definition of done for this architecture step
- Module boundaries are documented and accepted.
- Compatibility-first rules are explicit and testable.
- Migration sequence is concrete enough to implement incrementally.
