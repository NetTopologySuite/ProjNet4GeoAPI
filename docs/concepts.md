# ProjNET concepts and terminology

This guide defines the core terms used throughout ProjNET. The short version is:
ProjNET models coordinate reference systems (CRSs), parses and writes common CRS
formats, and creates coordinate transformations between compatible CRS pairs.

## Coordinate Reference System (CRS)

A coordinate reference system describes how numeric ordinates map to real-world
locations. A CRS combines a reference frame, axis order, axis units, and, when
needed, a map projection. In ProjNET, the most common CRS categories are:

- **Geographic CRS**: longitude and latitude on an ellipsoid, usually in degrees.
- **Projected CRS**: planar easting/northing coordinates derived from a geographic
  CRS through a projection, usually in metres or feet.
- **Vertical CRS**: height or depth relative to a vertical datum.
- **Compound CRS**: a horizontal CRS plus a vertical CRS handled together.

ProjNET also supports geocentric and bound CRS cases where the metadata requires
an explicit earth-centred frame or a mandated transformation to a hub CRS.

## Datum and datum ensemble

A datum anchors coordinates to the earth. For horizontal work, the datum defines
the ellipsoid and the realization used to position that ellipsoid relative to the
planet. For vertical work, the datum defines the zero-height surface. Modern
registries also use **datum ensembles** when a CRS intentionally refers to a
family of closely related realizations instead of one exact member.

## Ellipsoid

An ellipsoid is the mathematical earth model used by a datum. It is usually
described by a semi-major axis and an inverse flattening value. Geographic and
projected CRSs depend on the ellipsoid because projection formulae and datum
transformations operate on that geometric model.

## Projection

A projection converts angular geographic coordinates into planar coordinates.
Every projection introduces trade-offs: some preserve area, some preserve local
shape, some preserve distance or direction along limited paths, and none preserve
everything everywhere. ProjNET contains the projection implementation and the
metadata that binds projection parameters to a projected CRS.

## Transformation and pipeline

A transformation converts coordinates from one CRS into another. Simple cases may
only need axis normalization, unit conversion, and a projection forward or inverse
step. More complex cases may also require datum shifts, Helmert operations,
vertical adjustments, or grid-backed corrections. ProjNET composes these steps
into runtime pipelines so the resulting `MathTransform` matches the CRS metadata
as closely as the available catalog and grid data allow.

## WKT1, WKT2, and PROJJSON

ProjNET works with three important CRS interchange formats:

- **WKT1**: the older OGC/ESRI-era Well-Known Text family that is still common in
  existing databases and files.
- **WKT2:2019**: the newer ISO 19111-aligned form with richer CRS metadata,
  including bound, compound, and modern usage metadata.
- **PROJJSON**: the JSON representation used by the PROJ ecosystem for CRS and
  operation metadata exchange.

ProjNET reads WKT1, WKT2, and PROJJSON for the supported CRS shapes in the
library, and it can serialize supported coordinate-system models back to WKT and
PROJJSON.

## Grid shifts

Some transformations depend on sampled correction grids instead of a few numeric
parameters. ProjNET supports the main formats used by the current library:

- **NTv2 (`.gsb`)** for horizontal grid shifts.
- **GTX (`.gtx`)** for vertical grid shifts.
- **GeoTIFF (`.tif`)** for horizontal, vertical, and xyz grid-backed operations.

These grids are resolved from local search paths or an optional cache/network
workflow. If a transformation requires a grid and no substitute operation exists,
grid availability can determine whether the transformation can be created.

## EPSG catalog

The default `CoordinateSystemServices` constructor uses ProjNET's managed EPSG
catalog. That catalog is generated into the library, keyed by SRID, and available
without a runtime `proj.db` dependency. In practice, that means common lookups
such as EPSG:4326 or EPSG:3857 work out of the box in the default configuration.

## Further reading

- EPSG registry browser: <https://epsg.io/>
- OGC standards overview: <https://www.ogc.org/standards/>
