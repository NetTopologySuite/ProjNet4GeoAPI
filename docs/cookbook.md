# ProjNET cookbook

This cookbook collects short, copyable examples for the most common ProjNET
workflows. The snippets use the current public APIs and are intended to be
adapted into your application code.

## 1. Create the simplest EPSG transformation

Use the built-in managed EPSG catalog when you already know the SRIDs you want.

```csharp
using System;
using ProjNet;

var services = new CoordinateSystemServices();
var transformation = services.CreateTransformation(4326, 3857);

if (transformation is null)
{
    throw new InvalidOperationException("EPSG:4326 to EPSG:3857 is not available.");
}

double[] projected = transformation.MathTransform.Transform(new[] { 10d, 10d });
Console.WriteLine($"X={projected[0]}, Y={projected[1]}");
```

## 2. Transform many points with a reusable output buffer

For batch work, reuse buffers instead of allocating a fresh array for every point.

```csharp
using System;
using ProjNet;

var services = new CoordinateSystemServices();
var transformation = services.CreateTransformation(4326, 3857)
    ?? throw new InvalidOperationException("Transformation is not available.");

double[][] sourcePoints =
[
    [10d, 10d],
    [10.5d, 10.25d],
    [11d, 10.5d],
];

double[] buffer = new double[2];

foreach (double[] point in sourcePoints)
{
    transformation.MathTransform.Transform(point, buffer);
    Console.WriteLine($"{point[0]}, {point[1]} -> {buffer[0]}, {buffer[1]}");
}
```

## 3. Inspect a CRS from the catalog

Look up a CRS once and inspect its authority metadata or serialized form.

```csharp
using System;
using ProjNet;
using ProjNet.CoordinateSystems;

var services = new CoordinateSystemServices();

if (!services.TryGetCoordinateSystem(4326, out CoordinateSystem? wgs84))
{
    throw new InvalidOperationException("EPSG:4326 is missing from the catalog.");
}

Console.WriteLine($"{wgs84.Authority}:{wgs84.AuthorityCode} - {wgs84.Name}");
Console.WriteLine(wgs84.WKT);
Console.WriteLine(wgs84.ToProjJson());
```

## 4. Seed your own CRS definitions with WKT

Use `CoordinateSystemDefinition` when you need a private catalog entry instead of
the built-in managed EPSG set.

```csharp
using System;
using ProjNet;
using ProjNet.CoordinateSystems;
using ProjNet.Data;

var services = new CoordinateSystemServices(new[]
{
    new CoordinateSystemDefinition(4326, GeographicCoordinateSystem.WGS84.WKT),
    new CoordinateSystemDefinition(3857, ProjectedCoordinateSystem.WebMercator.WKT),
});

var transformation = services.CreateTransformation(4326, 3857)
    ?? throw new InvalidOperationException("Custom CRS transformation is not available.");

double[] projected = transformation.MathTransform.Transform(new[] { 10d, 10d });
```

## 5. Parse WKT2, emit PROJJSON, and roundtrip back to a CRS

You can parse a modern WKT2 definition, serialize it to PROJJSON, and then read
the JSON form back into a coordinate-system object.

```csharp
using System;
using ProjNet.CoordinateSystems;
using ProjNet.IO.CoordinateSystems;

string wkt2 = """
    GEOGCRS["WGS 84",
        DATUM["World Geodetic System 1984",
            ELLIPSOID["WGS 84",6378137,298.257223563,LENGTHUNIT["metre",1]]],
        PRIMEM["Greenwich",0,ANGLEUNIT["degree",0.0174532925199433]],
        CS[ellipsoidal,2],
            AXIS["latitude",north,ORDER[1],ANGLEUNIT["degree",0.0174532925199433]],
            AXIS["longitude",east,ORDER[2],ANGLEUNIT["degree",0.0174532925199433]],
        ID["EPSG",4326]]
    """;

CoordinateSystem fromWkt = CoordinateSystemWktReader.Parse(wkt2) as CoordinateSystem
    ?? throw new InvalidOperationException("WKT2 did not describe a supported CRS.");

string projJson = fromWkt.ToProjJson();

CoordinateSystem fromProjJson = ProjJsonReader.Parse(projJson) as CoordinateSystem
    ?? throw new InvalidOperationException("PROJJSON did not roundtrip to a CRS.");

Console.WriteLine(fromProjJson.Name);
```

## 6. Configure local and network-backed grid resolution

Grid-backed transformations can be pointed at local folders, a cache directory,
and an optional HTTP source before you create the transformation.

```csharp
using System;
using ProjNet;
using ProjNet.CoordinateSystems.Transformations;
using ProjNet.Resources;

CoordinateTransformationFactory.ConfigureGridResolution(
    new HttpGridResourceFetchClient("https://cdn.proj.org/"),
    new[] { @"C:\projnet\grids" },
    @"C:\projnet\grid-cache",
    GridResourceResolutionMode.LocalThenNetwork);

var services = new CoordinateSystemServices();
var transformation = services.CreateTransformation(31467, 25832)
    ?? throw new InvalidOperationException("Grid-backed transformation is not available.");
```

If you want grid-backed operations to fail fast when the required file is missing,
set `PROJNET_GRID_REQUIRED=true` before creating the transformation. See
`docs/grids.md` for the full environment-variable matrix.

## 7. Add a custom projection implementation

This recipe is for contributors or advanced hosts that register additional
projection implementations. A projection type must derive from `MapProjection`,
provide a public constructor that accepts `IEnumerable<ProjectionParameter>`, and
be registered in `ProjectionsRegistry`.

```csharp
using System.Collections.Generic;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Projections;
using ProjNet.CoordinateSystems.Transformations;

public sealed class DemoProjection : MapProjection
{
    public DemoProjection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    private DemoProjection(IEnumerable<ProjectionParameter> parameters, MapProjection? inverse)
        : base(parameters, inverse)
    {
        this.Name = "Demo";
    }

    public override MathTransform Inverse()
    {
        this.inverse ??= new DemoProjection(this.Parameters.ToProjectionParameter(), this);
        return this.inverse;
    }

    protected override void RadiansToMeters(ref double lon, ref double lat)
    {
        lon = this.SphericalRadius * lon;
        lat = this.SphericalRadius * lat;
    }

    protected override void MetersToRadians(ref double x, ref double y)
    {
        x *= this.InverseSphericalRadius;
        y *= this.InverseSphericalRadius;
    }
}

public static class ProjectionBootstrap
{
    public static void RegisterDemoProjection()
    {
        ProjectionsRegistry.Register("demo_projection", typeof(DemoProjection));
        ProjectionsRegistry.RegisterAlias("demo", "demo_projection");
    }
}
```

In production code you should also document the projection, validate its
parameters, and add runtime tests that compare the forward and inverse paths
against an external reference implementation.
