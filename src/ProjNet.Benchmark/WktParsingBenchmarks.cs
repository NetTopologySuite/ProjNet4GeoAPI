// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.Benchmark;

using System.Diagnostics.CodeAnalysis;
using BenchmarkDotNet.Attributes;
using ProjNet.CoordinateSystems;
using ProjNet.IO.CoordinateSystems;

/// <summary>
/// Measures <see cref="CoordinateSystemWktReader.Parse(string)"/> throughput for WKT strings of varying complexity.
/// </summary>
[SuppressMessage("Maintainability", "CA1515:Consider making public types internal", Justification = "Benchmark entry types are intentionally public for BenchmarkDotNet discovery.")]
[MemoryDiagnoser]
[SimpleJob]
public class WktParsingBenchmarks
{
    private readonly string simpleGeographicWkt =
        "GEOGCS[\"GCS_WGS_1984\",DATUM[\"D_WGS_1984\",SPHEROID[\"WGS_1984\",6378137,298.257223563]],PRIMEM[\"Greenwich\",0],UNIT[\"Degree\",0.0174532925199433]]";

    private readonly string projectedWkt =
        "PROJCS[\"WGS 84 / UTM zone 32N\",GEOGCS[\"WGS 84\",DATUM[\"WGS_1984\",SPHEROID[\"WGS 1984\",6378137,298.257223563]],PRIMEM[\"Greenwich\",0],UNIT[\"degree\",0.0174532925199433]],PROJECTION[\"Transverse_Mercator\"],PARAMETER[\"latitude_of_origin\",0],PARAMETER[\"central_meridian\",9],PARAMETER[\"scale_factor\",0.9996],PARAMETER[\"false_easting\",500000],PARAMETER[\"false_northing\",0],UNIT[\"metre\",1]]";

    private readonly string compoundWkt =
        "COMPD_CS[\"WGS 84 + EGM96 height\",GEOGCS[\"WGS 84\",DATUM[\"WGS_1984\",SPHEROID[\"WGS 1984\",6378137,298.257223563]],PRIMEM[\"Greenwich\",0],UNIT[\"degree\",0.0174532925199433]],VERT_CS[\"EGM96 height\",VERT_DATUM[\"EGM96 geoid\",2005],UNIT[\"metre\",1]]]";

    /// <summary>
    /// Parses a simple WGS84 geographic coordinate system WKT string.
    /// </summary>
    /// <returns>The parsed coordinate system info.</returns>
    [Benchmark(Baseline = true)]
    public IInfo ParseSimpleGeographicCs()
    {
        return CoordinateSystemWktReader.Parse(this.simpleGeographicWkt);
    }

    /// <summary>
    /// Parses a UTM Zone 32N projected coordinate system WKT string with Transverse Mercator projection.
    /// </summary>
    /// <returns>The parsed coordinate system info.</returns>
    [Benchmark]
    public IInfo ParseProjectedCs()
    {
        return CoordinateSystemWktReader.Parse(this.projectedWkt);
    }

    /// <summary>
    /// Parses a compound coordinate system WKT string with both horizontal and vertical components.
    /// </summary>
    /// <returns>The parsed coordinate system info.</returns>
    [Benchmark]
    public IInfo ParseCompoundCs()
    {
        return CoordinateSystemWktReader.Parse(this.compoundWkt);
    }
}
