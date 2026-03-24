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

namespace ProjNET.Tests;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Xml.Linq;
using Xunit;
using ProjNet;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;
using ProjNet.Data;

/// <summary>
/// Represents the documented type.
/// </summary>
public class CoordinateSystemServicesTest
{
    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    [Xunit.Fact]
    public void TestConstructor()
    {
        var css = new CoordinateSystemServices(
            new CoordinateSystemFactory(),
            new CoordinateTransformationFactory());

        Assert.NotNull(css.GetCoordinateSystem(4326));
        Assert.NotNull(css.GetCoordinateSystem(3857));
    }

    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    [Xunit.Fact]
    public void TestTryGetCoordinateSystemBySrid()
    {
        var css = new CoordinateSystemServices(
            new CoordinateSystemFactory(),
            new CoordinateTransformationFactory());

        bool found = css.TryGetCoordinateSystem(4326, out var coordinateSystem);
        bool missing = css.TryGetCoordinateSystem(999999, out var missingCoordinateSystem);

        Assert.True(found);
        Assert.NotNull(coordinateSystem);
        Assert.False(missing);
        Assert.Null(missingCoordinateSystem);
    }

    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    [Xunit.Fact]
    public void TestTryGetCoordinateSystemByAuthorityCode()
    {
        var css = new CoordinateSystemServices(
            new CoordinateSystemFactory(),
            new CoordinateTransformationFactory());

        bool found = css.TryGetCoordinateSystem("EPSG", 3857, out var coordinateSystem);
        bool missing = css.TryGetCoordinateSystem("EPSG", -1, out var missingCoordinateSystem);

        Assert.True(found);
        Assert.NotNull(coordinateSystem);
        Assert.False(missing);
        Assert.Null(missingCoordinateSystem);
    }

    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    [Xunit.Fact]
    public void TestGetAvailableSridValues()
    {
        var css = new CoordinateSystemServices(
            new CoordinateSystemFactory(),
            new CoordinateTransformationFactory());

        int[] srids = css.GetAvailableSridValues();
        Assert.NotNull(srids);
        Assert.True(Array.IndexOf(srids, 4326) >= 0);
        Assert.True(Array.IndexOf(srids, 3857) >= 0);
    }

    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    [Xunit.Fact]
    public void TestManagedProviderIncludesFullGeneratedCatalog()
    {
        var provider = new ManagedCoordinateSystemDefinitionProvider();
        var definitions = provider.GetDefinitions().ToList();

        Assert.True(definitions.Count > 7000);
        Assert.True(definitions.Any(item => item.Key == 4326));
        Assert.True(definitions.Any(item => item.Key == 3857));
    }

    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    [Xunit.Fact]
    public void TestManagedObjectProviderBypassesWktParsing()
    {
        var provider = new TestManagedProvider();
        var css = new CoordinateSystemServices(provider);

        Assert.NotNull(css.GetCoordinateSystem(4326));
        Assert.NotNull(css.GetCoordinateSystem(3857));
    }

    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    [Xunit.Fact]
    public void TestInitializationFailurePropagatesAsInvalidOperationException()
    {
        var css = new CoordinateSystemServices(new ThrowingDefinitionProvider());

        var exception = Assert.Throws<InvalidOperationException>(() => css.GetCoordinateSystem(4326));
        Assert.NotNull(exception.InnerException);
        Assert.Equal("Coordinate system initialization failed.", exception.Message);
    }

    /// <summary>
    /// Validates XML-backed constructor loading for coordinate system definitions.
    /// </summary>
    /// <param name="xmlPath">Path to the XML definition file.</param>
    [Xunit.Theory]
    [Xunit.InlineData(@"D:\temp\ConsoleApplication9\SpatialRefSys.xml")]
    public void TestConstructorLoadXml(string xmlPath)
    {
        if (!File.Exists(xmlPath))
        {
            Xunit.Assert.Skip("Specified file not found");
        }

        var css = new CoordinateSystemServices(
            new CoordinateSystemFactory(),
            new CoordinateTransformationFactory(),
            LoadXml(xmlPath));

        Assert.NotNull(css.GetCoordinateSystem(4326));
        Assert.NotNull(css.GetCoordinateSystem("EPSG", 4326));
        Assert.True(ReferenceEquals(css.GetCoordinateSystem("EPSG", 4326), css.GetCoordinateSystem(4326)));
    }

    /// <summary>
    /// Validates CSV-backed constructor loading for coordinate system definitions.
    /// </summary>
    /// <param name="csvPath">Path to the CSV definition file, or empty for embedded defaults.</param>
    [Xunit.Theory]
    [Xunit.InlineData(@"")]
    public void TestConstructorLoadCsv(string csvPath)
    {
        if (!string.IsNullOrWhiteSpace(csvPath))
        {
            if (!File.Exists(csvPath))
            {
                Xunit.Assert.Skip("Specified file not found");
            }
        }

        var css = new CoordinateSystemServices(
            new CoordinateSystemFactory(),
            new CoordinateTransformationFactory(),
            LoadCsv(csvPath));

        Assert.NotNull(css.GetCoordinateSystem(4326));
        Assert.NotNull(css.GetCoordinateSystem("EPSG", 4326));
        Assert.True(ReferenceEquals(css.GetCoordinateSystem("EPSG", 4326), css.GetCoordinateSystem(4326)));
        Thread.Sleep(1000);
    }

    /// <summary>
    /// Loads SRID/WKT definitions from CSV input or embedded defaults.
    /// </summary>
    /// <param name="csvPath">Optional path to an external CSV file.</param>
    /// <returns>Sequence of SRID/WKT pairs.</returns>
    internal static IEnumerable<KeyValuePair<int, string>> LoadCsv(string csvPath = null)
    {
        Debug.WriteLine(string.Format(CultureInfo.InvariantCulture, "Reading '{0}'.", csvPath ?? "SRID.csv from resources stream"));
        var sw = new Stopwatch();
        sw.Start();

        foreach (var sridWkt in SRIDReader.GetSrids(csvPath))
        {
            yield return new KeyValuePair<int, string>(sridWkt.WktId, sridWkt.Wkt);
        }

        sw.Stop();
        Debug.WriteLine(string.Format(CultureInfo.InvariantCulture, "Read '{1}' in {0:N0}ms", sw.ElapsedMilliseconds, csvPath ?? "SRID.csv from resources stream"));
    }

    private static IEnumerable<KeyValuePair<int, string>> LoadXml(string xmlPath)
    {
        var stream = System.IO.File.OpenRead(xmlPath);

        Debug.WriteLine(string.Format(CultureInfo.InvariantCulture, "Reading '{0}'.", xmlPath));
        var sw = new Stopwatch();
        sw.Start();

        var document = XDocument.Load(stream);

        var rs = from tmp in document.Elements("SpatialReference").Elements("ReferenceSystem") select tmp;

        foreach (var node in rs)
        {
            var sridElement = node.Element("SRID");
            if (sridElement != null)
            {
                int srid = int.Parse(sridElement.Value, CultureInfo.InvariantCulture);
                yield return new KeyValuePair<int, string>(srid, node.LastNode.ToString());
            }
        }

        sw.Stop();
        Debug.WriteLine(string.Format(CultureInfo.InvariantCulture, "Read '{1}' in {0:N0}ms", sw.ElapsedMilliseconds, xmlPath));
    }

    private sealed class TestManagedProvider : ICoordinateSystemDefinitionProvider, IManagedCoordinateSystemProvider
    {
        public IEnumerable<KeyValuePair<int, CoordinateSystem>> GetCoordinateSystems()
        {
            yield return new KeyValuePair<int, CoordinateSystem>(4326, GeographicCoordinateSystem.WGS84);
            yield return new KeyValuePair<int, CoordinateSystem>(3857, ProjectedCoordinateSystem.WebMercator);
        }

        public IEnumerable<KeyValuePair<int, string>> GetDefinitions()
        {
            yield return new KeyValuePair<int, string>(4326, "INVALID_WKT_SHOULD_NOT_BE_USED");
        }
    }

    private sealed class ThrowingDefinitionProvider : ICoordinateSystemDefinitionProvider
    {
        public IEnumerable<KeyValuePair<int, string>> GetDefinitions()
        {
            throw new InvalidOperationException("Synthetic provider failure.");
        }
    }
}

