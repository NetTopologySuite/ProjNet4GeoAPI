# ProjNet (for GeoAPI)
This library is an extended port of [ProjNet](http://projnet.codeplex.com)

## Important notice
The current team unfortunatly doesn't have the resources to dedicate to supporting this project at this moment.
If you see yourself in the position to help out please [reach out](https://github.com/NetTopologySuite/ProjNet4GeoAPI/issues/99).

Alternatives:
* [SharpProj](https://www.nuget.org/packages/SharpProj.NetTopologySuite/)
* [DotSpatial.Projections](https://www.nuget.org/packages/DotSpatial.Projections/)
* [DotSpatial.Projections (NetStandard)](https://www.nuget.org/packages/DotSpatial.Projections.NetStandard/)
* [GDAL/OGR](https://www.nuget.org/packages/GDAL/)

## .NET Spatial Reference and Projection Engine
Proj.NET performs point-to-point coordinate conversions between geodetic coordinate systems for use in fx. Geographic Information Systems (GIS) or GPS applications. The spatial reference model used adheres to the Simple Features specification.
* Read the [Frequently Asked Questions](https://github.com/NetTopologySuite/ProjNet4GeoAPI/wiki/Frequently-Asked-Questions) for common questions.
* Popular [Well-Known Text](https://github.com/NetTopologySuite/ProjNet4GeoAPI/wiki/Popular-Well-Known-Text-representations-of-Spatial-Reference-Systems) representations for Spatial Reference Systems

### Build status
| Branch | Status |
| --- | --- |
| develop | [![Build Status](https://travis-ci.org/NetTopologySuite/ProjNet4GeoAPI.svg?branch=develop)](https://travis-ci.org/NetTopologySuite/ProjNet4GeoAPI) |
| master | [![Build Status](https://travis-ci.org/NetTopologySuite/ProjNet4GeoAPI.svg?branch=master)](https://travis-ci.org/NetTopologySuite/ProjNet4GeoAPI) |


### Get it from NuGet
* For version 1.*
  `PM> Install-Package ProjNet4GeoAPI`  
  - More information on [NuGet](https://www.nuget.org/packages/ProjNet4GeoAPI)  
* For version 2.*  
  `PM> Install-Package ProjNet`


### Talk...
Join the [![Gitter](https://img.shields.io/gitter/room/TechnologyAdvice/Stardust.svg)](https://gitter.im/NetTopologySuite/ProjNet4GeoAPI) on ProjNet (for GeoAPI).


### Projects using ProjNet(4GeoAPI)
* [SharpMap](https://github.com/SharpMap/SharpMap)

(If your project is missing, there is an edit button up-right)

### Supports:
* Datum transformations
* Geographic, Geocentric, and Projected coordinate systems
* Compatible with Microsoft .NetStandard 2.0
* Converts coordinate systems to/from Well-Known Text (WKT) and to XML

### Modernization notes
Concise modernization artifacts and rollout notes are tracked in:
* `docs/modernization/`

### v3 migration notes (major release)
Version 3 introduces API modernization aligned with ongoing PROJ parity work.

* The `CoordinateSystemFactory.CreateFromWkt` parameter name changed from `WKT` to `wkt` (non-breaking at runtime, but visible in API metadata/baseline output).
* `MapProjection` constants were normalized to PascalCase (`FortPi`, `HalfPi`, `HugeVal`, `MaxVal`, `TwoPi`, `Eps10`, `Eps7`, `Epsln`, `DblLong`).
* Legacy constant names remain available as `[Obsolete]` aliases (`FORT_PI`, `HALF_PI`, `HUGE_VAL`, `MAX_VAL`, `TWO_PI`, `FORTPI`, `HALFPI`, `HUGEVAL`, `MAXVAL`, `TWOPI`, `EPS10`, `EPS7`, `EPSLN`, `DBLLONG`) for migration compatibility.
* Legacy projection field names remain available as `[Obsolete]` aliases (`central_meridian`, `false_easting`, `false_northing`, `lat_origin`, `scale_factor`) while internal code uses modernized names.
* Public API review now tracks in both shipped (`PublicAPI.Shipped.txt`) and in-flight (`PublicAPI.Unshipped.txt`) baselines.

### Release validation checklist
Current release hardening is validated with the following commands:
* `dotnet test .\test\ProjNet.Tests\ProjNet.Tests.csproj -c Release --framework net8 --filter "FullyQualifiedName~PublicApiBaselineTests"`
* `dotnet test .\test\ProjNet.Tests\ProjNet.Tests.csproj -c Release --framework net8 --filter "FullyQualifiedName~GieBuiltinsTheoryTests|FullyQualifiedName~Gigs5101TheoryTests"`
* `dotnet build .\src\ProjNet.Benchmark\ProjNet.Benchmark.csproj -c Release`
* `dotnet run -c Release --project .\src\ProjNet.Benchmark\ProjNet.Benchmark.csproj -- --list flat`
* `dotnet run -c Release --project .\src\ProjNet.Benchmark\ProjNet.Benchmark.csproj -- --filter *ProjParityBenchmarks*`

### Projection types currently supported:
* Albers
* Azimuthal Equidistant
* Aitoff
* Cassini Soldner
* Bonne
* Cylindrical Equal Area
* Equal Earth
* Equidistant Conic
* Equidistant Cylindrical (Equirectangular / Plate Carree)
* Gauss-Schreiber Transverse Mercator (Gauss-Laborde Reunion)
* Geostationary Satellite
* Gnomonic
* Goode Homolosine
* Hammer
* HEALPix
* Interrupted Goode Homolosine
* Hotine Oblique Mercator
* Krovak
* Laborde
* Lambert Azimuthal Equal Area
* Lambert Conformal
* Lambert Tangential Conformal Conic
* LatLong / LongLat (identity)
* Loximuthal
* Mercator
* Mercator Auxiliary Sphere
* Miller Cylindrical
* Mollweide
* Natural Earth
* Natural Earth 2
* Near-Sided Perspective
* New Zealand Map Grid
* Oblique Mercator
* Oblique Stereographic
* Orthographic
* Patterson
* Perspective Conic
* Polar Stereographic
* Transverse Cylindrical Equal Area
* Robinson
* Sinusoidal
* Polyconic
* Pseudo Mercator
* Transverse Mercator
* Swiss Oblique Mercator
* van der Grinten
* Winkel I
* Winkel II
* Winkel Tripel
