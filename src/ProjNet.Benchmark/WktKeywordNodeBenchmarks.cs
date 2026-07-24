// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.Benchmark;

using System;
using System.Diagnostics.CodeAnalysis;
using BenchmarkDotNet.Attributes;
using ProjNet.IO.CoordinateSystems;
using ProjNet.IO.Wkt;

/// <summary>
/// Measures direct keyword-child lookup throughput on representative WKT2 nodes.
/// </summary>
[SuppressMessage("Maintainability", "CA1515:Consider making public types internal", Justification = "Benchmark entry types are intentionally public for BenchmarkDotNet discovery.")]
[MemoryDiagnoser]
[SimpleJob]
public class WktKeywordNodeBenchmarks
{
    private const int IterationCount = 1_000;

    private readonly (WktKeywordNode Node, string Keyword)[] lookups = new (WktKeywordNode Node, string Keyword)[9];

    /// <summary>
    /// Parses a representative projected WKT2 sample and captures hot-path child lookups once per run.
    /// </summary>
    [GlobalSetup]
    public void GlobalSetup()
    {
        const string projectedWkt2 =
            """PROJCRS["WGS 84 / UTM zone 32N",BASEGEOGCRS["WGS 84",ENSEMBLE["World Geodetic System 1984 ensemble",MEMBER["World Geodetic System 1984 (Transit)",ID["EPSG",1166]],MEMBER["World Geodetic System 1984 (G730)",ID["EPSG",1152]],MEMBER["World Geodetic System 1984 (G873)",ID["EPSG",1153]],MEMBER["World Geodetic System 1984 (G1150)",ID["EPSG",1154]],MEMBER["World Geodetic System 1984 (G1674)",ID["EPSG",1155]],MEMBER["World Geodetic System 1984 (G1762)",ID["EPSG",1156]],MEMBER["World Geodetic System 1984 (G2139)",ID["EPSG",1309]],MEMBER["World Geodetic System 1984 (G2296)",ID["EPSG",1383]],ELLIPSOID["WGS 84",6378137,298.257223563,LENGTHUNIT["metre",1,ID["EPSG",9001]],ID["EPSG",7030]],ENSEMBLEACCURACY[2],ID["EPSG",6326]],ID["EPSG",4326]],CONVERSION["UTM zone 32N",METHOD["Transverse Mercator",ID["EPSG",9807]],PARAMETER["Latitude of natural origin",0,ANGLEUNIT["degree",0.0174532925199433,ID["EPSG",9102]],ID["EPSG",8801]],PARAMETER["Longitude of natural origin",9,ANGLEUNIT["degree",0.0174532925199433,ID["EPSG",9102]],ID["EPSG",8802]],PARAMETER["Scale factor at natural origin",0.9996,SCALEUNIT["unity",1,ID["EPSG",9201]],ID["EPSG",8805]],PARAMETER["False easting",500000,LENGTHUNIT["metre",1,ID["EPSG",9001]],ID["EPSG",8806]],PARAMETER["False northing",0,LENGTHUNIT["metre",1,ID["EPSG",9001]],ID["EPSG",8807]],ID["EPSG",16032]],CS[Cartesian,2,ID["EPSG",4400]],AXIS["Easting (E)",east],AXIS["Northing (N)",north],LENGTHUNIT["metre",1,ID["EPSG",9001]],ID["EPSG",32632]]""";

        var root = WktKeywordNode.ParseTree(new WktTokenizer(projectedWkt2));
        WktKeywordNode baseGeogCrs = root.FindChild("BASEGEOGCRS")
            ?? throw new InvalidOperationException("Projected WKT2 sample is missing BASEGEOGCRS.");
        WktKeywordNode conversion = root.FindChild("CONVERSION")
            ?? throw new InvalidOperationException("Projected WKT2 sample is missing CONVERSION.");
        WktKeywordNode cs = root.FindChild("CS")
            ?? throw new InvalidOperationException("Projected WKT2 sample is missing CS.");
        WktKeywordNode parameter = conversion.FindChild("PARAMETER")
            ?? throw new InvalidOperationException("Projected WKT2 sample is missing PARAMETER.");

        this.lookups[0] = (root, "BASEGEOGCRS");
        this.lookups[1] = (root, "CONVERSION");
        this.lookups[2] = (root, "CS");
        this.lookups[3] = (root, "ID");
        this.lookups[4] = (baseGeogCrs, "ENSEMBLE");
        this.lookups[5] = (baseGeogCrs, "ID");
        this.lookups[6] = (conversion, "METHOD");
        this.lookups[7] = (conversion, "ID");
        this.lookups[8] = (parameter, "ANGLEUNIT");
    }

    /// <summary>
    /// Measures single-keyword child lookups without the params-array overload.
    /// </summary>
    /// <returns>A checksum derived from the resolved keyword nodes.</returns>
    [Benchmark(Baseline = true)]
    public int FindSingleKeywordChild()
    {
        int checksum = 0;
        for (int iteration = 0; iteration < IterationCount; iteration++)
        {
            for (int i = 0; i < this.lookups.Length; i++)
            {
                WktKeywordNode? child = this.lookups[i].Node.FindChild(this.lookups[i].Keyword);
                checksum += child?.Keyword.Length ?? 0;
            }
        }

        return checksum;
    }
}
