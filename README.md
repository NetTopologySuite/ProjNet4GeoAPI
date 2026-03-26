# ProjNET 3.0 (modernized ProjNet4GeoAPI)

ProjNET is a managed .NET spatial reference and projection engine for geodetic coordinate system modeling and coordinate transformation workflows.

This repository contains an actively modernized codebase aligned with current PROJ behavior and expanded runtime coverage while preserving compatibility-focused API surfaces.

## What is included

- Managed coordinate system definitions and EPSG-backed lookup/catalog support.
- Projection registration with broad alias coverage (`321` aliases).
- Coordinate operation and transformation runtime (including affine, Helmert, Molodensky, deformation, grid-shift, topocentric, and pipeline-based paths).
- WKT parsing/writing support and modernization artifacts under `docs/modernization/`.

## Target frameworks

`ProjNET` currently targets:

- `netstandard2.0` (required shipping target)
- `netstandard2.1`
- `net8.0`

The project is built with C# 12 and includes .NET 8-specific runtime optimizations where applicable (for example conditional source-generated regex paths).

## Installation

```powershell
dotnet add package ProjNET
```

## Quick usage

```csharp
using ProjNet;

var services = new CoordinateSystemServices(new[]
{
    new KeyValuePair<int, string>(4326, GeographicCoordinateSystem.WGS84.WKT),
    new KeyValuePair<int, string>(3857, ProjectedCoordinateSystem.WebMercator.WKT),
});

var transform = services.CreateTransformation(4326, 3857);
double[] result = transform.MathTransform.Transform(new[] { 10d, 10d });
```

Expected reference point for `10°,10°` in EPSG:3857 is approximately:

- `X = 1113194.90793274`
- `Y = 1118889.97485796`

(Validated by `test/ProjNet.Tests/VerificationSuiteTests.cs`.)

## Build and test

From repository root:

```powershell
dotnet build .\ProjNet4GeoAPI.sln --tl:off -v minimal
dotnet test .\test\ProjNet.Tests\ProjNET.Tests.csproj --tl:off -v minimal
```

## Modernization highlights (v3 line)

- Added `net8.0` as a library target while preserving `netstandard` targets.
- Generator now uses EPSG WKT ZIP as primary source (no runtime `proj.db` dependency).
- Large generated eager arrays were replaced by on-demand switch-based lookup paths in the managed EPSG catalog.
- Test stack modernized to xUnit v3.
- Historical `SpecialtyProjectionBatch*` test naming was removed in favor of behavior-oriented class names.
- SPDX-based file attribution and `LICENSES/` + `NOTICE.md` consolidation completed.
- API XML documentation overhauled across projection, transformation, coordinate-system, and IO/service surfaces.
- Build/versioning was unified with Nerdbank.GitVersioning (`version.json` + shared build props).

## API baseline and coverage workflow

- Public API drift is guarded by `PublicApiBaselineTests` against `src/ProjNet/PublicAPI.Shipped.txt`.
- Baseline regeneration (intentional API change only) is controlled by `PROJNET_UPDATE_PUBLIC_API_BASELINE=1`.
- Modernization-wave verification artifacts (coverage baseline/delta, review/finalization/checkpoints) are tracked in `docs/modernization/`.

## Phase 3 completion snapshot (current)

- Final Release build: passed (`dotnet build src\ProjNet\ProjNET.csproj -c Release`).
- Final full tests: `3834 total`, `3003 passed`, `0 failed`, `831 skipped`.
- GIE builtins progress in M27: `2465 total`, `1664 passed`, `0 failed`, `801 skipped`.
- Benchmark validation: benchmark discovery run succeeds (`dotnet run -c Release -- --list flat` in `src\ProjNet.Benchmark`).
- Public API baseline is synchronized with shipped surface (`Wgs84ConversionInfo` includes `IEquatable<Wgs84ConversionInfo>` in `PublicAPI.Shipped.txt`).

## Transformation coverage summary

Implemented and validated transformation families include:

- Affine transforms (`AffineTransform`)
- Geocentric/geographic bridge transforms
- Axis swap and unit conversion
- Helmert and Molodensky families
- Deformation and deformation model transforms
- Horner and TIN shift transforms
- Horizontal/vertical/XYZ grid shifts (NTv2, GTX, GeoTIFF)
- Prime-meridian and topocentric transforms
- Pipeline composition and concatenation paths

For audit details, see:

- `docs/modernization/m1-transform-audit.md`
- `docs/modernization/m1-pipeline-ops-audit.md`

## Projection coverage summary

Total registered projection classes: **152**  
Projection aliases registered in `ProjectionsRegistry`: **321**

### Cylindrical and Mercator family (20)

`CalCoFiProjection`, `CentralCylindricalProjection`, `ColombiaUrbanProjection`, `CylindricalEqualAreaProjection`, `EquidistantCylindricalProjection`, `GaussSchreiberTransverseMercatorProjection`, `HotineObliqueMercatorProjection`, `LatLongProjection`, `Mercator`, `MercatorAuxiliarySphere`, `MillerCylindricalProjection`, `ObliqueCylindricalEqualAreaProjection`, `ObliqueMercatorProjection`, `PseudoMercator`, `SpaceObliqueMercatorProjection`, `SwissObliqueMercatorProjection`, `ToblerMercatorProjection`, `TransverseCentralCylindricalProjection`, `TransverseCylindricalEqualAreaProjection`, `TransverseMercator`.

### Transverse and oblique family (2)

`LabordeProjection`, `UpsProjection`.

### Conic family (18)

`BipolarConicProjection`, `BonneProjection`, `CentralConicProjection`, `EquidistantConicProjection`, `EulerProjection`, `InternationalMapWorldPolyconicProjection`, `KrovakProjection`, `LambertConformalConic2SP`, `LambertConformalConicAlternativeProjection`, `LambertEqualAreaConicProjection`, `Murdoch1Projection`, `Murdoch2Projection`, `Murdoch3Projection`, `PconicProjection`, `PolyconicProjection`, `RectangularPolyconicProjection`, `TissotProjection`, `Vitkovsky1Projection`.

### Azimuthal and perspective family (16)

`AiryProjection`, `AzimuthalEquidistantProjection`, `GeostationarySatelliteProjection`, `GnomonicProjection`, `LambertAzimuthalEqualAreaProjection`, `LeeOblatedStereographicProjection`, `MillerOblatedStereographicProjection`, `ModifiedStereographic48USProjection`, `ModifiedStereographic50USProjection`, `ModifiedStereographicAlaskaProjection`, `NearSidedPerspectiveProjection`, `OblatedEqualAreaProjection`, `ObliqueStereographicProjection`, `OrthographicProjection`, `PolarStereographicProjection`, `RoussilheStereographicProjection`.

### Pseudocylindrical and world map family (68)

`AitoffProjection`, `AlbersProjection`, `ApianProjection`, `AugustProjection`, `BaconProjection`, `BoggsProjection`, `CollignonProjection`, `CompactMillerProjection`, `CrasterProjection`, `DenoyerProjection`, `Eckert1Projection`, `Eckert2Projection`, `Eckert3Projection`, `Eckert4Projection`, `Eckert5Projection`, `Eckert6Projection`, `EqualEarthProjection`, `FaheyProjection`, `FoucautProjection`, `FoucautSinusoidalProjection`, `GallProjection`, `GeneralSinusoidalProjection`, `Ginsburg8Projection`, `HammerProjection`, `HatanoProjection`, `IghProjection`, `Kavrayskiy5Projection`, `Kavrayskiy7Projection`, `LagrangeProjection`, `LarriveeProjection`, `LaskowskiProjection`, `LoximuthalProjection`, `McBrydeThomasFlatPolarParabolicProjection`, `McBrydeThomasFlatPolarQuarticProjection`, `McBrydeThomasFlatPolarSineProjection`, `McBrydeThomasFlatPolarSinusoidalProjection`, `McBrydeThomasFlatPoleSineProjection`, `NaturalEarth2Projection`, `NaturalEarthProjection`, `NellHammerProjection`, `NellProjection`, `NicolosiProjection`, `OrteliusProjection`, `PattersonProjection`, `PutninsP1Projection`, `PutninsP2Projection`, `PutninsP3PrimeProjection`, `PutninsP3Projection`, `PutninsP4PProjection`, `PutninsP5PrimeProjection`, `PutninsP5Projection`, `PutninsP6PrimeProjection`, `PutninsP6Projection`, `QuarticAuthalicProjection`, `RobinsonProjection`, `SinusoidalProjection`, `TimesProjection`, `TwoPointEquidistantProjection`, `UrmaevFlatPolarSinusoidalProjection`, `UrmaevVProjection`, `Wagner1Projection`, `Wagner2Projection`, `Wagner3Projection`, `Wagner4Projection`, `Wagner5Projection`, `Wagner6Projection`, `Wagner7Projection`, `WerenskioldProjection`.

### Polyconic and related family (1)

`CassiniSoldnerProjection`.

### Van der Grinten and Winkel family (7)

`VanDerGrinten2Projection`, `VanDerGrinten3Projection`, `VanDerGrinten4Projection`, `VanDerGrintenProjection`, `Winkel1Projection`, `Winkel2Projection`, `WinkelTripelProjection`.

### Interrupted and composite family (7)

`Bertin1953Projection`, `GoodeProjection`, `InterruptedGoodeHomolosineOceanicProjection`, `InterruptedMollweideOceanicProjection`, `InterruptedMollweideProjection`, `MollweideProjection`, `SpilhausProjection`.

### Polyhedral and specialty geometric family (12)

`AdamsHemisphereInSquareProjection`, `AdamsWorldInSquareIIProjection`, `AdamsWorldInSquareIProjection`, `AiroceanProjection`, `ChamberlinTrimetricProjection`, `GuyouProjection`, `HealpixProjection`, `IseaProjection`, `NewZealandMapGridProjection`, `PeirceQuincuncialProjection`, `QuadrilateralizedSphericalCubeProjection`, `S2Projection`.

### Legacy runtime operation registration (1)

`SchMathTransform`.

## Documentation and governance

- Modernization and parity artifacts: `docs/modernization/`
- Projection parity matrix: `docs/projection-coverage.md`
- Engineering governance and API baseline policy: `src/ProjNet/ENGINEERING_GOVERNANCE.md`

## License and attribution

This project ships under **LGPL-2.1-or-later**.

- License texts: `LICENSES/`
- Attribution and provenance summary: `NOTICE.md`
- Per-file SPDX attribution is used across source and tests.
