# Milestone 1 Step 1: Projection Coverage Audit (`parity-1-projection-audit`)

## Scope

This audit compares all `PROJ_HEAD(...)` identifiers from `spec\PROJ\src\pj_list.h` against:

- `src\ProjNet\CoordinateSystems\Projections\ProjectionsRegistry.cs`
- `src\ProjNet\CoordinateSystems\Transformations\ProjPipelineMathTransformFactory.cs`
- related runtime entry points in `CoordinateTransformationFactory.cs` and existing runtime tests.

## Method

1. Parsed all `PROJ_HEAD` identifiers from `pj_list.h`.
2. Parsed all projection aliases registered through `Register("...")` in `ProjectionsRegistry`.
3. Parsed all pipeline conversion dispatches via `projCode.Equals("...")` in `ProjPipelineMathTransformFactory.TryCreateStepTransform`.
4. Classified each `PROJ_HEAD` identifier into:
   - projection registry mapping,
   - runtime pipeline mapping,
   - unresolved.
5. Verified unresolved items against runtime code paths and test fixtures.

## Quantitative results

- `PROJ_HEAD` identifiers discovered: **186**
- Projection aliases registered in `ProjectionsRegistry`: **321**
- Runtime pipeline conversion dispatches: **23**
- Classified as projection-registry backed: **160**
- Classified as runtime-pipeline backed: **18**
- Initially unresolved after pure registry+pipeline lookup: **8**

### Initially unresolved set (after direct lookup)

- `affine`
- `cart`
- `geoc`
- `geocent`
- `geogoffset`
- `molobadekas`
- `pop`
- `push`

## Resolution analysis for unresolved items

### `affine`

**Status:** implemented, but not exposed through `+proj=affine` pipeline dispatch in current `ProjPipelineMathTransformFactory`.

**Evidence:**

- `AffineTransform` class exists and is active (`src\ProjNet\CoordinateSystems\Transformations\AffineTransform.cs`).
- WKT reader constructs affine transforms (`MathTransformWktReader.ReadAffineTransform`).
- Affine behavior is covered by existing tests (`CoordinateTransformTests`, `WKTMathTransformParserTests`).
- GIE fixtures contain `+proj=affine` cases and are routed through pipeline conversion tests.

### `cart` / `geocent`

**Status:** implemented through geocentric transform runtime paths and factory composition, not currently exposed as explicit `+proj=cart` / `+proj=geocent` pipeline dispatcher tokens in `TryCreateStepTransform`.

**Evidence:**

- Geocentric transform implementation exists (`GeocentricTransform.cs`).
- Coordinate operation composition uses geocentric conversion path in `CoordinateTransformationFactory.CreateCoordinateOperation(GeocentricCoordinateSystem)`.
- Test fixtures include geocentric/cartesian operation vectors (`builtins.gie`, `4D-API_cs2cs-style.gie`, `deformation.gie`).

### `push` / `pop`

**Status:** not currently dispatched in `ProjPipelineMathTransformFactory` as dedicated steps.

**Evidence:**

- No `"push"` or `"pop"` handling branch in `TryCreateStepTransform`.
- GIE fixtures contain push/pop steps; the test harness filters runtime-supported operations through `GieBuiltinsTheoryTests.TryIsRuntimeOperationSupported`.

### `geogoffset` / `molobadekas` / `geoc`

**Status:** not currently dispatched as explicit pipeline operations in `TryCreateStepTransform`.

**Evidence:**

- No matching `projCode.Equals("geogoffset" | "molobadekas" | "geoc")` branches in `ProjPipelineMathTransformFactory`.
- `Molodensky-Badekas` appears in EPSG metadata (`EpsgGeneratedCatalog.g.cs`) but not as runtime `+proj` pipeline dispatcher.

## Classification outcome

### Category A: Fully projection-registry mapped (`PROJ_HEAD` -> `ProjectionsRegistry`)

**Count:** 160

These are covered via `Register("...")` aliases and instantiate through projection classes.

### Category B: Runtime conversion mapped in pipeline factory

**Count:** 18

Includes runtime transform operations such as `axisswap`, `unitconvert`, `gridshift` family, `defmodel`, `deformation`, `tinshift`, `topocentric`, `vertoffset`, `helmert`, `molodensky`, `ob_tran`, `sch`, `set`, and `pipeline`.

### Category C: Special runtime/factory-handled but not direct pipeline token-dispatched

**Count:** 2 primary families (`affine`, `cart/geocent`) plus geocentric-latitude alias (`geoc` unresolved as token)

These are implemented through non-`TryCreateStepTransform` paths (WKT/factory geocentric composition) and exercised by existing tests.

### Category D: Currently unresolved as direct `+proj` dispatcher tokens

**Count:** 5 identifiers

- `push`
- `pop`
- `geogoffset`
- `molobadekas`
- `geoc`

## Gap rationale

The remaining unresolved identifiers are **not missing projection classes**; they are **runtime operation-dispatch gaps** in current pipeline step parsing. They should be tracked under transformation/pipeline milestones rather than projection-class registration.

## Conclusion

Projection coverage is broadly complete from a class/registry perspective, but direct `+proj` runtime token dispatch parity still has targeted gaps (`push`, `pop`, `geogoffset`, `molobadekas`, `geoc`) and partial exposure differences for `affine` and `cart`/`geocent`.

This is suitable for documenting in the projection coverage matrix with explicit distinction between:

- projection class registration parity,
- runtime transform dispatch parity,
- factory-only/runtime-only support.

