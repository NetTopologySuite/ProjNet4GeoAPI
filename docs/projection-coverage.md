# Projection Coverage Matrix (C++ PROJ vs ProjNet)

This document tracks the projection feature-parity status between `spec\PROJ` (C++ reference) and `src\ProjNet` (.NET implementation).

## Scope

- C++ source of truth: `spec\PROJ\src\projections\*.cpp` (`PROJ_HEAD(...)` projection codes).
- .NET implementation source: `src\ProjNet\CoordinateSystems\Projections\*.cs` plus `ProjectionsRegistry.cs`.
- Status categories:
  - **Implemented**: projection class exists and aliases are registered in `ProjectionsRegistry`.
  - **Missing**: no registered .NET projection mapping yet.

## Current summary

- C++ `PROJ_HEAD` identifiers discovered: **186**.
- Projection aliases registered in `ProjectionsRegistry`: **321**.
- Runtime pipeline conversion dispatches (`ProjPipelineMathTransformFactory`): **23**.
- Classified as projection-registry backed: **160**.
- Classified as runtime-pipeline backed: **18**.
- Initially unresolved after direct registry+pipeline lookup: **8** (`affine`, `cart`, `geoc`, `geocent`, `geogoffset`, `molobadekas`, `pop`, `push`).
- Refined resolution:
  - implemented via non-dispatch runtime/factory paths: `affine`, `cart`, `geocent`
  - direct `+proj` dispatcher gaps: `push`, `pop`, `geogoffset`, `molobadekas`, `geoc`
- Detailed audit record: `docs/modernization/m1-projection-audit.md`.
- Pipeline dispatch audit record: `docs/modernization/m1-pipeline-ops-audit.md`.
- Baseline validation at audit time:
  - `dotnet build ProjNet4GeoAPI.sln -c Release` succeeded with 570 warnings (existing baseline),
  - `dotnet test test/ProjNet.Tests/ProjNET.Tests.csproj -c Release --no-build` succeeded (`3731 total / 3213 passed / 518 skipped / 0 failed`).

## Implemented projection families in ProjNet

| Projection family | Registered PROJ/alias codes |
| --- | --- |
| Mercator | `mercator`, `mercator_1sp`, `mercator_2sp`, `mercator_(variant_a)`, `mercator_(variant_b)` |
| Mercator Auxiliary Sphere | `mercator_auxiliary_sphere` |
| Pseudo Mercator | `pseudo_mercator`, `popular_visualisation_pseudo_mercator`, `google_mercator`, `web_mercator` |
| Miller Cylindrical | `miller_cylindrical`, `miller`, `mill` |
| Equidistant Cylindrical | `equidistant_cylindrical`, `equirectangular`, `plate_carree`, `eqc` |
| Lat/Long identity | `latlong`, `longlat` |
| Transverse Cylindrical Equal Area | `transverse_cylindrical_equal_area`, `tcea` |
| Cylindrical Equal Area | `cylindrical_equal_area`, `lambert_cylindrical_equal_area`, `equal_area_cylindrical`, `cea` |
| Loximuthal | `loximuthal`, `loxim` |
| Patterson | `patterson` |
| Transverse Mercator | `transverse_mercator`, `transverse_mercator_south_oriented`, `gauss_kruger`, `utm`, `etmerc`, `extended_transverse_mercator` |
| Swiss Oblique Mercator | `swiss_oblique_mercator`, `somerc` |
| Albers Equal Area | `albers`, `albers_conic_equal_area` |
| Krovak | `krovak` |
| Polyconic | `polyconic` |
| Lambert Conformal Conic | `lambert_conformal_conic`, `lambert_conformal_conic_1sp`, `lambert_conformal_conic_2sp`, `lambert_conformal_conic_2sp_belgium`, `lambert_conic_conformal_(1sp)`, `lambert_conic_conformal_(2sp)`, `lambert_tangential_conformal_conic_projection` |
| Equidistant Conic | `equidistant_conic`, `equidistant_conic_(spherical)`, `eqdc` |
| Bonne | `bonne` |
| Perspective Conic | `perspective_conic`, `pconic` |
| Lambert Azimuthal Equal Area | `lambert_azimuthal_equal_area` |
| Cassini-Soldner | `cassini_soldner` |
| Hotine Oblique Mercator | `hotine_oblique_mercator`, `hotine_oblique_mercator_azimuth_center` |
| Oblique Mercator | `oblique_mercator` |
| Oblique Stereographic | `oblique_stereographic` |
| Orthographic | `orthographic` |
| Near-sided Perspective / Tilted Perspective | `near_sided_perspective`, `nsper`, `tilted_perspective`, `tpers` |
| Laborde | `laborde`, `labrd` |
| Gauss-Schreiber Transverse Mercator | `gauss_schreiber_transverse_mercator`, `gauss_laborde_reunion`, `gstmerc` |
| Geostationary Satellite | `geostationary_satellite`, `geos` |
| New Zealand Map Grid | `new_zealand_map_grid`, `nzmg` |
| Polar Stereographic | `polar_stereographic` |
| Equal Earth | `equal_earth`, `eqearth` |
| Aitoff | `aitoff` |
| van der Grinten | `vandg`, `vandergrinten`, `van_der_grinten`, `van_der_grinten_i` |
| Winkel I | `wink1`, `winkel_i` |
| Winkel II | `wink2`, `winkel_ii` |
| Winkel Tripel | `wintri`, `winkel_tripel` |
| Hammer | `hammer` |
| Sinusoidal | `sinu`, `sinusoidal` |
| Goode Homolosine | `goode`, `goode_homolosine` |
| Interrupted Goode Homolosine | `igh`, `interrupted_goode_homolosine` |
| HEALPix | `healpix` |
| Natural Earth | `natural_earth`, `natearth` |
| Natural Earth 2 | `natural_earth_2`, `natural_earth2`, `natearth2` |
| Robinson | `robinson`, `robin` |
| Mollweide | `mollweide`, `moll` |
| Azimuthal Equidistant | `azimuthal_equidistant`, `aeqd` |
| Gnomonic | `gnomonic`, `gnom` |
| Spherical Cross-Track Height (runtime 3D) | `sch`, `spherical_cross_track_height` |

## Coverage classification (latest audit)

### Projection registry backed (class mapping)

These are mapped through `Register("...")` aliases in `ProjectionsRegistry` and instantiate through projection classes.

- Count: **160 `PROJ_HEAD` identifiers**.

### Runtime pipeline backed (conversion/transform dispatch)

These are mapped through `projCode.Equals("...")` dispatch in `ProjPipelineMathTransformFactory.TryCreateStepTransform`.

- Count: **18 `PROJ_HEAD` identifiers**.
- Includes: `axisswap`, `gridshift` family, `defmodel`, `deformation`, `tinshift`, `topocentric`, `vertoffset`, `helmert`, `molodensky`, `ob_tran`, `sch`, `set`, `unitconvert`, `pipeline`.

### Factory/runtime-only support (not direct `+proj` dispatch)

- `affine` is implemented by `AffineTransform` and WKT transform parsing paths.
- `cart`/`geocent` are implemented through geocentric conversion composition in `CoordinateTransformationFactory` and `GeocentricTransform`.

### Direct `+proj` dispatch gaps

The following `PROJ_HEAD` identifiers are not currently dispatched as direct `+proj` tokens in `ProjPipelineMathTransformFactory`:

- `push`
- `pop`
- `geogoffset`
- `molobadekas`
- `geoc`

Additional notable direct-dispatch gaps with existing runtime/factory support:

- `affine` (runtime class + WKT path exists)
- `cart` / `geocent` (geocentric conversion path exists via factory composition)

These are runtime operation-dispatch parity items, not projection-class registration items.

## Validation linkage

Recent parity-related validation evidence:

- Build baseline: `dotnet build ProjNet4GeoAPI.sln -c Release`
- Test baseline: `dotnet test test/ProjNet.Tests/ProjNET.Tests.csproj -c Release --no-build`
- Runtime GIE coverage harness: `GieBuiltinsTheoryTests`
- Dedicated runtime transform support checks for `ob_tran` and conversion pipeline operations.

