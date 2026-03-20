// Copyright 2005 - 2009 - Morten Nielsen (www.sharpgis.net)
//
// This file is part of ProjNet.
// ProjNet is free software; you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
//
// ProjNet is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public License
// along with ProjNet; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA

namespace ProjNET.Tests.WKT;

using System;
using System.Data;
using System.IO;
using Newtonsoft.Json.Linq;
using Npgsql;
using NUnit.Framework;
using ProjNet.CoordinateSystems;

/// <summary>
/// Represents the documented type.
/// </summary>
public class SpatialRefSysTableParser
{
    private static string connectionString;
    private static readonly Lazy<CoordinateSystemFactory> CoordinateSystemFactory =
        new Lazy<CoordinateSystemFactory>(() => new CoordinateSystemFactory());

    private static string ConnectionString
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(SpatialRefSysTableParser.connectionString))
            {
                return SpatialRefSysTableParser.connectionString;
            }

            if (!File.Exists("appsettings.json"))
            {
                return null;
            }

            JToken token = null;
            using (var jtr = new Newtonsoft.Json.JsonTextReader(new StreamReader("appsettings.json")))
            {
                token = JToken.ReadFrom(jtr);
            }

            string connectionString = (string)token["ConnectionString"];
            try
            {
                using (var cn = new NpgsqlConnection(connectionString))
                {
                    cn.Open();
                }
            }
            catch (Exception)
            {
                return null;
            }

            SpatialRefSysTableParser.connectionString = connectionString;
            return SpatialRefSysTableParser.connectionString;
        }
    }

    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    [Xunit.Fact]
    public void TestParsePostgisDefinitions()
    {
        if (string.IsNullOrWhiteSpace(ConnectionString))
        {
            Xunit.Assert.Skip("No Connection string provided or provided connection string invalid.");
        }

        using (var cn = new NpgsqlConnection(ConnectionString))
        {
            cn.Open();
            var cmd = cn.CreateCommand();
            cmd.CommandText = "SELECT \"srid\", \"srtext\" FROM \"public\".\"spatial_ref_sys\" ORDER BY \"srid\";";

            int counted = 0;
            int failed = 0;
            int tested = 0;
            using (var r = cmd.ExecuteReader(CommandBehavior.CloseConnection))
            {
                if (r != null)
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

                        if (srtext.StartsWith("COMPD_CS"))
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
            Assert.IsTrue(failed == 0);
        }
    }

    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    [Xunit.Fact]// , Ignore("Only run this if you want a new SRID.csv file")]
    public void TestCreateSridCsv()
    {
        if (string.IsNullOrWhiteSpace(ConnectionString))
        {
            Xunit.Assert.Skip("No Connection string provided or provided connection string invalid.");
        }

        if (File.Exists("SRID.csv"))
        {
            File.Delete("SRID.csv");
        }

        using (var sw = new StreamWriter(File.OpenWrite("SRID.csv")))
        using (var cn = new NpgsqlConnection(ConnectionString))
        {
            cn.Open();
            var cm = cn.CreateCommand();
            cm.CommandText = "SELECT \"srid\", \"srtext\" FROM \"public\".\"spatial_ref_sys\" ORDER BY srid;";
            using (var dr = cm.ExecuteReader(CommandBehavior.SequentialAccess))
            {
                while (dr.Read())
                {
                    int srid = dr.GetInt32(0);
                    string srtext = dr.GetString(1);
                    int bracketIndex = srtext.IndexOf("[", StringComparison.Ordinal);
                    if (bracketIndex < 0)
                    {
                        continue;
                    }

                    switch (srtext.Substring(0, bracketIndex))
                    {
                        case "PROJCS":
                        case "GEOGCS":
                        case "GEOCCS":
                            sw.WriteLine($"{srid};{srtext}");
                            break;
                    }
                }
            }

            cm.Dispose();
        }
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
