// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2005-2009 Morten Nielsen <www.sharpgis.net>
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.Tests.WKT;

using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.Json;
using Npgsql;
using ProjNet.CoordinateSystems;
using Xunit;

/// <summary>
/// Tests for parsing WKT coordinate system definitions from a PostGIS <c>spatial_ref_sys</c> table.
/// </summary>
public class PostGisSpatialRefSysTableParserTests
{
    private const string AppSettingsFileName = "appsettings.json";
    private const string ConnectionStringEnvironmentVariableName = "PROJNET_POSTGIS_CONNECTION";
    private const string MissingConnectionSkipReason = "No PostGIS connection string provided or configured connection string is invalid. Set PROJNET_POSTGIS_CONNECTION or add appsettings.json with ConnectionString.";

    private static readonly Lazy<CoordinateSystemFactory> CoordinateSystemFactory =
        new(() => new CoordinateSystemFactory());

    private static readonly Lazy<string> TrackedWebMercatorWkt =
        new(() => ProjectedCoordinateSystem.WebMercator.WKT);

    private static string? connectionString;

    private static string? ConnectionString
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(PostGisSpatialRefSysTableParserTests.connectionString))
            {
                return PostGisSpatialRefSysTableParserTests.connectionString;
            }

            foreach (string candidate in GetConfiguredConnectionStrings())
            {
                if (!TryValidateConnectionString(candidate))
                {
                    continue;
                }

                PostGisSpatialRefSysTableParserTests.connectionString = candidate;
                return PostGisSpatialRefSysTableParserTests.connectionString;
            }

            return null;
        }
    }

    /// <summary>
    /// Verifies that all WKT definitions in the PostGIS <c>spatial_ref_sys</c> table can be parsed without errors.
    /// </summary>
    [Fact]
    public void TestParsePostgisDefinitions()
    {
        if (string.IsNullOrWhiteSpace(ConnectionString))
        {
            Xunit.Assert.Skip(MissingConnectionSkipReason);
        }

        using (var cn = new NpgsqlConnection(ConnectionString))
        {
            cn.Open();
            NpgsqlCommand cmd = cn.CreateCommand();
            cmd.CommandText = "SELECT \"srid\", \"srtext\" FROM \"public\".\"spatial_ref_sys\" ORDER BY \"srid\";";

            int counted = 0;
            int failed = 0;
            int tested = 0;
            using (NpgsqlDataReader? r = cmd.ExecuteReader(CommandBehavior.CloseConnection))
            {
                if (r is not null)
                {
                    while (r.Read())
                    {
                        counted++;
                        int srid = r.GetInt32(0);
                        string srtext = r.GetString(1);
                        if (string.IsNullOrWhiteSpace(srtext))
                        {
                            continue;
                        }

                        if (srtext.StartsWith("COMPD_CS", StringComparison.Ordinal))
                        {
                            continue;
                        }

                        tested++;
                        if (!TestParse(srid, srtext))
                        {
                            failed++;
                        }
                    }
                }
            }

            Console.WriteLine("\n\nTotal number of Tests {0}, failed {1}", tested, failed);
            Assert.True(failed == 0);
        }
    }

    /// <summary>
    /// Generates the tracked <c>SRID.csv</c> file containing SRID and WKT pairs from the PostGIS <c>spatial_ref_sys</c> table.
    /// Known problematic EPSG rows are normalized back to the tracked canonical WKT so legacy PostGIS spellings do not regress semantics.
    /// </summary>
    [Fact] // Ignore("Only run this if you want a new SRID.csv file")
    public void TestCreateSridCsv()
    {
        if (string.IsNullOrWhiteSpace(ConnectionString))
        {
            Xunit.Assert.Skip(MissingConnectionSkipReason);
        }

        string outputPath = GetTrackedTestFilePath("SRID.csv");

        if (File.Exists(outputPath))
        {
            File.Delete(outputPath);
        }

        using (var sw = new StreamWriter(File.OpenWrite(outputPath)))
        using (var cn = new NpgsqlConnection(ConnectionString))
        {
            cn.Open();
            NpgsqlCommand cm = cn.CreateCommand();
            cm.CommandText = "SELECT \"srid\", \"srtext\" FROM \"public\".\"spatial_ref_sys\" ORDER BY srid;";
            using (NpgsqlDataReader dr = cm.ExecuteReader(CommandBehavior.SequentialAccess))
            {
                while (dr.Read())
                {
                    int srid = dr.GetInt32(0);
                    if (dr.IsDBNull(1))
                    {
                        continue;
                    }

                    string srtext = dr.GetString(1);
                    if (string.IsNullOrWhiteSpace(srtext))
                    {
                        continue;
                    }

                    if (!TryCreateCoordinateSystem(srtext, out CoordinateSystem? coordinateSystem))
                    {
                        continue;
                    }

                    if (ShouldIncludeInTrackedSridCsv(coordinateSystem))
                    {
                        sw.WriteLine($"{srid};{GetTrackedSridCsvWkt(srid, srtext, coordinateSystem)}");
                    }
                }
            }

            cm.Dispose();
        }
    }

    /// <summary>
    /// Verifies that the legacy PostGIS EPSG:3857 row is normalized to the tracked Web Mercator WKT.
    /// </summary>
    [Fact]
    public void GetTrackedSridCsvWkt_WithDifferentManagedDefinition_PrefersTrackedWebMercatorWkt()
    {
        const string legacyPseudoMercator = """PROJCS["WGS 84 / Pseudo-Mercator",GEOGCS["WGS 84",DATUM["WGS_1984",SPHEROID["WGS 84",6378137,298.257223563,AUTHORITY["EPSG","7030"]],AUTHORITY["EPSG","6326"]],PRIMEM["Greenwich",0,AUTHORITY["EPSG","8901"]],UNIT["degree",0.0174532925199433,AUTHORITY["EPSG","9122"]],AUTHORITY["EPSG","4326"]],PROJECTION["Mercator_1SP"],PARAMETER["central_meridian",0],PARAMETER["scale_factor",1],PARAMETER["false_easting",0],PARAMETER["false_northing",0],UNIT["metre",1,AUTHORITY["EPSG","9001"]],AXIS["X",EAST],AXIS["Y",NORTH],EXTENSION["PROJ4","+proj=merc +a=6378137 +b=6378137 +lat_ts=0.0 +lon_0=0.0 +x_0=0.0 +y_0=0 +k=1.0 +units=m +nadgrids=@null +wktext +no_defs"],AUTHORITY["EPSG","3857"]]""";
        CoordinateSystem parsed = CreateRequiredCoordinateSystem(legacyPseudoMercator);

        string normalized = GetTrackedSridCsvWkt(3857, legacyPseudoMercator, parsed);

        Assert.Equal(TrackedWebMercatorWkt.Value, normalized);
        Assert.NotEqual(legacyPseudoMercator, normalized);
        Assert.DoesNotContain("Mercator_1SP", normalized, StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies that semantically equivalent managed definitions keep the original PostGIS WKT in the tracked export.
    /// </summary>
    [Fact]
    public void GetTrackedSridCsvWkt_WithEquivalentManagedDefinition_PreservesOriginalWkt()
    {
        const string wgs84 = """GEOGCS["WGS 84",DATUM["WGS_1984",SPHEROID["WGS 84",6378137,298.257223563,AUTHORITY["EPSG","7030"]],AUTHORITY["EPSG","6326"]],PRIMEM["Greenwich",0,AUTHORITY["EPSG","8901"]],UNIT["degree",0.0174532925199433,AUTHORITY["EPSG","9122"]],AUTHORITY["EPSG","4326"]]""";
        CoordinateSystem parsed = CreateRequiredCoordinateSystem(wgs84);

        Assert.Equal(wgs84, GetTrackedSridCsvWkt(4326, wgs84, parsed));
    }

    /// <summary>
    /// Verifies that SRIDs not present in the managed catalog keep their original WKT unchanged.
    /// </summary>
    [Fact]
    public void GetTrackedSridCsvWkt_WithoutManagedDefinition_PreservesOriginalWkt()
    {
        const string customWkt = """GEOGCS["Custom CRS",DATUM["Custom datum",SPHEROID["WGS 84",6378137,298.257223563]],PRIMEM["Greenwich",0],UNIT["degree",0.0174532925199433]]""";
        CoordinateSystem parsed = CreateRequiredCoordinateSystem(customWkt);

        Assert.Equal(customWkt, GetTrackedSridCsvWkt(999999, customWkt, parsed));
    }

    private static IEnumerable<string> GetConfiguredConnectionStrings()
    {
        string? environmentConnectionString = Environment.GetEnvironmentVariable(ConnectionStringEnvironmentVariableName);
        if (!string.IsNullOrWhiteSpace(environmentConnectionString))
        {
            yield return environmentConnectionString;
        }

        string? appSettingsConnectionString = TryReadAppSettingsConnectionString();
        if (!string.IsNullOrWhiteSpace(appSettingsConnectionString)
            && !string.Equals(appSettingsConnectionString, environmentConnectionString, StringComparison.Ordinal))
        {
            yield return appSettingsConnectionString;
        }
    }

    private static string? TryReadAppSettingsConnectionString()
    {
        if (!File.Exists(AppSettingsFileName))
        {
            return null;
        }

        using (FileStream fs = File.OpenRead(AppSettingsFileName))
        using (var doc = JsonDocument.Parse(fs))
        {
            if (doc.RootElement.TryGetProperty(nameof(ConnectionString), out JsonElement connElement))
            {
                string? connectionStringValue = connElement.GetString();
                if (!string.IsNullOrWhiteSpace(connectionStringValue))
                {
                    return connectionStringValue;
                }
            }

            if (doc.RootElement.TryGetProperty("ConnectionStrings", out JsonElement connectionStringsElement)
                && connectionStringsElement.ValueKind == JsonValueKind.Object
                && connectionStringsElement.TryGetProperty("PostGisSpatialRefSys", out JsonElement namedConnectionElement))
            {
                string? connectionStringValue = namedConnectionElement.GetString();
                if (!string.IsNullOrWhiteSpace(connectionStringValue))
                {
                    return connectionStringValue;
                }
            }

            return null;
        }
    }

    private static bool TryValidateConnectionString(string candidate)
    {
        try
        {
            using (var connection = new NpgsqlConnection(candidate))
            {
                connection.Open();
            }

            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
        catch (FormatException)
        {
            return false;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
        catch (NpgsqlException)
        {
            return false;
        }
    }

    private static bool ShouldIncludeInTrackedSridCsv(CoordinateSystem coordinateSystem)
        => coordinateSystem is GeographicCoordinateSystem or ProjectedCoordinateSystem or GeocentricCoordinateSystem;

    private static string GetTrackedSridCsvWkt(int srid, string srtext, CoordinateSystem parsedCoordinateSystem)
    {
        if (srid != 3857
            || parsedCoordinateSystem is not ProjectedCoordinateSystem projectedCoordinateSystem
            || string.Equals(projectedCoordinateSystem.Projection.ClassName, "Popular Visualisation Pseudo-Mercator", StringComparison.Ordinal)
            || string.IsNullOrWhiteSpace(TrackedWebMercatorWkt.Value))
        {
            return srtext;
        }

        return TrackedWebMercatorWkt.Value;
    }

    private static CoordinateSystem CreateRequiredCoordinateSystem(string wkt)
        => CoordinateSystemFactory.Value.CreateFromWkt(wkt)
            ?? throw new InvalidOperationException("Expected WKT to parse into a coordinate system.");

    private static bool TryCreateCoordinateSystem(string srtext, [NotNullWhen(true)] out CoordinateSystem? coordinateSystem)
    {
        try
        {
            coordinateSystem = CoordinateSystemFactory.Value.CreateFromWkt(srtext);
            return coordinateSystem is not null;
        }
        catch (ArgumentException)
        {
            coordinateSystem = null;
            return false;
        }
        catch (FormatException)
        {
            coordinateSystem = null;
            return false;
        }
        catch (InvalidOperationException)
        {
            coordinateSystem = null;
            return false;
        }
        catch (NotSupportedException)
        {
            coordinateSystem = null;
            return false;
        }
    }

    private static string GetTrackedTestFilePath(string fileName)
    {
        string? directory = AppContext.BaseDirectory;
        while (!string.IsNullOrWhiteSpace(directory))
        {
            if (File.Exists(Path.Combine(directory, "ProjNET.Tests.csproj")))
            {
                return Path.Combine(directory, fileName);
            }

            directory = Path.GetDirectoryName(directory);
        }

        throw new InvalidOperationException("Unable to locate the ProjNET.Tests project directory.");
    }

    private static bool TestParse(int srid, string srtext)
    {
        try
        {
            CoordinateSystemFactory.Value.CreateFromWkt(srtext);

            // CoordinateSystemWktReader.Parse(srtext);
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Test {0} failed:\n  {1}\n  {2}", srid, srtext, ex.Message);
            return false;
        }
    }
}
