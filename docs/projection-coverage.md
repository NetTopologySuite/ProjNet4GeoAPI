# Projection Coverage Matrix (C++ PROJ vs ProjNet)

This document tracks the projection feature-parity status between `spec\PROJ` (C++ reference) and `src\ProjNet` (.NET implementation).

## Scope

- C++ source of truth: `spec\PROJ\src\projections\*.cpp` (`PROJ_HEAD(...)` projection codes).
- .NET implementation source: `src\ProjNet\CoordinateSystems\Projections\*.cs` plus `ProjectionsRegistry.cs`.
- Status categories:
  - **Implemented**: projection class exists and aliases are registered in `ProjectionsRegistry`.
  - **Missing**: no registered .NET projection mapping yet.

## Current summary

- C++ projection codes discovered: **161** (`PROJ_HEAD` entries, including aliases and compatibility names).
- .NET projection registry parity gap: **1 code** (`ob_tran`).
- `sch` is now available in ProjNet as a **runtime 3D transform** (`SchMathTransform`) and is also registered under projection aliases (`sch`, `spherical_cross_track_height`) for operation creation parity.
- `ob_tran` remains implemented as a **runtime transform** (`ObTranMathTransform`) with dedicated parity tests; it is intentionally not instantiated through the 2D map-projection implementation path.
- Latest focused regression: `Phase7SpecialtyProjectionBatchD8/D9/D10` + `GieBuiltinsTheoryTests` passed with `2533 total / 2044 passed / 489 skipped / 0 failed`.

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

## High-priority missing projection codes (next tiers)

These codes are present in C++ PROJ but currently not registered in ProjNet:

- Remaining registry-name gap from `PROJ_HEAD`: `ob_tran` (implemented in runtime transform pipeline).

## Validation linkage

Recent M7 closure validation:

- `Phase7SpecialtyProjectionBatchD8Tests`
- `Phase7SpecialtyProjectionBatchD9Tests`
- `Phase7SpecialtyProjectionBatchD10Tests`
- `Phase6ObTranRuntimeTests`
- `GieBuiltinsTheoryTests`

