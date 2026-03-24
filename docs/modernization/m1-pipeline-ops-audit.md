# Milestone 1 Step 3: Pipeline Dispatch Audit (`parity-3-pipeline-ops-audit`)

## Scope

Audit target:

- `src\ProjNet\CoordinateSystems\Transformations\ProjPipelineMathTransformFactory.cs`

Reference context:

- PROJ pipeline semantics in `spec\PROJ\docs\source\operations\pipeline.rst`
- `PROJ_HEAD` identifiers from `spec\PROJ\src\pj_list.h`

Goal:

- verify which `+proj=` step tokens are dispatched in .NET,
- identify missing dispatch entries relevant to transformation/pipeline parity.

## Dispatch surface found in .NET

`ProjPipelineMathTransformFactory.TryCreateStepTransform(...)` directly dispatches **23** `+proj` tokens:

- `latlong`, `longlat`, `latlon`, `lonlat`, `noop`
- `set`
- `axisswap`
- `unitconvert`
- `hgridshift`, `gridshift`
- `vgridshift`
- `xyzgridshift`
- `defmodel`
- `deformation`
- `tinshift`
- `topocentric`
- `vertoffset`
- `helmert`
- `molodensky`
- `horner`
- `ob_tran`
- `sch`, `spherical_cross_track_height`

Of those, **22** match `PROJ_HEAD` identifiers; `spherical_cross_track_height` is an additional alias for `sch`.

## Pipeline-document parity observations

`pipeline.rst` examples and rules explicitly exercise or mention:

- `+proj=pipeline` with `+step` semantics,
- `axisswap`,
- `unitconvert`,
- `cart`,
- `helmert`,
- `hgridshift`,
- `vgridshift`,
- `push`,
- `pop`.

### Supported from this documented set

- `axisswap` ✅
- `unitconvert` ✅
- `helmert` ✅
- `hgridshift` ✅
- `vgridshift` ✅
- `proj=pipeline` container parsing via `TrySplitPipelineSteps(...)` ✅

### Missing direct dispatch from this documented set

- `cart` ❌
- `push` ❌
- `pop` ❌

## Focus-gap verification (from milestone requirements)

Direct `projCode.Equals("...")` dispatch entries are missing for:

- `affine`
- `cart`
- `geocent`
- `geoc`
- `geogoffset`
- `molobadekas`
- `push`
- `pop`

## Runtime-path nuance

Some missing tokens are still represented by other runtime/factory paths:

- `affine`: implemented as `AffineTransform` and used in WKT transform parsing.
- `cart`/`geocent`: geocentric conversion capability exists via `GeocentricTransform` and `CoordinateTransformationFactory` composition.

However, these capabilities are not currently exposed as direct `+proj=...` step dispatches in `TryCreateStepTransform(...)`.

## Conclusion

Pipeline parsing and dispatch are functional for the currently implemented conversion set, but direct token parity with PROJ pipeline usage is incomplete.

The high-impact missing dispatch entries are:

1. `push` / `pop` (pipeline stack mechanics),
2. `cart` / `geocent` / `geoc` (explicit geodetic-cartesian/geocentric step parity),
3. `affine`, `geogoffset`, `molobadekas` (explicit runtime step aliases not currently dispatched).

These gaps should remain tracked as runtime pipeline-parity follow-up work.

